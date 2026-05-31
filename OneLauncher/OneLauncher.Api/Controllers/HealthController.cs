using Microsoft.AspNetCore.Mvc;

namespace OneLauncher.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth() =>
        Ok(new { status = "online", service = "OneLauncher.Api" });
}
