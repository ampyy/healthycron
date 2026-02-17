using System.Net;
using StackExchange.Redis;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Utilities
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConnectionMultiplexer _redis;
        private readonly IAxiomLogger _logger;
        private const int MaxRequests = 100;
        private const int WindowSeconds = 60;

        public RateLimitingMiddleware(RequestDelegate next, IConnectionMultiplexer redis, IAxiomLogger logger)
        {
            _next = next;
            _redis = redis;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "unknown";
            }

            var key = $"ratelimit:api:{ipAddress}";
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var uniqueId = Guid.NewGuid().ToString();

            var script = @"
                local key = KEYS[1]
                local limit = tonumber(ARGV[1])
                local now = tonumber(ARGV[2])
                local window = tonumber(ARGV[3])
                local unique_id = ARGV[4]

                redis.call('ZREMRANGEBYSCORE', key, 0, now - window)
                local count = redis.call('ZCARD', key)

                if count < limit then
                    redis.call('ZADD', key, now, unique_id)
                    redis.call('EXPIRE', key, window)
                    return 1
                else
                    return 0
                end
            ";

            var result = (int)await db.ScriptEvaluateAsync(script, 
                new RedisKey[] { key }, 
                new RedisValue[] { MaxRequests, now, WindowSeconds, uniqueId });

            if (result == 0)
            {
                await _logger.LogWarn($"Rate limit exceeded for IP: {ipAddress}");
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = WindowSeconds.ToString();
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            await _next(context);
        }
    }
}
