using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("{projectSlug}/logs")]
    public class LogController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IMonitorRepository _monitorRepository;
        private readonly AxiomLogger _axiomLogger;
        private readonly IProjectAuthService _projectAuth;

        public LogController(
            IProjectRepository projectRepository,
            IMonitorRepository monitorRepository,
            AxiomLogger axiomLogger,
            IProjectAuthService projectAuth)
        {
            _projectRepository = projectRepository;
            _monitorRepository = monitorRepository;
            _axiomLogger = axiomLogger;
            _projectAuth = projectAuth;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string projectSlug, int? status, string? search, int limit = 25)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var project = await _projectRepository.GetProjectBySlugAsync(projectSlug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);

            ViewBag.Project = project;
            ViewBag.Monitors = monitors;
            ViewBag.UserEmail = user.Email;
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.Limit = limit;
            ViewBag.UserTimezone = string.IsNullOrWhiteSpace(user.Timezone) ? "UTC" : user.Timezone;

            var logs = await _monitorRepository.GetPingsWithFiltersAsync(project.Id, null, status, search, limit);

            return View("~/Views/Shared/Logs.cshtml", logs);
        }

        [HttpGet("/checks/{monitorId:guid}/log")]
        public async Task<IActionResult> MonitorLog(Guid monitorId, int? status, string? search, int limit = 25)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var monitor = await _monitorRepository.GetMonitorByIdAsync(monitorId);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);

            ViewBag.Project = project;
            ViewBag.Monitors = monitors;
            ViewBag.UserEmail = user.Email;
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.MonitorId = monitorId;
            ViewBag.Limit = limit;
            ViewBag.UserTimezone = string.IsNullOrWhiteSpace(user.Timezone) ? "UTC" : user.Timezone;

            var logs = await _monitorRepository.GetPingsWithFiltersAsync(project.Id, monitorId, status, search, limit);

            return View("~/Views/Shared/Logs.cshtml", logs);
        }

        [HttpGet("/monitor/{slug}/logs")]
        public async Task<IActionResult> MonitorLogBySlug(string slug, int? status, string? search, int limit = 25)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var monitor = await _monitorRepository.GetMonitorBySlugAsync(slug);
            if (monitor == null) return NotFound();

            var project = await _projectRepository.GetProjectByIdAsync(monitor.ProjectId);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            return await MonitorLog(monitor.Id, status, search, limit);
        }

        [HttpGet("partials/logs")]
        public async Task<IActionResult> GetLogsPartial(Guid? monitorId, int? status, string? search, int limit = 25, int offset = 0)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            if (!RouteData.Values.TryGetValue("projectSlug", out var slugObj) || slugObj == null)
                return BadRequest("Project Context Missing");

            string projectSlug = slugObj.ToString()!;

            var project = await _projectRepository.GetProjectBySlugAsync(projectSlug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            ViewBag.UserTimezone = string.IsNullOrWhiteSpace(user.Timezone) ? "UTC" : user.Timezone;

            var pings = await _monitorRepository.GetPingsWithFiltersAsync(project.Id, monitorId, status, search, limit, offset);

            return PartialView("~/Views/Shared/_LogRows.cshtml", pings);
        }
    }
}
