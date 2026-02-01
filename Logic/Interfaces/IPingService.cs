using HealthyCron.Models;
using System;
using System.Threading.Tasks;

namespace HealthyCron.Logic.Interfaces
{
    public interface IPingService
    {
        Task ProcessPingAsync(Guid monitorId, string statusFromUrl, string? statusHeader, string? bodyJson, PingMetadata metadata);
        Task ProcessPingBySlugAsync(string pingKey, string slug, string statusFromUrl, string? statusHeader, string? bodyJson, PingMetadata metadata);
    }

    public class PingMetadata
    {
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string Method { get; set; } = "GET";
        public string? HeadersJson { get; set; }
        public int ResponseTimeMs { get; set; }
    }
}
