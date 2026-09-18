using EfratAgro.Adubos.Application.Customers;
using EfratAgro.Adubos.Domain.Customers;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Customers;

public sealed class CustomerService
    : ICustomerService
{
    private readonly AdubosDbContext _dbContext;

    public CustomerService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer =
            new Customer(
                request.Name,
                request.Phone);

        _dbContext.Customers.Add(
            customer);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return customer.Id;
    }

    public async Task UpdateAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer =
            await _dbContext.Customers
                .SingleOrDefaultAsync(
                    x => x.Id == customerId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Cliente não encontrado.");

        customer.Update(
            request.Name,
            request.Phone);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
