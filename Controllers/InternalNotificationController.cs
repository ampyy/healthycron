using HealthyCron.Data.Interfaces;
using HealthyCron.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using HealthyCron.Models.Configuration;

namespace HealthyCron.Controllers
{
    [ApiController]
    [Route("api/internal")]
    public class InternalNotificationController : ControllerBase
    {
        private readonly IMonitorRepository _monitorRepository;
        private readonly IHubContext<MonitorHub> _hubContext;
        private readonly ILogger<InternalNotificationController> _logger;
        private readonly InternalApiSettings _settings;

        public InternalNotificationController(
            IMonitorRepository monitorRepository,
            IHubContext<MonitorHub> hubContext,
            ILogger<InternalNotificationController> logger,
            IOptions<InternalApiSettings> settings)
        {
            _monitorRepository = monitorRepository;
            _hubContext = hubContext;
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpPost("notify-status")]
        public async Task<IActionResult> NotifyStatusChange([FromBody] NotifyStatusRequest request)
        {
            // Validate Internal Token
            if (!Request.Headers.TryGetValue("X-Internal-Token", out var token) || token != _settings.AuthToken)
            {
                return Unauthorized("Invalid or missing internal token.");
            }

            if (request == null || request.MonitorId == Guid.Empty)
            {
                return BadRequest("Invalid request.");
            }

            var monitor = await _monitorRepository.GetMonitorByIdAsync(request.MonitorId);
            if (monitor == null)
            {
                return NotFound($"Monitor {request.MonitorId} not found.");
            }

            _logger.LogInformation("External status notification received for monitor {MonitorId} ({MonitorName}). New status: {Status}", 
                request.MonitorId, monitor.Name, request.NewStatus);

            // Broadcast status change via SignalR
            // This ensures the dashboard UI updates immediately when the external worker detects a failure
            await _hubContext.Clients.Group(request.MonitorId.ToString()).SendAsync("StatusChanged", new
            {
                monitorId = request.MonitorId,
                newStatus = request.NewStatus,
                statusDisplay = request.NewStatus == "Failed" ? "Down" : "Up"
            });

            // Also notify the project group in case there's a project-level dashboard
            await _hubContext.Clients.Group(monitor.ProjectId.ToString()).SendAsync("StatusChanged", new
            {
                monitorId = request.MonitorId,
                newStatus = request.NewStatus,
                statusDisplay = request.NewStatus == "Failed" ? "Down" : "Up"
            });

            return Ok(new { message = "Status update broadcasted successfully." });
        }
    }

    public class NotifyStatusRequest
    {
        public Guid MonitorId { get; set; }
        public string NewStatus { get; set; } = "Failed";
    }
}
