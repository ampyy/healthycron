using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HealthyCron.Logic.Service
{
    public class SlackOAuthService : ISlackOAuthService
    {
        private readonly SlackSettings _settings;
        private readonly HttpClient _httpClient;

        public SlackOAuthService(SlackSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;
        }

        public string GenerateAuthorizationUrl(Guid projectId)
        {
            var state = GenerateState(projectId);
            var scopes = "incoming-webhook,chat:write,channels:read";

            return $"https://slack.com/oauth/v2/authorize?" +
                   $"client_id={Uri.EscapeDataString(_settings.ClientId)}&" +
                   $"scope={Uri.EscapeDataString(scopes)}&" +
                   $"redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}&" +
                   $"state={Uri.EscapeDataString(state)}";
        }

        public bool ValidateState(string state, Guid projectId)
        {
            var expectedState = GenerateState(projectId);
            return state == expectedState;
        }

        public async Task<SlackOAuthResponse> ExchangeCodeForTokenAsync(string code)
        {
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "code", code },
                { "redirect_uri", _settings.RedirectUri }
            };

            var response = await _httpClient.PostAsync(
                "https://slack.com/api/oauth.v2.access",
                new FormUrlEncodedContent(requestBody)
            );

            var responseContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var oauthResponse = JsonSerializer.Deserialize<SlackOAuthResponse>(responseContent, options);

            return oauthResponse ?? new SlackOAuthResponse { Ok = false, Error = "Failed to parse response" };
        }

        private string GenerateState(Guid projectId)
        {
            var message = $"{projectId}:{_settings.StateSecret}";
            var hash = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(_settings.StateSecret),
                Encoding.UTF8.GetBytes(message)
            );
            return $"{projectId}:{Convert.ToBase64String(hash)}";
        }
    }
}
