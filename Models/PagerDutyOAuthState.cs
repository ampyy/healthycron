namespace HealthyCron.Models
{
    public class PagerDutyOAuthState
    {
        public string State { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
