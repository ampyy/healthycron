namespace HealthyCron.Models
{
    public enum MonitorStatus : short
    {
        Success = 0,
        Running = 1,
        Failed = 2,
        Missed = 3,
        Paused = 4
    }
}
