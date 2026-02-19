using HealthyCron.Data.Interfaces;
using HealthyCron.Filters;
using HealthyCron.Models;
using HealthyCron.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthRepository authRepository, ILogger<AccountController> logger)
        {
            _authRepository = authRepository;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            // Re-fetch from DB to get latest values
            var freshUser = await _authRepository.GetUserByIdAsync(user.Id);
            if (freshUser == null) return Redirect("/login");

            ViewBag.IanaTimezones = TimezoneHelper.GetAllIanaTimezones();
            return View(freshUser);
        }

        [HttpPost("update-timezone")]
        public async Task<IActionResult> UpdateTimezone([FromForm] string timezone)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var freshUser = await _authRepository.GetUserByIdAsync(user.Id);
            if (freshUser == null) return Redirect("/login");

            if (!TimezoneHelper.IsValidIana(timezone))
            {
                TempData["ErrorMessage"] = $"Invalid timezone: '{timezone}'. Please select a valid IANA timezone.";
                return Redirect("/account");
            }

            freshUser.Timezone = timezone;
            await _authRepository.UpdateUserAsync(freshUser);

            TempData["SuccessMessage"] = $"Timezone updated to {timezone}.";
            return Redirect("/account");
        }

        [HttpPost("update-reports")]
        public async Task<IActionResult> UpdateReports(
            [FromForm] bool receiveWeeklyReports,
            [FromForm] bool receiveMonthlyReports,
            [FromForm] bool receiveIncidentReminders)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");

            var freshUser = await _authRepository.GetUserByIdAsync(user.Id);
            if (freshUser == null) return Redirect("/login");

            freshUser.ReceiveWeeklyReports = receiveWeeklyReports;
            freshUser.ReceiveMonthlyReports = receiveMonthlyReports;
            freshUser.ReceiveIncidentReminders = receiveIncidentReminders;

            await _authRepository.UpdateUserAsync(freshUser);

            TempData["SuccessMessage"] = "Report preferences updated.";
            return Redirect("/account");
        }

        [HttpPost("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            await Task.CompletedTask; // Placeholder
            return Redirect("/account");
        }
    }
}
