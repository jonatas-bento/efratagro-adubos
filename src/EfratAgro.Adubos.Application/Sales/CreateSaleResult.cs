using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Application.Sales;

public sealed record CreateSaleResult(
    Guid SaleId,
    Guid CustomerId,
    string CustomerName,
    int Items,
    decimal TotalQuantity,
    decimal TotalValue,
    DateTime OccurredAtUtc,
    DeliveryMethod DeliveryMethod,
    DeliveryStatus DeliveryStatus,
    int Receivables,
    decimal ScheduledAmount)
{
    public SaleStockMode StockMode { get; init; } =
        SaleStockMode.Immediate;
}
