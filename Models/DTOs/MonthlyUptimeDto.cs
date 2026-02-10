namespace HealthyCron.Models.DTOs
{
    public class MonthlyUptimeDto
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int FailureCount { get; set; }
        public int TotalCount { get; set; }
        public double UptimePercentage { get; set; }
        public string DowntimeDuration { get; set; } = string.Empty;
    }
}
