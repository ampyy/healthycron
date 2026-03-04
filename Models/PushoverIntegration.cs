namespace HealthyCron.Models
{
    public class PushoverIntegration
    {
        public Guid IntegrationId { get; set; }
        public string SubscriptionKey { get; set; } = string.Empty;
        public string? Device { get; set; }
        public string? Sound { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
