using HealthyCron.Enums;

namespace HealthyCron.Models
{
    public class Integration
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public IntegrationType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
