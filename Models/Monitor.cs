namespace HealthyCron.Models
{
    public class Monitor
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Schedule
        public ScheduleType ScheduleType { get; set; }
        public int? PeriodSeconds { get; set; }
        public string? CronExpression { get; set; }
        public string? CronTimezone { get; set; }
        public string? CalendarExpression { get; set; }
        public string? CalendarTimezone { get; set; }
        public int? GraceSeconds { get; set; }

        // Status
        public DateTime? LastPingAt { get; set; }
        public MonitorStatus? LastStatus { get; set; }
        public DateTime? NextExpectedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
