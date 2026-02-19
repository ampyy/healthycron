using HealthyCron.Models;

namespace HealthyCron.Data.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<Guid> CreateUserAsync(string email, string? passwordHash = null);
        Task UpdateUserPasswordAsync(Guid userId, string passwordHash);
        Task UpdateUserAsync(User user);

        Task CreateMagicTokenAsync(Guid userId, string tokenHash, DateTime expiresAt);
        Task<MagicToken?> GetMagicTokenByHashAsync(string tokenHash);
        Task MarkMagicTokenAsUsedAsync(Guid tokenId);

        Task CreateSessionAsync(Guid userId, string sessionTokenHash, DateTime expiresAt);
        Task<UserSession?> GetSessionByHashAsync(string sessionTokenHash);
        Task DeleteSessionAsync(string sessionTokenHash);
    }
}
