using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(
    Policy =
        AuthorizationPolicies.Operational)]
public sealed class InventoryController
    : ControllerBase
{
    private readonly IInventoryQueryService
        _inventory;

    public InventoryController(
        IInventoryQueryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var result =
            await _inventory
                .GetInventoryAsync(
                    q,
                    cancellationToken);

        return Ok(result);
    }
}
