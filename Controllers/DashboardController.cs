using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly AxiomLogger _axiomLogger;

        public DashboardController(IProjectRepository projectRepository, AxiomLogger axiomLogger)
        {
            _projectRepository = projectRepository;
            _axiomLogger = axiomLogger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.Items["User"] as User;
            ViewBag.UserEmail = user!.Email;

            var stats = await _projectRepository.GetDashboardStatsAsync(user.Id);
            var chartData = await _projectRepository.GetRecentPingStatsAsync(user.Id);

            ViewBag.ChartData = chartData;
            return View("Stats", stats);
        }

        [HttpGet("projects")]
        public async Task<IActionResult> Projects()
        {
            var user = HttpContext.Items["User"] as User;
            ViewBag.UserEmail = user!.Email;

            var projects = await _projectRepository.GetProjectsByUserIdAsync(user.Id);
            return View("Projects", projects);
        }

    }
}
