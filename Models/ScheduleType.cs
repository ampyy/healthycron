namespace HealthyCron.Models
{
    public enum ScheduleType : short
    {
        Interval = 0,   // Every X seconds
        Cron = 1,       // Cron expression
        Calendar = 2    // Calendar-based schedule (future feature)
    }
}
