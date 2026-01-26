using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using Microsoft.AspNetCore.Mvc;
using HealthyCron.Logic.Service;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ProjectService _projectService;

        public DashboardController(IProjectRepository projectRepository, ProjectService projectService)
        {
            _projectRepository = projectRepository;
            _projectService = projectService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.Items["User"] as User;
            ViewBag.UserEmail = user!.Email;

            var projects = await _projectRepository.GetProjectsByUserIdAsync(user.Id);
            // In a real app, we'd aggregate stats. For now, we'll pass the projects.
            return View("Stats", projects);
        }

        [HttpGet("projects")]
        public async Task<IActionResult> Projects()
        {
            var user = HttpContext.Items["User"] as User;
            ViewBag.UserEmail = user!.Email;

            var projects = await _projectRepository.GetProjectsByUserIdAsync(user.Id);
            return View("Projects", projects);
        }

        [HttpPost("create-project")]
        public async Task<IActionResult> CreateProject([FromForm] string name)
        {
            var user = HttpContext.Items["User"] as User;
            var slug = _projectService.GenerateSlug(name);

            // Ensure unique slug (simple check for now, can be improved)
            if (await _projectRepository.SlugExistsAsync(slug))
            {
                slug = $"{slug}-{DateTime.UtcNow.Ticks}";
            }

            var project = new Project
            {
                UserId = user!.Id,
                Name = name,
                Slug = slug,
                Color = GetRandomColor(),
                Icon = GetRandomIcon(),
                CreatedAt = DateTime.UtcNow
            };

            await _projectRepository.CreateProjectAsync(project);

            return RedirectToAction("Projects");
        }
        private string GetRandomColor()
        {
            var colors = new[] { "blue", "purple", "green", "orange", "pink", "cyan" };
            return colors[new Random().Next(colors.Length)];
        }

        private string GetRandomIcon()
        {
            var icons = new[] { "server", "activity", "cloud", "database", "terminal", "globe", "cpu", "layers" };
            return icons[new Random().Next(icons.Length)];
        }
    }
}
