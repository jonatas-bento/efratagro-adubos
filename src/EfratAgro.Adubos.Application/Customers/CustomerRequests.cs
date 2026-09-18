namespace EfratAgro.Adubos.Application.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string? Phone);

public sealed record UpdateCustomerRequest(
    string Name,
    string? Phone);
