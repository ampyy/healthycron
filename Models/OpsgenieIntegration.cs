namespace HealthyCron.Models
{
    public class OpsgenieIntegration
    {
        public Guid IntegrationId { get; set; }
        public string ApiKey { get; set; } = string.Empty;  // Encrypted
        public string Region { get; set; } = "us";  // us or eu
        public string? TeamName { get; set; }
        public string Priority { get; set; } = "P1";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
