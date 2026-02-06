using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CronMonitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("monitor")]
    public class MonitorController : Controller
    {
        private readonly IMonitorRepository _monitorRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly Logic.Service.ProjectService _projectService;
        private readonly Logic.Interfaces.IAccessKeyService _accessKeyService;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly AxiomLogger _axiomLogger;

        public MonitorController(
            IMonitorRepository monitorRepository,
            IProjectRepository projectRepository,
            Logic.Service.ProjectService projectService,
            Logic.Interfaces.IAccessKeyService accessKeyService,
            IIntegrationRepository integrationRepository,
            AxiomLogger axiomLogger)
        {
            _monitorRepository = monitorRepository;
            _projectRepository = projectRepository;
            _projectService = projectService;
            _accessKeyService = accessKeyService;
            _integrationRepository = integrationRepository;
            _axiomLogger = axiomLogger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] MonitorCreationModel model)
        {
            try
            {
                var user = HttpContext.Items["User"] as User;
                if (user == null) return Unauthorized();

                var project = await _projectRepository.GetProjectBySlugAsync(model.ProjectSlug);
                if (project == null || project.UserId != user.Id)
                {
                    return NotFound("Project not found or access denied");
                }

                var monitorSlug = !string.IsNullOrWhiteSpace(model.Slug)
                    ? model.Slug
                    : _projectService.GenerateSlug(model.Name);

                if (await _monitorRepository.SlugExistsAsync(project.Id, monitorSlug))
                {
                    if (!string.IsNullOrWhiteSpace(model.Slug))
                    {
                        monitorSlug = _projectService.GenerateSlug(model.Name);
                    }

                    if (await _monitorRepository.SlugExistsAsync(project.Id, monitorSlug))
                    {
                        monitorSlug = $"{monitorSlug}-{DateTime.UtcNow.Ticks}";
                    }
                }

                var monitor = new CronMonitor
                {
                    ProjectId = project.Id,
                    Name = model.Name,
                    Slug = monitorSlug,
                    ScheduleType = model.ScheduleType,
                    GraceSeconds = model.GraceSeconds * GetSecondsMultiplier(model.GraceUnit),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                switch (model.ScheduleType)
                {
                    case ScheduleType.Interval:
                        monitor.PeriodSeconds = model.PeriodValue * GetSecondsMultiplier(model.PeriodUnit);
                        break;
                    case ScheduleType.Cron:
                        monitor.CronExpression = model.CronExpression;
                        monitor.CronTimezone = model.CronTimezone;
                        break;
                    case ScheduleType.Calendar:
                        monitor.CalendarExpression = model.CalendarExpression;
                        monitor.CalendarTimezone = model.CalendarTimezone;
                        break;
                }

                await _monitorRepository.CreateMonitorAsync(monitor);

                await _axiomLogger.LogInfo("Monitor created", new Dictionary<string, object>
                {
                    ["monitor_id"] = monitor.Id,
                    ["monitor_name"] = monitor.Name,
                    ["project_id"] = project.Id,
                    ["user_id"] = user.Id
                });

                return Ok(new { success = true, redirectUrl = $"/monitor/{monitor.Slug}" });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Failed to create monitor", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["project_slug"] = model.ProjectSlug
                });
                return StatusCode(500, new { error = "Failed to create monitor" });
            }
        }

        [HttpPost("update-details")]
        public async Task<IActionResult> UpdateDetails([FromBody] MonitorDetailsUpdateModel model)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(model.Id);
            if (monitor == null) return NotFound("Monitor not found");

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound("Access denied");

            if (!string.Equals(monitor.Slug, model.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var newSlug = !string.IsNullOrWhiteSpace(model.Slug) ? model.Slug : _projectService.GenerateSlug(model.Name);

                if (!System.Text.RegularExpressions.Regex.IsMatch(newSlug, "^[a-z0-9-]+$"))
                {
                    return BadRequest("Slug must contain only lowercase letters, numbers, and hyphens");
                }

                if (await _monitorRepository.SlugExistsAsync(project.Id, newSlug))
                {
                    return BadRequest("Slug already exists in this project");
                }
                monitor.Slug = newSlug;
            }

            monitor.Name = model.Name;
            monitor.UpdatedAt = DateTime.UtcNow;

            await _monitorRepository.UpdateMonitorAsync(monitor);
            return Ok(new { success = true, redirectUrl = $"/monitor/{monitor.Slug}" });
        }

        [HttpPost("update-schedule")]
        public async Task<IActionResult> UpdateSchedule([FromBody] MonitorScheduleUpdateModel model)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(model.Id);
            if (monitor == null) return NotFound("Monitor not found");

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound("Access denied");

            monitor.ScheduleType = model.ScheduleType;
            monitor.GraceSeconds = model.GraceSeconds * GetSecondsMultiplier(model.GraceUnit);
            monitor.UpdatedAt = DateTime.UtcNow;

            switch (model.ScheduleType)
            {
                case ScheduleType.Interval:
                    monitor.PeriodSeconds = model.PeriodValue * GetSecondsMultiplier(model.PeriodUnit);
                    break;
                case ScheduleType.Cron:
                    monitor.CronExpression = model.CronExpression;
                    monitor.CronTimezone = model.CronTimezone;
                    break;
                case ScheduleType.Calendar:
                    monitor.CalendarExpression = model.CalendarExpression;
                    monitor.CalendarTimezone = model.CalendarTimezone;
                    break;
            }

            await _monitorRepository.UpdateMonitorAsync(monitor);
            return Ok(new { success = true });
        }

        private int GetSecondsMultiplier(string unit)
        {
            return unit switch
            {
                "minutes" => 60,
                "hours" => 3600,
                "days" => 86400,
                _ => 1 // seconds
            };
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> Index(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var monitor = await _monitorRepository.GetMonitorBySlugAsync(slug);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound();

            ViewBag.Project = project;
            ViewBag.UserEmail = user.Email;

            var pings = await _monitorRepository.GetPingsByMonitorIdAsync(monitor.Id, 100);
            ViewBag.RecentPings = pings;

            var keys = await _accessKeyService.GetKeysByProjectIdAsync(project.Id);
            var pingKey = keys.FirstOrDefault(k => k.KeyType == ApiKeyType.Ping && k.RevokedAt == null);
            ViewBag.PingKey = pingKey?.KeyPrefix ?? "PING_KEY";

            var now = DateTime.UtcNow;
            var hourlyData = new int[24];
            for (int i = 0; i < 24; i++)
            {
                var hourStart = now.AddHours(-(i + 1));
                var hourEnd = now.AddHours(-i);
                hourlyData[23 - i] = pings.Count(p => p.ReceivedAt >= hourStart && p.ReceivedAt < hourEnd);
            }
            ViewBag.GraphData = hourlyData;

            return View(monitor);
        }

        [HttpPost("{id:guid}/pause")]
        public async Task<IActionResult> Pause(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound();

            var newStatus = monitor.LastStatus == MonitorStatus.Paused ? MonitorStatus.Success : MonitorStatus.Paused;
            await _monitorRepository.UpdateStatusAsync(id, newStatus);

            return Ok(new { success = true, status = newStatus.ToString() });
        }

        [HttpPost("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var user = HttpContext.Items["User"] as User;
                if (user == null) return Unauthorized();

                var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
                if (monitor == null) return NotFound();

                var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
                if (project == null || project.UserId != user.Id) return NotFound();

                await _monitorRepository.DeleteMonitorAsync(id);

                await _axiomLogger.LogInfo("Monitor deleted", new Dictionary<string, object>
                {
                    ["monitor_id"] = id,
                    ["user_id"] = user.Id
                });

                return Ok(new { success = true, redirectUrl = $"/{project.Slug}/monitors" });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Failed to delete monitor", new Dictionary<string, object>
                {
                    ["monitor_id"] = id,
                    ["error"] = ex.Message
                });
                return StatusCode(500, new { error = "Failed to delete monitor" });
            }
        }

        [HttpGet("api/{projectId:guid}/status")]
        public async Task<IActionResult> ApiStatus(Guid projectId)
        {
            var accessKey = HttpContext.Items["AccessKey"] as ProjectAccessKey;
            if (accessKey == null || accessKey.ProjectId != projectId)
            {
                return Unauthorized(new { message = "Invalid or missing API key" });
            }

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(projectId);
            return Json(monitors.Select(m => new
            {
                m.Id,
                m.Name,
                m.Slug,
                m.LastStatus,
                m.LastPingAt,
                m.NextExpectedAt
            }));
        }

        [HttpGet("{id:guid}/integrations")]
        public async Task<IActionResult> GetIntegrations(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound();

            var integrations = await _integrationRepository.GetMonitorIntegrationsAsync(id);
            return Json(integrations);
        }

        [HttpPost("{id:guid}/integrations")]
        public async Task<IActionResult> AddIntegration(Guid id, [FromBody] AddIntegrationModel model)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound();

            var integration = await _integrationRepository.GetIntegrationByIdAsync(model.IntegrationId);
            if (integration == null || integration.ProjectId != project.Id)
            {
                return BadRequest("Integration not found or does not belong to this project");
            }

            await _integrationRepository.AddMonitorIntegrationAsync(id, model.IntegrationId);
            return Ok(new { success = true });
        }

        [HttpDelete("{id:guid}/integrations/{integrationId:guid}")]
        public async Task<IActionResult> RemoveIntegration(Guid id, Guid integrationId)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id) return NotFound();

            await _integrationRepository.RemoveMonitorIntegrationAsync(id, integrationId);
            return Ok(new { success = true });
        }
    }
}
