namespace HealthyCron.Models.DTOs
{
    public class ApiCheckCreateModel
    {
        public string? name { get; set; }
        public string? slug { get; set; }
        public string? tags { get; set; }
        public string? desc { get; set; }
        public int? timeout { get; set; }
        public int? grace { get; set; }
        public string? schedule { get; set; }
        public string? tz { get; set; }
        public string? channels { get; set; }
    }
}
