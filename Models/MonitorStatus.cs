namespace HealthyCron.Models
{
    public enum MonitorStatus : short
    {
        Up = 0,
        Late = 1, // Future use
        Down = 2,
        Paused = 3
    }
}
