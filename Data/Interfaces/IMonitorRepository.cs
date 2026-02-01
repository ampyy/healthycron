using HealthyCron.Models;
using CronMonitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Data.Interfaces
{
    public interface IMonitorRepository
    {
        Task<CronMonitor?> GetMonitorByIdAsync(Guid id);
        Task<CronMonitor?> GetMonitorBySlugAsync(string slug, Guid? projectId = null);
        Task<IEnumerable<CronMonitor>> GetMonitorsByProjectIdAsync(Guid projectId);
        Task<Guid> CreateMonitorAsync(CronMonitor monitor);
        Task<bool> SlugExistsAsync(Guid projectId, string slug);
        Task<bool> RecordPingAsync(MonitorPing ping, MonitorStatus newStatus, DateTime? lastStartAt);
        Task<IEnumerable<MonitorPing>> GetPingsByMonitorIdAsync(Guid monitorId, int limit = 50);
        Task<IEnumerable<MonitorPing>> GetPingsByProjectIdAsync(Guid projectId, int limit = 100);
        Task<IEnumerable<MonitorPing>> GetPingsWithFiltersAsync(Guid projectId, Guid? monitorId, int? status, string? search, int limit = 100);
        Task<bool> UpdateMonitorAsync(CronMonitor monitor);
        Task<bool> DeleteMonitorAsync(Guid id);
        Task<bool> UpdateStatusAsync(Guid id, MonitorStatus status);
        Task<IEnumerable<CronMonitor>> GetOverdueMonitorsAsync();
    }
}
