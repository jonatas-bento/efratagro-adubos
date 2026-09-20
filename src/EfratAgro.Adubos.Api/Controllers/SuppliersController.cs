using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(
    Policy =
        AuthorizationPolicies.Management)]
public sealed class SuppliersController
    : ControllerBase
{
    private readonly ISupplierQueryService
        _suppliers;

    public SuppliersController(
        ISupplierQueryService suppliers)
    {
        _suppliers = suppliers;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result =
            await _suppliers
                .GetAllAsync(
                    cancellationToken);

        return Ok(result);
    }
}
