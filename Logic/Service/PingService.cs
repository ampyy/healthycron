using HealthyCron.Data.Interfaces;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using Monitor = HealthyCron.Models.Monitor;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace HealthyCron.Logic.Service
{
    public class PingService : IPingService
    {
        private readonly IMonitorRepository _monitorRepository;
        private readonly IAccessKeyService _accessKeyService;
        private readonly IAlertService _alertService;

        public PingService(IMonitorRepository monitorRepository, IAccessKeyService accessKeyService, IAlertService alertService)
        {
            _monitorRepository = monitorRepository;
            _accessKeyService = accessKeyService;
            _alertService = alertService;
        }

        public async Task ProcessPingAsync(Guid monitorId, string statusFromUrl, string? statusHeader, string? bodyJson, PingMetadata metadata)
        {
            var monitor = await _monitorRepository.GetMonitorByIdAsync(monitorId);
            if (monitor == null) return;

            await ExecutePingAsync(monitor, statusFromUrl, statusHeader, bodyJson, metadata);
        }

        public async Task ProcessPingBySlugAsync(string pingKey, string slug, string statusFromUrl, string? statusHeader, string? bodyJson, PingMetadata metadata)
        {
            var keyModel = await _accessKeyService.ValidateKeyAsync(pingKey);
            if (keyModel == null || keyModel.KeyType != ApiKeyType.Ping) return;

            var monitor = await _monitorRepository.GetMonitorBySlugAsync(slug, keyModel.ProjectId);
            if (monitor == null) return;

            await ExecutePingAsync(monitor, statusFromUrl, statusHeader, bodyJson, metadata);
        }

        private async Task ExecutePingAsync(Monitor monitor, string statusFromUrl, string? statusHeader, string? bodyJson, PingMetadata metadata)
        {
            var (pingType, message) = ResolveStatusAndMessage(statusFromUrl, statusHeader, bodyJson);

            int? durationMs = null;
            var now = DateTime.UtcNow;

            if (pingType == PingType.Success && monitor.LastStartAt.HasValue)
            {
                durationMs = (int)(now - monitor.LastStartAt.Value).TotalMilliseconds;
            }

            var ping = new MonitorPing
            {
                MonitorId = monitor.Id,
                Status = pingType,
                Message = message,
                IpAddress = metadata.IpAddress,
                UserAgent = metadata.UserAgent,
                HttpMethod = metadata.Method,
                RequestHeaders = metadata.HeadersJson,
                ResponseTimeMs = metadata.ResponseTimeMs,
                DurationMs = durationMs,
                ReceivedAt = now
            };

            // Update Monitor State based on Ping Type
            var newStatus = pingType switch
            {
                PingType.Start => MonitorStatus.Running,
                PingType.Success => MonitorStatus.Success,
                PingType.Fail => MonitorStatus.Failed,
                _ => MonitorStatus.Success
            };

            // We update everything in one go via the repository
            await _monitorRepository.RecordPingAsync(ping, newStatus, pingType == PingType.Start ? now : (pingType == PingType.Success ? null : monitor.LastStartAt));

            // Trigger alert if failed
            if (pingType == PingType.Fail)
            {
                await _alertService.TriggerAlertAsync(monitor, "Job Failed", message);
            }
        }

        private (PingType Type, string? Message) ResolveStatusAndMessage(string urlStatus, string? headerStatus, string? bodyJson)
        {
            string statusStr = urlStatus.ToLower();
            string? message = null;

            // Priority 1: URL
            if (statusStr == "start" || statusStr == "success" || statusStr == "fail")
            {
                // Already set
            }
            // Priority 2: Header
            else if (!string.IsNullOrEmpty(headerStatus))
            {
                statusStr = headerStatus.ToLower();
            }
            // Priority 3: Body
            else if (!string.IsNullOrEmpty(bodyJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(bodyJson);
                    if (doc.RootElement.TryGetProperty("status", out var statusProp))
                    {
                        statusStr = statusProp.GetString()?.ToLower() ?? statusStr;
                    }
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.GetString();
                    }
                }
                catch { /* Ignore invalid JSON */ }
            }

            var type = statusStr switch
            {
                "start" => PingType.Start,
                "fail" => PingType.Fail,
                _ => PingType.Success
            };

            return (type, message);
        }
    }
}
