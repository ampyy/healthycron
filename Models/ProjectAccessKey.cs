using System;

namespace HealthyCron.Models
{
    public enum ApiKeyType
    {
        Ping = 0,
        FullAccess = 1,
        ReadAccess = 2
    }

    public class ProjectAccessKey
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public ApiKeyType KeyType { get; set; }
        public string KeyHash { get; set; } = string.Empty;
        public string KeyPrefix { get; set; } = string.Empty;
        public string? PlaintextKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
