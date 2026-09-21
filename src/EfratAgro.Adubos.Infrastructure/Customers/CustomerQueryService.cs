using EfratAgro.Adubos.Application.Customers;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Common;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Customers;

public sealed class CustomerQueryService
    : ICustomerQueryService
{
    private readonly AdubosDbContext _dbContext;

    public CustomerQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<CustomerListItemDto>> GetAsync(
        string? search,
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetAsync(
            search,
            take,
            from: null,
            to: null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerListItemDto>> GetAsync(
        string? search,
        int take,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        BusinessDate.ValidateRange(
            from,
            to);

        DateTime? fromUtc =
            from.HasValue
                ? BusinessDate.StartOfDayUtc(
                    from.Value)
                : null;

        DateTime? toExclusiveUtc =
            to.HasValue
                ? BusinessDate.ExclusiveEndUtc(
                    to.Value)
                : null;

        var limit =
            Math.Clamp(
                take,
                1,
                1000);

        var query =
            _dbContext.Customers
                .AsNoTracking()
                .Where(
                    x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed =
                search.Trim();

            var normalized =
                trimmed.ToUpperInvariant();

            query =
                query.Where(
                    x =>
                        x.NormalizedName.Contains(
                            normalized) ||
                        (
                            x.Phone != null &&
                            x.Phone.Contains(
                                trimmed)
                        ));
        }

        var customers =
            await query
                .OrderBy(
                    x => x.Name)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Phone
                })
                .ToListAsync(
                    cancellationToken);

        if (customers.Count == 0)
        {
            return [];
        }

        var allSales =
            await _dbContext.Sales
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.CustomerId,
                    x.OccurredAtUtc,
                    x.Origin
                })
                .ToListAsync(
                    cancellationToken);

        var saleItems =
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

        var receivables =
            await _dbContext.Receivables
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.SaleId,
                    x.DueDate,
                    x.OriginalAmount
                })
                .ToListAsync(
                    cancellationToken);

        var payments =
            await _dbContext.Payments
                .Where(
                    x =>
                        x.Reversal == null)
                .AsNoTracking()
                .Select(x => new
                {
                    x.ReceivableId,
                    x.Amount
                })
                .ToListAsync(
                    cancellationToken);

        var paymentTotals =
            payments
                .GroupBy(
                    x => x.ReceivableId)
                .ToDictionary(
                    x => x.Key,
                    x =>
                        x.Sum(
                            y => y.Amount));

        var today =
            BusinessDate.Today;

        return customers
            .Select(customer =>
            {
                /*
                 * Duas visões deliberadamente diferentes:
                 *
                 * allCustomerSales
                 *   = posição financeira atual.
                 *
                 * commercialSales
                 *   = atividade comercial no período.
                 */
                var allCustomerSales =
                    allSales
                        .Where(
                            x =>
                                x.CustomerId ==
                                customer.Id)
                        .ToList();

                var commercialSales =
                    allCustomerSales
                        .Where(
                            x =>
                                IsInPeriod(
                                    x.OccurredAtUtc,
                                    fromUtc,
                                    toExclusiveUtc))
                        .ToList();

                var commercialSaleIds =
                    commercialSales
                        .Select(
                            x => x.Id)
                        .ToHashSet();

                var allCustomerSaleIds =
                    allCustomerSales
                        .Select(
                            x => x.Id)
                        .ToHashSet();

                var commercialItems =
                    saleItems
                        .Where(
                            x =>
                                commercialSaleIds
                                    .Contains(
                                        x.SaleId))
                        .ToList();

                /*
                 * Financeiro permanece all-time/current.
                 */
                var customerReceivables =
                    receivables
                        .Where(
                            x =>
                                allCustomerSaleIds
                                    .Contains(
                                        x.SaleId))
                        .ToList();

                var receivableSaleIds =
                    customerReceivables
                        .Select(
                            x => x.SaleId)
                        .ToHashSet();

                decimal received = 0m;
                decimal outstanding = 0m;
                decimal overdue = 0m;

                foreach (
                    var receivable
                    in customerReceivables)
                {
                    var paid =
                        paymentTotals
                            .GetValueOrDefault(
                                receivable.Id);

                    var balance =
                        Math.Max(
                            0m,
                            receivable.OriginalAmount -
                            paid);

                    received +=
                        paid;

                    outstanding +=
                        balance;

                    if (
                        balance > 0m &&
                        receivable.DueDate.Date <
                        today)
                    {
                        overdue +=
                            balance;
                    }
                }

                return new CustomerListItemDto(
                    customer.Id,
                    customer.Name,
                    customer.Phone,
                    commercialSales.Count,
                    commercialItems.Sum(
                        x =>
                            x.Quantity *
                            x.UnitPrice),
                    commercialItems.Sum(
                        x =>
                            x.Quantity),
                    commercialSales
                        .OrderByDescending(
                            x =>
                                x.OccurredAtUtc)
                        .Select(
                            x =>
                                (DateTime?)
                                x.OccurredAtUtc)
                        .FirstOrDefault(),
                    received,
                    outstanding,
                    overdue,
                    allCustomerSales.Count(
                        x =>
                            x.Origin ==
                                SaleOrigin.Operational &&
                            !receivableSaleIds.Contains(
                                x.Id)));
            })
            .ToList();
    }

    public Task<CustomerDetailsDto?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(
            customerId,
            from: null,
            to: null,
            cancellationToken);
    }

    public async Task<CustomerDetailsDto?> GetByIdAsync(
        Guid customerId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        BusinessDate.ValidateRange(
            from,
            to);

        DateTime? fromUtc =
            from.HasValue
                ? BusinessDate.StartOfDayUtc(
                    from.Value)
                : null;

        DateTime? toExclusiveUtc =
            to.HasValue
                ? BusinessDate.ExclusiveEndUtc(
                    to.Value)
                : null;

        var customer =
            await _dbContext.Customers
                .AsNoTracking()
                .Where(
                    x =>
                        x.Id ==
                            customerId &&
                        x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Phone
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (customer is null)
        {
            return null;
        }

        /*
         * Carregamos todas as vendas deste cliente.
         * Depois separamos atividade comercial do
         * período e posição financeira atual.
         */
        var allSales =
            await _dbContext.Sales
                .AsNoTracking()
                .Where(
                    x =>
                        x.CustomerId ==
                        customerId)
                .OrderByDescending(
                    x =>
                        x.OccurredAtUtc)
                .ThenByDescending(
                    x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.OccurredAtUtc,
                    x.Origin
                })
                .ToListAsync(
                    cancellationToken);

        var commercialSales =
            allSales
                .Where(
                    x =>
                        IsInPeriod(
                            x.OccurredAtUtc,
                            fromUtc,
                            toExclusiveUtc))
                .ToList();

        var allSaleIds =
            allSales
                .Select(
                    x => x.Id)
                .ToHashSet();

        var commercialSaleIds =
            commercialSales
                .Select(
                    x => x.Id)
                .ToHashSet();

        var allItems =
            await _dbContext.SaleItems
                .AsNoTracking()
                .Select(x => new
                {
                    x.SaleId,
                    x.ProductId,
                    ProductName =
                        x.Product.Name,
                    x.Quantity,
                    x.UnitPrice
                })
                .ToListAsync(
                    cancellationToken);

        var commercialItems =
            allItems
                .Where(
                    x =>
                        commercialSaleIds
                            .Contains(
                                x.SaleId))
                .ToList();

        /*
         * Recebíveis usam TODAS as vendas do cliente,
         * não somente as vendas do período.
         */
        var allReceivables =
            await _dbContext.Receivables
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.SaleId,
                    x.DueDate,
                    x.OriginalAmount
                })
                .ToListAsync(
                    cancellationToken);

        var receivables =
            allReceivables
                .Where(
                    x =>
                        allSaleIds.Contains(
                            x.SaleId))
                .ToList();

        var receivableIds =
            receivables
                .Select(
                    x => x.Id)
                .ToHashSet();

        var allPayments =
            await _dbContext.Payments
                .Where(
                    x =>
                        x.Reversal == null)
                .AsNoTracking()
                .Select(x => new
                {
                    x.ReceivableId,
                    x.Amount
                })
                .ToListAsync(
                    cancellationToken);

        var payments =
            allPayments
                .Where(
                    x =>
                        receivableIds.Contains(
                            x.ReceivableId))
                .ToList();

        var paymentTotals =
            payments
                .GroupBy(
                    x => x.ReceivableId)
                .ToDictionary(
                    x => x.Key,
                    x =>
                        x.Sum(
                            y => y.Amount));

        var receivableSaleIds =
            receivables
                .Select(
                    x => x.SaleId)
                .ToHashSet();

        var today =
            BusinessDate.Today;

        decimal totalReceived = 0m;
        decimal outstanding = 0m;
        decimal overdue = 0m;

        var openInstallments = 0;
        var overdueInstallments = 0;

        foreach (
            var receivable
            in receivables)
        {
            var paid =
                paymentTotals
                    .GetValueOrDefault(
                        receivable.Id);

            var balance =
                Math.Max(
                    0m,
                    receivable.OriginalAmount -
                    paid);

            totalReceived +=
                paid;

            outstanding +=
                balance;

            if (balance > 0m)
            {
                openInstallments++;
            }

            if (
                balance > 0m &&
                receivable.DueDate.Date <
                today)
            {
                overdue +=
                    balance;

                overdueInstallments++;
            }
        }

        var products =
            commercialItems
                .GroupBy(
                    x => new
                    {
                        x.ProductId,
                        x.ProductName
                    })
                .Select(group =>
                    new CustomerProductSummaryDto(
                        group.Key.ProductId,
                        group.Key.ProductName,
                        group.Sum(
                            x =>
                                x.Quantity),
                        group.Sum(
                            x =>
                                x.Quantity *
                                x.UnitPrice)))
                .OrderByDescending(
                    x =>
                        x.TotalQuantity)
                .ToList();

        var purchases =
            commercialSales
                .Select(sale =>
                {
                    var saleItems =
                        commercialItems
                            .Where(
                                x =>
                                    x.SaleId ==
                                    sale.Id)
                            .ToList();

                    return new CustomerPurchaseDto(
                        sale.Id,
                        sale.OccurredAtUtc,
                        sale.Origin ==
                            SaleOrigin.Legacy
                                ? "Legado"
                                : "EfratAgro",
                        saleItems.Count,
                        saleItems.Sum(
                            x =>
                                x.Quantity),
                        saleItems.Sum(
                            x =>
                                x.Quantity *
                                x.UnitPrice),
                        saleItems
                            .Select(item =>
                                new CustomerPurchaseProductDto(
                                    item.ProductId,
                                    item.ProductName,
                                    item.Quantity,
                                    item.UnitPrice,
                                    item.Quantity *
                                    item.UnitPrice))
                            .ToList());
                })
                .ToList();

        return new CustomerDetailsDto(
            customer.Id,
            customer.Name,
            customer.Phone,
            commercialSales.Count,
            commercialItems.Sum(
                x =>
                    x.Quantity *
                    x.UnitPrice),
            commercialItems.Sum(
                x =>
                    x.Quantity),
            commercialSales
                .Select(
                    x =>
                        (DateTime?)
                        x.OccurredAtUtc)
                .FirstOrDefault(),
            new CustomerFinancialSummaryDto(
                receivables.Sum(
                    x =>
                        x.OriginalAmount),
                totalReceived,
                outstanding,
                overdue,
                openInstallments,
                overdueInstallments,
                allSales.Count(
                    x =>
                        x.Origin ==
                            SaleOrigin.Operational &&
                        !receivableSaleIds.Contains(
                            x.Id))),
            products,
            purchases);
    }

    private static bool IsInPeriod(
        DateTime occurredAtUtc,
        DateTime? fromUtc,
        DateTime? toExclusiveUtc)
    {
        if (
            fromUtc.HasValue &&
            occurredAtUtc <
            fromUtc.Value)
        {
            return false;
        }

        if (
            toExclusiveUtc.HasValue &&
            occurredAtUtc >=
            toExclusiveUtc.Value)
        {
            return false;
        }

        return true;
    }
}
