using HealthyCron.Logic.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace HealthyCron.Utilities
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAccessKeyService accessKeyService)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader != null && authHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = authHeader.Substring("Bearer ".Length).Trim();
                var key = await accessKeyService.ValidateKeyAsync(apiKey);

                if (key != null)
                {
                    context.Items["AccessKey"] = key;
                    context.Items["ProjectId"] = key.ProjectId;
                }
            }

            await _next(context);
        }
    }
}
