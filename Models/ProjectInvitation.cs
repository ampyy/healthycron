using HealthyCron.Enums;

namespace HealthyCron.Models
{
    public class ProjectInvitation
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Email { get; set; } = string.Empty;
        public ProjectRole Role { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsAccepted => AcceptedAt.HasValue;
    }
}
