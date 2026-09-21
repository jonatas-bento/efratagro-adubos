using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Application.Sales;

public sealed record CreateSaleRequest(
    Guid CustomerId,
    IReadOnlyList<CreateSaleItemRequest> Items,
    DeliveryMethod DeliveryMethod,
    IReadOnlyList<CreateSaleReceivableRequest> Receivables)
{
    public SaleStockMode StockMode { get; init; } =
        SaleStockMode.Immediate;
}

public sealed record CreateSaleItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CreateSaleReceivableRequest(
    int InstallmentNumber,
    DateTime DueDate,
    decimal Amount);
