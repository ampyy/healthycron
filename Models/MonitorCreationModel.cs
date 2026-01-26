using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models
{
    public class MonitorCreationModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }

        [Required]
        public string ProjectSlug { get; set; } = string.Empty;

        public ScheduleType ScheduleType { get; set; }

        // Simple
        public int PeriodValue { get; set; }
        public string PeriodUnit { get; set; } = "minutes";

        // Cron
        public string? CronExpression { get; set; }
        public string? CronTimezone { get; set; }

        // Calendar
        public string? CalendarExpression { get; set; }
        public string? CalendarTimezone { get; set; }

        // Grace
        public int GraceSeconds { get; set; }
        public string GraceUnit { get; set; } = "minutes";
    }
}
