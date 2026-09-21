using EfratAgro.Adubos.Application.Common;
using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Infrastructure.Common;
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

    public async Task<
        IReadOnlyList<PurchaseSummaryDto>>
        GetRecentAsync(
            int take,
            DateOnly? from = null,
            DateOnly? to = null,
            CancellationToken cancellationToken = default)
    {
        var page =
            await GetPageAsync(
                page: 1,
                pageSize:
                    Math.Clamp(
                        take,
                        1,
                        100),
                from,
                to,
                cancellationToken);

        return page.Items;
    }

    public async Task<
        PagedResult<PurchaseSummaryDto>>
        GetPageAsync(
            int page,
            int pageSize,
            DateOnly? from = null,
            DateOnly? to = null,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(
            page,
            pageSize);

        BusinessDate.ValidateRange(
            from,
            to);

        var query =
            _dbContext.Purchases
                .AsNoTracking()
                .AsQueryable();

        if (from.HasValue)
        {
            var fromUtc =
                BusinessDate.StartOfDayUtc(
                    from.Value);

            query =
                query.Where(
                    x =>
                        x.OccurredAtUtc >=
                        fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusiveUtc =
                BusinessDate.ExclusiveEndUtc(
                    to.Value);

            query =
                query.Where(
                    x =>
                        x.OccurredAtUtc <
                        toExclusiveUtc);
        }

        var totalItems =
            await query.CountAsync(
                cancellationToken);

        var skip =
            (page - 1) *
            pageSize;

        var purchases =
            await query
                .OrderByDescending(
                    x => x.OccurredAtUtc)
                .ThenByDescending(
                    x => x.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    SupplierName =
                        x.Supplier.Name,
                    x.OccurredAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        if (purchases.Count == 0)
        {
            return new PagedResult<PurchaseSummaryDto>(
                [],
                page,
                pageSize,
                totalItems);
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
                .ToListAsync(
                    cancellationToken);

        var purchaseIds =
            purchases
                .Select(
                    x => x.Id)
                .ToHashSet();

        var totals =
            itemRows
                .Where(
                    x =>
                        purchaseIds.Contains(
                            x.PurchaseId))
                .GroupBy(
                    x => x.PurchaseId)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Items =
                            group.Count(),

                        TotalQuantity =
                            group.Sum(
                                x =>
                                    x.Quantity),

                        TotalValue =
                            group.Sum(
                                x =>
                                    x.Quantity *
                                    x.UnitCost)
                    });

        var items =
            purchases
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

        return new PagedResult<PurchaseSummaryDto>(
            items,
            page,
            pageSize,
            totalItems);
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "A página deve ser maior que zero.");
        }

        if (
            pageSize <= 0 ||
            pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "O tamanho da página deve estar entre 1 e 100.");
        }
    }
}
