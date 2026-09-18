namespace EfratAgro.Adubos.Application.Sales;

public sealed record CreateSaleResult(
    Guid SaleId,
    Guid CustomerId,
    string CustomerName,
    int Items,
    decimal TotalQuantity,
    decimal TotalValue,
    DateTime OccurredAtUtc);
