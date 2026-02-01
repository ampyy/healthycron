using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for Redis cache settings
    /// </summary>
    public class RedisSettings
    {
        public const string SectionName = "Redis";

        /// <summary>
        /// Redis connection string (supports rediss:// for SSL/TLS)
        /// </summary>
        [Required(ErrorMessage = "ConnectionString is required")]
        public string ConnectionString { get; set; } = string.Empty;
    }
}
