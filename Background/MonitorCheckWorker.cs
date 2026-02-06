using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using HealthyCron.Logic.Interfaces;
using Microsoft.AspNetCore.SignalR;
using HealthyCron.Hubs;
namespace HealthyCron.Background
{
    public class MonitorCheckWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonitorCheckWorker> _logger;
        private readonly IHubContext<MonitorHub> _hubContext;

        public MonitorCheckWorker(
            IServiceProvider serviceProvider,
            ILogger<MonitorCheckWorker> logger,
            IHubContext<MonitorHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MonitorCheckWorker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckMonitorsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking monitors.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task CheckMonitorsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                var overdueMonitors = await repo.GetOverdueMonitorsAsync();

                foreach (var monitor in overdueMonitors)
                {
                    _logger.LogWarning("Monitor {MonitorName} ({MonitorId}) is overdue!", monitor.Name, monitor.Id);
                    await repo.UpdateStatusAsync(monitor.Id, MonitorStatus.Missed);

                    // Broadcast status change
                    await _hubContext.Clients.Group(monitor.Id.ToString()).SendAsync("StatusChanged", new
                    {
                        monitorId = monitor.Id,
                        newStatus = "Missed",
                        statusDisplay = "Down"
                    });

                    await alertService.TriggerAlertAsync(monitor, "Job Missed");
                }
            }
        }


    }
}
