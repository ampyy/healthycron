using HealthyCron.Enums;

namespace HealthyCron.Models
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public ProjectRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        // Joined from users table when listing members
        public string? UserEmail { get; set; }

        // Populated when joining with projects table (global teams view)
        public string? ProjectName { get; set; }
        public string? ProjectSlug { get; set; }
    }
}
