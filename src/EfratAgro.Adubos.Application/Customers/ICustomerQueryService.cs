namespace EfratAgro.Adubos.Application.Customers;

public interface ICustomerQueryService
{
    Task<IReadOnlyList<CustomerListItemDto>> GetAsync(
        string? search,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerListItemDto>> GetAsync(
        string? search,
        int take,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto?> GetByIdAsync(
        Guid customerId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
