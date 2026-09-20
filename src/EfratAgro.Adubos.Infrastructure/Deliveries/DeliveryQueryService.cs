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
            Math.Clamp(
                take,
                1,
                100);

        var query =
            _dbContext.Sales
                .AsNoTracking()
                .Where(
                    x =>
                        x.Origin ==
                            SaleOrigin.Operational
                        &&
                        (
                            x.DeliveryStatus ==
                                DeliveryStatus.Pending
                            ||
                            x.DeliveryStatus ==
                                DeliveryStatus.Delivered
                        ));

        if (pendingOnly)
        {
            query =
                query.Where(
                    x =>
                        x.DeliveryStatus ==
                            DeliveryStatus.Pending);
        }

        var sales =
            await query
                .OrderByDescending(
                    x => x.OccurredAtUtc)
                .Take(limit)
                .Select(
                    x => new
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
                .Select(
                    x => new
                    {
                        x.SaleId,
                        x.Quantity
                    })
                .ToListAsync(
                    cancellationToken);

        var quantities =
            itemRows
                .GroupBy(
                    x => x.SaleId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group.Sum(
                            x => x.Quantity));

        return sales
            .Select(
                sale =>
                    new DeliverySummaryDto(
                        sale.Id,
                        sale.CustomerName,
                        AsUtc(
                        sale.OccurredAtUtc),
                        quantities.GetValueOrDefault(
                            sale.Id),
                        MethodLabel(
                            sale.DeliveryMethod),
                        ToStatusCode(
                            sale.DeliveryStatus),
                        StatusLabel(
                            sale.DeliveryStatus),
                        AsUtc(
                        sale.DeliveredAtUtc)))
            .ToList();
    }

    private static DateTime AsUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };
    }

    private static DateTime? AsUtc(
        DateTime? value)
    {
        return value.HasValue
            ? AsUtc(value.Value)
            : null;
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

    private static DeliveryStatusCode ToStatusCode(
        DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending =>
                DeliveryStatusCode.Pending,
            DeliveryStatus.Delivered =>
                DeliveryStatusCode.Delivered,
            DeliveryStatus.NotTracked =>
                DeliveryStatusCode.NotTracked,
            _ =>
                DeliveryStatusCode.Unspecified
        };
    }

    private static string StatusLabel(
        DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending =>
                "Pendente",

            DeliveryStatus.Delivered =>
                "Entregue",

            DeliveryStatus.NotTracked =>
                "Não rastreado",

            _ =>
                "Não informado"
        };
    }
}
