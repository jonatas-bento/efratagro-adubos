using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize(
    Policy =
        AuthorizationPolicies.Management)]
public sealed class PurchasesController
    : ControllerBase
{
    private readonly IPurchasesQueryService
        _queries;

    private readonly IPurchaseService
        _purchases;

    public PurchasesController(
        IPurchasesQueryService queries,
        IPurchaseService purchases)
    {
        _queries = queries;
        _purchases = purchases;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] int? take,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var effectivePage =
                page ?? 1;

            var effectivePageSize =
                pageSize ??
                take ??
                20;

            var result =
                await _queries
                    .GetPageAsync(
                        effectivePage,
                        effectivePageSize,
                        from,
                        to,
                        cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _purchases
                    .CreateAsync(
                        request,
                        cancellationToken);

            return Created(
                $"/api/purchases/{result.PurchaseId}",
                result);
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
