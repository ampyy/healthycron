namespace HealthyCron.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; }
        public string? PasswordHash { get; set; }

        // Settings
        public string? Timezone { get; set; }
        public bool ReceiveWeeklyReports { get; set; } = true;
        public bool ReceiveMonthlyReports { get; set; } = true;
        public bool ReceiveIncidentReminders { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
