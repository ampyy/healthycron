namespace HealthyCron.Models
{
    public class SlackOAuthResponse
    {
        public bool Ok { get; set; }
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
        public string? Scope { get; set; }
        public string? BotUserId { get; set; }
        public string? AppId { get; set; }
        public SlackTeam? Team { get; set; }
        public SlackIncomingWebhook? IncomingWebhook { get; set; }
        public string? Error { get; set; }
    }

    public class SlackTeam
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class SlackIncomingWebhook
    {
        public string? Channel { get; set; }
        public string? ChannelId { get; set; }
        public string? ConfigurationUrl { get; set; }
        public string? Url { get; set; }
    }
}
