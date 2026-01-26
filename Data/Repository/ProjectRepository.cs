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
            const string sql = "SELECT * FROM projects WHERE id = @Id";
            return await QueryFirstOrDefaultAsync<Project>(sql, new { Id = id });
        }

        public async Task<Project?> GetProjectBySlugAsync(string slug)
        {
            const string sql = "SELECT * FROM projects WHERE slug = @Slug";
            return await QueryFirstOrDefaultAsync<Project>(sql, new { Slug = slug });
        }

        public async Task<IEnumerable<Project>> GetProjectsByUserIdAsync(Guid userId)
        {
            const string sql = @"
                SELECT p.*, 
                    (SELECT COUNT(1) FROM monitors m WHERE m.project_id = p.id) as MonitorCount,
                    (SELECT COUNT(1) FROM monitor_pings mp JOIN monitors m ON mp.monitor_id = m.id WHERE m.project_id = p.id) as InteractionsCount
                FROM projects p 
                WHERE p.user_id = @UserId 
                ORDER BY p.created_at DESC";
            return await QueryAsync<Project>(sql, new { UserId = userId });
        }

        public async Task<Guid> CreateProjectAsync(Project project)
        {
            const string sql = @"
                INSERT INTO projects (user_id, name, slug, color, icon) 
                VALUES (@UserId, @Name, @Slug, @Color, @Icon) 
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, project);
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            const string sql = "SELECT COUNT(1) FROM projects WHERE slug = @Slug";
            return await ExecuteScalarAsync<int>(sql, new { Slug = slug }) > 0;
        }
    }
}
