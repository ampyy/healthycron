namespace HealthyCron.Utilities.Interface
{
    public interface IEmailService
    {
        Task SendMagicLinkEmailAsync(string toEmail, string magicLink);
        Task SendInviteEmailAsync(string toEmail, string projectName, string inviterEmail, string role, string acceptUrl);
    }
}
