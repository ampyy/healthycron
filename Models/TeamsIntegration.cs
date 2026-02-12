namespace HealthyCron.Models
{
    public class TeamsIntegration
    {
        public Guid IntegrationId { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
