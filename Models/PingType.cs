namespace HealthyCron.Models
{
    public enum PingType : short
    {
        Fail = 0,
        Start = 1,
        Success = 2,
        Paused = 3,
        Resumed = 4
    }
}
