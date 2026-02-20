using System.Net;
using System.Net.Mail;
using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Utilities.Service
{
    public class SmtpEmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;

        public SmtpEmailService(EmailSettings emailSettings)
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
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #ffffff; color: #111827; margin: 0; padding: 0; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #ffffff; padding-bottom: 40px; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
        .logo {{ font-size: 20px; font-weight: 800; color: #111827; letter-spacing: -0.5px; margin-bottom: 48px; display: block; text-decoration: none; }}
        h1 {{ font-size: 24px; font-weight: 700; margin-bottom: 16px; color: #111827; letter-spacing: -0.02em; }}
        p {{ color: #4b5563; font-size: 16px; line-height: 24px; margin-bottom: 24px; }}
        .btn {{ display: inline-block; background-color: #4F94FF; color: #ffffff !important; font-size: 15px; font-weight: 600; text-decoration: none; padding: 12px 28px; border-radius: 10px; transition: all 0.2s; }}
        .footer {{ margin-top: 64px; padding-top: 24px; border-top: 1px solid #f3f4f6; font-size: 13px; color: #9ca3af; }}
        .link-alt {{ font-size: 12px; color: #9ca3af; margin-top: 32px; word-break: break-all; }}
    </style>
</head>
<body>
    <div class='wrapper'>
        <div class='container'>
            <a href='#' class='logo'>HealthyCron</a>
            <h1>Log in to HealthyCron</h1>
            <p>Click the button below to sign in securely. This link will expire in 15 minutes.</p>
            <div style='margin: 32px 0;'>
                <a href='{magicLink}' class='btn'>Sign in to Dashboard</a>
            </div>
            <p class='link-alt'>
                Trouble with the button? Copy and paste this link:<br>
                <span style='color: #4F94FF;'>{magicLink}</span>
            </p>
            <div class='footer'>
                &copy; {{DateTime.UtcNow.Year}} HealthyCron. simplest cron job monitoring.
            </div>
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

        public async Task SendInviteEmailAsync(string toEmail, string projectName, string inviterEmail, string role, string acceptUrl)
        {
            var subject = $"You've been invited to {projectName} on HealthyCron";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #ffffff; color: #111827; margin: 0; padding: 0; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #ffffff; padding-bottom: 40px; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
        .logo {{ font-size: 20px; font-weight: 800; color: #111827; letter-spacing: -0.5px; margin-bottom: 48px; display: block; text-decoration: none; }}
        h1 {{ font-size: 24px; font-weight: 700; margin-bottom: 16px; color: #111827; letter-spacing: -0.02em; }}
        p {{ color: #4b5563; font-size: 16px; line-height: 24px; margin-bottom: 24px; }}
        .card {{ background: #f9fafb; border: 1px solid #f3f4f6; border-radius: 12px; padding: 24px; margin-bottom: 32px; }}
        .btn {{ display: inline-block; background-color: #4F94FF; color: #ffffff !important; font-size: 15px; font-weight: 600; text-decoration: none; padding: 12px 28px; border-radius: 10px; }}
        .footer {{ margin-top: 64px; padding-top: 24px; border-top: 1px solid #f3f4f6; font-size: 13px; color: #9ca3af; }}
    </style>
</head>
<body>
    <div class='wrapper'>
        <div class='container'>
            <a href='#' class='logo'>HealthyCron</a>
            <h1>You've been invited</h1>
            <p><strong>{inviterEmail}</strong> has invited you to collaborate on <strong>{projectName}</strong>.</p>
            <div class='card'>
                <p style='margin-bottom: 0;'><strong>Role:</strong> {role}</p>
            </div>
            <div style='margin: 32px 0;'>
                <a href='{acceptUrl}' class='btn'>Accept Invitation</a>
            </div>
            <p style='font-size: 13px; color: #9ca3af;'>This invitation expires in 7 days. If you weren't expecting this, you can ignore this email.</p>
            <div class='footer'>
                &copy; {{DateTime.UtcNow.Year}} HealthyCron. simplest cron job monitoring.
            </div>
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
