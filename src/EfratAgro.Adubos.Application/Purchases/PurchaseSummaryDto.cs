namespace EfratAgro.Adubos.Application.Purchases;

public sealed record PurchaseSummaryDto(
    Guid PurchaseId,
    string SupplierName,
    int Items,
    decimal TotalQuantity,
    decimal TotalValue,
    DateTime OccurredAtUtc);
