using HealthyCron.Models;

namespace HealthyCron.Data.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetProjectByIdAsync(Guid id);
        Task<Project?> GetProjectBySlugAsync(string slug);
        Task<IEnumerable<Project>> GetProjectsByUserIdAsync(Guid userId);
        Task<Guid> CreateProjectAsync(Project project);
        Task<bool> SlugExistsAsync(string slug);
    }
}
