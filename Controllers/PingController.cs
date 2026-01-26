using HealthyCron.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : Controller
    {
        private readonly IMonitorRepository _monitorRepository;

        public PingController(IMonitorRepository monitorRepository)
        {
            _monitorRepository = monitorRepository;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> PingById(Guid id)
        {
            var success = await _monitorRepository.RegisterPingByIdAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Monitor not found" });
            }
            return Ok(new { message = "Ping registered successfully", timestamp = DateTime.UtcNow });
        }
    }
}
