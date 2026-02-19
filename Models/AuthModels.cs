namespace HealthyCron.Models
{
    public class MagicLinkRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    public class TokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class PasswordAuthRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
