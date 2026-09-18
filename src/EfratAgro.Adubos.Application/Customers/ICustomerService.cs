namespace EfratAgro.Adubos.Application.Customers;

public interface ICustomerService
{
    Task<Guid> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);
}
