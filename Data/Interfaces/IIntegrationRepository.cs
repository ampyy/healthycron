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

        // Slack Integration
        Task CreateSlackIntegrationAsync(SlackIntegration slackIntegration);
        Task<SlackIntegration?> GetSlackIntegrationByIntegrationIdAsync(Guid integrationId);

        // Monitor Integration Mapping
        Task<IEnumerable<Integration>> GetMonitorIntegrationsAsync(Guid monitorId);
        Task<bool> AddMonitorIntegrationAsync(Guid monitorId, Guid integrationId);
        Task<bool> RemoveMonitorIntegrationAsync(Guid monitorId, Guid integrationId);

        // Notification Jobs
        Task<Guid> CreateNotificationJobAsync(int monitorPingId, Guid integrationId, AlertType alertType);
    }
}
