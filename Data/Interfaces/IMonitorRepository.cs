using HealthyCron.Models;
using CronMonitor = HealthyCron.Models.Monitor;

namespace HealthyCron.Data.Interfaces
{
    public interface IMonitorRepository
    {
        Task<CronMonitor?> GetMonitorByIdAsync(Guid id);
        Task<CronMonitor?> GetMonitorBySlugAsync(string slug);
        Task<IEnumerable<CronMonitor>> GetMonitorsByProjectIdAsync(Guid projectId);
        Task<Guid> CreateMonitorAsync(CronMonitor monitor);
        Task<bool> SlugExistsAsync(Guid projectId, string slug);
        Task<bool> RecordPingAsync(MonitorPing pingData);
        Task<IEnumerable<MonitorPing>> GetPingsByMonitorIdAsync(Guid monitorId, int limit = 50);
        Task<bool> UpdateMonitorAsync(CronMonitor monitor);
        Task<bool> DeleteMonitorAsync(Guid id);
        Task<bool> UpdateStatusAsync(Guid id, MonitorStatus status);
    }
}
