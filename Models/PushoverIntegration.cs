namespace HealthyCron.Models
{
    public class PushoverIntegration
    {
        public Guid IntegrationId { get; set; }
        public string UserKey { get; set; } = string.Empty;
        public string? Device { get; set; }
        public short Priority { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
