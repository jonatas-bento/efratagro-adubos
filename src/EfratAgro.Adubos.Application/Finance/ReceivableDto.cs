namespace EfratAgro.Adubos.Application.Finance;

public sealed record ReceivableDto(
    Guid Id,
    Guid SaleId,
    Guid CustomerId,
    string CustomerName,
    int InstallmentNumber,
    DateTime DueDate,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    ReceivableStatus StatusCode,
    string Status);
