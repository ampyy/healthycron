using HealthyCron.Models;

namespace HealthyCron.Models.ViewModels
{
    public class IntegrationListItemViewModel
    {
        public Integration Integration { get; set; } = null!;
        public SlackIntegration? SlackDetails { get; set; }
        public TeamsIntegration? TeamsDetails { get; set; }
        public GoogleChatIntegration? GoogleChatDetails { get; set; }
        public DiscordIntegration? DiscordDetails { get; set; }
        public EmailIntegration? EmailDetails { get; set; }
        public PagerDutyIntegration? PagerDutyDetails { get; set; }
        public TelegramIntegration? TelegramDetails { get; set; }
        public PushoverIntegration? PushoverDetails { get; set; }
        public SpikeIntegration? SpikeDetails { get; set; }
        public bool IsEnabledForMonitor { get; set; }
    }
}
