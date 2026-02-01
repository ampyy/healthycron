using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for database connection settings
    /// </summary>
    public class DatabaseSettings
    {
        public const string SectionName = "ConnectionStrings";

        /// <summary>
        /// PostgreSQL connection string
        /// </summary>
        [Required(ErrorMessage = "DefaultConnection is required")]
        public string DefaultConnection { get; set; } = string.Empty;
    }
}
