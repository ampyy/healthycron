using System.Text.Json;
using StackExchange.Redis;

namespace HealthyCron.Utilities.Interface
{
    /// <summary>
    /// Generic Cache Service for managing Redis keys.
    /// Provides standard Get, Set, and Remove operations for any data type.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Retrieves a cached value by key.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Sets a value in the cache with an optional expiration time.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a value from the cache by key.
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        Task<bool> ExistsAsync(string key);
    }
}
