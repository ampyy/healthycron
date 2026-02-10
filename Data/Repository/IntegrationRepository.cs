using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Enums;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Data.Repository
{
    public class IntegrationRepository : BaseRepository, IIntegrationRepository
    {
        public IntegrationRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<IEnumerable<Integration>> GetIntegrationsByProjectIdAsync(Guid projectId)
        {
            const string sql = @"
                SELECT id, project_id, type, name, is_active, is_deleted, created_at 
                FROM integrations 
                WHERE project_id = @ProjectId AND is_deleted = false
                ORDER BY created_at DESC";

            return await QueryAsync<Integration>(sql, new { ProjectId = projectId });
        }

        public async Task<Integration?> GetIntegrationByIdAsync(Guid id)
        {
            const string sql = @"
                SELECT id, project_id, type, name, is_active, is_deleted, created_at 
                FROM integrations 
                WHERE id = @Id AND is_deleted = false";

            return await QueryFirstOrDefaultAsync<Integration>(sql, new { Id = id });
        }

        public async Task<Guid> CreateIntegrationAsync(Integration integration)
        {
            if (integration.Id == Guid.Empty)
            {
                integration.Id = Guid.NewGuid();
            }

            const string sql = @"
                INSERT INTO integrations (id, project_id, type, name, is_active)
                VALUES (@Id, @ProjectId, @Type, @Name, @IsActive)
                RETURNING id";

            return await ExecuteScalarAsync<Guid>(sql, integration);
        }

        public async Task<bool> UpdateIntegrationStatusAsync(Guid id, bool isActive)
        {
            const string sql = @"
                UPDATE integrations 
                SET is_active = @IsActive 
                WHERE id = @Id";

            var rows = await ExecuteAsync(sql, new { Id = id, IsActive = isActive });
            return rows > 0;
        }

        public async Task<bool> DeleteIntegrationAsync(Guid id)
        {
            const string sql = @"
                UPDATE integrations 
                SET is_deleted = true, is_active = false
                WHERE id = @Id";

            var rows = await ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task CreateSlackIntegrationAsync(SlackIntegration slackIntegration)
        {
            const string sql = @"
                INSERT INTO slack_integrations (
                    integration_id, workspace_id, channel_id, channel_name, 
                    encrypted_bot_token, workspace_name, app_id, webhook_url
                )
                VALUES (
                    @IntegrationId, @WorkspaceId, @ChannelId, @ChannelName, 
                    @EncryptedBotToken, @WorkspaceName, @AppId, @WebhookUrl
                )";

            await ExecuteAsync(sql, slackIntegration);
        }

        public async Task<SlackIntegration?> GetSlackIntegrationByIntegrationIdAsync(Guid integrationId)
        {
            const string sql = @"
                SELECT integration_id, workspace_id, channel_id, channel_name, 
                       encrypted_bot_token, workspace_name, app_id, webhook_url, created_at
                FROM slack_integrations 
                WHERE integration_id = @IntegrationId";

            return await QueryFirstOrDefaultAsync<SlackIntegration>(sql, new { IntegrationId = integrationId });
        }

        public async Task<IEnumerable<HealthyCron.Models.ViewModels.IntegrationListItemViewModel>> GetMonitorIntegrationsAsync(Guid monitorId)
        {
            const string sql = @"
                SELECT i.id, i.project_id, i.type, i.name, i.is_active, i.created_at, mi.is_enabled
                FROM integrations i
                INNER JOIN monitor_integrations mi ON i.id = mi.integration_id
                WHERE mi.monitor_id = @MonitorId AND i.is_deleted = false
                ORDER BY i.created_at DESC";

            using var connection = _connectionFactory.CreateConnection();
            var results = await connection.QueryAsync<dynamic>(sql, new { MonitorId = monitorId });
            
            return results.Select(r => new HealthyCron.Models.ViewModels.IntegrationListItemViewModel {
                Integration = new Integration {
                    Id = r.id,
                    ProjectId = r.project_id,
                    Type = (IntegrationType)r.type,
                    Name = r.name,
                    IsActive = r.is_active,
                    CreatedAt = r.created_at
                },
                IsEnabledForMonitor = r.is_enabled
            });
        }

        public async Task<bool> AddMonitorIntegrationAsync(Guid monitorId, Guid integrationId)
        {
            const string sql = @"
                INSERT INTO monitor_integrations (monitor_id, integration_id)
                VALUES (@MonitorId, @IntegrationId)
                ON CONFLICT (monitor_id, integration_id) DO NOTHING";

            var rows = await ExecuteAsync(sql, new { MonitorId = monitorId, IntegrationId = integrationId });
            return rows > 0;
        }

        public async Task<bool> RemoveMonitorIntegrationAsync(Guid monitorId, Guid integrationId)
        {
            const string sql = @"
                DELETE FROM monitor_integrations 
                WHERE monitor_id = @MonitorId AND integration_id = @IntegrationId";

            var rows = await ExecuteAsync(sql, new { MonitorId = monitorId, IntegrationId = integrationId });
            return rows > 0;
        }

        public async Task<IEnumerable<Guid>> GetMappedMonitorIdsAsync(Guid integrationId)
        {
            const string sql = "SELECT monitor_id FROM monitor_integrations WHERE integration_id = @IntegrationId";
            return await QueryAsync<Guid>(sql, new { IntegrationId = integrationId });
        }

        public async Task<bool> SyncMonitorIntegrationsAsync(Guid integrationId, List<Guid> monitorIds)
        {
            const string deleteSql = "DELETE FROM monitor_integrations WHERE integration_id = @IntegrationId";
            const string insertSql = "INSERT INTO monitor_integrations (monitor_id, integration_id) VALUES (@MonitorId, @IntegrationId)";

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(deleteSql, new { IntegrationId = integrationId }, transaction);

                foreach (var monitorId in monitorIds)
                {
                    await connection.ExecuteAsync(insertSql, new { MonitorId = monitorId, IntegrationId = integrationId }, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateMonitorIntegrationStatusAsync(Guid monitorId, Guid integrationId, bool isEnabled)
        {
            const string sql = @"
                UPDATE monitor_integrations 
                SET is_enabled = @IsEnabled 
                WHERE monitor_id = @MonitorId AND integration_id = @IntegrationId";

            var rows = await ExecuteAsync(sql, new { MonitorId = monitorId, IntegrationId = integrationId, IsEnabled = isEnabled });
            return rows > 0;
        }

        public async Task<Guid> CreateNotificationJobAsync(int monitorPingId, Guid integrationId, AlertType alertType)
        {
            var id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO notification_jobs (id, monitor_ping_id, integration_id, alert_type, status)
                VALUES (@Id, @MonitorPingId, @IntegrationId, @AlertType, 0)
                RETURNING id";

            return await ExecuteScalarAsync<Guid>(sql, new
            {
                Id = id,
                MonitorPingId = monitorPingId,
                IntegrationId = integrationId,
                AlertType = (short)alertType
            });
        }
    }
}
