using HealthyCron.Logic.Interfaces;

namespace HealthyCron.Utilities
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private const string SessionCookieName = "hc_session";

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            if (context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken))
            {
                var user = await authService.GetUserFromSessionAsync(sessionToken);
                if (user != null)
                {
                    context.Items["User"] = user;
                    // Store timezone for use in views (defaults to UTC)
                    var tz = !string.IsNullOrWhiteSpace(user.Timezone) ? user.Timezone : "UTC";
                    context.Items["UserTimezone"] = tz;

                    // Create ClaimsPrincipal for [Authorize] attribute
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(System.Security.Claims.ClaimTypes.Email, user.Email),
                        new(System.Security.Claims.ClaimTypes.Name, user.Email)
                    };
                    var identity = new System.Security.Claims.ClaimsIdentity(claims, "CustomSession");
                    context.User = new System.Security.Claims.ClaimsPrincipal(identity);
                }
            }

            await _next(context);
        }
    }

    public static class SessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionMiddleware>();
        }
    }
}
