using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for email service settings
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "Email";

        /// <summary>
        /// SMTP server host (e.g., smtp.gmail.com)
        /// </summary>
        [Required(ErrorMessage = "SmtpHost is required")]
        public string SmtpHost { get; set; } = string.Empty;

        /// <summary>
        /// SMTP server port (typically 587 for TLS)
        /// </summary>
        [Range(1, 65535, ErrorMessage = "SmtpPort must be between 1 and 65535")]
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Email address to send from
        /// </summary>
        [Required(ErrorMessage = "FromEmail is required")]
        [EmailAddress(ErrorMessage = "FromEmail must be a valid email address")]
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Password or app-specific password for the email account
        /// </summary>
        [Required(ErrorMessage = "FromPassword is required")]
        public string FromPassword { get; set; } = string.Empty;
    }
}
