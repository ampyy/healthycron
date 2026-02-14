namespace HealthyCron.Models
{
    public class PagerDutyOAuthSession
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
