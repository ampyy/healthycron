namespace HealthyCron.Models
{
    public class TelegramIntegration
    {
        public Guid IntegrationId { get; set; }
        public string ChatId { get; set; } = string.Empty;
        public string? ChatName { get; set; }
        public string? ChatType { get; set; }
        public string? SetupToken { get; set; }
        public DateTime? SetupTokenExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
