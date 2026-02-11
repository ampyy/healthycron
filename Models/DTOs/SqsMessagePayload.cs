using System;
using Newtonsoft.Json;

namespace HealthyCron.Models.DTOs
{
    public class SqsMessagePayload
    {
        [JsonProperty("job_id")]
        public Guid JobId { get; set; }

        [JsonProperty("monitor_id")]
        public Guid MonitorId { get; set; }

        [JsonProperty("integration_id")]
        public Guid IntegrationId { get; set; }
    }
}
