using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Application.Sales;

public sealed record CreateSaleRequest(
    string CustomerName,
    string? CustomerPhone,
    IReadOnlyList<CreateSaleItemRequest> Items,
    DeliveryMethod DeliveryMethod = DeliveryMethod.Delivery);

public sealed record CreateSaleItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);
