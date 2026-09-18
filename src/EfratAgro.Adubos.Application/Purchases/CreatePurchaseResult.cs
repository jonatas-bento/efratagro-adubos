namespace EfratAgro.Adubos.Application.Purchases;

public sealed record CreatePurchaseResult(
    Guid PurchaseId,
    Guid SupplierId,
    string SupplierName,
    int Items,
    decimal TotalQuantity,
    decimal TotalValue,
    DateTime OccurredAtUtc);
