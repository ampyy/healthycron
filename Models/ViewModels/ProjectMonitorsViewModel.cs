using System;
using System.Collections.Generic;
using HealthyCron.Models;
using HealthyCron.Models.ViewModels;

namespace HealthyCron.Models.ViewModels
{
    public class ProjectMonitorsViewModel
    {
        public Project Project { get; set; } = null!;
        public List<MonitorWithIntegrations> Monitors { get; set; } = new();
        public bool CanManage { get; set; }
    }

    public class MonitorWithIntegrations
    {
        public Monitor Monitor { get; set; } = null!;
        public List<IntegrationListItemViewModel> Integrations { get; set; } = new();
    }
}
