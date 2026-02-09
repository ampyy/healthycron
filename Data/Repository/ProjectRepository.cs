using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Data.Repository
{
    public class ProjectRepository : BaseRepository, IProjectRepository
    {
        public ProjectRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<Project?> GetProjectByIdAsync(Guid id)
        {
            const string sql = "SELECT * FROM projects WHERE id = @Id AND is_deleted = FALSE";
            return await QueryFirstOrDefaultAsync<Project>(sql, new { Id = id });
        }

        public async Task<Project?> GetProjectBySlugAsync(string slug)
        {
            const string sql = "SELECT * FROM projects WHERE slug = @Slug AND is_deleted = FALSE";
            return await QueryFirstOrDefaultAsync<Project>(sql, new { Slug = slug });
        }

        public async Task<IEnumerable<Project>> GetProjectsByUserIdAsync(Guid userId)
        {
            const string sql = @"
                SELECT p.*, 
                    (SELECT COUNT(1) FROM monitors m WHERE m.project_id = p.id) as MonitorCount,
                    (SELECT COUNT(1) FROM monitor_pings mp JOIN monitors m ON mp.monitor_id = m.id WHERE m.project_id = p.id) as InteractionsCount
                FROM projects p 
                WHERE p.user_id = @UserId AND p.is_deleted = FALSE
                ORDER BY p.created_at DESC";
            return await QueryAsync<Project>(sql, new { UserId = userId });
        }

        public async Task<Guid> CreateProjectAsync(Project project)
        {
            if (project.Id == Guid.Empty)
            {
                project.Id = Guid.NewGuid();
            }

            const string sql = @"
                INSERT INTO projects (id, user_id, name, slug) 
                VALUES (@Id, @UserId, @Name, @Slug) 
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, project);
        }

        public async Task<bool> UpdateProjectAsync(Project project)
        {
            const string sql = @"
                UPDATE projects 
                SET name = @Name
                WHERE id = @Id";
            return await ExecuteAsync(sql, project) > 0;
        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            const string sql = "UPDATE projects SET is_deleted = TRUE WHERE id = @Id";
            return await ExecuteAsync(sql, new { Id = id }) > 0;
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            const string sql = "SELECT COUNT(1) FROM projects WHERE slug = @Slug AND is_deleted = FALSE";
            return await ExecuteScalarAsync<int>(sql, new { Slug = slug }) > 0;
        }

        public async Task<HealthyCron.Models.ViewModels.DashboardStats> GetDashboardStatsAsync(Guid userId)
        {
            const string sql = @"
                SELECT 
                    COUNT(DISTINCT p.id) as TotalProjects,
                    COUNT(DISTINCT m.id) as TotalMonitors,
                    COUNT(mp.id) as TotalBeats,
                    COUNT(DISTINCT CASE WHEN m.last_status = 1 THEN m.id END) as HealthyMonitors,
                    COUNT(DISTINCT CASE WHEN m.last_status = 0 THEN m.id END) as FailedMonitors,
                    COUNT(DISTINCT CASE WHEN m.next_expected_at < CURRENT_TIMESTAMP AND m.last_status != 2 AND m.is_deleted = FALSE THEN m.id END) as MissedMonitors
                FROM projects p
                LEFT JOIN monitors m ON m.project_id = p.id AND m.is_deleted = FALSE
                LEFT JOIN monitor_pings mp ON mp.monitor_id = m.id
                WHERE p.user_id = @UserId AND p.is_deleted = FALSE";

            var stats = await QueryFirstOrDefaultAsync<HealthyCron.Models.ViewModels.DashboardStats>(sql, new { UserId = userId });
            return stats ?? new HealthyCron.Models.ViewModels.DashboardStats();
        }

        private class PingStatsRow
        {
            public Guid ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public DateTime Hour { get; set; }
            public int Count { get; set; }
        }

        public async Task<IEnumerable<HealthyCron.Models.ViewModels.ProjectPingStats>> GetRecentPingStatsAsync(Guid userId, int hours = 24)
        {
            const string sql = @"
                WITH RECURSIVE hours AS (
                    SELECT date_trunc('hour', CURRENT_TIMESTAMP) as hr
                    UNION ALL
                    SELECT hr - interval '1 hour'
                    FROM hours
                    WHERE hr > date_trunc('hour', CURRENT_TIMESTAMP) - (interval '1 hour' * (@Hours - 1))
                ),
                project_list AS (
                    SELECT id, name FROM projects WHERE user_id = @UserId AND is_deleted = FALSE
                )
                SELECT 
                    pl.id as ProjectId,
                    pl.name as ProjectName,
                    h.hr as Hour,
                    COUNT(mp.id) as Count
                FROM hours h
                CROSS JOIN project_list pl
                LEFT JOIN monitors m ON m.project_id = pl.id AND m.is_deleted = FALSE
                LEFT JOIN monitor_pings mp ON mp.monitor_id = m.id AND date_trunc('hour', mp.received_at) = h.hr
                GROUP BY pl.id, pl.name, h.hr
                ORDER BY pl.name, h.hr";

            var results = await QueryAsync<PingStatsRow>(sql, new { UserId = userId, Hours = hours });
            
            var statsMap = new Dictionary<Guid, HealthyCron.Models.ViewModels.ProjectPingStats>();
            var colors = new[] { "#3B82F6", "#10B981", "#8B5CF6", "#F59E0B", "#EF4444", "#06B6D4" };
            int colorIdx = 0;

            foreach (var row in results)
            {
                if (!statsMap.TryGetValue(row.ProjectId, out HealthyCron.Models.ViewModels.ProjectPingStats? stats))
                {
                    stats = new HealthyCron.Models.ViewModels.ProjectPingStats
                    {
                        ProjectId = row.ProjectId,
                        ProjectName = row.ProjectName,
                        Color = colors[colorIdx % colors.Length],
                        Data = new List<HealthyCron.Models.ViewModels.PingCountByHour>()
                    };
                    statsMap[row.ProjectId] = stats;
                    colorIdx++;
                }

                stats.Data.Add(new HealthyCron.Models.ViewModels.PingCountByHour
                {
                    Hour = row.Hour,
                    Count = row.Count
                });
            }

            return statsMap.Values;
        }
    }
}
