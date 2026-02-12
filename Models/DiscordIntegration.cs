namespace HealthyCron.Models
{
    public class DiscordIntegration
    {
        public Guid IntegrationId { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
        public string? ChannelName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
