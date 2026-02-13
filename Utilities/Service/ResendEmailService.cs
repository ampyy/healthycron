using System.Text;
using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace HealthyCron.Utilities.Service
{
    public class ResendEmailService : IEmailService
    {
        private readonly ILogger<ResendEmailService> _logger;
        private readonly string _apiKey;

        public ResendEmailService(
            IConfiguration configuration,
            ILogger<ResendEmailService> logger)
        {
            _logger = logger;

            // Get API key from environment variable
            _apiKey = configuration["RESEND_API_KEY"] ??
                      configuration["Resend:ApiKey"] ??
                      throw new InvalidOperationException("RESEND_API_KEY is not configured");
        }

        public async Task SendMagicLinkEmailAsync(string toEmail, string magicLink)
        {
            try
            {
                IResend resend = ResendClient.Create(_apiKey);

                var subject = "Log in to HealthyCron";
                var htmlBody = GetCleanMagicLinkHtml(magicLink);

                var message = new EmailMessage
                {
                    From = "HealthyCron <team@healthycron.com>",
                    To = toEmail,
                    Subject = subject,
                    HtmlBody = htmlBody
                };

                var response = await resend.EmailSendAsync(message);

                // The SDK might not throw on error response content, but usually throws on HTTP failure.
                // Assuming success if no exception for now. 

                _logger.LogInformation($"Magic link email sent successfully to {toEmail} via Resend.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending email to {toEmail} via Resend");
                throw;
            }
        }

        private string GetCleanMagicLinkHtml(string magicLink)
        {
            return $@"
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
        }
    }
}
