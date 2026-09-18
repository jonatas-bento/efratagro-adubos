using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Sales;

public sealed class SalesQueryService
    : ISalesQueryService
{
    private readonly AdubosDbContext _dbContext;

    public SalesQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SaleSummaryDto>>
        GetRecentAsync(
            int take,
            CancellationToken cancellationToken = default)
    {
        var limit =
            Math.Clamp(
                take,
                1,
                100);

        var recentSales =
            await _dbContext.Sales
                .AsNoTracking()
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    CustomerName = x.Customer.Name,
                    x.OccurredAtUtc
                })
                .ToListAsync(cancellationToken);

        if (recentSales.Count == 0)
        {
            return [];
        }

        // Deliberately simple query.
        // We already identified provider limitations around
        // Guid collections, so aggregation is completed in memory.
        var itemRows =
            await _dbContext.SaleItems
                .AsNoTracking()
                .Select(x => new
                {
                    x.SaleId,
                    x.Quantity,
                    x.UnitPrice
                })
                .ToListAsync(cancellationToken);

        var totals =
            itemRows
                .GroupBy(x => x.SaleId)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Items = group.Count(),
                        TotalQuantity =
                            group.Sum(x => x.Quantity),
                        TotalValue =
                            group.Sum(
                                x =>
                                    x.Quantity *
                                    x.UnitPrice)
                    });

        return recentSales
            .Select(sale =>
            {
                totals.TryGetValue(
                    sale.Id,
                    out var total);

                return new SaleSummaryDto(
                    sale.Id,
                    sale.CustomerName,
                    total?.Items ?? 0,
                    total?.TotalQuantity ?? 0m,
                    total?.TotalValue ?? 0m,
                    sale.OccurredAtUtc);
            })
            .ToList();
    }
}
