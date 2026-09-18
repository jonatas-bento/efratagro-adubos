namespace EfratAgro.Adubos.Application.Sales;

public sealed record SaleSummaryDto(
    Guid SaleId,
    string CustomerName,
    int Items,
    decimal TotalQuantity,
    decimal TotalValue,
    DateTime OccurredAtUtc);
