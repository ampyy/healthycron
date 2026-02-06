using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HealthyCron.Hubs
{
    public class MonitorHub : Hub
    {
        public async Task JoinMonitor(string monitorId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, monitorId);
        }

        public async Task JoinProject(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
        }

        public async Task LeaveMonitor(string monitorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, monitorId);
        }

        public async Task LeaveProject(string projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
        }
    }
}
