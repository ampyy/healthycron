namespace HealthyCron.Models.Configuration
{
    public class InternalApiSettings
    {
        public const string SectionName = "InternalApi";

        public string AuthToken { get; set; } = string.Empty;
    }
}
