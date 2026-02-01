using HealthyCron.Models;
using Microsoft.Extensions.Logging;
using Monitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Logic.Interfaces
{
    public interface IAlertService
    {
        Task TriggerAlertAsync(Monitor monitor, string reason, string? message = null);
    }
}

namespace HealthyCron.Logic.Service
{
    using HealthyCron.Logic.Interfaces;

    public class AlertService : IAlertService
    {
        private readonly ILogger<AlertService> _logger;

        public AlertService(ILogger<AlertService> logger)
        {
            _logger = logger;
        }

        public Task TriggerAlertAsync(Monitor monitor, string reason, string? message = null)
        {
            _logger.LogWarning("ALERT for Monitor {MonitorName} ({MonitorId}): {Reason}. Extra Info: {Message}", 
                monitor.Name, monitor.Id, reason, message ?? "None");
            
            // In a real app, you'd send emails, Slack messages, etc.
            return Task.CompletedTask;
        }
    }
}
