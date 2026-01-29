using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using NCrontab;
using CronMonitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Data.Repository
{
    public class MonitorRepository : BaseRepository, IMonitorRepository
    {
        public MonitorRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<CronMonitor?> GetMonitorByIdAsync(Guid id)
        {
            const string sql = "SELECT * FROM monitors WHERE id = @Id";
            return await QueryFirstOrDefaultAsync<CronMonitor>(sql, new { Id = id });
        }

        public async Task<CronMonitor?> GetMonitorBySlugAsync(string slug)
        {
            const string sql = "SELECT * FROM monitors WHERE slug = @Slug";
            return await QueryFirstOrDefaultAsync<CronMonitor>(sql, new { Slug = slug });
        }

        public async Task<IEnumerable<CronMonitor>> GetMonitorsByProjectIdAsync(Guid projectId)
        {
            const string sql = "SELECT * FROM monitors WHERE project_id = @ProjectId ORDER BY created_at DESC";
            return await QueryAsync<CronMonitor>(sql, new { ProjectId = projectId });
        }

        public async Task<Guid> CreateMonitorAsync(CronMonitor monitor)
        {
            const string sql = @"
                INSERT INTO monitors (
                    project_id, name, slug, schedule_type, 
                    period_seconds, cron_expression, cron_timezone, 
                    calendar_expression, calendar_timezone, grace_seconds,
                    next_expected_at
                ) 
                VALUES (
                    @ProjectId, @Name, @Slug, @ScheduleType, 
                    @PeriodSeconds, @CronExpression, @CronTimezone, 
                    @CalendarExpression, @CalendarTimezone, @GraceSeconds,
                    @NextExpectedAt
                ) 
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, monitor);
        }

        public async Task<bool> SlugExistsAsync(Guid projectId, string slug)
        {
            const string sql = "SELECT COUNT(1) FROM monitors WHERE project_id = @ProjectId AND slug = @Slug";
            return await ExecuteScalarAsync<int>(sql, new { ProjectId = projectId, Slug = slug }) > 0;
        }

        public async Task<IEnumerable<MonitorPing>> GetPingsByMonitorIdAsync(Guid monitorId, int limit = 50)
        {
            const string sql = @"
                SELECT 
                    id, monitor_id, received_at, status, message,
                    ip_address, user_agent, http_method, request_headers, response_time_ms
                FROM monitor_pings 
                WHERE monitor_id = @MonitorId 
                ORDER BY received_at DESC 
                LIMIT @Limit";
            return await QueryAsync<MonitorPing>(sql, new { MonitorId = monitorId, Limit = limit });
        }

        public async Task<bool> RecordPingAsync(MonitorPing pingData)
        {
            var monitor = await GetMonitorByIdAsync(pingData.MonitorId);
            if (monitor == null) return false;

            var nextExpectedAt = CalculateNextExpectedAt(monitor);
            var now = DateTime.UtcNow;

            const string sql = @"
                UPDATE monitors 
                SET last_ping_at = @LastPingAt, 
                    last_status = @LastStatus, 
                    next_expected_at = @NextExpectedAt,
                    updated_at = @UpdatedAt
                WHERE id = @Id;

                INSERT INTO monitor_pings (
                    monitor_id, received_at, status, message,
                    ip_address, user_agent, http_method, request_headers, response_time_ms
                )
                VALUES (
                    @MonitorId, @ReceivedAt, @Status, @Message,
                    CAST(@IpAddress AS inet), @UserAgent, @HttpMethod, CAST(@RequestHeaders AS jsonb), @ResponseTimeMs
                );";

            var rowsAffected = await ExecuteAsync(sql, new
            {
                Id = pingData.MonitorId,
                LastPingAt = now,
                LastStatus = (int)MonitorStatus.Up,
                NextExpectedAt = nextExpectedAt,
                UpdatedAt = now,
                // Ping Data
                MonitorId = pingData.MonitorId,
                ReceivedAt = now,
                Status = (int)pingData.Status,
                Message = pingData.Message,
                IpAddress = pingData.IpAddress, // Dapper handles IPAddress/string for inet if string is valid IP
                UserAgent = pingData.UserAgent,
                HttpMethod = pingData.HttpMethod,
                RequestHeaders = pingData.RequestHeaders,
                ResponseTimeMs = pingData.ResponseTimeMs
            });

            return rowsAffected > 0;
        }

        private DateTime? CalculateNextExpectedAt(CronMonitor monitor)
        {
            var utcNow = DateTime.UtcNow;

            if (monitor.ScheduleType == ScheduleType.Interval && monitor.PeriodSeconds.HasValue)
            {
                return utcNow.AddSeconds(monitor.PeriodSeconds.Value);
            }

            if (monitor.ScheduleType == ScheduleType.Cron && !string.IsNullOrWhiteSpace(monitor.CronExpression))
            {
                try
                {
                    // NCrontab parses 5-part cron expressions (minute hour day month day-of-week)
                    // It can also handle 6-part including seconds if configured, but standard unix cron is 5
                    var schedule = CrontabSchedule.Parse(monitor.CronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
                    return schedule.GetNextOccurrence(utcNow);
                }
                catch
                {
                    // Invalid cron expression, fallback or log
                    return null;
                }
            }

            return null;
        }

        public async Task<bool> UpdateMonitorAsync(CronMonitor monitor)
        {
            const string sql = @"
                UPDATE monitors 
                SET name = @Name, 
                    schedule_type = @ScheduleType, 
                    period_seconds = @PeriodSeconds, 
                    cron_expression = @CronExpression, 
                    cron_timezone = @CronTimezone, 
                    grace_seconds = @GraceSeconds,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            var rows = await ExecuteAsync(sql, monitor);
            return rows > 0;
        }
        public async Task<bool> DeleteMonitorAsync(Guid id)
        {
            const string sql = "DELETE FROM monitors WHERE id = @Id";
            var rows = await ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<bool> UpdateStatusAsync(Guid id, MonitorStatus status)
        {
            const string sql = "UPDATE monitors SET last_status = @Status, updated_at = CURRENT_TIMESTAMP WHERE id = @Id";
            var rows = await ExecuteAsync(sql, new { Id = id, Status = (int)status });
            return rows > 0;
        }
    }
}
