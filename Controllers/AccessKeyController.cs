using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Service;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace HealthyCron.Controllers
{
    [Auth]
    [Route("projects/{projectId:guid}/access-keys")]
    public class AccessKeyController : Controller
    {
        private readonly IAccessKeyService _accessKeyService;
        private readonly AxiomLogger _axiomLogger;

        public AccessKeyController(IAccessKeyService accessKeyService, AxiomLogger axiomLogger)
        {
            _accessKeyService = accessKeyService;
            _axiomLogger = axiomLogger;
        }

        [HttpGet]
        public async Task<IActionResult> List(Guid projectId)
        {
            var keys = await _accessKeyService.GetKeysByProjectIdAsync(projectId);
            return Json(keys);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid projectId, [FromForm] ApiKeyType type)
        {
            try
            {
                var (fullKey, keyModel) = await _accessKeyService.CreateKeyAsync(projectId, type);

                await _axiomLogger.LogInfo("API key created", new Dictionary<string, object>
                {
                    ["project_id"] = projectId,
                    ["key_type"] = type.ToString()
                });

                return Json(new
                {
                    key = fullKey,
                    model = keyModel,
                    message = "Make sure to copy your API key now. You won't be able to see it again!"
                });
            }
            catch (Exception ex)
            {
                await _axiomLogger.LogError("Failed to create API key", new Dictionary<string, object>
                {
                    ["project_id"] = projectId,
                    ["error"] = ex.Message
                });
                return StatusCode(500, new { error = "Failed to create API key" });
            }
        }

        [HttpPost("{keyId:guid}/revoke")]
        public async Task<IActionResult> Revoke(Guid projectId, Guid keyId)
        {
            var success = await _accessKeyService.RevokeKeyAsync(keyId);
            return Json(new { success });
        }
    }
}
