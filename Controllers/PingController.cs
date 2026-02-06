using HealthyCron.Data.Interfaces;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyCron.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AxiomLogger _axiomLogger;

        public PingController(IServiceProvider serviceProvider, AxiomLogger axiomLogger)
        {
            _serviceProvider = serviceProvider;
            _axiomLogger = axiomLogger;
        }

        [AcceptVerbs("GET", "POST", "PUT", "HEAD")]
        [Route("{id:guid}/{status?}")]
        public async Task<IActionResult> PingById(Guid id, string? status = "success")
        {
            try
            {
                var metadata = await CaptureMetadata();
                var body = await ReadBody();
                var headerStatus = HttpContext.Request.Headers["X-Job-Status"].FirstOrDefault();

                // Log ping received
                await _axiomLogger.LogInfo("Ping received by ID", new Dictionary<string, object>
                {
                    ["monitor_id"] = id,
                    ["status"] = status ?? "success",
                    ["ip_address"] = metadata.IpAddress ?? "unknown",
                    ["user_agent"] = metadata.UserAgent ?? "unknown",
                    ["method"] = metadata.Method
                });

                // Background processing to not block the caller
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var pingService = scope.ServiceProvider.GetRequiredService<IPingService>();
                        await pingService.ProcessPingAsync(id, status ?? "success", headerStatus, body, metadata);
                    }
                    catch (Exception ex)
                    {
                        await _axiomLogger.LogError("Failed to process ping by ID", new Dictionary<string, object>
                        {
                            ["monitor_id"] = id,
                            ["error"] = ex.Message,
                            ["stack_trace"] = ex.StackTrace ?? ""
                        });
                    }
                });

                return Ok(new { message = "Ping received", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Error in PingById endpoint", new Dictionary<string, object>
                {
                    ["monitor_id"] = id,
                    ["error"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace ?? ""
                });
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [AcceptVerbs("GET", "POST", "PUT", "HEAD")]
        [Route("{pingKey}/{slug}/{status?}")]
        public async Task<IActionResult> PingBySlug(string pingKey, string slug, string? status = "success")
        {
            try
            {
                var metadata = await CaptureMetadata();
                var body = await ReadBody();
                var headerStatus = HttpContext.Request.Headers["X-Job-Status"].FirstOrDefault();

                // Log ping received
                await _axiomLogger.LogInfo("Ping received by slug", new Dictionary<string, object>
                {
                    ["ping_key"] = pingKey,
                    ["slug"] = slug,
                    ["status"] = status ?? "success",
                    ["ip_address"] = metadata.IpAddress ?? "unknown",
                    ["user_agent"] = metadata.UserAgent ?? "unknown",
                    ["method"] = metadata.Method
                });

                // Background processing
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var pingService = scope.ServiceProvider.GetRequiredService<IPingService>();
                        await pingService.ProcessPingBySlugAsync(pingKey, slug, status ?? "success", headerStatus, body, metadata);
                    }
                    catch (Exception ex)
                    {
                        await _axiomLogger.LogError("Failed to process ping by slug", new Dictionary<string, object>
                        {
                            ["ping_key"] = pingKey,
                            ["slug"] = slug,
                            ["error"] = ex.Message,
                            ["stack_trace"] = ex.StackTrace ?? ""
                        });
                    }
                });

                return Ok(new { message = "Ping received", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Error in PingBySlug endpoint", new Dictionary<string, object>
                {
                    ["ping_key"] = pingKey,
                    ["slug"] = slug,
                    ["error"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace ?? ""
                });
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private async Task<PingMetadata> CaptureMetadata()
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();
            var method = HttpContext.Request.Method;

            var headers = HttpContext.Request.Headers
                .Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            return new PingMetadata
            {
                IpAddress = ip,
                UserAgent = ua,
                Method = method,
                HeadersJson = System.Text.Json.JsonSerializer.Serialize(headers)
            };
        }

        private async Task<string?> ReadBody()
        {
            if (HttpContext.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(HttpContext.Request.Body);
                return await reader.ReadToEndAsync();
            }
            return null;
        }
    }
}
