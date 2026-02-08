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
    }
}
