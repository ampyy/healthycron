namespace HealthyCron.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Color { get; set; } = "blue";
        public string Icon { get; set; } = "folder";
        public DateTime CreatedAt { get; set; }
        public int MonitorCount { get; set; }
        public int InteractionsCount { get; set; }
    }
}
