namespace HealthyCron.Utilities.Interface
{
    public interface IEmailService
    {
        Task SendMagicLinkEmailAsync(string toEmail, string magicLink);
    }
}
