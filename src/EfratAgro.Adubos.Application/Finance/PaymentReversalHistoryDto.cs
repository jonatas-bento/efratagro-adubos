namespace EfratAgro.Adubos.Application.Finance;

public sealed record PaymentReversalHistoryDto(
    Guid Id,
    Guid ReversedByUserId,
    string? ReversedByUserEmail,
    string Reason,
    DateTime ReversedAtUtc);
