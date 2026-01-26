using System.Text.Json;
using HealthyCron.Utilities.Interface;
using StackExchange.Redis;

namespace HealthyCron.Utilities.Service
{
    /// <summary>
    /// Generic implementation of ICacheService using Redis.
    /// Handles serialization and deserialization of objects automatically.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public CacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value);
            if (expiry.HasValue)
            {
                await _db.StringSetAsync(key, json, expiry.Value);
            }
            else
            {
                await _db.StringSetAsync(key, json);
            }
        }

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }
    }
}
