using EfratAgro.Adubos.Application.Common;
using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Infrastructure.Common;
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

    public async Task<
        IReadOnlyList<SaleSummaryDto>>
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

    public async Task<PagedResult<SaleSummaryDto>>
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
            _dbContext.Sales
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

        var sales =
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
                    CustomerName =
                        x.Customer.Name,
                    x.OccurredAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        if (sales.Count == 0)
        {
            return new PagedResult<SaleSummaryDto>(
                [],
                page,
                pageSize,
                totalItems);
        }

        /*
         * Mantemos deliberadamente a consulta de itens
         * simples por compatibilidade com o provider.
         *
         * A paginação ocorre nas vendas antes desta etapa;
         * apenas agregamos itens pertencentes à página.
         */
        var itemRows =
            await _dbContext.SaleItems
                .AsNoTracking()
                .Select(x => new
                {
                    x.SaleId,
                    x.Quantity,
                    x.UnitPrice
                })
                .ToListAsync(
                    cancellationToken);

        var saleIds =
            sales
                .Select(
                    x => x.Id)
                .ToHashSet();

        var totals =
            itemRows
                .Where(
                    x =>
                        saleIds.Contains(
                            x.SaleId))
                .GroupBy(
                    x => x.SaleId)
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
                                    x.UnitPrice)
                    });

        var items =
            sales
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

        return new PagedResult<SaleSummaryDto>(
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
