namespace EfratAgro.Adubos.Application.Purchases;

public sealed record CreatePurchaseRequest(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseItemRequest> Items);

public sealed record CreatePurchaseItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost);
