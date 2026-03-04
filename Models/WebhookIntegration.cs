namespace HealthyCron.Models
{
    public class WebhookIntegration
    {
        public Guid IntegrationId { get; set; }

        // DOWN config (optional if UP is set)
        public string? DownMethod { get; set; } = "POST";
        public string? DownUrl { get; set; }
        public string? DownHeaders { get; set; }
        public string? DownBody { get; set; }

        // UP config (nullable = don't call on recovery)
        public string? UpMethod { get; set; }
        public string? UpUrl { get; set; }
        public string? UpHeaders { get; set; }
        public string? UpBody { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
