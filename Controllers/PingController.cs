using HealthyCron.Data.Interfaces;
using HealthyCron.Logic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyCron.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : Controller
    {
        private readonly IServiceProvider _serviceProvider;

        public PingController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [AcceptVerbs("GET", "POST", "PUT", "HEAD")]
        [Route("{id:guid}/{status?}")]
        public async Task<IActionResult> PingById(Guid id, string? status = "success")
        {
            var metadata = await CaptureMetadata();
            var body = await ReadBody();
            var headerStatus = HttpContext.Request.Headers["X-Job-Status"].FirstOrDefault();

            // Background processing to not block the caller
            _ = Task.Run(async () => {
                using var scope = _serviceProvider.CreateScope();
                var pingService = scope.ServiceProvider.GetRequiredService<IPingService>();
                await pingService.ProcessPingAsync(id, status ?? "success", headerStatus, body, metadata);
            });

            return Ok(new { message = "Ping received", timestamp = DateTime.UtcNow });
        }

        [AcceptVerbs("GET", "POST", "PUT", "HEAD")]
        [Route("{pingKey}/{slug}/{status?}")]
        public async Task<IActionResult> PingBySlug(string pingKey, string slug, string? status = "success")
        {
            var metadata = await CaptureMetadata();
            var body = await ReadBody();
            var headerStatus = HttpContext.Request.Headers["X-Job-Status"].FirstOrDefault();

            // Background processing
            _ = Task.Run(async () => {
                using var scope = _serviceProvider.CreateScope();
                var pingService = scope.ServiceProvider.GetRequiredService<IPingService>();
                await pingService.ProcessPingBySlugAsync(pingKey, slug, status ?? "success", headerStatus, body, metadata);
            });

            return Ok(new { message = "Ping received", timestamp = DateTime.UtcNow });
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
                HeadersJson = System.Text.Json.JsonSerializer.Serialize(headers),
                ResponseTimeMs = 0 // Initialized, will be updated if needed but we respond quickly
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
