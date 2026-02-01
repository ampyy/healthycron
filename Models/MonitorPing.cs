namespace HealthyCron.Models
{
    public class MonitorPing
    {
        public int Id { get; set; }
        public Guid MonitorId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public PingType Status { get; set; }
        public string? Message { get; set; }
        
        // Metadata
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestHeaders { get; set; } // JSON string
        public int? ResponseTimeMs { get; set; }
        public int? DurationMs { get; set; }

        // Joined Data
        public string? MonitorName { get; set; }
    }
}
