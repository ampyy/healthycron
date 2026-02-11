using HealthyCron.Data.Interfaces;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using Monitor = HealthyCron.Models.Monitor;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using HealthyCron.Hubs;
using System.Collections.Generic;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Logic.Service
{
    public class PingService : IPingService
    {
        private readonly IMonitorRepository _monitorRepository;
        private readonly IAccessKeyService _accessKeyService;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly ILogger<PingService> _logger;
        private readonly IHubContext<MonitorHub> _hubContext;
        private readonly IQueueService _queueService;

        public PingService(
            IMonitorRepository monitorRepository,
            IAccessKeyService accessKeyService,
            IIntegrationRepository integrationRepository,
            ILogger<PingService> logger,
            IHubContext<MonitorHub> hubContext,
            IQueueService queueService)
        {
            _monitorRepository = monitorRepository;
            _accessKeyService = accessKeyService;
            _integrationRepository = integrationRepository;
            _logger = logger;
            _hubContext = hubContext;
            _queueService = queueService;
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
            var now = DateTime.UtcNow;

            var ping = new MonitorPing
            {
                MonitorId = monitor.Id,
                Status = pingType,
                Message = message,
                IpAddress = metadata.IpAddress,
                UserAgent = metadata.UserAgent,
                HttpMethod = metadata.Method,
                RequestHeaders = metadata.HeadersJson,
                DurationMs = null, // Duration monitoring removed as it depends on last_start_at
                ReceivedAt = now
            };

            // CRITICAL: Alert Decision Logic
            // Store previous state before updating
            var previousStatus = monitor.LastStatus;

            // Determine new status based on ping type
            var newStatus = pingType switch
            {
                PingType.Start => MonitorStatus.Success,
                PingType.Success => MonitorStatus.Success,
                PingType.Fail => MonitorStatus.Failed,
                _ => MonitorStatus.Success
            };

            // Record the ping and update monitor state
            var pingId = await _monitorRepository.RecordPingAsync(ping, newStatus);

            // Broadcast real-time update via SignalR
            var signalRPayload = new
            {
                id = ping.Id,
                receivedAt = ping.ReceivedAt,
                timeStr = ping.ReceivedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                dateDisplay = ping.ReceivedAt.ToString("MMM dd"),
                timeDisplay = ping.ReceivedAt.ToString("HH:mm:ss"),
                status = pingType.ToString(),
                message = ping.Message,
                ipAddress = ping.IpAddress,
                userAgent = ping.UserAgent,
                method = ping.HttpMethod,
                headers = ping.RequestHeaders,
                newMonitorStatus = newStatus.ToString(),
                monitorId = monitor.Id,
                monitorName = monitor.Name
            };

            await _hubContext.Clients.Group(monitor.Id.ToString()).SendAsync("PingReceived", signalRPayload);
            await _hubContext.Clients.Group(monitor.ProjectId.ToString()).SendAsync("PingReceived", signalRPayload);

            // Detect state transitions and create notification jobs
            Enums.AlertType? alertType = null;

            // UP → DOWN: Trigger DOWN alert
            if ((previousStatus == MonitorStatus.Success || previousStatus == null)
                && newStatus == MonitorStatus.Failed)
            {
                alertType = Enums.AlertType.Down;
                _logger.LogWarning("Monitor {MonitorId} ({MonitorName}) transitioned from {PreviousStatus} to DOWN",
                    monitor.Id, monitor.Name, previousStatus?.ToString() ?? "null");
            }
            // DOWN → UP: Trigger RECOVERY alert
            else if (previousStatus == MonitorStatus.Failed && newStatus == MonitorStatus.Success)
            {
                alertType = Enums.AlertType.Up;
                _logger.LogInformation("Monitor {MonitorId} ({MonitorName}) RECOVERED from DOWN to UP",
                    monitor.Id, monitor.Name);
            }

            // If there's an alert, create notification jobs for all monitor integrations
            if (alertType.HasValue && pingId.HasValue)
            {
                var integrations = await _integrationRepository.GetMonitorIntegrationsAsync(monitor.Id);

                foreach (var item in integrations)
                {
                    if (!item.IsEnabledForMonitor) continue;

                    var integration = item.Integration;
                    try
                    {
                        var jobId = await _integrationRepository.CreateNotificationJobAsync(
                            pingId.Value,
                            integration.Id
                        );

                        _logger.LogInformation("Created notification job {JobId} for monitor {MonitorId}, integration {IntegrationId}, alert type {AlertType}",
                            jobId, monitor.Id, integration.Id, alertType.Value);

                        // Send full payload to SQS queue for processing
                        try
                        {
                            var sqsPayload = new HealthyCron.Models.DTOs.SqsMessagePayload
                            {
                                JobId = jobId,
                                MonitorId = monitor.Id,
                                IntegrationId = integration.Id
                            };
                            await _queueService.SendMessageAsync(sqsPayload);
                            _logger.LogInformation("Successfully sent Job ID {JobId} to SQS queue", jobId);
                        }
                        catch (Exception queueEx)
                        {
                            _logger.LogError(queueEx, "Failed to send Job ID {JobId} to SQS queue", jobId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create notification job for monitor {MonitorId}, integration {IntegrationId}",
                            monitor.Id, integration.Id);
                    }
                }
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
