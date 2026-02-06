using HealthyCron.Enums;

namespace HealthyCron.Models
{
    public class NotificationJob
    {
        public Guid Id { get; set; }
        public Guid MonitorPingId { get; set; }
        public Guid IntegrationId { get; set; }
        public AlertType AlertType { get; set; }
        public NotificationJobStatus Status { get; set; }
        public short RetryCount { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
