namespace EfratAgro.Adubos.Application.Sales;

public interface ISalesQueryService
{
    Task<IReadOnlyList<SaleSummaryDto>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
}
