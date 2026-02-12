using HealthyCron.Data.Interfaces;
using HealthyCron.Enums;
using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Auth]
    public class IntegrationsController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMonitorRepository _monitorRepository;
        private readonly ISlackOAuthService _slackOAuthService;
        private readonly IEncryptionService _encryptionService;
        private readonly AxiomLogger _axiomLogger;

        public IntegrationsController(
            IProjectRepository projectRepository,
            IIntegrationRepository integrationRepository,
            IMonitorRepository monitorRepository,
            ISlackOAuthService slackOAuthService,
            IEncryptionService encryptionService,
            AxiomLogger axiomLogger)
        {
            _projectRepository = projectRepository;
            _integrationRepository = integrationRepository;
            _monitorRepository = monitorRepository;
            _slackOAuthService = slackOAuthService;
            _encryptionService = encryptionService;
            _axiomLogger = axiomLogger;
        }

        [HttpGet("project/{slug}/integrations")]
        public async Task<IActionResult> Index(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

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

            return View();
        }

        [HttpGet("project/{slug}/integrations/slack/authorize")]
        public async Task<IActionResult> SlackAuthorize(string slug)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

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

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/teams")]
        public async Task<IActionResult> TeamsCreate(string slug, [FromForm] string webhookUrl, [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

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

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/googlechat")]
        public async Task<IActionResult> GoogleChatCreate(string slug, [FromForm] string webhookUrl, [FromForm] string? name, [FromForm] string? spaceName)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

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

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/discord")]
        public async Task<IActionResult> DiscordCreate(string slug, [FromForm] string webhookUrl, [FromForm] string? name, [FromForm] string? channelName)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

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

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            ViewBag.User = user;

            return View();
        }

        [HttpPost("project/{slug}/integrations/email")]
        public async Task<IActionResult> EmailCreate(string slug, [FromForm] string email, [FromForm] string? name)
        {
            var user = HttpContext.Items["User"] as User;
            var project = await _projectRepository.GetProjectBySlugAsync(slug);

            if (project == null || project.UserId != user!.Id)
            {
                return NotFound();
            }

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
            if (project == null || project.UserId != user!.Id)
            {
                return Forbid();
            }

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
            if (project == null || project.UserId != user!.Id)
            {
                return Forbid();
            }

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
            if (project == null || project.UserId != user!.Id)
            {
                return Forbid();
            }

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
            if (project == null || project.UserId != user!.Id)
            {
                return Forbid();
            }

            await _integrationRepository.SyncMonitorIntegrationsAsync(id, monitorIds);

            return Ok();
        }
    }
}
