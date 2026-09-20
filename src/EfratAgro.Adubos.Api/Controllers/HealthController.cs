using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
public sealed class HealthController
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/health")]
    public IActionResult Get()
    {
        return Ok(
            new
            {
                status = "healthy",
                service = "EfratAgro.Adubos.Api",
                timestampUtc = DateTime.UtcNow
            });
    }
}
