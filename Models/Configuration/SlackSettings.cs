using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    public class SlackSettings
    {
        public const string SectionName = "Slack";

        [Required(ErrorMessage = "Slack:ClientId is required")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slack:ClientSecret is required")]
        public string ClientSecret { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slack:RedirectUri is required")]
        public string RedirectUri { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slack:StateSecret is required")]
        public string StateSecret { get; set; } = string.Empty;
    }
}
