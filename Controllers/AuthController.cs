using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private const string SessionCookieName = "hc_session";

        public AuthController(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [HttpGet("/signup")]
        public IActionResult Signup()
        {
            // Redirect if already logged in
            var user = HttpContext.Items["User"] as HealthyCron.Models.User;
            if (user != null) return Redirect("/dashboard");

            return View();
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            // Redirect if already logged in
            var user = HttpContext.Items["User"] as HealthyCron.Models.User;
            if (user != null) return Redirect("/dashboard");

            return View();
        }

        [HttpPost("request-magic-link")]
        public async Task<IActionResult> RequestMagicLink([FromBody] MagicLinkRequest request)
        {
            var rawToken = await _authService.RequestMagicLinkAsync(request.Email);
            var magicLink = $"http://localhost:5032/auth/magic?token={rawToken}";

            // Send email with magic link
            await _emailService.SendMagicLinkEmailAsync(request.Email, magicLink);

            return Ok(new { message = "Magic link sent to your email", link = magicLink });
        }



        [HttpGet("magic")]
        public async Task<IActionResult> VerifyMagicLink([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Invalid magic link - no token provided.";
                return View("MagicLinkError");
            }

            var sessionToken = await _authService.VerifyMagicTokenAsync(token);
            if (sessionToken == null)
            {
                ViewBag.Error = "Invalid or expired magic link.";
                return View("MagicLinkError");
            }

            SetSessionCookie(sessionToken);
            return Redirect("/dashboard");
        }

        [HttpPost("magic-login")]

        public async Task<IActionResult> MagicLogin([FromBody] TokenRequest request)
        {
            var sessionToken = await _authService.VerifyMagicTokenAsync(request.Token);
            if (sessionToken == null) return Unauthorized("Invalid or expired magic link.");

            SetSessionCookie(sessionToken);
            return Ok(new { message = "Logged in successfully via magic link" });
        }

        [HttpPost("register-password")]
        public async Task<IActionResult> RegisterPassword([FromBody] PasswordAuthRequest request)
        {
            var success = await _authService.RegisterPasswordAsync(request.Email, request.Password);
            if (!success) return BadRequest("User already exists with a password.");

            return Ok(new { message = "Password registered successfully" });
        }

        [HttpPost("login-password")]
        public async Task<IActionResult> LoginPassword([FromBody] PasswordAuthRequest request)
        {
            var sessionToken = await _authService.LoginPasswordAsync(request.Email, request.Password);
            if (sessionToken == null) return Unauthorized("Invalid email or password.");

            SetSessionCookie(sessionToken);
            return Ok(new { message = "Logged in successfully" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken))
            {
                await _authService.LogoutAsync(sessionToken);
                Response.Cookies.Delete(SessionCookieName);
            }
            return Ok(new { message = "Logged out successfully" });
        }

        private void SetSessionCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30)
            };
            Response.Cookies.Append(SessionCookieName, token, cookieOptions);
        }
    }
}
