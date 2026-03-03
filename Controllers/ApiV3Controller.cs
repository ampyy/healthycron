using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using CronMonitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Controllers
{
    [Route("api/v1")]
    [ApiController]
    [ApiKeyAuth]
    public class ApiV3Controller : ControllerBase
    {
        private readonly IMonitorRepository _monitorRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly Logic.Service.ProjectService _projectService;

        public ApiV3Controller(
            IMonitorRepository monitorRepository,
            IProjectRepository projectRepository,
            IIntegrationRepository integrationRepository,
            Logic.Service.ProjectService projectService)
        {
            _monitorRepository = monitorRepository;
            _projectRepository = projectRepository;
            _integrationRepository = integrationRepository;
            _projectService = projectService;
        }

        // ─── Helpers ────────────────────────────────────────────────

        private ProjectAccessKey GetApiKey() => (ProjectAccessKey)HttpContext.Items["ApiKey"]!;

        private bool IsReadOnly() => GetApiKey().KeyType == ApiKeyType.ReadAccess;

        private IActionResult WriteProtected()
            => StatusCode(403, new { error = "Read-only API keys cannot perform write operations." });

        private object MapCheck(CronMonitor m)
        {
            var status = m.LastStatus switch
            {
                MonitorStatus.Success => "up",
                MonitorStatus.Failed => "down",
                MonitorStatus.Paused => "paused",
                _ => "new"
            };

            var schedule = m.ScheduleType switch
            {
                ScheduleType.Interval => $"every {m.PeriodSeconds}s",
                ScheduleType.Cron => m.CronExpression ?? "",
                ScheduleType.Calendar => m.CalendarExpression ?? "",
                _ => ""
            };

            return new
            {
                name = m.Name,
                slug = m.Slug,
                tags = "",
                desc = "",
                grace = m.GraceSeconds ?? 0,
                n_pings = 0,
                status,
                last_ping = m.LastPingAt?.ToString("o"),
                next_ping = m.NextExpectedAt?.ToString("o"),
                manual_resume = false,
                schedule,
                tz = m.CronTimezone ?? "UTC",
                uuid = m.Id,
                ping_url = $"{Request.Scheme}://{Request.Host}/ping/{m.Id}",
                pause_url = $"{Request.Scheme}://{Request.Host}/api/v1/checks/{m.Id}/pause",
                created_at = m.CreatedAt.ToString("o"),
                updated_at = m.UpdatedAt.ToString("o")
            };
        }

        private object MapPing(MonitorPing p)
        {
            return new
            {
                type = p.Status.ToString().ToLower(),
                date = p.ReceivedAt.ToString("o"),
                n = p.Id,
                duration = p.DurationMs,
                remote_addr = p.IpAddress,
                method = p.HttpMethod,
                ua = p.UserAgent
            };
        }

        private object MapIntegration(Integration i)
        {
            return new
            {
                id = i.Id,
                name = i.Name,
                kind = i.Type.ToString().ToLower()
            };
        }

        // ─── Checks ─────────────────────────────────────────────────

        /// <summary>List all monitors for the project attached to the API key.</summary>
        [HttpGet("checks")]
        public async Task<IActionResult> ListChecks()
        {
            var key = GetApiKey();
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(key.ProjectId);
            var checks = monitors.Where(m => !m.IsDeleted).Select(MapCheck);
            return Ok(new { checks });
        }

        /// <summary>Get a single monitor by UUID.</summary>
        [HttpGet("checks/{uuid:guid}")]
        public async Task<IActionResult> GetCheck(Guid uuid)
        {
            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorByIdAsync(uuid);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });
            return Ok(MapCheck(monitor));
        }

        /// <summary>Get a single monitor by slug (unique_key).</summary>
        [HttpGet("checks/{slug}")]
        public async Task<IActionResult> GetCheckBySlug(string slug)
        {
            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorBySlugAsync(slug, key.ProjectId);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });
            return Ok(MapCheck(monitor));
        }

        /// <summary>Create a new monitor.</summary>
        [HttpPost("checks")]
        public async Task<IActionResult> CreateCheck([FromBody] ApiCheckCreateModel model)
        {
            if (IsReadOnly()) return WriteProtected();

            var key = GetApiKey();

            if (string.IsNullOrWhiteSpace(model.name))
                return BadRequest(new { error = "name is required." });

            var slug = !string.IsNullOrWhiteSpace(model.slug)
                ? model.slug
                : _projectService.GenerateSlug(model.name);

            if (await _monitorRepository.SlugExistsAsync(key.ProjectId, slug))
                slug = $"{slug}-{DateTime.UtcNow.Ticks}";

            var scheduleType = ScheduleType.Interval;
            string? cronExpr = null;
            int? periodSeconds = null;

            if (!string.IsNullOrWhiteSpace(model.schedule))
            {
                // If schedule looks like a cron expression, use cron
                if (model.schedule.Trim().Split(' ').Length >= 5)
                {
                    scheduleType = ScheduleType.Cron;
                    cronExpr = model.schedule.Trim();
                }
                else if (int.TryParse(model.schedule, out var secs))
                {
                    periodSeconds = Math.Max(60, secs);
                }
            }

            if (periodSeconds == null && scheduleType == ScheduleType.Interval)
            {
                // Default interval of 1 hour
                periodSeconds = model.timeout ?? 3600;
            }

            var monitor = new CronMonitor
            {
                ProjectId = key.ProjectId,
                Name = model.name,
                Slug = slug,
                ScheduleType = scheduleType,
                PeriodSeconds = periodSeconds,
                CronExpression = cronExpr,
                CronTimezone = model.tz ?? "UTC",
                GraceSeconds = model.grace ?? 300,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _monitorRepository.CreateMonitorAsync(monitor);
            return StatusCode(201, MapCheck(monitor));
        }

        /// <summary>Update an existing monitor.</summary>
        [HttpPost("checks/{uuid:guid}")]
        public async Task<IActionResult> UpdateCheck(Guid uuid, [FromBody] ApiCheckCreateModel model)
        {
            if (IsReadOnly()) return WriteProtected();

            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorByIdAsync(uuid);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });

            if (!string.IsNullOrWhiteSpace(model.name)) monitor.Name = model.name;
            if (!string.IsNullOrWhiteSpace(model.slug)) monitor.Slug = model.slug;
            if (model.grace.HasValue) monitor.GraceSeconds = model.grace.Value;

            if (!string.IsNullOrWhiteSpace(model.schedule))
            {
                if (model.schedule.Trim().Split(' ').Length >= 5)
                {
                    monitor.ScheduleType = ScheduleType.Cron;
                    monitor.CronExpression = model.schedule.Trim();
                    monitor.PeriodSeconds = null;
                }
                else if (int.TryParse(model.schedule, out var secs))
                {
                    monitor.ScheduleType = ScheduleType.Interval;
                    monitor.PeriodSeconds = Math.Max(60, secs);
                    monitor.CronExpression = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(model.tz)) monitor.CronTimezone = model.tz;

            monitor.UpdatedAt = DateTime.UtcNow;
            await _monitorRepository.UpdateMonitorAsync(monitor);

            return Ok(MapCheck(monitor));
        }

        /// <summary>Pause a monitor.</summary>
        [HttpPost("checks/{uuid:guid}/pause")]
        public async Task<IActionResult> PauseCheck(Guid uuid)
        {
            if (IsReadOnly()) return WriteProtected();

            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorByIdAsync(uuid);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });

            await _monitorRepository.UpdateStatusAsync(uuid, MonitorStatus.Paused);

            monitor.LastStatus = MonitorStatus.Paused;
            return Ok(MapCheck(monitor));
        }

        /// <summary>Resume a paused monitor.</summary>
        [HttpPost("checks/{uuid:guid}/resume")]
        public async Task<IActionResult> ResumeCheck(Guid uuid)
        {
            if (IsReadOnly()) return WriteProtected();

            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorByIdAsync(uuid);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });

            await _monitorRepository.UpdateStatusAsync(uuid, MonitorStatus.Success);

            monitor.LastStatus = MonitorStatus.Success;
            return Ok(MapCheck(monitor));
        }

        /// <summary>Soft-delete a monitor.</summary>
        [HttpDelete("checks/{uuid:guid}")]
        public async Task<IActionResult> DeleteCheck(Guid uuid)
        {
            if (IsReadOnly()) return WriteProtected();

            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorByIdAsync(uuid);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });

            await _monitorRepository.DeleteMonitorAsync(uuid);
            return Ok(new { msg = "Check deleted." });
        }

        // ─── Pings ──────────────────────────────────────────────────

        /// <summary>List logged pings for a monitor.</summary>
        [HttpGet("checks/{uuid:guid}/pings")]
        public async Task<IActionResult> ListPings(Guid uuid)
        {
            var key = GetApiKey();
            var monitor = await _monitorRepository.GetMonitorByIdAsync(uuid);
            if (monitor == null || monitor.ProjectId != key.ProjectId || monitor.IsDeleted)
                return NotFound(new { error = "Check not found." });

            var pings = await _monitorRepository.GetPingsByMonitorIdAsync(uuid, 100);
            return Ok(new { pings = pings.Select(MapPing) });
        }

        /// <summary>Get a ping's logged body (stub).</summary>
        [HttpGet("checks/{uuid:guid}/pings/{n:int}/body")]
        public IActionResult GetPingBody(Guid uuid, int n)
        {
            return Ok(new { body = "" });
        }

        // ─── Flips ──────────────────────────────────────────────────

        /// <summary>List check's status changes (stub).</summary>
        [HttpGet("checks/{uuid:guid}/flips")]
        public IActionResult ListFlips(Guid uuid)
        {
            return Ok(new { flips = Array.Empty<object>() });
        }

        [HttpGet("checks/{slug}/flips")]
        public IActionResult ListFlipsBySlug(string slug)
        {
            return Ok(new { flips = Array.Empty<object>() });
        }

        // ─── Integrations (Channels) ────────────────────────────────

        /// <summary>List integrations for the project.</summary>
        [HttpGet("channels")]
        public async Task<IActionResult> ListChannels()
        {
            var key = GetApiKey();
            var integrations = await _integrationRepository.GetIntegrationsByProjectIdAsync(key.ProjectId);
            return Ok(new { channels = integrations.Select(MapIntegration) });
        }

        // ─── Badges ─────────────────────────────────────────────────

        /// <summary>List project badges (stub).</summary>
        [HttpGet("badges")]
        public IActionResult ListBadges()
        {
            return Ok(new { badges = new { } });
        }

        // ─── Status ─────────────────────────────────────────────────

        /// <summary>Service health / DB connectivity check.</summary>
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new { status = "ok" });
        }
    }
}
