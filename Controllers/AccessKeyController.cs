using HealthyCron.Filters;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
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

        public AccessKeyController(IAccessKeyService accessKeyService)
        {
            _accessKeyService = accessKeyService;
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
            var (fullKey, keyModel) = await _accessKeyService.CreateKeyAsync(projectId, type);

            return Json(new
            {
                key = fullKey,
                model = keyModel,
                message = "Make sure to copy your API key now. You won't be able to see it again!"
            });
        }

        [HttpPost("{keyId:guid}/revoke")]
        public async Task<IActionResult> Revoke(Guid projectId, Guid keyId)
        {
            var success = await _accessKeyService.RevokeKeyAsync(keyId);
            return Json(new { success });
        }
    }
}
