namespace HealthyCron.Models
{
    public class GoogleChatIntegration
    {
        public Guid IntegrationId { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
        public string? SpaceName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
