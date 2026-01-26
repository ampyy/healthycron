using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using Microsoft.AspNetCore.Mvc;
using HealthyCron.Logic.Service;
using CronMonitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("project")]
    public class ProjectController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IMonitorRepository _monitorRepository;
        private readonly ProjectService _projectService;

        public ProjectController(IProjectRepository projectRepository, IMonitorRepository monitorRepository, ProjectService projectService)
        {
            _projectRepository = projectRepository;
            _monitorRepository = monitorRepository;
            _projectService = projectService;
        }

        [HttpGet("/{slug}/monitors")]
        [HttpGet("{slug}/monitors")]
        public async Task<IActionResult> Monitors(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            ViewBag.UserEmail = user!.Email;

            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null || project.UserId != user.Id)
            {
                return NotFound();
            }

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);

            ViewBag.Project = project;
            return View(monitors);
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
    }
}
