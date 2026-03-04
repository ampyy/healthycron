using HealthyCron.Enums;
using HealthyCron.Models;

namespace HealthyCron.Data.Interfaces
{
    public interface IIntegrationRepository
    {
        // Integration Management
        Task<IEnumerable<Integration>> GetIntegrationsByProjectIdAsync(Guid projectId);
        Task<Integration?> GetIntegrationByIdAsync(Guid id);
        Task<Guid> CreateIntegrationAsync(Integration integration);
        Task<bool> UpdateIntegrationStatusAsync(Guid id, bool isActive);
        Task<bool> DeleteIntegrationAsync(Guid id);

        // Slack Integration
        Task CreateSlackIntegrationAsync(SlackIntegration slackIntegration);
        Task<SlackIntegration?> GetSlackIntegrationByIntegrationIdAsync(Guid integrationId);

        // Teams Integration
        Task CreateTeamsIntegrationAsync(TeamsIntegration teamsIntegration);
        Task<TeamsIntegration?> GetTeamsIntegrationByIntegrationIdAsync(Guid integrationId);

        // Google Chat Integration
        Task CreateGoogleChatIntegrationAsync(GoogleChatIntegration googleChatIntegration);
        Task<GoogleChatIntegration?> GetGoogleChatIntegrationByIntegrationIdAsync(Guid integrationId);

        // Discord Integration
        Task CreateDiscordIntegrationAsync(DiscordIntegration discordIntegration);
        Task<DiscordIntegration?> GetDiscordIntegrationByIntegrationIdAsync(Guid integrationId);

        // Email Integration
        Task CreateEmailIntegrationAsync(EmailIntegration emailIntegration);
        Task<EmailIntegration?> GetEmailIntegrationByIntegrationIdAsync(Guid integrationId);

        // PagerDuty Integration
        Task CreatePagerDutyIntegrationAsync(PagerDutyIntegration pagerDutyIntegration);
        Task<PagerDutyIntegration?> GetPagerDutyIntegrationByIntegrationIdAsync(Guid integrationId);
        Task UpdatePagerDutyTokensAsync(Guid integrationId, string accessToken, string refreshToken, DateTime expiresAt);

        // Opsgenie Integration


        // Telegram Temp Handshakes
        Task CreateTempTelegramHandshakeAsync(TempTelegramHandshake handshake);
        Task<TempTelegramHandshake?> GetTempTelegramHandshakeAsync(string token);
        Task MarkTempTelegramHandshakeUsedAsync(string token);
        Task DeleteTempTelegramHandshakeAsync(string token);

        // Telegram Integration
        Task CreateTelegramIntegrationAsync(TelegramIntegration telegramIntegration);
        Task<TelegramIntegration?> GetTelegramIntegrationByIntegrationIdAsync(Guid integrationId);

        // Pushover Integration
        Task CreatePushoverIntegrationAsync(PushoverIntegration pushoverIntegration);
        Task<PushoverIntegration?> GetPushoverIntegrationByIntegrationIdAsync(Guid integrationId);

        // Pushover Pending Subscriptions
        Task CreatePushoverPendingSubscriptionAsync(PushoverPendingSubscription subscription);
        Task<PushoverPendingSubscription?> GetPushoverPendingSubscriptionAsync(string token);
        Task MarkPushoverPendingSubscriptionUsedAsync(string token);

        // Spike.sh Integration
        Task CreateSpikeIntegrationAsync(SpikeIntegration spikeIntegration);
        Task<SpikeIntegration?> GetSpikeIntegrationByIntegrationIdAsync(Guid integrationId);


        // Monitor Integration Mapping
        Task<IEnumerable<HealthyCron.Models.ViewModels.IntegrationListItemViewModel>> GetMonitorIntegrationsAsync(Guid monitorId);
        Task<IEnumerable<Guid>> GetMappedMonitorIdsAsync(Guid integrationId);
        Task<bool> AddMonitorIntegrationAsync(Guid monitorId, Guid integrationId);
        Task<bool> RemoveMonitorIntegrationAsync(Guid monitorId, Guid integrationId);
        Task<bool> SyncMonitorIntegrationsAsync(Guid integrationId, List<Guid> monitorIds);
        Task<bool> UpdateMonitorIntegrationStatusAsync(Guid monitorId, Guid integrationId, bool isEnabled);

        // Notification Jobs
        Task<Guid> CreateNotificationJobAsync(int monitorPingId, Guid integrationId);
    }
}
