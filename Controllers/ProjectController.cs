using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;
using HealthyCron.Logic.Service;
using HealthyCron.Logic.Interfaces;
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
        private readonly IAccessKeyService _accessKeyService;
        private readonly AxiomLogger _axiomLogger;

        public ProjectController(IProjectRepository projectRepository, IMonitorRepository monitorRepository, ProjectService projectService, IAccessKeyService accessKeyService, AxiomLogger axiomLogger)
        {
            _projectRepository = projectRepository;
            _monitorRepository = monitorRepository;
            _projectService = projectService;
            _accessKeyService = accessKeyService;
            _axiomLogger = axiomLogger;
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

        [HttpGet("{slug}/settings")]
        public async Task<IActionResult> Settings(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            ViewBag.User = user;
            ViewBag.AccessKeys = await _accessKeyService.GetKeysByProjectIdAsync(project.Id);
            return View();
        }

        [HttpPost("{slug}/update")]
        public async Task<IActionResult> UpdateProject(string slug, [FromForm] string name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            project.Name = name;

            await _projectRepository.UpdateProjectAsync(project);

            return RedirectToAction("Settings", new { slug = project.Slug });
        }

        [HttpPost("{slug}/delete")]
        public async Task<IActionResult> DeleteProject(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            await _projectRepository.DeleteProjectAsync(project.Id);

            return RedirectToAction("Projects", "Dashboard");
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProject([FromForm] string name)
        {
            try
            {
                var user = HttpContext.Items["User"] as User;
                var slug = _projectService.GenerateSlug(name);

                if (await _projectRepository.SlugExistsAsync(slug))
                {
                    slug = $"{slug}-{DateTime.UtcNow.Ticks}";
                }

                var project = new Project
                {
                    UserId = user!.Id,
                    Name = name,
                    Slug = slug,
                    CreatedAt = DateTime.UtcNow
                };

                await _projectRepository.CreateProjectAsync(project);

                await _axiomLogger.LogInfo("Project created", new Dictionary<string, object>
                {
                    ["project_id"] = project.Id,
                    ["project_name"] = name,
                    ["user_id"] = user.Id
                });

                return RedirectToAction("Projects", "Dashboard");
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Failed to create project", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["project_name"] = name
                });
                return StatusCode(500, "Failed to create project");
            }
        }
    }
}
