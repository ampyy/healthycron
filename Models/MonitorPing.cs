namespace HealthyCron.Models
{
    public class MonitorPing
    {
        public Guid Id { get; set; }
        public Guid MonitorId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public PingType Status { get; set; }
        public string? Message { get; set; }
    }
}
