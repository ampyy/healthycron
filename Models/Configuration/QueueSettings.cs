using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for AWS SQS queue settings
    /// </summary>
    public class QueueSettings
    {
        public const string SectionName = "QueueSettings";

        /// <summary>
        /// AWS SQS queue URL for heartbeat messages
        /// </summary>
        [Required(ErrorMessage = "HeartbeatQueueUrl is required")]
        [Url(ErrorMessage = "HeartbeatQueueUrl must be a valid URL")]
        public string HeartbeatQueueUrl { get; set; } = string.Empty;
    }
}
