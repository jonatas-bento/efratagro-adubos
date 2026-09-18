namespace EfratAgro.Adubos.Application.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    string Name,
    string? Phone,
    int SalesCount,
    decimal TotalPurchased,
    decimal TotalQuantity,
    DateTime? LastPurchaseAtUtc,
    decimal TotalReceived,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int SalesWithoutFinancialSchedule);
