using EfratAgro.Adubos.Application.Customers;
using EfratAgro.Adubos.Domain.Sales;
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

    public async Task<IReadOnlyList<CustomerListItemDto>>
        GetAsync(
            string? search,
            int take,
            CancellationToken cancellationToken = default)
    {
        var limit =
            Math.Clamp(take, 1, 200);

        var query =
            _dbContext.Customers
                .AsNoTracking()
                .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized =
                search.Trim()
                    .ToUpperInvariant();

            query =
                query.Where(
                    x =>
                        x.NormalizedName.Contains(
                            normalized) ||
                        (
                            x.Phone != null &&
                            x.Phone.Contains(search.Trim())
                        ));
        }

        var customers =
            await query
                .OrderBy(x => x.Name)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Phone
                })
                .ToListAsync(
                    cancellationToken);

        var sales =
            await _dbContext.Sales
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.CustomerId,
                    x.OccurredAtUtc
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
                .GroupBy(x => x.ReceivableId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(y => y.Amount));

        var today =
            DateTime.UtcNow.Date;

        return customers
            .Select(customer =>
            {
                var customerSales =
                    sales
                        .Where(
                            x =>
                                x.CustomerId ==
                                customer.Id)
                        .ToList();

                var saleIds =
                    customerSales
                        .Select(x => x.Id)
                        .ToHashSet();

                var customerItems =
                    saleItems
                        .Where(
                            x =>
                                saleIds.Contains(
                                    x.SaleId))
                        .ToList();

                var customerReceivables =
                    receivables
                        .Where(
                            x =>
                                saleIds.Contains(
                                    x.SaleId))
                        .ToList();

                var receivableSaleIds =
                    customerReceivables
                        .Select(x => x.SaleId)
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

                    received += paid;
                    outstanding += balance;

                    if (
                        balance > 0 &&
                        receivable.DueDate.Date <
                        today)
                    {
                        overdue += balance;
                    }
                }

                return new CustomerListItemDto(
                    customer.Id,
                    customer.Name,
                    customer.Phone,
                    customerSales.Count,
                    customerItems.Sum(
                        x =>
                            x.Quantity *
                            x.UnitPrice),
                    customerItems.Sum(
                        x => x.Quantity),
                    customerSales
                        .OrderByDescending(
                            x => x.OccurredAtUtc)
                        .Select(
                            x =>
                                (DateTime?)
                                x.OccurredAtUtc)
                        .FirstOrDefault(),
                    received,
                    outstanding,
                    overdue,
                    customerSales.Count(
                        x =>
                            !receivableSaleIds.Contains(
                                x.Id)));
            })
            .ToList();
    }

    public async Task<CustomerDetailsDto?>
        GetByIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
    {
        var customer =
            await _dbContext.Customers
                .AsNoTracking()
                .Where(
                    x =>
                        x.Id == customerId &&
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

        var sales =
            await _dbContext.Sales
                .AsNoTracking()
                .Where(
                    x =>
                        x.CustomerId ==
                        customerId)
                .OrderByDescending(
                    x => x.OccurredAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.OccurredAtUtc,
                    x.Origin
                })
                .ToListAsync(
                    cancellationToken);

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

        var saleIds =
            sales
                .Select(x => x.Id)
                .ToHashSet();

        var items =
            allItems
                .Where(
                    x =>
                        saleIds.Contains(
                            x.SaleId))
                .ToList();

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
                        saleIds.Contains(
                            x.SaleId))
                .ToList();

        var receivableIds =
            receivables
                .Select(x => x.Id)
                .ToHashSet();

        var allPayments =
            await _dbContext.Payments
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
                .GroupBy(x => x.ReceivableId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(y => y.Amount));

        var receivableSaleIds =
            receivables
                .Select(x => x.SaleId)
                .ToHashSet();

        var today =
            DateTime.UtcNow.Date;

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

            totalReceived += paid;
            outstanding += balance;

            if (balance > 0)
            {
                openInstallments++;
            }

            if (
                balance > 0 &&
                receivable.DueDate.Date <
                today)
            {
                overdue += balance;
                overdueInstallments++;
            }
        }

        var products =
            items
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
                            x => x.Quantity),
                        group.Sum(
                            x =>
                                x.Quantity *
                                x.UnitPrice)))
                .OrderByDescending(
                    x => x.TotalQuantity)
                .ToList();

        var purchases =
            sales
                .Select(sale =>
                {
                    var saleItems =
                        items
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
                            x => x.Quantity),
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
            sales.Count,
            items.Sum(
                x =>
                    x.Quantity *
                    x.UnitPrice),
            items.Sum(
                x => x.Quantity),
            sales
                .Select(
                    x =>
                        (DateTime?)
                        x.OccurredAtUtc)
                .FirstOrDefault(),
            new CustomerFinancialSummaryDto(
                receivables.Sum(
                    x => x.OriginalAmount),
                totalReceived,
                outstanding,
                overdue,
                openInstallments,
                overdueInstallments,
                sales.Count(
                    x =>
                        !receivableSaleIds.Contains(
                            x.Id))),
            products,
            purchases);
    }
}
