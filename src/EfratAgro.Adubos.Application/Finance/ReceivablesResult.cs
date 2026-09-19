namespace EfratAgro.Adubos.Application.Finance;

public sealed record ReceivablesResult(
    ReceivableSummaryDto Summary,
    IReadOnlyList<ReceivableDto> Items);
