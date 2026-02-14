using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using HealthyCron.Utilities.Service;

namespace HealthyCron.Logic.Service
{
    public interface IPagerDutyService
    {
        Task<PagerDutyTokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri);
        Task<PagerDutyTokenResponse?> RefreshAccessTokenAsync(string refreshToken);
        Task<PagerDutyAccountInfo?> GetAccountInfoAsync(string accessToken);
        Task<List<PagerDutyServiceInfo>?> GetServicesAsync(string accessToken);
        Task<string?> CreateIncidentAsync(string accessToken, string title, string description, string? serviceId = null);
    }

    public class PagerDutyService : IPagerDutyService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly HttpClient _httpClient;
        private readonly AxiomLogger _logger;

        public PagerDutyService(
            IConfiguration configuration,
            HttpClient httpClient,
            AxiomLogger logger)
        {
            _clientId = configuration["PagerDuty:ClientId"] ??
                       configuration["PAGERDUTY_CLIENT_ID"] ??
                       throw new InvalidOperationException("PagerDuty Client ID not configured");

            _clientSecret = configuration["PagerDuty:ClientSecret"] ??
                           configuration["PAGERDUTY_CLIENT_SECRET"] ??
                           throw new InvalidOperationException("PagerDuty Client Secret not configured");

            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PagerDutyTokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri)
        {
            try
            {
                await _logger.LogInfo("Exchanging code for tokens", new Dictionary<string, object>
                {
                    ["redirect_uri"] = redirectUri,
                    ["code_length"] = code.Length
                });
                
                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "code", code },
                    { "redirect_uri", redirectUri }
                };

                var content = new FormUrlEncodedContent(formData);

                var response = await _httpClient.PostAsync("https://app.pagerduty.com/oauth/token", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogError("PagerDuty token exchange failed", new Dictionary<string, object>
                    {
                        ["status_code"] = (int)response.StatusCode,
                        ["error_response"] = responseBody
                    });
                    return null;
                }

                await _logger.LogInfo("PagerDuty token exchange successful", new Dictionary<string, object> { ["response_length"] = responseBody.Length });
                
                return JsonSerializer.Deserialize<PagerDutyTokenResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                await _logger.LogError("Error exchanging PagerDuty authorization code for tokens", new Dictionary<string, object>
                {
                    ["exception"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace ?? "No stack trace"
                });
                return null;
            }
        }

        public async Task<PagerDutyTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "refresh_token", refreshToken }
                };

                var content = new FormUrlEncodedContent(formData);

                var response = await _httpClient.PostAsync("https://app.pagerduty.com/oauth/token", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    await _logger.LogError("PagerDuty token refresh failed", new Dictionary<string, object>
                    {
                        ["status_code"] = (int)response.StatusCode,
                        ["error_response"] = errorBody
                    });
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagerDutyTokenResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                await _logger.LogError("Error refreshing PagerDuty access token", new Dictionary<string, object>
                {
                    ["exception"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace ?? "No stack trace"
                });
                return null;
            }
        }

        public async Task<PagerDutyAccountInfo?> GetAccountInfoAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pagerduty.com/users/me");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/vnd.pagerduty+json;version=2");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogError("Failed to get PagerDuty account info", new Dictionary<string, object>
                    {
                        ["status_code"] = (int)response.StatusCode
                    });
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<PagerDutyUserResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return apiResponse?.User;
            }
            catch (Exception ex)
            {
                await _logger.LogError("Error getting PagerDuty account info", new Dictionary<string, object>
                {
                    ["exception"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace ?? "No stack trace"
                });
                return null;
            }
        }

        public async Task<List<PagerDutyServiceInfo>?> GetServicesAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pagerduty.com/services");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/vnd.pagerduty+json;version=2");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogError("Failed to get PagerDuty services", new Dictionary<string, object>
                    {
                        ["status_code"] = (int)response.StatusCode
                    });
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<PagerDutyServicesResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return apiResponse?.Services;
            }
            catch (Exception ex)
            {
                await _logger.LogError("Error getting PagerDuty services", new Dictionary<string, object>
                {
                    ["exception"] = ex.Message,
                    [" stack_trace"] = ex.StackTrace ?? "No stack trace"
                });
                return null;
            }
        }

        public async Task<string?> CreateIncidentAsync(string accessToken, string title, string description, string? serviceId = null)
        {
            try
            {
                // Use default service if not provided
                var actualServiceId = serviceId ?? "default";

                var requestBody = new
                {
                    incident = new
                    {
                        type = "incident",
                        title = title,
                        service = new
                        {
                            id = actualServiceId,
                            type = "service_reference"
                        },
                        body = new
                        {
                            type = "incident_body",
                            details = description
                        }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.pagerduty.com/incidents");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/vnd.pagerduty+json;version=2");
                request.Headers.Add("From", "healthycron@example.com"); // Required by PagerDuty API

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    await _logger.LogError("Failed to create PagerDuty incident", new Dictionary<string, object>
                    {
                        ["status_code"] = (int)response.StatusCode,
                        ["error_response"] = errorBody
                    });
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<PagerDutyIncidentResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return apiResponse?.Incident?.Id;
            }
            catch (Exception ex)
            {
                await _logger.LogError("Error creating PagerDuty incident", new Dictionary<string, object>
                {
                    ["exception"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace ?? "No stack trace"
                });
                return null;
            }
        }
    }

    // Response models
    public class PagerDutyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    public class PagerDutyUserResponse
    {
        public PagerDutyAccountInfo? User { get; set; }
    }

    public class PagerDutyAccountInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public class PagerDutyServicesResponse
    {
        public List<PagerDutyServiceInfo>? Services { get; set; }
    }

    public class PagerDutyServiceInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
    }

    public class PagerDutyIncidentResponse
    {
        public PagerDutyIncident? Incident { get; set; }
    }

    public class PagerDutyIncident
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }
}
