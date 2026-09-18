namespace EfratAgro.Adubos.Application.Sales;

public sealed record CreateSaleRequest(
    string CustomerName,
    string? CustomerPhone,
    IReadOnlyList<CreateSaleItemRequest> Items);

public sealed record CreateSaleItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);
