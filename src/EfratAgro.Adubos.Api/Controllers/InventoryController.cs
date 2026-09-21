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
        [FromQuery] DateOnly? asOf,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _inventory
                    .GetInventoryAsync(
                        q,
                        asOf,
                        cancellationToken);

            return Ok(result);
        }
        catch (
            Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }
}
