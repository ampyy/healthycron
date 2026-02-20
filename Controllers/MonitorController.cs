using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using HealthyCron.Models.DTOs;
using HealthyCron.Enums;
using HealthyCron.Utilities.Interface;
using HealthyCron.Logic.Interfaces;
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
        private readonly IProjectAuthService _projectAuth;

        public MonitorController(
            IMonitorRepository monitorRepository,
            IProjectRepository projectRepository,
            Logic.Service.ProjectService projectService,
            Logic.Interfaces.IAccessKeyService accessKeyService,
            IIntegrationRepository integrationRepository,
            AxiomLogger axiomLogger,
            IProjectAuthService projectAuth)
        {
            _monitorRepository = monitorRepository;
            _projectRepository = projectRepository;
            _projectService = projectService;
            _accessKeyService = accessKeyService;
            _integrationRepository = integrationRepository;
            _axiomLogger = axiomLogger;
            _projectAuth = projectAuth;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] MonitorCreationModel model)
        {
            try
            {
                var user = HttpContext.Items["User"] as User;
                if (user == null) return Unauthorized();

                var project = await _projectRepository.GetProjectBySlugAsync(model.ProjectSlug);
                if (project == null) return NotFound("Project not found");

                if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id))
                    return Forbid();

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
                        var periodSeconds = model.PeriodValue * GetSecondsMultiplier(model.PeriodUnit);
                        if (periodSeconds < 60)
                        {
                            return BadRequest(new { success = false, message = "Minimum monitor interval is 60 seconds" });
                        }
                        monitor.PeriodSeconds = periodSeconds;
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
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id))
                return Forbid();

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
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            monitor.ScheduleType = model.ScheduleType;
            monitor.GraceSeconds = model.GraceSeconds * GetSecondsMultiplier(model.GraceUnit);
            monitor.UpdatedAt = DateTime.UtcNow;

            switch (model.ScheduleType)
            {
                case ScheduleType.Interval:
                    var periodSeconds = model.PeriodValue * GetSecondsMultiplier(model.PeriodUnit);
                    if (periodSeconds < 60)
                    {
                        return BadRequest(new { success = false, message = "Minimum monitor interval is 60 seconds" });
                    }
                    monitor.PeriodSeconds = periodSeconds;
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
            if (project == null) return NotFound();

            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var canManage = await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id);

            ViewBag.Project = project;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserTimezone = string.IsNullOrWhiteSpace(user.Timezone) ? "UTC" : user.Timezone;
            ViewBag.CanManage = canManage;
            ViewBag.IsOwner = _projectAuth.IsOwner(project.UserId, user.Id);
            ViewBag.ProjectRole = await _projectAuth.GetMemberRoleAsync(project.Id, user.Id);

            var pings = await _monitorRepository.GetPingsByMonitorIdAsync(monitor.Id, 100);
            ViewBag.RecentPings = pings;

            var keys = await _accessKeyService.GetKeysByProjectIdAsync(project.Id);
            var pingKey = keys.OrderByDescending(k => k.CreatedAt).FirstOrDefault(k => k.KeyType == ApiKeyType.Ping && k.RevokedAt == null);
            ViewBag.PingKey = pingKey?.PlaintextKey ?? pingKey?.KeyPrefix ?? "PING_KEY";

            var now = DateTime.UtcNow;
            var hourlyData = new int[24];
            for (int i = 0; i < 24; i++)
            {
                var hourStart = now.AddHours(-(i + 1));
                var hourEnd = now.AddHours(-i);
                hourlyData[23 - i] = pings.Count(p => p.ReceivedAt >= hourStart && p.ReceivedAt < hourEnd);
            }
            ViewBag.GraphData = hourlyData;

            var integrations = await _integrationRepository.GetMonitorIntegrationsAsync(monitor.Id);
            ViewBag.MonitorIntegrations = integrations.ToList();

            var monthlyStats = await _monitorRepository.GetMonthlyStatsAsync(monitor.Id);
            ViewBag.MonthlyStats = monthlyStats.ToList();

            return View(monitor);
        }

        [HttpPost("{id:guid}/pause")]
        public async Task<IActionResult> Pause(Guid id, [FromServices] IPingService pingService)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var isPausing = monitor.LastStatus != MonitorStatus.Paused;
            var newStatus = isPausing ? MonitorStatus.Paused : MonitorStatus.Success;

            // Update status
            await _monitorRepository.UpdateStatusAsync(id, newStatus);

            // Record a manual ping for the state change
            var now = DateTime.UtcNow;
            var alertType = isPausing ? Enums.AlertType.Paused : Enums.AlertType.Resumed;
            
            var ping = new MonitorPing
            {
                MonitorId = monitor.Id,
                Status = isPausing ? PingType.Paused : PingType.Resumed,
                Message = isPausing ? "Monitor paused manually" : "Monitor resumed manually",
                ReceivedAt = now,
                HttpMethod = "INTERNAL",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var pingId = await _monitorRepository.RecordPingAsync(ping, newStatus);

            if (pingId.HasValue)
            {
                var integrations = await _integrationRepository.GetMonitorIntegrationsAsync(monitor.Id);
                foreach (var item in integrations)
                {
                    if (!item.IsEnabledForMonitor) continue;

                    var integration = item.Integration;
                    try
                    {
                        var jobId = await _integrationRepository.CreateNotificationJobAsync(
                            pingId.Value,
                            integration.Id
                        );

                        // Trigger SQS
                        var queueService = HttpContext.RequestServices.GetRequiredService<IQueueService>();
                        var sqsPayload = new HealthyCron.Models.DTOs.SqsMessagePayload
                        {
                            JobId = jobId,
                            MonitorId = monitor.Id,
                            IntegrationId = integration.Id
                        };
                        await queueService.SendMessageAsync(sqsPayload);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to trigger pause notification: {ex.Message}");
                    }
                }
            }

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
                if (project == null) return NotFound();

                if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id))
                    return Forbid();

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
            if (project == null) return NotFound();
            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id)) return Forbid();

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
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id)) return Forbid();

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
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id)) return Forbid();

            await _integrationRepository.RemoveMonitorIntegrationAsync(id, integrationId);
            return Ok(new { success = true });
        }

        [HttpPost("{id:guid}/integrations/{integrationId:guid}/toggle")]
        public async Task<IActionResult> ToggleIntegration(Guid id, Guid integrationId, [FromBody] ToggleIntegrationRequest request)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var monitor = await _monitorRepository.GetMonitorByIdAsync(id);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id)) return Forbid();

            await _integrationRepository.UpdateMonitorIntegrationStatusAsync(id, integrationId, request.IsEnabled);
            return Ok(new { success = true });
        }
    }
}
