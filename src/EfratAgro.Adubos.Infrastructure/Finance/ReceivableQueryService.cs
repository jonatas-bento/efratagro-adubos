using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Infrastructure.Common;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Finance;

public sealed class ReceivableQueryService
    : IReceivableQueryService
{
    private readonly AdubosDbContext _dbContext;

    public ReceivableQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReceivablesResult> GetAsync(
        bool openOnly,
        int take,
        CancellationToken cancellationToken = default)
    {
        var limit =
            Math.Clamp(
                take,
                1,
                500);

        var receivables =
            await _dbContext.Receivables
                .AsNoTracking()
                .OrderBy(x => x.DueDate)
                .Select(x => new
                {
                    x.Id,
                    x.SaleId,
                    CustomerId =
                        x.Sale.CustomerId,
                    CustomerName =
                        x.Sale.Customer.Name,
                    x.InstallmentNumber,
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

        var paidByReceivable =
            payments
                .GroupBy(
                    x => x.ReceivableId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group.Sum(
                            x => x.Amount));

        var today =
            BusinessDate.Today;

        var calculated =
            receivables
                .Select(receivable =>
                {
                    var paid =
                        paidByReceivable
                            .GetValueOrDefault(
                                receivable.Id);

                    var outstanding =
                        Math.Max(
                            0m,
                            receivable.OriginalAmount -
                            paid);

                    var status =
                        CalculateStatus(
                            receivable.DueDate,
                            paid,
                            outstanding,
                            today);

                    return new ReceivableDto(
                        receivable.Id,
                        receivable.SaleId,
                        receivable.CustomerId,
                        receivable.CustomerName,
                        receivable.InstallmentNumber,
                        receivable.DueDate,
                        receivable.OriginalAmount,
                        paid,
                        outstanding,
                        status);
                })
                .ToList();

        var summary =
            new ReceivableSummaryDto(
                calculated.Sum(
                    x => x.OriginalAmount),

                calculated.Sum(
                    x => x.PaidAmount),

                calculated.Sum(
                    x => x.OutstandingAmount),

                calculated
                    .Where(
                        x =>
                            x.OutstandingAmount > 0 &&
                            x.DueDate.Date < today)
                    .Sum(
                        x => x.OutstandingAmount),

                calculated.Count(
                    x =>
                        x.OutstandingAmount > 0),

                calculated.Count(
                    x =>
                        x.OutstandingAmount > 0 &&
                        x.DueDate.Date < today));

        var items =
            calculated
                .Where(
                    x =>
                        !openOnly ||
                        x.OutstandingAmount > 0)
                .Take(limit)
                .ToList();

        return new ReceivablesResult(
            summary,
            items);
    }

    private static string CalculateStatus(
        DateTime dueDate,
        decimal paid,
        decimal outstanding,
        DateTime today)
    {
        if (outstanding <= 0m)
        {
            return "Pago";
        }

        if (dueDate.Date < today)
        {
            return paid > 0
                ? "Parcial em atraso"
                : "Vencido";
        }

        if (paid > 0)
        {
            return "Parcial";
        }

        return "Pendente";
    }
}
