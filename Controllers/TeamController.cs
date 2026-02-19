using HealthyCron.Data.Interfaces;
using HealthyCron.Enums;
using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("project/{slug}/team")]
    public class TeamController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectMemberRepository _memberRepository;
        private readonly IProjectAuthService _projectAuth;
        private readonly IEmailService _emailService;

        public TeamController(
            IProjectRepository projectRepository,
            IProjectMemberRepository memberRepository,
            IProjectAuthService projectAuth,
            IEmailService emailService)
        {
            _projectRepository = projectRepository;
            _memberRepository = memberRepository;
            _projectAuth = projectAuth;
            _emailService = emailService;
        }

        // GET /project/{slug}/team
        [HttpGet("")]
        public async Task<IActionResult> Index(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMembersAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var members = await _memberRepository.GetMembersAsync(project.Id);
            var invitations = await _memberRepository.GetPendingInvitationsAsync(project.Id);

            ViewBag.Project = project;
            ViewBag.UserEmail = user.Email;
            ViewBag.IsOwner = _projectAuth.IsOwner(project.UserId, user.Id);
            ViewBag.Members = members.ToList();
            ViewBag.Invitations = invitations.ToList();

            return View("~/Views/Project/Team.cshtml");
        }

        // POST /project/{slug}/team/invite
        [HttpPost("invite")]
        public async Task<IActionResult> Invite(string slug, [FromForm] string email, [FromForm] int role)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMembersAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            var projectRole = (ProjectRole)role;
            var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                               .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var invitation = new ProjectInvitation
            {
                ProjectId = project.Id,
                Email = email.Trim().ToLowerInvariant(),
                Role = projectRole,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _memberRepository.CreateInvitationAsync(invitation);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var acceptUrl = $"{baseUrl}/team/invite/accept?token={token}";
            var roleName = projectRole.ToString();

            try
            {
                await _emailService.SendInviteEmailAsync(invitation.Email, project.Name, user.Email, roleName, acceptUrl);
            }
            catch
            {
                // Don't fail the request if email fails; invitation is still saved
            }

            TempData["Success"] = $"Invitation sent to {email}.";
            return Redirect($"/project/{slug}/settings");
        }

        // DELETE /project/{slug}/team/members/{userId}
        [HttpPost("members/{memberId:guid}/remove")]
        public async Task<IActionResult> RemoveMember(string slug, Guid memberId)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMembersAsync(project.Id, project.UserId, user.Id))
                return Forbid();

            // Prevent removing the owner
            if (memberId == project.UserId)
                return BadRequest(new { error = "Cannot remove project owner." });

            await _memberRepository.RemoveMemberAsync(project.Id, memberId);
            TempData["Success"] = "Member removed.";
            return Redirect($"/project/{slug}/settings");
        }
    }

    // Separate controller for the public accept-invite flow (no [Auth] guard here —
    // user must be logged in but we redirect to login if not)
    [Route("team/invite")]
    public class InviteAcceptController : Controller
    {
        private readonly IProjectMemberRepository _memberRepository;
        private readonly IProjectRepository _projectRepository;

        public InviteAcceptController(
            IProjectMemberRepository memberRepository,
            IProjectRepository projectRepository)
        {
            _memberRepository = memberRepository;
            _projectRepository = projectRepository;
        }

        // GET /team/invite/accept?token=...
        [HttpGet("accept")]
        public async Task<IActionResult> Accept(string token)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null)
            {
                var encodedReturn = Uri.EscapeDataString($"/team/invite/accept?token={token}");
                return Redirect($"/login?returnUrl={encodedReturn}");
            }

            var invitation = await _memberRepository.GetInvitationByTokenAsync(token);
            if (invitation == null)
                return View("~/Views/Team/AcceptInvite.cshtml", new AcceptInviteViewModel { Error = "Invitation not found or already used." });

            if (invitation.IsAccepted)
                return View("~/Views/Team/AcceptInvite.cshtml", new AcceptInviteViewModel { Error = "This invitation has already been accepted." });

            if (invitation.IsExpired)
                return View("~/Views/Team/AcceptInvite.cshtml", new AcceptInviteViewModel { Error = "This invitation has expired. Please ask the project owner to send a new one." });

            var project = await _projectRepository.GetProjectByIdAsync(invitation.ProjectId);
            if (project == null)
                return View("~/Views/Team/AcceptInvite.cshtml", new AcceptInviteViewModel { Error = "Project not found." });

            return View("~/Views/Team/AcceptInvite.cshtml", new AcceptInviteViewModel
            {
                Token = token,
                ProjectName = project.Name,
                ProjectSlug = project.Slug,
                Role = invitation.Role.ToString(),
                InvitedEmail = invitation.Email
            });
        }

        // POST /team/invite/accept
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptPost([FromForm] string token)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null)
            {
                var encodedReturn = Uri.EscapeDataString($"/team/invite/accept?token={token}");
                return Redirect($"/login?returnUrl={encodedReturn}");
            }

            var invitation = await _memberRepository.GetInvitationByTokenAsync(token);
            if (invitation == null || invitation.IsAccepted || invitation.IsExpired)
            {
                TempData["Error"] = "Invalid or expired invitation.";
                return Redirect("/");
            }

            var project = await _projectRepository.GetProjectByIdAsync(invitation.ProjectId);
            if (project == null)
            {
                TempData["Error"] = "Project not found.";
                return Redirect("/");
            }

            // Add to project_members
            await _memberRepository.AddMemberAsync(invitation.ProjectId, user.Id, invitation.Role);

            // Mark invitation accepted
            await _memberRepository.AcceptInvitationAsync(invitation.Id, DateTime.UtcNow);

            return Redirect($"/{project.Slug}/monitors");
        }
    }

    public class AcceptInviteViewModel
    {
        public string? Token { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectSlug { get; set; }
        public string? Role { get; set; }
        public string? InvitedEmail { get; set; }
        public string? Error { get; set; }
    }
}
