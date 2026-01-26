using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using Microsoft.AspNetCore.Mvc;
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

        public MonitorController(IMonitorRepository monitorRepository, IProjectRepository projectRepository, Logic.Service.ProjectService projectService)
        {
            _monitorRepository = monitorRepository;
            _projectRepository = projectRepository;
            _projectService = projectService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] MonitorCreationModel model)
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
                // If the user provided a slug that already exists, generate a new one based on the name
                if (!string.IsNullOrWhiteSpace(model.Slug))
                {
                    monitorSlug = _projectService.GenerateSlug(model.Name);
                }

                // If even the name-based slug exists, append a timestamp
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

            return Ok(new { success = true, redirectUrl = $"/monitor/{monitor.Slug}" });
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
            if (monitor == null)
            {
                return NotFound();
            }

            // Verify ownership via project
            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null || project.UserId != user.Id)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            ViewBag.UserEmail = user.Email;
            return View(monitor);
        }
    }
}
