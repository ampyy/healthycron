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
            const string sql = "SELECT * FROM monitors WHERE id = @Id AND is_deleted = FALSE";
            return await QueryFirstOrDefaultAsync<CronMonitor>(sql, new { Id = id });
        }

        public async Task<CronMonitor?> GetMonitorBySlugAsync(string slug, Guid? projectId = null)
        {
            string sql = "SELECT * FROM monitors WHERE slug = @Slug AND is_deleted = FALSE";
            if (projectId.HasValue)
            {
                sql += " AND project_id = @ProjectId";
            }
            return await QueryFirstOrDefaultAsync<CronMonitor>(sql, new { Slug = slug, ProjectId = projectId });
        }

        public async Task<IEnumerable<CronMonitor>> GetMonitorsByProjectIdAsync(Guid projectId)
        {
            const string sql = "SELECT * FROM monitors WHERE project_id = @ProjectId AND is_deleted = FALSE ORDER BY created_at DESC";
            return await QueryAsync<CronMonitor>(sql, new { ProjectId = projectId });
        }

        public async Task<Guid> CreateMonitorAsync(CronMonitor monitor)
        {
            if (monitor.Id == Guid.Empty)
            {
                monitor.Id = Guid.NewGuid();
            }

            const string sql = @"
                INSERT INTO monitors (
                    id, project_id, name, slug, schedule_type, 
                    period_seconds, cron_expression, cron_timezone, 
                    calendar_expression, calendar_timezone, grace_seconds,
                    next_expected_at
                ) 
                VALUES (
                    @Id, @ProjectId, @Name, @Slug, @ScheduleType, 
                    @PeriodSeconds, @CronExpression, @CronTimezone, 
                    @CalendarExpression, @CalendarTimezone, @GraceSeconds,
                    @NextExpectedAt
                ) 
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, monitor);
        }

        public async Task<bool> SlugExistsAsync(Guid projectId, string slug)
        {
            const string sql = "SELECT COUNT(1) FROM monitors WHERE project_id = @ProjectId AND slug = @Slug AND is_deleted = FALSE";
            return await ExecuteScalarAsync<int>(sql, new { ProjectId = projectId, Slug = slug }) > 0;
        }

        public async Task<IEnumerable<MonitorPing>> GetPingsByMonitorIdAsync(Guid monitorId, int limit = 50)
        {
            const string sql = @"
                SELECT 
                    id, monitor_id, received_at, status, message,
                    ip_address, user_agent, http_method, request_headers, duration_ms
                FROM monitor_pings 
                WHERE monitor_id = @MonitorId 
                ORDER BY received_at DESC 
                LIMIT @Limit";
            return await QueryAsync<MonitorPing>(sql, new { MonitorId = monitorId, Limit = limit });
        }

        public async Task<IEnumerable<MonitorPing>> GetPingsByProjectIdAsync(Guid projectId, int limit = 100)
        {
            const string sql = @"
                SELECT 
                    p.id, p.monitor_id, p.received_at, p.status, p.message,
                    p.ip_address, p.user_agent, p.http_method, p.request_headers, p.duration_ms,
                    m.name as MonitorName
                FROM monitor_pings p
                JOIN monitors m ON p.monitor_id = m.id
                WHERE m.project_id = @ProjectId
                ORDER BY p.received_at DESC
                LIMIT @Limit";
            return await QueryAsync<MonitorPing>(sql, new { ProjectId = projectId, Limit = limit });
        }

        public async Task<IEnumerable<MonitorPing>> GetPingsWithFiltersAsync(Guid projectId, Guid? monitorId, int? status, string? search, int limit = 100, int offset = 0)
        {
            var sql = @$"
                SELECT 
                    p.id, p.monitor_id, p.received_at, p.status, p.message,
                    p.ip_address, p.user_agent, p.http_method, p.request_headers, p.duration_ms,
                    m.name as MonitorName
                FROM monitor_pings p
                JOIN monitors m ON p.monitor_id = m.id
                WHERE m.project_id = @ProjectId";

            if (monitorId.HasValue)
            {
                sql += " AND p.monitor_id = @MonitorId";
            }

            if (status.HasValue)
            {
                sql += " AND p.status = @Status";
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (m.name ILIKE @Search OR p.message ILIKE @Search)";
            }

            sql += " ORDER BY p.received_at DESC LIMIT @Limit OFFSET @Offset";

            return await QueryAsync<MonitorPing>(sql, new { ProjectId = projectId, MonitorId = monitorId, Status = status, Search = $"%{search}%", Limit = limit, Offset = offset });
        }

        public async Task<bool> RecordPingAsync(MonitorPing ping, MonitorStatus newStatus)
        {
            var monitor = await GetMonitorByIdAsync(ping.MonitorId);
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
                    ip_address, user_agent, http_method, request_headers, duration_ms
                )
                VALUES (
                    @MonitorId, @ReceivedAt, @Status, @Message,
                    CAST(@IpAddress AS inet), @UserAgent, @HttpMethod, CAST(@RequestHeaders AS jsonb), @DurationMs
                );";

            var rowsAffected = await ExecuteAsync(sql, new
            {
                Id = ping.MonitorId,
                LastPingAt = now,
                LastStatus = (int)newStatus,
                NextExpectedAt = nextExpectedAt,
                UpdatedAt = now,
                // Ping Data
                MonitorId = ping.MonitorId,
                ReceivedAt = ping.ReceivedAt,
                Status = (int)ping.Status,
                Message = ping.Message,
                IpAddress = ping.IpAddress,
                UserAgent = ping.UserAgent,
                HttpMethod = ping.HttpMethod,
                RequestHeaders = ping.RequestHeaders,
                DurationMs = ping.DurationMs
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
                    var schedule = CrontabSchedule.Parse(monitor.CronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
                    return schedule.GetNextOccurrence(utcNow);
                }
                catch
                {
                    return null;
                }
            }

            if (monitor.ScheduleType == ScheduleType.Calendar && !string.IsNullOrWhiteSpace(monitor.CalendarExpression))
            {
                // Basic Systemd OnCalendar parsing implementation or placeholder
                // For now, let's treat it as a cron-like if it looks like one, 
                // but real OnCalendar is complex. Let's at least store it correctly.
                return utcNow.AddDays(1); // Default placeholder for now
            }

            return null;
        }

        public async Task<bool> UpdateMonitorAsync(CronMonitor monitor)
        {
            const string sql = @"
                UPDATE monitors 
                SET name = @Name, 
                    slug = @Slug,
                    schedule_type = @ScheduleType, 
                    period_seconds = @PeriodSeconds, 
                    cron_expression = @CronExpression, 
                    cron_timezone = @CronTimezone, 
                    calendar_expression = @CalendarExpression,
                    calendar_timezone = @CalendarTimezone,
                    grace_seconds = @GraceSeconds,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            var rows = await ExecuteAsync(sql, monitor);
            return rows > 0;
        }
        public async Task<bool> DeleteMonitorAsync(Guid id)
        {
            const string sql = "UPDATE monitors SET is_deleted = TRUE, updated_at = CURRENT_TIMESTAMP WHERE id = @Id";
            var rows = await ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<bool> UpdateStatusAsync(Guid id, MonitorStatus status)
        {
            const string sql = "UPDATE monitors SET last_status = @Status, updated_at = CURRENT_TIMESTAMP WHERE id = @Id";
            var rows = await ExecuteAsync(sql, new { Id = id, Status = (int)status });
            return rows > 0;
        }

        public async Task<IEnumerable<CronMonitor>> GetOverdueMonitorsAsync()
        {
            const string sql = @"
                SELECT * FROM monitors 
                WHERE is_deleted = FALSE 
                AND last_status != @FailedStatus
                AND last_status != @PausedStatus
                AND next_expected_at IS NOT NULL
                AND (next_expected_at + (grace_seconds || ' seconds')::interval) < CURRENT_TIMESTAMP";

            return await QueryAsync<CronMonitor>(sql, new
            {
                FailedStatus = (int)MonitorStatus.Failed,
                PausedStatus = (int)MonitorStatus.Paused
            });
        }

        public async Task<IEnumerable<HealthyCron.Models.DTOs.MonthlyUptimeDto>> GetMonthlyStatsAsync(Guid monitorId)
        {
            const string sql = @"
                WITH MonthlyStats AS (
                    SELECT 
                        DATE_TRUNC('month', received_at) as MonthStart,
                        COUNT(*) as TotalCount,
                        COUNT(CASE WHEN status = 0 THEN 1 END) as FailureCount
                    FROM monitor_pings
                    WHERE monitor_id = @MonitorId
                    AND received_at >= DATE_TRUNC('month', CURRENT_DATE - INTERVAL '5 months')
                    GROUP BY DATE_TRUNC('month', received_at)
                )
                SELECT 
                    TO_CHAR(MonthStart, 'Mon YYYY') as Month,
                    EXTRACT(YEAR FROM MonthStart) as Year,
                    TotalCount,
                    FailureCount
                FROM MonthlyStats
                ORDER BY MonthStart DESC";

            var stats = await QueryAsync<HealthyCron.Models.DTOs.MonthlyUptimeDto>(sql, new { MonitorId = monitorId });
            
            // Post-process to calculate percentages and duration
            // We need the monitor's period to estimate downtime duration
            var monitor = await GetMonitorByIdAsync(monitorId);
            var periodSeconds = monitor?.PeriodSeconds ?? 60; // Default to 60s if not found or varying

            foreach (var stat in stats)
            {
                if (stat.TotalCount > 0)
                {
                    stat.UptimePercentage = Math.Round(((double)(stat.TotalCount - stat.FailureCount) / stat.TotalCount) * 100, 2);
                }
                else
                {
                    stat.UptimePercentage = 100; // No data = no downtime recorded
                }

                if (stat.FailureCount > 0)
                {
                    var totalDowntimeSeconds = stat.FailureCount * periodSeconds;
                    stat.DowntimeDuration = FormatDuration(totalDowntimeSeconds);
                }
                else
                {
                    stat.DowntimeDuration = "0m";
                }
            }

            // Fill in missing months if needed (optional, but good for UX)
            // For now return what we have
            return stats;
        }

        private string FormatDuration(int? seconds)
        {
            if (!seconds.HasValue) return "0m";
            if (seconds >= 86400) return $"{seconds / 86400}d {(seconds % 86400) / 3600}h";
            if (seconds >= 3600) return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
            return $"{seconds / 60}m";
        }
    }
}
