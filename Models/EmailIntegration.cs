namespace HealthyCron.Models
{
    public class EmailIntegration
    {
        public Guid IntegrationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
