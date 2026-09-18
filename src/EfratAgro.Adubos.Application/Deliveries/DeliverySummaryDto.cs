namespace EfratAgro.Adubos.Application.Deliveries;

public sealed record DeliverySummaryDto(
    Guid SaleId,
    string CustomerName,
    DateTime OccurredAtUtc,
    decimal TotalQuantity,
    string Method,
    string Status,
    DateTime? DeliveredAtUtc);
