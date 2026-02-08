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

            // Get Slack details for each Slack integration
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
