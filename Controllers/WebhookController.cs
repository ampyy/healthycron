using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using Microsoft.Extensions.Logging;

namespace HealthyCron.Controllers
{
    [ApiController]
    [Route("webhooks")]
    public class WebhookController : ControllerBase
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            IIntegrationRepository integrationRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<WebhookController> logger)
        {
            _integrationRepository = integrationRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("telegram")]
        [AllowAnonymous] // Telegram will call this directly without our app auth
        public async Task<IActionResult> TelegramWebhook([FromBody] JsonElement update)
        {
            try
            {
                if (!update.TryGetProperty("message", out var message))
                {
                    return Ok(); // Ignore non-message updates
                }

                if (!message.TryGetProperty("text", out var textEl))
                {
                    return Ok();
                }

                string text = textEl.GetString() ?? string.Empty;

                if (text.StartsWith("/start"))
                {
                    var chat = message.GetProperty("chat");
                    string chatId = chat.GetProperty("id").ToString();
                    string chatType = chat.GetProperty("type").GetString() ?? "unknown";
                    
                    string chatName = string.Empty;
                    if (chat.TryGetProperty("title", out var titleEl))
                    {
                        chatName = titleEl.GetString() ?? string.Empty;
                    }
                    else if (chat.TryGetProperty("first_name", out var fnEl))
                    {
                        chatName = fnEl.GetString() ?? string.Empty;
                        if (chat.TryGetProperty("last_name", out var lnEl))
                        {
                            chatName += " " + lnEl.GetString();
                        }
                    }

                    // Generate Handshake Token
                    string token = Guid.NewGuid().ToString("N");
                    var handshake = new TempTelegramHandshake
                    {
                        Token = token,
                        ChatId = chatId,
                        ChatName = chatName,
                        ChatType = chatType,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                    };

                    await _integrationRepository.CreateTempTelegramHandshakeAsync(handshake);

                    // Send Reply
                    string botToken = _configuration["Telegram:BotToken"] ?? _configuration["TELEGRAM_BOT_TOKEN"] ?? string.Empty;
                    string baseUrl = _configuration["APP_BASE_URL"] ?? $"{Request.Scheme}://{Request.Host}";
                    string messageText = $"Click here to connect to Healthycron:\n{baseUrl}/integrations/telegram/confirm?token={token}\n\nThis link expires in 10 minutes.";

                    var client = _httpClientFactory.CreateClient();
                    var payload = new
                    {
                        chat_id = chatId,
                        text = messageText,
                        disable_web_page_preview = true
                    };

                    await client.PostAsJsonAsync($"https://api.telegram.org/bot{botToken}/sendMessage", payload);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Telegram webhook");
                return Ok(); // Return 200 so Telegram doesn't retry endlessly on bad parses
            }
        }
    }
}
