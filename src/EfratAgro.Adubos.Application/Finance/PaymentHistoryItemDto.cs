using EfratAgro.Adubos.Domain.Finance;

namespace EfratAgro.Adubos.Application.Finance;

public sealed record PaymentHistoryItemDto(
    Guid Id,
    Guid ReceivableId,
    decimal Amount,
    DateTime PaidAtUtc,
    PaymentMethod Method,
    string? Reference,
    string? Notes,
    PaymentReversalHistoryDto? Reversal);
