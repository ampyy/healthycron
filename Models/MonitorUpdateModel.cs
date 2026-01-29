using System;
using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models
{
    public class MonitorUpdateModel : MonitorCreationModel
    {
        [Required]
        public Guid Id { get; set; }
    }

    public class MonitorDetailsUpdateModel
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        [Required]
        public string ProjectSlug { get; set; } = string.Empty;
    }

    public class MonitorScheduleUpdateModel
    {
        [Required]
        public Guid Id { get; set; }
        public ScheduleType ScheduleType { get; set; }
        public int PeriodValue { get; set; }
        public string PeriodUnit { get; set; } = "seconds";
        public string? CronExpression { get; set; }
        public string? CronTimezone { get; set; }
        public string? CalendarExpression { get; set; }
        public string? CalendarTimezone { get; set; }
        public int GraceSeconds { get; set; }
        public string GraceUnit { get; set; } = "seconds";
    }
}
