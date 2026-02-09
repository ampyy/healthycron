using System;
using System.Collections.Generic;

namespace HealthyCron.Models.ViewModels
{
    public class DashboardStats
    {
        public int TotalProjects { get; set; }
        public int TotalMonitors { get; set; }
        public long TotalBeats { get; set; }
        public int HealthyMonitors { get; set; }
        public int MissedMonitors { get; set; }
        public int FailedMonitors { get; set; }
        
        // Quotas
        public int ProjectQuota { get; set; } = 5;
        public int MonitorQuota { get; set; } = 20;
    }

    public class ProjectPingStats
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Color { get; set; } = "#3B82F6"; // Default blue
        public List<PingCountByHour> Data { get; set; } = new();
    }

    public class PingCountByHour
    {
        public DateTime Hour { get; set; }
        public int Count { get; set; }
    }
}
