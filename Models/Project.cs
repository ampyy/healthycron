namespace HealthyCron.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MonitorCount { get; set; }
        public int InteractionsCount { get; set; }

        // Populated by join on users table when listing projects
        public string? OwnerEmail { get; set; }
    }
}
