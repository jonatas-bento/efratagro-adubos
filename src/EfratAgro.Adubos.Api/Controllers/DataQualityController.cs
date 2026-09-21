using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.DataQuality;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/data-quality")]
[Authorize(
    Policy =
        AuthorizationPolicies.Operational)]
public sealed class DataQualityController
    : ControllerBase
{
    private readonly IDataQualityService
        _dataQuality;

    public DataQualityController(
        IDataQualityService dataQuality)
    {
        _dataQuality = dataQuality;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _dataQuality.GetAsync(
                cancellationToken));
    }

    [HttpPut("operational-trusted-from")]
    [Authorize(
        Policy =
            AuthorizationPolicies.Management)]
    public async Task<IActionResult>
        SetOperationalTrustedFrom(
            UpdateOperationalTrustedFromRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            return Ok(
                await _dataQuality
                    .SetOperationalTrustedFromAsync(
                        request.OperationalTrustedFrom,
                        cancellationToken));
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
