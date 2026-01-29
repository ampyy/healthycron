using HealthyCron.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyCron.Data.Interfaces
{
    public interface IProjectAccessKeyRepository
    {
        Task<ProjectAccessKey?> GetKeyByIdAsync(Guid id);
        Task<IEnumerable<ProjectAccessKey>> GetKeysByProjectIdAsync(Guid projectId);
        Task<Guid> CreateKeyAsync(ProjectAccessKey key);
        Task<bool> RevokeKeyAsync(Guid id);
        Task<ProjectAccessKey?> GetKeyByHashAsync(string hash);
        Task<ProjectAccessKey?> GetActiveKeyByTypeAsync(Guid projectId, ApiKeyType type);
    }
}
