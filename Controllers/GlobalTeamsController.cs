using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("teams")]
    public class GlobalTeamsController : Controller
    {
        private readonly IProjectMemberRepository _memberRepository;
        private readonly IProjectRepository _projectRepository;

        public GlobalTeamsController(
            IProjectMemberRepository memberRepository,
            IProjectRepository projectRepository)
        {
            _memberRepository = memberRepository;
            _projectRepository = projectRepository;
        }

        // GET /teams
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            // All members across all projects owned by this user
            var allMembers = (await _memberRepository.GetAllMembersForOwnerAsync(user.Id)).ToList();

            ViewBag.UserEmail = user.Email;
            ViewBag.Members = allMembers;

            return View("~/Views/Teams/Index.cshtml");
        }
    }
}
