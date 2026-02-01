using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using HealthyCron.Logic.Interfaces;
using Monitor = HealthyCron.Models.Monitor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthyCron.Background
{
    public class MonitorCheckWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonitorCheckWorker> _logger;

        public MonitorCheckWorker(IServiceProvider serviceProvider, ILogger<MonitorCheckWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
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
                    
                    await alertService.TriggerAlertAsync(monitor, "Job Missed");
                }
            }
        }


    }
}
