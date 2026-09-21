using EfratAgro.Adubos.Application.Common;

namespace EfratAgro.Adubos.Application.Sales;

public interface ISalesQueryService
{
    Task<PagedResult<SaleSummaryDto>> GetPageAsync(
        int page,
        int pageSize,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleSummaryDto>> GetRecentAsync(
        int take,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
