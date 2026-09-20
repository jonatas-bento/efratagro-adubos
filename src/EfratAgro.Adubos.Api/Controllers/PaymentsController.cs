using System.IdentityModel.Tokens.Jwt;
using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(
    Policy =
        AuthorizationPolicies.Management)]
public sealed class PaymentsController
    : ControllerBase
{
    private readonly IPaymentService
        _payments;

    public PaymentsController(
        IPaymentService payments)
    {
        _payments = payments;
    }

    [HttpPost("{paymentId:guid}/reversal")]
    public async Task<IActionResult> Reverse(
        Guid paymentId,
        ReversePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var subject =
            User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)
                ?.Value;

        if (
            !Guid.TryParse(
                subject,
                out var reversedByUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _payments
                    .ReverseAsync(
                        paymentId,
                        reversedByUserId,
                        request,
                        cancellationToken);

            return Ok(result);
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
