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
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background-color: #f9fafb;
            color: #111827;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .container {{
            max-width: 480px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            padding: 40px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        }}
        .header {{
            margin-bottom: 32px;
        }}
        .logo {{
            font-size: 24px;
            font-weight: 700;
            color: #111827;
            text-decoration: none;
            letter-spacing: -0.5px;
        }}
        h1 {{
            font-size: 20px;
            font-weight: 600;
            margin-bottom: 24px;
            color: #111827;
        }}
        p {{
            color: #4b5563;
            font-size: 16px;
            margin-bottom: 24px;
        }}
        .btn {{
            display: inline-block;
            background-color: #111827;
            color: #ffffff;
            font-size: 15px;
            font-weight: 500;
            text-decoration: none;
            padding: 12px 24px;
            border-radius: 6px;
            text-align: center;
        }}
        .btn:hover {{
            background-color: #000000;
        }}
        .link-text {{
            font-size: 14px;
            color: #6b7280;
            margin-top: 32px;
            word-break: break-all;
        }}
        .footer {{
            margin-top: 40px;
            font-size: 13px;
            color: #9ca3af;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <span class='logo'>HealthyCron</span>
        </div>
        
        <h1>Log in to your account</h1>
        
        <p>Welcome back! Use the link below to sign in directly to your dashboard.</p>
        
        <div style='margin: 32px 0;'>
            <a href='{magicLink}' class='btn'>Sign in to HealthyCron</a>
        </div>
        
        <p class='link-text'>
            Or paste this link into your browser:<br>
            <a href='{magicLink}' style='color: #4b5563;'>{magicLink}</a>
        </p>

        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} HealthyCron. All rights reserved.
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
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f9fafb; color: #111827; margin: 0; padding: 0; line-height: 1.6; }}
        .container {{ max-width: 480px; margin: 40px auto; background-color: #ffffff; border-radius: 8px; padding: 40px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }}
        .logo {{ font-size: 24px; font-weight: 700; color: #111827; letter-spacing: -0.5px; }}
        h1 {{ font-size: 20px; font-weight: 600; margin-bottom: 8px; color: #111827; }}
        p {{ color: #4b5563; font-size: 16px; margin-bottom: 16px; }}
        .badge {{ display: inline-block; background: #f3f4f6; border-radius: 4px; padding: 4px 10px; font-size: 13px; font-weight: 600; color: #374151; margin-bottom: 24px; }}
        .btn {{ display: inline-block; background-color: #111827; color: #ffffff; font-size: 15px; font-weight: 500; text-decoration: none; padding: 12px 24px; border-radius: 6px; }}
        .link-text {{ font-size: 13px; color: #6b7280; margin-top: 24px; word-break: break-all; }}
        .footer {{ margin-top: 40px; font-size: 13px; color: #9ca3af; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div style='margin-bottom: 32px'><span class='logo'>HealthyCron</span></div>
        <h1>You're invited to join {projectName}</h1>
        <p><strong>{inviterEmail}</strong> has invited you to collaborate on <strong>{projectName}</strong>.</p>
        <div class='badge'>Role: {role}</div>
        <div style='margin: 24px 0;'>
            <a href='{acceptUrl}' class='btn'>Accept Invitation</a>
        </div>
        <p class='link-text'>Or paste this link:<br><a href='{acceptUrl}' style='color:#4b5563'>{acceptUrl}</a></p>
        <p style='color:#9ca3af;font-size:13px'>This invitation expires in 7 days. If you weren't expecting this, you can ignore this email.</p>
        <div class='footer'>&copy; {DateTime.UtcNow.Year} HealthyCron. All rights reserved.</div>
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
