using System.Net;
using System.Net.Mail;
using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Utilities.Service
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;

        public EmailService(EmailSettings emailSettings)
        {
            _smtpHost = emailSettings.SmtpHost;
            _smtpPort = emailSettings.SmtpPort;
            _fromEmail = emailSettings.FromEmail;
            _fromPassword = emailSettings.FromPassword;
        }

        public async Task SendMagicLinkEmailAsync(string toEmail, string magicLink)
        {
            var subject = "Your HealthyCron Magic Login Link";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #0D1117; color: #E5E5E5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: linear-gradient(135deg, rgba(22, 27, 34, 0.8), rgba(22, 27, 34, 0.6)); border: 1px solid rgba(48, 54, 61, 0.5); border-radius: 16px; padding: 40px; }}
        .logo {{ text-align: center; margin-bottom: 30px; }}
        .logo-icon {{ display: inline-block; width: 48px; height: 48px; background: linear-gradient(135deg, #4F94FF, #9B5DE5); border-radius: 12px; padding: 12px; }}
        h1 {{ color: #FFFFFF; font-size: 28px; margin-bottom: 16px; text-align: center; }}
        p {{ color: #8B949E; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #4F94FF, #9B5DE5); color: #FFFFFF; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 600; font-size: 16px; margin: 20px 0; }}
        .button:hover {{ background: linear-gradient(135deg, #6BA5FF, #AB6DF5); }}
        .link-box {{ background: rgba(48, 54, 61, 0.3); border: 1px solid rgba(48, 54, 61, 0.5); border-radius: 8px; padding: 16px; margin: 20px 0; word-break: break-all; }}
        .footer {{ text-align: center; color: #6E7681; font-size: 14px; margin-top: 40px; padding-top: 20px; border-top: 1px solid rgba(48, 54, 61, 0.5); }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>
            <div class='logo-icon'>⏰</div>
            <h1>HealthyCron</h1>
        </div>
        
        <p>Hi there!</p>
        <p>You requested a magic link to sign in to your HealthyCron account. Click the button below to log in instantly:</p>
        
        <div style='text-align: center;'>
            <a href='{magicLink}' class='button'>Sign in to HealthyCron</a>
        </div>
        
        <p>Or copy and paste this link into your browser:</p>
        <div class='link-box'>
            <a href='{magicLink}' style='color: #4F94FF; text-decoration: none;'>{magicLink}</a>
        </div>
        
        <p><strong>This link will expire in 15 minutes</strong> for security reasons.</p>
        
        <p>If you didn't request this email, you can safely ignore it.</p>
        
        <div class='footer'>
            <p>© 2026 HealthyCron. Never miss a job run.</p>
        </div>
    </div>
</body>
</html>";

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, "HealthyCron"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
