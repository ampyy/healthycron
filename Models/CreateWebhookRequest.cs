namespace HealthyCron.Models
{
    public class CreateWebhookRequest
    {
        public Guid ProjectId { get; set; }
        public string? Name { get; set; }

        // DOWN
        public string DownMethod { get; set; } = "POST";
        public string DownUrl { get; set; } = string.Empty;
        public string? DownHeaders { get; set; }
        public string? DownBody { get; set; }

        // UP (all nullable — omit for "don't fire on recovery")
        public string? UpMethod { get; set; }
        public string? UpUrl { get; set; }
        public string? UpHeaders { get; set; }
        public string? UpBody { get; set; }
    }
}
