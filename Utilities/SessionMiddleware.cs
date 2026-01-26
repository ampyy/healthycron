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
