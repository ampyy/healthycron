using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly AxiomLogger _axiomLogger;
        private const string SessionCookieName = "hc_session";

        public AuthController(IAuthService authService, IEmailService emailService, AxiomLogger axiomLogger)
        {
            _authService = authService;
            _emailService = emailService;
            _axiomLogger = axiomLogger;
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
            try
            {
                var rawToken = await _authService.RequestMagicLinkAsync(request.Email);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var magicLink = $"{baseUrl}/auth/magic?token={rawToken}";

                await _emailService.SendMagicLinkEmailAsync(request.Email, magicLink);

                await _axiomLogger.LogInfo("Magic link requested", new Dictionary<string, object>
                {
                    ["email"] = request.Email
                });

                return Ok(new { message = "Magic link sent to your email", link = magicLink });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Failed to request magic link", new Dictionary<string, object>
                {
                    ["email"] = request.Email,
                    ["error"] = ex.Message
                });
                return StatusCode(500, new { error = "Failed to send magic link" });
            }
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
            try
            {
                var sessionToken = await _authService.LoginPasswordAsync(request.Email, request.Password);
                if (sessionToken == null)
                {
                    await _axiomLogger.LogWarn("Failed login attempt", new Dictionary<string, object>
                    {
                        ["email"] = request.Email
                    });
                    return Unauthorized("Invalid email or password.");
                }

                SetSessionCookie(sessionToken);
                await _axiomLogger.LogInfo("User logged in", new Dictionary<string, object>
                {
                    ["email"] = request.Email
                });
                return Ok(new { message = "Logged in successfully" });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Error during password login", new Dictionary<string, object>
                {
                    ["email"] = request.Email,
                    ["error"] = ex.Message
                });
                return StatusCode(500, new { error = "Login failed" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                if (Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken))
                {
                    await _authService.LogoutAsync(sessionToken);
                    Response.Cookies.Delete(SessionCookieName);
                    await _axiomLogger.LogInfo("User logged out", new Dictionary<string, object>());
                }
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Error during logout", new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
                return StatusCode(500, new { error = "Logout failed" });
            }
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
