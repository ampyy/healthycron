using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;

namespace HealthyCron.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var accessKeyService = context.HttpContext.RequestServices.GetRequiredService<IAccessKeyService>();

            // Try X-Api-Key header first, then query param
            string? apiKey = context.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = context.HttpContext.Request.Query["api_key"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new JsonResult(new { error = "API key required. Pass via X-Api-Key header or api_key query parameter." })
                {
                    StatusCode = 401
                };
                return;
            }

            var key = await accessKeyService.ValidateKeyAsync(apiKey);
            if (key == null)
            {
                context.Result = new JsonResult(new { error = "Invalid or revoked API key." })
                {
                    StatusCode = 401
                };
                return;
            }

            // Reject Ping keys — those are for /ping/ endpoints only
            if (key.KeyType == ApiKeyType.Ping)
            {
                context.Result = new JsonResult(new { error = "Ping keys cannot access the management API. Use a Full Access or Read-only key." })
                {
                    StatusCode = 403
                };
                return;
            }

            // Store the validated key for downstream use
            context.HttpContext.Items["ApiKey"] = key;
        }
    }
}
