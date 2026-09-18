namespace EfratAgro.Adubos.Application.Purchases;

public interface IPurchasesQueryService
{
    Task<IReadOnlyList<PurchaseSummaryDto>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
}
