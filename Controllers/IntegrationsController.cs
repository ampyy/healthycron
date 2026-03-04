using HealthyCron.Data.Interfaces;
using HealthyCron.Enums;
using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Logic.Service;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Microsoft.Extensions.Options;
using HealthyCron.Models.Configuration;

namespace HealthyCron.Controllers
{
    [Auth]
    public class IntegrationsController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMonitorRepository _monitorRepository;
        private readonly ISlackOAuthService _slackOAuthService;
        private readonly IPagerDutyService _pagerDutyService;
        private readonly IEncryptionService _encryptionService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly AxiomLogger _axiomLogger;
        private readonly IProjectAuthService _projectAuth;
        private readonly IQueueService _queueService;

        public IntegrationsController(
            IProjectRepository projectRepository,
            IIntegrationRepository integrationRepository,
            IMonitorRepository monitorRepository,
            ISlackOAuthService slackOAuthService,
            IPagerDutyService pagerDutyService,
            IEncryptionService encryptionService,
            ICacheService cacheService,
            IConfiguration configuration,
            AxiomLogger axiomLogger,
            IProjectAuthService projectAuth,
            IQueueService queueService)
        {
            _projectRepository = projectRepository;
            _integrationRepository = integrationRepository;
            _monitorRepository = monitorRepository;
            _slackOAuthService = slackOAuthService;
            _pagerDutyService = pagerDutyService;
            _encryptionService = encryptionService;
            _cacheService = cacheService;
            _configuration = configuration;
            _axiomLogger = axiomLogger;
            _projectAuth = projectAuth;
            _queueService = queueService;
        }

        [HttpGet("project/{slug}/integrations")]
        public async Task<IActionResult> Index(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanViewProjectAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var canManage = await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id);

            var integrations = await _integrationRepository.GetIntegrationsByProjectIdAsync(project.Id);

