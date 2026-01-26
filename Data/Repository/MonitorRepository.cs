using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
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

        public async Task<bool> RegisterPingByIdAsync(Guid id)
        {
            var monitor = await GetMonitorByIdAsync(id);
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

                INSERT INTO monitor_pings (monitor_id, received_at, status, message)
                VALUES (@Id, @LastPingAt, @PingStatus, NULL);";

            var rowsAffected = await ExecuteAsync(sql, new
            {
                Id = id,
                LastPingAt = now,
                LastStatus = (int)MonitorStatus.Up, // 0 = Up
                NextExpectedAt = nextExpectedAt,
                UpdatedAt = now,
                PingStatus = (int)PingType.Success // 0 = Success
            });

            return rowsAffected > 0;
        }

        private DateTime? CalculateNextExpectedAt(CronMonitor monitor)
        {
            if (monitor.ScheduleType == ScheduleType.Interval && monitor.PeriodSeconds.HasValue)
            {
                return DateTime.UtcNow.AddSeconds(monitor.PeriodSeconds.Value);
            }
            // For Cron and OnCalendar, we would need a cron parser library
            // For now, return null and implement later
            return null;
        }
    }
}
