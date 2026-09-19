namespace EfratAgro.Adubos.Application.Finance;

public sealed record ReceivableSummaryDto(
    decimal TotalScheduled,
    decimal TotalReceived,
    decimal TotalOutstanding,
    decimal TotalOverdue,
    int OpenInstallments,
    int OverdueInstallments);
