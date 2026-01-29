using HealthyCron.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyCron.Logic.Interfaces
{
    public interface IAccessKeyService
    {
        Task<(string FullKey, ProjectAccessKey KeyModel)> CreateKeyAsync(Guid projectId, ApiKeyType type);
        Task<IEnumerable<ProjectAccessKey>> GetKeysByProjectIdAsync(Guid projectId);
        Task<bool> RevokeKeyAsync(Guid keyId);
        Task<ProjectAccessKey?> ValidateKeyAsync(string fullKey);
        string HashKey(string fullKey);
    }
}
