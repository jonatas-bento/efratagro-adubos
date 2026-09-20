namespace EfratAgro.Adubos.Application.Finance;

public sealed record ReversePaymentResult(
    Guid ReversalId,
    Guid PaymentId,
    Guid ReceivableId,
    decimal ReversedAmount,
    decimal TotalPaid,
    decimal OutstandingAmount,
    DateTime ReversedAtUtc,
    Guid ReversedByUserId);
