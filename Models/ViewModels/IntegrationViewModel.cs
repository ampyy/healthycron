using HealthyCron.Models;

namespace HealthyCron.Models.ViewModels
{
    public class IntegrationListItemViewModel
    {
        public Integration Integration { get; set; } = null!;
        public SlackIntegration? SlackDetails { get; set; }
        public bool IsEnabledForMonitor { get; set; }
    }
}
