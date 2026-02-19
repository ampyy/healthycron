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
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IProjectAuthService _projectAuth;
        private readonly IProjectMemberRepository _memberRepository;

        public ProjectController(
            IProjectRepository projectRepository,
            IMonitorRepository monitorRepository,
            ProjectService projectService,
            IAccessKeyService accessKeyService,
            AxiomLogger axiomLogger,
            IIntegrationRepository integrationRepository,
            IProjectAuthService projectAuth,
            IProjectMemberRepository memberRepository)
        {
            _projectRepository = projectRepository;
            _monitorRepository = monitorRepository;
            _projectService = projectService;
            _accessKeyService = accessKeyService;
            _axiomLogger = axiomLogger;
            _integrationRepository = integrationRepository;
            _projectAuth = projectAuth;
            _memberRepository = memberRepository;
        }


        [HttpGet("/{slug}/monitors")]
        [HttpGet("{slug}/monitors")]
        public async Task<IActionResult> Monitors(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            ViewBag.UserEmail = user!.Email;

            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var userRole = _projectAuth.IsOwner(project.UserId, user.Id)
                ? "Owner"
                : (await _projectAuth.GetMemberRoleAsync(project.Id, user.Id))?.ToString() ?? "ReadOnly";

            var monitorsList = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            var viewModel = new HealthyCron.Models.ViewModels.ProjectMonitorsViewModel
            {
                Project = project,
                Monitors = new List<HealthyCron.Models.ViewModels.MonitorWithIntegrations>()
            };

            foreach (var monitor in monitorsList)
            {
                var monitorIntegrations = await _integrationRepository.GetMonitorIntegrationsAsync(monitor.Id);
                viewModel.Monitors.Add(new HealthyCron.Models.ViewModels.MonitorWithIntegrations
                {
                    Monitor = monitor,
                    Integrations = monitorIntegrations.Where(i => i.IsEnabledForMonitor).ToList()
                });
            }

            ViewBag.UserRole = userRole;
            ViewBag.CanManage = await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user.Id);
            ViewBag.CanManageMembers = await _projectAuth.CanManageMembersAsync(project.Id, project.UserId, user.Id);
            ViewBag.IsOwner = _projectAuth.IsOwner(project.UserId, user.Id);

            return View(viewModel);
        }

        [HttpGet("{slug}/settings")]
        public async Task<IActionResult> Settings(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            // Settings page requires at least Manager or Owner
            if (!await _projectAuth.CanManageMembersAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            ViewBag.Project = project;
            ViewBag.User = user;
            ViewBag.IsOwner = _projectAuth.IsOwner(project.UserId, user.Id);
            ViewBag.AccessKeys = await _accessKeyService.GetKeysByProjectIdAsync(project.Id);
            ViewBag.Members = (await _memberRepository.GetMembersAsync(project.Id)).ToList();
            ViewBag.Invitations = (await _memberRepository.GetPendingInvitationsAsync(project.Id)).ToList();
            return View();
        }

        [HttpPost("{slug}/update")]
        public async Task<IActionResult> UpdateProject(string slug, [FromForm] string name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMembersAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            project.Name = name;
            await _projectRepository.UpdateProjectAsync(project);

            return RedirectToAction("Settings", new { slug = project.Slug });
        }

        [HttpPost("{slug}/delete")]
        public async Task<IActionResult> DeleteProject(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            // Owner only
            if (!_projectAuth.CanDeleteProject(project.UserId, user!.Id))
                return Forbid();

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
