namespace HealthyCron.Models
{
    public class SlackIntegration
    {
        public Guid IntegrationId { get; set; }
        public string WorkspaceId { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string EncryptedBotToken { get; set; } = string.Empty;
        public string WorkspaceName { get; set; } = string.Empty;
        public string? AppId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
