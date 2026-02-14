namespace HealthyCron.Models
{
    public class PagerDutyIntegration
    {
        public Guid IntegrationId { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string? ServiceId { get; set; }
        public string AccessToken { get; set; } = string.Empty;  // Encrypted
        public string RefreshToken { get; set; } = string.Empty; // Encrypted
        public DateTime TokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
