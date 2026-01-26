using HealthyCron.Models;

namespace HealthyCron.Logic.Interfaces
{
    public interface IAuthService
    {
        Task<string> RequestMagicLinkAsync(string email);
        Task<string?> VerifyMagicTokenAsync(string token);
        Task<bool> RegisterPasswordAsync(string email, string password);
        Task<string?> LoginPasswordAsync(string email, string password);
        Task LogoutAsync(string sessionToken);
        Task<User?> GetUserFromSessionAsync(string sessionToken);
    }
}
