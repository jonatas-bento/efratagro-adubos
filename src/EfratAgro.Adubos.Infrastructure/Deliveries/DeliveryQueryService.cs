using EfratAgro.Adubos.Application.Deliveries;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Deliveries;

public sealed class DeliveryQueryService
    : IDeliveryQueryService
{
    private readonly AdubosDbContext _dbContext;

    public DeliveryQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DeliverySummaryDto>>
        GetAsync(
            bool pendingOnly,
            int take,
            CancellationToken cancellationToken = default)
    {
        var limit =
            Math.Clamp(take, 1, 100);

        var query =
            _dbContext.Sales
                .AsNoTracking()
                .AsQueryable();

        if (pendingOnly)
        {
            query = query.Where(
                x =>
                    x.DeliveryStatus ==
                    DeliveryStatus.Pending);
        }

        var sales =
            await query
                .OrderByDescending(
                    x => x.OccurredAtUtc)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    CustomerName =
                        x.Customer.Name,
                    x.OccurredAtUtc,
                    x.DeliveryMethod,
                    x.DeliveryStatus,
                    x.DeliveredAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        var itemRows =
            await _dbContext.SaleItems
                .AsNoTracking()
                .Select(x => new
                {
                    x.SaleId,
                    x.Quantity
                })
                .ToListAsync(
                    cancellationToken);

        var quantities =
            itemRows
                .GroupBy(x => x.SaleId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group.Sum(
                            x => x.Quantity));

        return sales
            .Select(sale =>
                new DeliverySummaryDto(
                    sale.Id,
                    sale.CustomerName,
                    sale.OccurredAtUtc,
                    quantities.GetValueOrDefault(
                        sale.Id),
                    MethodLabel(
                        sale.DeliveryMethod),
                    StatusLabel(
                        sale.DeliveryStatus),
                    sale.DeliveredAtUtc))
            .ToList();
    }

    private static string MethodLabel(
        DeliveryMethod method)
    {
        return method switch
        {
            DeliveryMethod.Delivery =>
                "Entrega",

            DeliveryMethod.StorePickup =>
                "Retirada na loja",

            DeliveryMethod.WarehousePickup =>
                "Retirada no armazém",

            _ =>
                "Não informado"
        };
    }

    private static string StatusLabel(
        DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Delivered =>
                "Entregue",

            _ =>
                "Pendente"
        };
    }
}
