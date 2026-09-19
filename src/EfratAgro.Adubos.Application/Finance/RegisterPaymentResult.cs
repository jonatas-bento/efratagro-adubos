namespace EfratAgro.Adubos.Application.Finance;

public sealed record RegisterPaymentResult(
    Guid PaymentId,
    Guid ReceivableId,
    decimal Amount,
    decimal TotalPaid,
    decimal OutstandingAmount,
    DateTime PaidAtUtc);
