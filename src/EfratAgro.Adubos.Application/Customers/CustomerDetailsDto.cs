namespace EfratAgro.Adubos.Application.Customers;

public sealed record CustomerDetailsDto(
    Guid Id,
    string Name,
    string? Phone,
    int SalesCount,
    decimal TotalPurchased,
    decimal TotalQuantity,
    DateTime? LastPurchaseAtUtc,
    CustomerFinancialSummaryDto Financial,
    IReadOnlyList<CustomerProductSummaryDto> Products,
    IReadOnlyList<CustomerPurchaseDto> Purchases);

public sealed record CustomerFinancialSummaryDto(
    decimal TotalReceivables,
    decimal TotalReceived,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OpenInstallments,
    int OverdueInstallments,
    int SalesWithoutFinancialSchedule);

public sealed record CustomerProductSummaryDto(
    Guid ProductId,
    string ProductName,
    decimal TotalQuantity,
    decimal TotalValue);

public sealed record CustomerPurchaseDto(
    Guid SaleId,
    DateTime OccurredAtUtc,
    string Origin,
    int Items,
    decimal TotalQuantity,
    decimal TotalValue,
    IReadOnlyList<CustomerPurchaseProductDto> Products);

public sealed record CustomerPurchaseProductDto(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalValue);