            // Get integration details for each integration
            var integrationsWithDetails = new List<HealthyCron.Models.ViewModels.IntegrationListItemViewModel>();
            foreach (var integration in integrations)
            {
                if (integration.Type == IntegrationType.Slack)
                {
                    var slackDetails = await _integrationRepository.GetSlackIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        SlackDetails = slackDetails
                    });
                }
                else if (integration.Type == IntegrationType.Teams)
                {
                    var teamsDetails = await _integrationRepository.GetTeamsIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        TeamsDetails = teamsDetails
                    });
                }
                else if (integration.Type == IntegrationType.GoogleChat)
                {
                    var googleChatDetails = await _integrationRepository.GetGoogleChatIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        GoogleChatDetails = googleChatDetails
                    });
                }
                else if (integration.Type == IntegrationType.Discord)
                {
                    var discordDetails = await _integrationRepository.GetDiscordIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        DiscordDetails = discordDetails
                    });
                }
                else if (integration.Type == IntegrationType.Email)
                {
                    var emailDetails = await _integrationRepository.GetEmailIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        EmailDetails = emailDetails
                    });
                }
                else if (integration.Type == IntegrationType.PagerDuty)
                {
                    var pagerDutyDetails = await _integrationRepository.GetPagerDutyIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        PagerDutyDetails = pagerDutyDetails
                    });
                }
                else if (integration.Type == IntegrationType.Telegram)
                {
                    var telegramDetails = await _integrationRepository.GetTelegramIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        TelegramDetails = telegramDetails
                    });
                }
                else if (integration.Type == IntegrationType.Pushover)
                {
                    var pushoverDetails = await _integrationRepository.GetPushoverIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        PushoverDetails = pushoverDetails
                    });
                }
                else if (integration.Type == IntegrationType.Spike)
                {
                    var spikeDetails = await _integrationRepository.GetSpikeIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        SpikeDetails = spikeDetails
                    });
                }
                else if (integration.Type == IntegrationType.Webhook)
                {
                    var webhookDetails = await _integrationRepository.GetWebhookIntegrationByIntegrationIdAsync(integration.Id);
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        WebhookDetails = webhookDetails
                    });
                }
                else
                {
                    integrationsWithDetails.Add(new HealthyCron.Models.ViewModels.IntegrationListItemViewModel
                    {
                        Integration = integration,
                        SlackDetails = null
                    });
                }
            }

            ViewBag.Project = project;
            ViewBag.User = user;
            ViewBag.Integrations = integrationsWithDetails;
            ViewBag.CanManage = canManage;

            return View();
        }

        // ─── TELEGRAM ──────────────────────────────────────────────────────────

        [HttpGet("project/{slug}/integrations/telegram/add")]
        public async Task<IActionResult> TelegramAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();
            ViewBag.Project = project;
            ViewBag.User = user;
            return View();
        }

        [HttpGet("integrations/telegram/confirm")]
        public async Task<IActionResult> TelegramConfirm([FromQuery] string token)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");
            
            ViewBag.Token = token;
            ViewBag.User = user;

            // Get user's projects for the dropdown
            var projects = await _projectRepository.GetProjectsByUserIdAsync(user.Id);
            ViewBag.Projects = projects;
            
            return View();
        }

        [HttpGet("api/v1/integrations/telegram/confirm-info")]
        public async Task<IActionResult> TelegramConfirmInfo([FromQuery] string token)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            var handshake = await _integrationRepository.GetTempTelegramHandshakeAsync(token);
            if (handshake == null || handshake.ExpiresAt < DateTime.UtcNow || handshake.UsedAt != null)
                return NotFound(new { error = "Link expired. Please send /start to the bot again." });

            return Ok(new
            {
                chat_id = handshake.ChatId,
                chat_name = handshake.ChatName,
                chat_type = handshake.ChatType,
                token = handshake.Token
            });
        }

        [HttpPost("api/v1/integrations/telegram/confirm")]
        public async Task<IActionResult> TelegramConfirmSubmit([FromBody] System.Text.Json.JsonElement body)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            if (!body.TryGetProperty("token", out var tokenEl) || 
                !body.TryGetProperty("project_id", out var projectIdEl) || 
                !body.TryGetProperty("name", out var nameEl))
                return BadRequest("Missing required fields");

            string token = tokenEl.GetString() ?? "";
            Guid projectId = Guid.Parse(projectIdEl.GetString() ?? "");
            string name = nameEl.GetString() ?? "Telegram";

            if (!await _projectAuth.CanManageMonitorsAsync(projectId, user.Id, user.Id))
                return Forbid();

            var handshake = await _integrationRepository.GetTempTelegramHandshakeAsync(token);
            if (handshake == null || handshake.ExpiresAt < DateTime.UtcNow || handshake.UsedAt != null)
                return BadRequest("Token is invalid, expired, or already used.");

            var project = await _projectRepository.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound("Project not found");

            // Create Integration
            var integration = new Integration
            {
                ProjectId = projectId,
                Type = IntegrationType.Telegram,
                Name = string.IsNullOrWhiteSpace(name) ? $"Telegram - {handshake.ChatName}" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create TelegramIntegration
            await _integrationRepository.CreateTelegramIntegrationAsync(new TelegramIntegration
            {
                IntegrationId = integrationId,
                ChatId = handshake.ChatId,
                ChatName = handshake.ChatName,
                ChatType = handshake.ChatType,
                ConfirmedAt = DateTime.UtcNow
            });

            // Mark handshake as used (audit trail)
            await _integrationRepository.MarkTempTelegramHandshakeUsedAsync(token);

            // Send Confirmation
            var botToken = _configuration["TELEGRAM_BOT_TOKEN"] ?? _configuration["Telegram:BotToken"];
            if (!string.IsNullOrEmpty(botToken))
            {
                try
                {
                    using var http = new System.Net.Http.HttpClient();
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        chat_id = handshake.ChatId,
                        text = $"✅ Connected to Healthycron project '{project.Name}'! You'll receive monitor alerts here."
                    });
                    await http.PostAsync($"https://api.telegram.org/bot{botToken}/sendMessage", 
                        new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
                }
                catch { }
            }

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(projectId);
            foreach (var monitor in monitors)
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);

            return Ok(new { integration_id = integrationId, chat_name = handshake.ChatName });
        }

        // ─── PUSHOVER ──────────────────────────────────────────────────────────

        [HttpGet("project/{slug}/integrations/pushover/add")]
        public async Task<IActionResult> PushoverAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();
            ViewBag.Project = project;
            ViewBag.User = user;
            return View();
        }

        [HttpGet("api/v1/integrations/pushover/subscribe-url")]
        public async Task<IActionResult> PushoverSubscribeUrl([FromQuery] Guid project_id)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Unauthorized();

            if (!await _projectAuth.CanManageMonitorsAsync(project_id, user.Id, user.Id))
                return Forbid();

            string randomToken = Guid.NewGuid().ToString("N");
            
            // Store the token → projectId in the DB (not Redis) so it survives process restarts
            await _integrationRepository.CreatePushoverPendingSubscriptionAsync(new PushoverPendingSubscription
            {
                Token = randomToken,
                ProjectId = project_id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            
            // Always use the inbound request's hostname so redirects work smoothly through ngrok tunnels
            var requestHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;
            var requestScheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            string baseUrl = $"{requestScheme}://{requestHost}";

            // The user has "Expect OAuth2 parameters" checked, so we use `redirect_uri` and `state`
            string redirectUrl = $"{baseUrl}/integrations/pushover/callback";
            string subscriptionCode = _configuration["PUSHOVER_SUBSCRIPTION_CODE"];

            Console.WriteLine($"[PushoverConfig] Generated redirect URL: {redirectUrl}");

            // For Web-based subscriptions with OAuth2 params enabled
            string url = $"https://pushover.net/subscribe/{subscriptionCode}" +
                         $"?redirect_uri={HttpUtility.UrlEncode(redirectUrl)}" +
                         $"&state={randomToken}";

            return Ok(new { url = url });
        }

        [HttpGet("integrations/pushover/callback")]
        public async Task<IActionResult> PushoverCallback(
            [FromQuery] string? pushover_user_key, 
            [FromQuery] string? code,
            [FromQuery] string? device, 
            [FromQuery] string? sound,
            [FromQuery] string? state)
        {
            var rawQuery = string.Join("&", HttpContext.Request.Query.Select(q => $"{q.Key}={q.Value}"));
            Console.WriteLine($"[PushoverCallback] Raw Query received: {rawQuery}");

            // Pushover sends 'code' instead of 'pushover_user_key' when "Expect OAuth2 parameters" is checked.
            var subscriptionKey = pushover_user_key ?? code;

            var user = HttpContext.Items["User"] as User;
            if (user == null) {
                Console.WriteLine("[PushoverCallback] User is null, redirecting to login.");
                return Redirect("/login");
            }

            if (string.IsNullOrEmpty(subscriptionKey)) {
                Console.WriteLine("[PushoverCallback] Missing subscription key.");
                return BadRequest($"Missing user key or code parameter from Pushover callback. Received: {rawQuery}");
            }

            if (string.IsNullOrEmpty(state)) {
                Console.WriteLine("[PushoverCallback] State is empty.");
                return BadRequest("Missing state parameter from Pushover callback.");
            }

            // Look up project via DB-backed pending subscription record
            var pending = await _integrationRepository.GetPushoverPendingSubscriptionAsync(state);
            if (pending == null) {
                Console.WriteLine($"[PushoverCallback] Pending subscription not found for state: {state}");
                return BadRequest("Pushover subscription session expired or already used. Please try again.");
            }
            if (pending.ExpiresAt < DateTime.UtcNow) {
                Console.WriteLine($"[PushoverCallback] Pending subscription expired at {pending.ExpiresAt}");
                return BadRequest("Pushover subscription session expired or already used. Please try again.");
            }
            if (pending.UsedAt != null) {
                Console.WriteLine($"[PushoverCallback] Pending subscription already used at {pending.UsedAt}");
                return BadRequest("Pushover subscription session expired or already used. Please try again.");
            }

            var project = await _projectRepository.GetProjectByIdAsync(pending.ProjectId);
            if (project == null) {
                Console.WriteLine($"[PushoverCallback] Project {pending.ProjectId} not found");
                return NotFound("Project not found");
            }

            Console.WriteLine($"[PushoverCallback] Found pending subscription for project {project.Name}. Creating integration...");

            var integration = new Integration
            {
                ProjectId = pending.ProjectId,
                Type = IntegrationType.Pushover,
                Name = "Pushover",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            await _integrationRepository.CreatePushoverIntegrationAsync(new PushoverIntegration
            {
                IntegrationId = integrationId,
                SubscriptionKey = subscriptionKey,
                Device = device,
                Sound = sound
            });

            // Mark subscription as used (prevents replay)
            await _integrationRepository.MarkPushoverPendingSubscriptionUsedAsync(state);

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(pending.ProjectId);
            foreach (var monitor in monitors)
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);

            return Redirect($"/project/{project.Slug}/integrations?success=pushover");
        }

        [HttpGet("integrations/pushover/failed")]
        public async Task<IActionResult> PushoverFailed([FromQuery] string? state)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return Redirect("/login");
            
            if (!string.IsNullOrEmpty(state))
            {
                var pending = await _integrationRepository.GetPushoverPendingSubscriptionAsync(state);
                if (pending != null)
                {
                    var project = await _projectRepository.GetProjectByIdAsync(pending.ProjectId);
                    if (project != null) return Redirect($"/project/{project.Slug}/integrations?error=pushover_cancelled");
                }
            }

            return Redirect("/dashboard");
        }

        // ─── SPIKE.SH ──────────────────────────────────────────────────────────

        [HttpGet("project/{slug}/integrations/spike/add")]
        public async Task<IActionResult> SpikeAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();
            ViewBag.Project = project;
            ViewBag.User = user;
            return View();
        }

        [HttpPost("project/{slug}/integrations/spike")]
        public async Task<IActionResult> SpikeCreate(string slug,
            [FromForm] string webhookUrl,
            [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                TempData["Error"] = "Webhook URL is required";
                return RedirectToAction("SpikeAdd", new { slug });
            }

            if (!webhookUrl.StartsWith("https://hooks.spike.sh/", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Webhook URL must start with https://hooks.spike.sh/";
                return RedirectToAction("SpikeAdd", new { slug });
            }

            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.Spike,
                Name = string.IsNullOrWhiteSpace(name) ? "Spike.sh" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            await _integrationRepository.CreateSpikeIntegrationAsync(new HealthyCron.Models.SpikeIntegration
            {
                IntegrationId = integrationId,
                WebhookUrl = webhookUrl.Trim()
            });

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);

            return RedirectToAction("Index", new { slug = project.Slug });
        }



        // ─── WEBHOOK ───────────────────────────────────────────────────────────

        [HttpGet("project/{slug}/integrations/webhook/add")]
        public async Task<IActionResult> WebhookAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            ViewBag.Project = project;
            ViewBag.User = user;
            return View();
        }

        [HttpPost("project/{slug}/integrations/webhook")]
        public async Task<IActionResult> WebhookCreate(string slug,
            [FromForm] string? downUrl,
            [FromForm] string? downMethod,
            [FromForm] string? downHeaders,
            [FromForm] string? downBody,
            [FromForm] string? upUrl,
            [FromForm] string? upMethod,
            [FromForm] string? upHeaders,
            [FromForm] string? upBody,
            [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            if (string.IsNullOrWhiteSpace(downUrl) && string.IsNullOrWhiteSpace(upUrl))
            {
                TempData["Error"] = "At least one URL (Down or Up) is required";
                return RedirectToAction("WebhookAdd", new { slug });
            }
            if (!string.IsNullOrWhiteSpace(downUrl) && !Uri.TryCreate(downUrl, UriKind.Absolute, out _))
            {
                TempData["Error"] = "Down URL must be a valid URL";
                return RedirectToAction("WebhookAdd", new { slug });
            }

            // DOWN is optional if UP is set
            string? normalizedDownMethod = null;
            string? normalizedDownUrl = null;
            if (!string.IsNullOrWhiteSpace(downUrl))
            {
                normalizedDownMethod = (downMethod ?? "POST").ToUpper();
                normalizedDownUrl = downUrl.Trim();
            }

            // UP is entirely optional — only validate if URL is provided
            string? normalizedUpMethod = null;
            string? normalizedUpUrl = null;
            if (!string.IsNullOrWhiteSpace(upUrl))
            {
                if (!Uri.TryCreate(upUrl, UriKind.Absolute, out _))
                {
                    TempData["Error"] = "Up URL must be a valid URL";
                    return RedirectToAction("WebhookAdd", new { slug });
                }
                normalizedUpMethod = (upMethod ?? "POST").ToUpper();
                normalizedUpUrl = upUrl.Trim();
            }

            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.Webhook,
                Name = string.IsNullOrWhiteSpace(name) ? "Webhook" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            await _integrationRepository.CreateWebhookIntegrationAsync(new WebhookIntegration
            {
                IntegrationId = integrationId,
                DownMethod = normalizedDownMethod,
                DownUrl = normalizedDownUrl,
                DownHeaders = string.IsNullOrWhiteSpace(downHeaders) ? null : downHeaders,
                DownBody = string.IsNullOrWhiteSpace(downBody) ? null : downBody,
                UpMethod = normalizedUpMethod,
                UpUrl = normalizedUpUrl,
                UpHeaders = string.IsNullOrWhiteSpace(upHeaders) ? null : upHeaders,
                UpBody = string.IsNullOrWhiteSpace(upBody) ? null : upBody
            });

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpPost("integrations/{id}/test-webhook")]
        public async Task<IActionResult> TestWebhook(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null) return NotFound();
            var project = await _projectRepository.GetProjectByIdAsync(integration.ProjectId);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return StatusCode(403);

            var wh = await _integrationRepository.GetWebhookIntegrationByIntegrationIdAsync(id);
            if (wh == null) return NotFound(new { error = "Webhook details not found" });

            // Replace placeholders with test values
            string Replace(string? s) => (s ?? string.Empty)
                .Replace("$NAME", "Test Monitor")
                .Replace("$STATUS", "down")
                .Replace("$PROJECT", project.Name)
                .Replace("$MONITOR_ID", "00000000-0000-0000-0000-000000000000")
                .Replace("$TIMESTAMP", DateTime.UtcNow.ToString("o"));

            var url = Replace(wh.DownUrl);
            var method = wh.DownMethod?.ToUpper() ?? "POST";
            var body = Replace(wh.DownBody);
            var headers = wh.DownHeaders;

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var request = new HttpRequestMessage(new HttpMethod(method), url);
                if (!string.IsNullOrEmpty(headers))
                {
                    foreach (var line in headers.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var colon = line.IndexOf(':');
                        if (colon > 0)
                            request.Headers.TryAddWithoutValidation(line[..colon].Trim(), line[(colon + 1)..].Trim());
                    }
                }
                if ((method == "POST" || method == "PUT") && !string.IsNullOrEmpty(body))
                    request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

                var response = await http.SendAsync(request);
                var statusCode = (int)response.StatusCode;
                var success = statusCode is >= 200 and < 300;
                return Ok(new { success, status_code = statusCode, message = success ? $"Test sent! Status {statusCode}" : $"Got status {statusCode}" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, error = ex.Message });
            }
        }


        [HttpGet("project/{slug}/integrations/slack/authorize")]
        public async Task<IActionResult> SlackAuthorize(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var authUrl = _slackOAuthService.GenerateAuthorizationUrl(project.Id);
            return Redirect(authUrl);
        }

        [HttpGet("integrations/slack/callback")]
        public async Task<IActionResult> SlackCallback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return BadRequest("Missing code or state parameter");
            }

            // Extract project ID from state
            var stateParts = state.Split(':');
            if (stateParts.Length != 2 || !Guid.TryParse(stateParts[0], out var projectId))
            {
                return BadRequest("Invalid state parameter");
            }

            // Validate state
            if (!_slackOAuthService.ValidateState(state, projectId))
            {
                return BadRequest("Invalid state parameter");
            }

            // Exchange code for token
            var oauthResponse = await _slackOAuthService.ExchangeCodeForTokenAsync(code);

            if (!oauthResponse.Ok || string.IsNullOrEmpty(oauthResponse.AccessToken))
            {
                await _axiomLogger.LogError("Slack OAuth failed", new Dictionary<string, object>
                {
                    ["error"] = oauthResponse.Error ?? "Unknown error",
                    ["project_id"] = projectId
                });
                return BadRequest($"Slack OAuth failed: {oauthResponse.Error}");
            }

            // Encrypt bot token
            var encryptedToken = _encryptionService.Encrypt(oauthResponse.AccessToken);

            // Create integration record
            var integration = new Integration
            {
                ProjectId = projectId,
                Type = IntegrationType.Slack,
                Name = $"Slack - {oauthResponse.IncomingWebhook?.Channel ?? "Unknown Channel"}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create Slack integration record
            var slackIntegration = new SlackIntegration
            {
                IntegrationId = integrationId,
                WorkspaceId = oauthResponse.Team?.Id ?? "",
                ChannelId = oauthResponse.IncomingWebhook?.ChannelId ?? "",
                ChannelName = oauthResponse.IncomingWebhook?.Channel ?? "",
                EncryptedBotToken = encryptedToken,
                WorkspaceName = oauthResponse.Team?.Name ?? "",
                AppId = oauthResponse.AppId,
                WebhookUrl = oauthResponse.IncomingWebhook?.Url,
                CreatedAt = DateTime.UtcNow
            };

            await _integrationRepository.CreateSlackIntegrationAsync(slackIntegration);

            // Auto-enable for all project monitors
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(projectId);
            foreach (var monitor in monitors)
            {
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);
            }

            await _axiomLogger.LogInfo("Slack integration created", new Dictionary<string, object>
            {
                ["project_id"] = projectId,
                ["workspace_name"] = oauthResponse.Team?.Name ?? "",
                ["channel_name"] = oauthResponse.IncomingWebhook?.Channel ?? ""
            });

            // Get project slug for redirect
            var project = await _projectRepository.GetProjectByIdAsync(projectId);
            if (project == null)
            {
                return BadRequest("Project not found");
            }

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpGet("project/{slug}/integrations/teams/add")]
        public async Task<IActionResult> TeamsAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/teams")]
        public async Task<IActionResult> TeamsCreate(string slug, [FromForm] string webhookUrl, [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            // Validate webhook URL
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                TempData["Error"] = "Webhook URL is required";
                return RedirectToAction("TeamsAdd", new { slug });
            }

            // Validate HTTPS
            if (!webhookUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Webhook URL must use HTTPS";
                return RedirectToAction("TeamsAdd", new { slug });
            }

            // Validate Teams webhook URL format - support both legacy and new formats
            // Legacy format: https://outlook.office.com/webhook/...
            // New PowerPlatform format: https://*.api.powerplatform.com/.../triggers/manual/paths/invoke?...&sig=...
            var isLegacyFormat = System.Text.RegularExpressions.Regex.IsMatch(
                webhookUrl, 
                @"^https:\/\/outlook\.office\.com\/webhook\/.+$", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            var isNewFormat = System.Text.RegularExpressions.Regex.IsMatch(
                webhookUrl,
                @"^https:\/\/.*\.api\.powerplatform\.com(:\d+)?\/.*\/triggers\/manual\/paths\/invoke.*sig=.*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (!isLegacyFormat && !isNewFormat)
            {
                TempData["Error"] = "Invalid Microsoft Teams webhook URL. Must be either:\n" +
                    "• Legacy format: https://outlook.office.com/webhook/...\n" +
                    "• Power Automate format: https://*.api.powerplatform.com/.../triggers/manual/paths/invoke?...&sig=...";
                return RedirectToAction("TeamsAdd", new { slug });
            }

            // Create integration record
            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.Teams,
                Name = string.IsNullOrWhiteSpace(name) ? "Microsoft Teams" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create Teams integration record
            var teamsIntegration = new TeamsIntegration
            {
                IntegrationId = integrationId,
                WebhookUrl = webhookUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _integrationRepository.CreateTeamsIntegrationAsync(teamsIntegration);

            // Auto-enable for all project monitors
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
            {
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);
            }

            await _axiomLogger.LogInfo("Teams integration created", new Dictionary<string, object>
            {
                ["project_id"] = project.Id,
                ["integration_name"] = integration.Name
            });

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpGet("project/{slug}/integrations/googlechat/add")]
        public async Task<IActionResult> GoogleChatAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/googlechat")]
        public async Task<IActionResult> GoogleChatCreate(string slug, [FromForm] string webhookUrl, [FromForm] string? name, [FromForm] string? spaceName)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            // Validate webhook URL
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                TempData["Error"] = "Webhook URL is required";
                return RedirectToAction("GoogleChatAdd", new { slug });
            }

            // Validate HTTPS
            if (!webhookUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Webhook URL must use HTTPS";
                return RedirectToAction("GoogleChatAdd", new { slug });
            }

            // Validate Google Chat webhook URL format
            // Must contain: chat.googleapis.com, /spaces/, key=, token=
            if (!webhookUrl.Contains("chat.googleapis.com", StringComparison.OrdinalIgnoreCase) ||
                !webhookUrl.Contains("/spaces/", StringComparison.OrdinalIgnoreCase) ||
                !webhookUrl.Contains("key=", StringComparison.OrdinalIgnoreCase) ||
                !webhookUrl.Contains("token=", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Invalid Google Chat webhook URL. Must contain chat.googleapis.com, /spaces/, key=, and token=";
                return RedirectToAction("GoogleChatAdd", new { slug });
            }

            // Create integration record
            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.GoogleChat,
                Name = string.IsNullOrWhiteSpace(name) ? "Google Chat" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create Google Chat integration record
            var googleChatIntegration = new GoogleChatIntegration
            {
                IntegrationId = integrationId,
                WebhookUrl = webhookUrl,
                SpaceName = spaceName,
                CreatedAt = DateTime.UtcNow
            };

            await _integrationRepository.CreateGoogleChatIntegrationAsync(googleChatIntegration);

            // Auto-enable for all project monitors
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
            {
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);
            }

            await _axiomLogger.LogInfo("Google Chat integration created", new Dictionary<string, object>
            {
                ["project_id"] = project.Id,
                ["integration_name"] = integration.Name
            });

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpGet("project/{slug}/integrations/discord/add")]
        public async Task<IActionResult> DiscordAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return NotFound();

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/discord")]
        public async Task<IActionResult> DiscordCreate(string slug, [FromForm] string webhookUrl, [FromForm] string? name, [FromForm] string? channelName)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            // Validate webhook URL
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                TempData["Error"] = "Webhook URL is required";
                return RedirectToAction("DiscordAdd", new { slug });
            }

            // Validate HTTPS
            if (!webhookUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Webhook URL must use HTTPS";
                return RedirectToAction("DiscordAdd", new { slug });
            }

            // Validate Discord webhook URL format
            if ((!webhookUrl.Contains("discord.com", StringComparison.OrdinalIgnoreCase) && 
                 !webhookUrl.Contains("discordapp.com", StringComparison.OrdinalIgnoreCase)) ||
                !webhookUrl.Contains("/api/webhooks/", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Invalid Discord webhook URL. Must be from discord.com or discordapp.com and contain /api/webhooks/";
                return RedirectToAction("DiscordAdd", new { slug });
            }

            // Create integration record
            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.Discord,
                Name = string.IsNullOrWhiteSpace(name) ? "Discord" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create Discord integration record
            var discordIntegration = new DiscordIntegration
            {
                IntegrationId = integrationId,
                WebhookUrl = webhookUrl,
                ChannelName = channelName,
                CreatedAt = DateTime.UtcNow
            };

            await _integrationRepository.CreateDiscordIntegrationAsync(discordIntegration);

            // Auto-enable for all project monitors
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
            {
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);
            }

            await _axiomLogger.LogInfo("Discord integration created", new Dictionary<string, object>
            {
                ["project_id"] = project.Id,
                ["integration_name"] = integration.Name
            });

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpGet("project/{slug}/integrations/email/add")]
        public async Task<IActionResult> EmailAdd(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/email")]
        public async Task<IActionResult> EmailCreate(string slug, [FromForm] string email, [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            // Validate email
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email address is required";
                return RedirectToAction("EmailAdd", new { slug });
            }

            // Basic email validation
            if (!email.Contains("@") || !email.Contains("."))
            {
                TempData["Error"] = "Invalid email address format";
                return RedirectToAction("EmailAdd", new { slug });
            }

            // Create integration record
            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.Email,
                Name = string.IsNullOrWhiteSpace(name) ? $"Email - {email}" : name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create Email integration record
            var emailIntegration = new EmailIntegration
            {
                IntegrationId = integrationId,
                Email = email.Trim().ToLower(),
                CreatedAt = DateTime.UtcNow
            };

            await _integrationRepository.CreateEmailIntegrationAsync(emailIntegration);

            // Auto-enable for all project monitors
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
            {
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);
            }

            await _axiomLogger.LogInfo("Email integration created", new Dictionary<string, object>
            {
                ["project_id"] = project.Id,
                ["integration_name"] = integration.Name,
                ["email"] = email
            });

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        // ─── EMAIL EDIT ────────────────────────────────────────────────────────

        [HttpGet("project/{slug}/integrations/email/{id}/edit")]
        public async Task<IActionResult> EmailEdit(string slug, Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null || integration.ProjectId != project.Id) return NotFound();

            var emailDetails = await _integrationRepository.GetEmailIntegrationByIntegrationIdAsync(id);
            if (emailDetails == null) return NotFound();

            ViewBag.Project = project;
            ViewBag.Integration = integration;
            ViewBag.EmailDetails = emailDetails;
            return View();
        }

        [HttpPost("project/{slug}/integrations/email/{id}/edit")]
        public async Task<IActionResult> EmailEditPost(string slug, Guid id,
            [FromForm] string email, [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null || integration.ProjectId != project.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                TempData["Error"] = "A valid email address is required";
                return RedirectToAction("EmailEdit", new { slug, id });
            }

            var finalName = string.IsNullOrWhiteSpace(name) ? $"Email - {email}" : name;
            await _integrationRepository.UpdateEmailIntegrationAsync(id, email.Trim().ToLower(), finalName);
            TempData["Success"] = "Email integration updated";
            return RedirectToAction("Index", new { slug = project.Slug });
        }

        // ─── WEBHOOK EDIT ──────────────────────────────────────────────────────

        [HttpGet("project/{slug}/integrations/webhook/{id}/edit")]
        public async Task<IActionResult> WebhookEdit(string slug, Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null || integration.ProjectId != project.Id) return NotFound();

            var webhookDetails = await _integrationRepository.GetWebhookIntegrationByIntegrationIdAsync(id);
            if (webhookDetails == null) return NotFound();

            ViewBag.Project = project;
            ViewBag.Integration = integration;
            ViewBag.WebhookDetails = webhookDetails;
            return View();
        }

        [HttpPost("project/{slug}/integrations/webhook/{id}/edit")]
        public async Task<IActionResult> WebhookEditPost(string slug, Guid id,
            [FromForm] string? downUrl, [FromForm] string? downMethod,
            [FromForm] string? downHeaders, [FromForm] string? downBody,
            [FromForm] string? upUrl, [FromForm] string? upMethod,
            [FromForm] string? upHeaders, [FromForm] string? upBody,
            [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null || integration.ProjectId != project.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(downUrl) && string.IsNullOrWhiteSpace(upUrl))
            {
                TempData["Error"] = "At least one URL (Down or Up) is required";
                return RedirectToAction("WebhookEdit", new { slug, id });
            }

            string? normalizedDownMethod = null, normalizedDownUrl = null;
            if (!string.IsNullOrWhiteSpace(downUrl))
            {
                if (!Uri.TryCreate(downUrl, UriKind.Absolute, out _))
                {
                    TempData["Error"] = "Down URL must be a valid URL";
                    return RedirectToAction("WebhookEdit", new { slug, id });
                }
                normalizedDownMethod = (downMethod ?? "POST").ToUpper();
                normalizedDownUrl = downUrl.Trim();
            }

            string? normalizedUpMethod = null, normalizedUpUrl = null;
            if (!string.IsNullOrWhiteSpace(upUrl))
            {
                if (!Uri.TryCreate(upUrl, UriKind.Absolute, out _))
                {
                    TempData["Error"] = "Up URL must be a valid URL";
                    return RedirectToAction("WebhookEdit", new { slug, id });
                }
                normalizedUpMethod = (upMethod ?? "POST").ToUpper();
                normalizedUpUrl = upUrl.Trim();
            }

            var finalName = string.IsNullOrWhiteSpace(name) ? "Webhook" : name;
            await _integrationRepository.UpdateWebhookIntegrationAsync(id,
                normalizedDownMethod, normalizedDownUrl,
                string.IsNullOrWhiteSpace(downHeaders) ? null : downHeaders,
                string.IsNullOrWhiteSpace(downBody) ? null : downBody,
                normalizedUpMethod, normalizedUpUrl,
                string.IsNullOrWhiteSpace(upHeaders) ? null : upHeaders,
                string.IsNullOrWhiteSpace(upBody) ? null : upBody,
                finalName);

            TempData["Success"] = "Webhook integration updated";
            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpGet("project/{slug}/integrations/pagerduty/authorize")]
        public async Task<IActionResult> PagerDutyAuthorize(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return NotFound();

            // Generate cryptographically secure state parameter
            var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-");

            // Store state with project/user context in Redis (5 min TTL)
            var oauthState = new PagerDutyOAuthState
            {
                State = state,
                ProjectId = project.Id,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            await _cacheService.SetAsync($"pagerduty_oauth:{state}", oauthState, TimeSpan.FromMinutes(5));

            // Build PagerDuty OAuth URL
            var clientId = _configuration["PagerDuty:ClientId"] ?? _configuration["PAGERDUTY_CLIENT_ID"];
            var redirectUri = _configuration["PagerDuty:RedirectUri"] ?? "https://localhost:5032/integrations/pagerduty/callback";
            var scope = "incidents.write incidents.read services.read users.read";

            var authUrl = $"https://app.pagerduty.com/oauth/authorize?" +
                         $"client_id={clientId}&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"response_type=code&" +
                         $"scope={HttpUtility.UrlEncode(scope)}&" +
                         $"state={state}";

            return Redirect(authUrl);
        }

        [AllowAnonymous]
        [HttpGet("integrations/pagerduty/callback")]
        public async Task<IActionResult> PagerDutyCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
        {
            // Handle error from PagerDuty
            if (!string.IsNullOrEmpty(error))
            {
                await _axiomLogger.LogError($"PagerDuty OAuth error: {error}", new Dictionary<string, object> { ["error"] = error });
                return BadRequest($"PagerDuty authorization failed: {error}");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return BadRequest("Missing code or state parameter");
            }

            // Validate state and retrieve project context
            var oauthState = await _cacheService.GetAsync<PagerDutyOAuthState>($"pagerduty_oauth:{state}");
            if (oauthState == null || oauthState.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired state parameter");
            }

            // Delete state from cache (one-time use)
            await _cacheService.RemoveAsync($"pagerduty_oauth:{state}");

            // Exchange code for tokens
            var redirectUri = _configuration["PagerDuty:RedirectUri"] ?? "https://localhost:5032/integrations/pagerduty/callback";
            
            await _axiomLogger.LogInfo("Attempting PagerDuty token exchange", new Dictionary<string, object>
            {
                ["redirect_uri"] = redirectUri,
                ["code_length"] = code.Length,
                ["state"] = state
            });
            
            var tokenResponse = await _pagerDutyService.ExchangeCodeForTokensAsync(code, redirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                await _axiomLogger.LogError("Failed to exchange PagerDuty authorization code for tokens - token response was null or empty", new Dictionary<string, object>
                {
                    ["redirect_uri"] = redirectUri,
                    ["has_token_response"] = tokenResponse != null,
                    ["has_access_token"] = tokenResponse?.AccessToken != null
                });
                return BadRequest("Failed to complete PagerDuty authorization");
            }

            // Get account info to verify token and get account ID
            var accountInfo = await _pagerDutyService.GetAccountInfoAsync(tokenResponse.AccessToken);
            if (accountInfo == null || string.IsNullOrEmpty(accountInfo.Id))
            {
                await _axiomLogger.LogError("Failed to get PagerDuty account info", new Dictionary<string, object>());
                return BadRequest("Failed to verify PagerDuty account");
            }

            // Create temporary session for service selection
            var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var session = new PagerDutyOAuthSession
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
                ExpiresIn = tokenResponse.ExpiresIn,
                AccountId = accountInfo.Id,
                AccountName = accountInfo.Name ?? accountInfo.Email ?? "Unknown",
                ProjectId = oauthState.ProjectId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            // Store session in Redis with 10-minute TTL
            await _cacheService.SetAsync($"pagerduty_session:{sessionId}", session, TimeSpan.FromMinutes(10));

           await _axiomLogger.LogInfo("PagerDuty OAuth session created", new Dictionary<string, object>
            {
                ["session_id"] = sessionId,
                ["project_id"] = oauthState.ProjectId,
                ["account_id"] = accountInfo.Id
            });

            // Redirect to service selection page
            return Redirect($"/integrations/pagerduty/select-service?session={sessionId}");
        }

        [AllowAnonymous]
        [HttpGet("integrations/pagerduty/select-service")]
        public async Task<IActionResult> PagerDutySelectService([FromQuery] string session)
        {
            // Retrieve session from Redis
            var oauthSession = await _cacheService.GetAsync<PagerDutyOAuthSession>($"pagerduty_session:{session}");
            if (oauthSession == null || oauthSession.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Session expired or invalid. Please try connecting again.");
            }

            // Fetch services from PagerDuty
            var services = await _pagerDutyService.GetServicesAsync(oauthSession.AccessToken);
            if (services == null || !services.Any())
            {
                // If no services found, show error
                return BadRequest("No PagerDuty services found in your account. Please create a service in PagerDuty first.");
            }

            // Pass data to view
            ViewBag.SessionId = session;
            ViewBag.AccountName = oauthSession.AccountName;
            ViewBag.Services = services;

            return View("SelectPagerDutyService");
        }

        [AllowAnonymous]
        [HttpPost("integrations/pagerduty/complete")]
        public async Task<IActionResult> PagerDutyComplete([FromForm] string session, [FromForm] string serviceId)
        {
            // Retrieve session from Redis
            var oauthSession = await _cacheService.GetAsync<PagerDutyOAuthSession>($"pagerduty_session:{session}");
            if (oauthSession == null || oauthSession.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Session expired. Please try connecting again.");
            }

            // Delete session from Redis
            await _cacheService.RemoveAsync($"pagerduty_session:{session}");

            // Load project
            var project = await _projectRepository.GetProjectByIdAsync(oauthSession.ProjectId);
            if (project == null)
            {
                return NotFound("Project not found");
            }

            // Encrypt tokens before storing
            var encryptedAccessToken = _encryptionService.Encrypt(oauthSession.AccessToken);
            var encryptedRefreshToken = _encryptionService.Encrypt(oauthSession.RefreshToken);
            var tokenExpiresAt = DateTime.UtcNow.AddSeconds(oauthSession.ExpiresIn);

            // Create integration record
            var integration = new Integration
            {
                ProjectId = project.Id,
                Type = IntegrationType.PagerDuty,
                Name = $"PagerDuty - {oauthSession.AccountName}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var integrationId = await _integrationRepository.CreateIntegrationAsync(integration);

            // Create PagerDuty integration record with selected service
            var pagerDutyIntegration = new PagerDutyIntegration
            {
                IntegrationId = integrationId,
                AccountId = oauthSession.AccountId,
                ServiceId = serviceId,
                AccessToken = encryptedAccessToken,
                RefreshToken = encryptedRefreshToken,
                TokenExpiresAt = tokenExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _integrationRepository.CreatePagerDutyIntegrationAsync(pagerDutyIntegration);

            // Auto-enable for all project monitors
            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            foreach (var monitor in monitors)
            {
                await _integrationRepository.AddMonitorIntegrationAsync(monitor.Id, integrationId);
            }

            await _axiomLogger.LogInfo("PagerDuty integration created with service", new Dictionary<string, object>
            {
                ["project_id"] = project.Id,
                ["integration_name"] = integration.Name,
                ["account_id"] = oauthSession.AccountId,
                ["service_id"] = serviceId
            });

            // Redirect back to integrations page
            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpPost("integrations/{id}/disable")]
        public async Task<IActionResult> DisableIntegration(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);

            if (integration == null)
            {
                return NotFound();
            }

            var project = await _projectRepository.GetProjectByIdAsync(integration.ProjectId);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            await _integrationRepository.UpdateIntegrationStatusAsync(id, false);

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpPost("integrations/{id}/delete")]
        public async Task<IActionResult> DeleteIntegration(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);

            if (integration == null)
            {
                return NotFound();
            }

            var project = await _projectRepository.GetProjectByIdAsync(integration.ProjectId);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            await _integrationRepository.DeleteIntegrationAsync(id);

            return RedirectToAction("Index", new { slug = project.Slug });
        }

        [HttpGet("integrations/{id}/monitors")]
        public async Task<IActionResult> GetIntegrationMonitors(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null)
            {
                return NotFound();
            }

            var project = await _projectRepository.GetProjectByIdAsync(integration.ProjectId);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            var monitors = await _monitorRepository.GetMonitorsByProjectIdAsync(project.Id);
            var mappedMonitorIds = await _integrationRepository.GetMappedMonitorIdsAsync(id);

            return Json(new
            {
                monitors = monitors.Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    isSelected = mappedMonitorIds.Contains(m.Id)
                })
            });
        }

        [HttpPost("integrations/{id}/monitors")]
        public async Task<IActionResult> UpdateIntegrationMonitors(Guid id, [FromBody] List<Guid> monitorIds)
        {
            var user = HttpContext.Items["User"] as User;
            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            if (integration == null)
            {
                return NotFound();
            }

            var project = await _projectRepository.GetProjectByIdAsync(integration.ProjectId);
            if (project == null) return NotFound();
            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            await _integrationRepository.SyncMonitorIntegrationsAsync(id, monitorIds);

            return Ok();
        }

        [HttpPost("{id}/test")]
        public async Task<IActionResult> TestIntegration(Guid id)
        {
            var user = HttpContext.Items["User"] as User;
            var integration = await _integrationRepository.GetIntegrationByIdAsync(id);
            
            if (integration == null) return NotFound();
            
            var project = await _projectRepository.GetProjectByIdAsync(integration.ProjectId);
            if (project == null) return NotFound();

            if (!await _projectAuth.CanManageMonitorsAsync(project.Id, project.UserId, user!.Id))
                return Forbid();

            // Create notification job for testing
            // We use -1 as a dummy monitor ping ID for test pings
            var jobId = await _integrationRepository.CreateNotificationJobAsync(-1, integration.Id);

            // Send to SQS
            var sqsPayload = new HealthyCron.Models.DTOs.SqsMessagePayload
            {
                JobId = jobId,
                MonitorId = Guid.Empty, // Dummy
                IntegrationId = integration.Id
            };
            await _queueService.SendMessageAsync(sqsPayload);

            return Ok(new { message = "Test ping sent. Please check your integration." });
        }
    }
}
