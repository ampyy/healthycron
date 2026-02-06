using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    public class EncryptionSettings
    {
        public const string SectionName = "Encryption";

        [Required(ErrorMessage = "Encryption:Key is required")]
        public string Key { get; set; } = string.Empty;
    }
}
