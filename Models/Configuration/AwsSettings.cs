using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for AWS credentials and settings
    /// </summary>
    public class AwsSettings
    {
        public const string SectionName = "AWS";

        /// <summary>
        /// AWS profile name (optional, for local development)
        /// </summary>
        public string? Profile { get; set; }

        /// <summary>
        /// AWS region (e.g., ap-south-1, us-east-1)
        /// </summary>
        [Required(ErrorMessage = "Region is required")]
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// AWS Access Key ID
        /// </summary>
        [Required(ErrorMessage = "AccessKey is required")]
        public string AccessKey { get; set; } = string.Empty;

        /// <summary>
        /// AWS Secret Access Key
        /// </summary>
        [Required(ErrorMessage = "SecretKey is required")]
        public string SecretKey { get; set; } = string.Empty;
    }
}
