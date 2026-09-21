using EfratAgro.Adubos.Application.Common;

namespace EfratAgro.Adubos.Application.Purchases;

public interface IPurchasesQueryService
{
    Task<PagedResult<PurchaseSummaryDto>> GetPageAsync(
        int page,
        int pageSize,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseSummaryDto>> GetRecentAsync(
        int take,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
