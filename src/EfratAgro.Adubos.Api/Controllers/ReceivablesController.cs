using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/receivables")]
[Authorize(
    Policy =
        AuthorizationPolicies.Management)]
public sealed class ReceivablesController
    : ControllerBase
{
    private readonly IReceivableQueryService
        _receivables;

    private readonly IPaymentQueryService
        _paymentQueries;

    private readonly IPaymentService
        _payments;

    public ReceivablesController(
        IReceivableQueryService receivables,
        IPaymentQueryService paymentQueries,
        IPaymentService payments)
    {
        _receivables = receivables;
        _paymentQueries = paymentQueries;
        _payments = payments;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool? openOnly,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var result =
            await _receivables
                .GetAsync(
                    openOnly ?? true,
                    take ?? 200,
                    cancellationToken);

        return Ok(result);
    }

    [HttpGet("{receivableId:guid}/payments")]
    public async Task<IActionResult> GetPayments(
        Guid receivableId,
        CancellationToken cancellationToken)
    {
        var result =
            await _paymentQueries
                .GetByReceivableAsync(
                    receivableId,
                    cancellationToken);

        return Ok(result);
    }

    [HttpPost("{receivableId:guid}/payments")]
    public async Task<IActionResult> RegisterPayment(
        Guid receivableId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _payments
                    .RegisterAsync(
                        receivableId,
                        request,
                        cancellationToken);

            return Created(
                $"/api/receivables/{receivableId}/payments/{result.PaymentId}",
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
