using HealthyCron.Models;

namespace HealthyCron.Data.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetProjectByIdAsync(Guid id);
        Task<Project?> GetProjectBySlugAsync(string slug);
        Task<IEnumerable<Project>> GetProjectsByUserIdAsync(Guid userId);
        Task<Guid> CreateProjectAsync(Project project);
        Task<bool> UpdateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(Guid id);
        Task<bool> SlugExistsAsync(string slug);
        Task<HealthyCron.Models.ViewModels.DashboardStats> GetDashboardStatsAsync(Guid userId);
        Task<IEnumerable<HealthyCron.Models.ViewModels.ProjectPingStats>> GetRecentPingStatsAsync(Guid userId, int hours = 24);
    }
}
