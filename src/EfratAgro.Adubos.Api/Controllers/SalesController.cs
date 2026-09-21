using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(
    Policy =
        AuthorizationPolicies.Operational)]
public sealed class SalesController
    : ControllerBase
{
    private readonly ISalesQueryService
        _queries;

    private readonly ISaleService
        _sales;

    public SalesController(
        ISalesQueryService queries,
        ISaleService sales)
    {
        _queries = queries;
        _sales = sales;
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
        CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _sales
                    .CreateAsync(
                        request,
                        cancellationToken);

            return Created(
                $"/api/sales/{result.SaleId}",
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
