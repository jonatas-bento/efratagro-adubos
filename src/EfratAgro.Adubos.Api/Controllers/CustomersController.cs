using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(
    Policy =
        AuthorizationPolicies.Operational)]
public sealed class CustomersController
    : ControllerBase
{
    private readonly ICustomerQueryService
        _queries;

    private readonly ICustomerService
        _customers;

    public CustomersController(
        ICustomerQueryService queries,
        ICustomerService customers)
    {
        _queries = queries;
        _customers = customers;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? q,
        [FromQuery] int? take,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _queries
                    .GetAsync(
                        q,
                        take ?? 100,
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

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> GetById(
        Guid customerId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer =
                await _queries
                    .GetByIdAsync(
                        customerId,
                        from,
                        to,
                        cancellationToken);

            return customer is null
                ? NotFound()
                : Ok(customer);
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
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id =
                await _customers
                    .CreateAsync(
                        request,
                        cancellationToken);

            return Created(
                $"/api/customers/{id}",
                new
                {
                    id
                });
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

    [HttpPut("{customerId:guid}")]
    public async Task<IActionResult> Update(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _customers
                .UpdateAsync(
                    customerId,
                    request,
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
