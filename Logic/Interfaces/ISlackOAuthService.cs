using HealthyCron.Models;

namespace HealthyCron.Logic.Interfaces
{
    public interface ISlackOAuthService
    {
        string GenerateAuthorizationUrl(Guid projectId);
        bool ValidateState(string state, Guid projectId);
        Task<SlackOAuthResponse> ExchangeCodeForTokenAsync(string code);
    }
}
