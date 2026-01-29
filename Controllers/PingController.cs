using HealthyCron.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : Controller
    {
        private readonly IMonitorRepository _monitorRepository;

        public PingController(IMonitorRepository monitorRepository)
        {
            _monitorRepository = monitorRepository;
        }

        [HttpGet("{id:guid}")]
        [HttpPost("{id:guid}")]
        public async Task<IActionResult> PingById(Guid id)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Capture Metadata
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();
            var method = HttpContext.Request.Method;

            // Filter sensitive headers
            var headers = HttpContext.Request.Headers
                .Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            var headersJson = System.Text.Json.JsonSerializer.Serialize(headers);

            sw.Stop(); // Measure up to point of processing

            var ping = new HealthyCron.Models.MonitorPing
            {
                MonitorId = id,
                Status = HealthyCron.Models.PingType.Success,
                IpAddress = ip,
                UserAgent = ua,
                HttpMethod = method,
                RequestHeaders = headersJson,
                ResponseTimeMs = (int)sw.ElapsedMilliseconds
            };

            var success = await _monitorRepository.RecordPingAsync(ping);

            if (!success)
            {
                return NotFound(new { message = "Monitor not found" });
            }
            return Ok(new { message = "Ping registered successfully", timestamp = DateTime.UtcNow });
        }
    }
}
