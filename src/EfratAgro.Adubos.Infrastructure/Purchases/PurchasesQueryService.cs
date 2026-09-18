using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Purchases;

public sealed class PurchasesQueryService
    : IPurchasesQueryService
{
    private readonly AdubosDbContext _dbContext;

    public PurchasesQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PurchaseSummaryDto>>
        GetRecentAsync(
            int take,
            CancellationToken cancellationToken = default)
    {
        var limit =
            Math.Clamp(take, 1, 100);

        var purchases =
            await _dbContext.Purchases
                .AsNoTracking()
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    SupplierName = x.Supplier.Name,
                    x.OccurredAtUtc
                })
                .ToListAsync(cancellationToken);

        if (purchases.Count == 0)
        {
            return [];
        }

        var itemRows =
            await _dbContext.PurchaseItems
                .AsNoTracking()
                .Select(x => new
                {
                    x.PurchaseId,
                    x.Quantity,
                    x.UnitCost
                })
                .ToListAsync(cancellationToken);

        var totals =
            itemRows
                .GroupBy(x => x.PurchaseId)
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
                                    x.UnitCost)
                    });

        return purchases
            .Select(purchase =>
            {
                totals.TryGetValue(
                    purchase.Id,
                    out var total);

                return new PurchaseSummaryDto(
                    purchase.Id,
                    purchase.SupplierName,
                    total?.Items ?? 0,
                    total?.TotalQuantity ?? 0m,
                    total?.TotalValue ?? 0m,
                    purchase.OccurredAtUtc);
            })
            .ToList();
    }
}
