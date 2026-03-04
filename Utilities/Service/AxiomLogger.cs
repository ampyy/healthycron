using HealthyCron.Utilities.Interface;
using System.Text;
using System.Text.Json;

namespace HealthyCron.Utilities.Service
{
    public class AxiomLogger : IAxiomLogger
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AxiomLogger> _logger;

        public AxiomLogger(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AxiomLogger> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public Task LogInfo(string message, Dictionary<string, object>? additionalData = null)
        {
            SendLog("INFO", message, additionalData);
            return Task.CompletedTask;
        }

        public Task LogWarn(string message, Dictionary<string, object>? additionalData = null)
        {
            SendLog("WARN", message, additionalData);
            return Task.CompletedTask;
        }

        public Task LogError(string message, Dictionary<string, object>? additionalData = null)
        {
            SendLog("ERROR", message, additionalData);
            return Task.CompletedTask;
        }

        private void SendLog(string level, string message, Dictionary<string, object>? additionalData)
        {
            try
            {
                // Read Axiom configuration
                var enableDevLogging = _configuration.GetValue<bool>("Axiom:EnableDevLogging");

                // Skip if dev logging is disabled
                if (_environment.IsDevelopment() && !enableDevLogging)
                {
                    return;
                }

                // Get dataset and token based on environment
                string? dataset, token;
                if (_environment.IsDevelopment())
                {
                    dataset = _configuration["Axiom:DevDataset"];
                    token = _configuration["Axiom:DevToken"];
                }
                else
                {
                    dataset = _configuration["Axiom:ProdDataset"];
                    token = _configuration["Axiom:ProdToken"];
                }

                // Validate we have required settings
                if (string.IsNullOrEmpty(dataset) || string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Axiom dataset or token is missing");
                    return;
                }

                // Build the data object
                var dataObject = new Dictionary<string, object>
                {
                    ["level"] = level,
                    ["message"] = message,
                    ["environment"] = _environment.EnvironmentName
                };

                // Add additional data if provided
                if (additionalData != null)
                {
                    foreach (var kvp in additionalData)
                    {
                        dataObject[kvp.Key] = kvp.Value;
                    }
                }

                // Create Axiom payload format: { time, data }
                var logEntry = new
                {
                    time = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                    data = dataObject
                };

                // Serialize to JSON array
                var jsonPayload = JsonSerializer.Serialize(new[] { logEntry });

                // Build Axiom URL
                var axiomDomain = _configuration["Axiom:AxiomDomain"] ?? "us-east-1.aws.edge.axiom.co";
                var axiomUrl = $"https://{axiomDomain}/v1/ingest/{dataset}";

                // Send asynchronously (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(axiomUrl, content);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Axiom log failed: {Status} - {Response}",
                                response.StatusCode, responseBody);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send log to Axiom");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to prepare Axiom log");
            }
        }
    }
}
