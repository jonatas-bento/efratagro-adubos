using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Deliveries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
[Authorize(
    Policy =
        AuthorizationPolicies.Operational)]
public sealed class DeliveriesController
    : ControllerBase
{
    private readonly IDeliveryQueryService
        _queries;

    private readonly IDeliveryService
        _deliveries;

    public DeliveriesController(
        IDeliveryQueryService queries,
        IDeliveryService deliveries)
    {
        _queries = queries;
        _deliveries = deliveries;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool? pendingOnly,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var result =
            await _queries
                .GetAsync(
                    pendingOnly ?? true,
                    take ?? 100,
                    cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{saleId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid saleId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _deliveries
                .MarkDeliveredAsync(
                    saleId,
                    cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
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
