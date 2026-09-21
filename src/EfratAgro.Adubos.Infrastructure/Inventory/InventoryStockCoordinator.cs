using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Inventory;

internal sealed record ProductStockAvailability(
    Guid ProductId,
    decimal PhysicalQuantity,
    decimal ReservedQuantity)
{
    public decimal AvailableQuantity =>
        PhysicalQuantity -
        ReservedQuantity;
}

internal static class InventoryStockCoordinator
{
    public static async Task<
        IReadOnlyDictionary<Guid, Product>>
        LockProductsAsync(
            AdubosDbContext dbContext,
            IEnumerable<Guid> productIds,
            bool requireActive,
            CancellationToken cancellationToken)
    {
        var ids =
            productIds
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

        var products =
            new Dictionary<Guid, Product>();

        var providerName =
            dbContext.Database.ProviderName;

        var isMySql =
            providerName?.Contains(
                "MySql",
                StringComparison.OrdinalIgnoreCase) ==
            true;

        if (isMySql)
        {
            foreach (var productId in ids)
            {
                var rows =
                    await dbContext.Products
                        .FromSqlInterpolated(
                            $"""
                            SELECT *
                            FROM `products`
                            WHERE `Id` = {productId}
                            FOR UPDATE
                            """)
                        .ToListAsync(
                            cancellationToken);

                var product =
                    rows.SingleOrDefault();

                if (product is null ||
                    (
                        requireActive &&
                        !product.IsActive
                    ))
                {
                    throw new InvalidOperationException(
                        "Produto não encontrado.");
                }

                products.Add(
                    product.Id,
                    product);
            }

            return products;
        }

        var allProducts =
            await dbContext.Products
                .ToListAsync(
                    cancellationToken);

        foreach (var productId in ids)
        {
            var product =
                allProducts.SingleOrDefault(
                    x => x.Id == productId);

            if (product is null ||
                (
                    requireActive &&
                    !product.IsActive
                ))
            {
                throw new InvalidOperationException(
                    "Produto não encontrado.");
            }

            products.Add(
                product.Id,
                product);
        }

        return products;
    }

    public static Task<
        IReadOnlyDictionary<
            Guid,
            ProductStockAvailability>>
        GetAvailabilityAsync(
            AdubosDbContext dbContext,
            IEnumerable<Guid> productIds,
            CancellationToken cancellationToken)
    {
        return GetAvailabilityAsync(
            dbContext,
            productIds,
            asOfExclusiveUtc: null,
            cancellationToken);
    }

    public static async Task<
        IReadOnlyDictionary<
            Guid,
            ProductStockAvailability>>
        GetAvailabilityAsync(
            AdubosDbContext dbContext,
            IEnumerable<Guid> productIds,
            DateTime? asOfExclusiveUtc,
            CancellationToken cancellationToken)
    {
        var ids =
            productIds
                .Distinct()
                .ToHashSet();

        var movementQuery =
            dbContext.InventoryMovements
                .AsNoTracking()
                .AsQueryable();

        if (asOfExclusiveUtc.HasValue)
        {
            var cutoffUtc =
                asOfExclusiveUtc.Value;

            movementQuery =
                movementQuery.Where(
                    x =>
                        x.OccurredAtUtc <
                        cutoffUtc);
        }

        var movementRows =
            await movementQuery
                .Select(
                    x => new
                    {
                        x.ProductId,
                        x.Quantity
                    })
                .ToListAsync(
                    cancellationToken);

        var reservationQuery =
            dbContext.InventoryReservations
                .AsNoTracking()
                .AsQueryable();

        if (asOfExclusiveUtc.HasValue)
        {
            var cutoffUtc =
                asOfExclusiveUtc.Value;

            reservationQuery =
                reservationQuery.Where(
                    x =>
                        x.ReservedAtUtc <
                            cutoffUtc &&
                        (
                            x.FulfilledAtUtc == null ||
                            x.FulfilledAtUtc >=
                                cutoffUtc
                        ));
        }
        else
        {
            reservationQuery =
                reservationQuery.Where(
                    x =>
                        x.Status ==
                        InventoryReservationStatus.Active);
        }

        var reservationRows =
            await reservationQuery
                .Select(
                    x => new
                    {
                        x.ProductId,
                        x.Quantity
                    })
                .ToListAsync(
                    cancellationToken);

        var physicalByProduct =
            movementRows
                .Where(
                    x =>
                        ids.Contains(
                            x.ProductId))
                .GroupBy(
                    x => x.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group.Sum(
                            x => x.Quantity));

        var reservedByProduct =
            reservationRows
                .Where(
                    x =>
                        ids.Contains(
                            x.ProductId))
                .GroupBy(
                    x => x.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group.Sum(
                            x => x.Quantity));

        return ids.ToDictionary(
            productId => productId,
            productId =>
                new ProductStockAvailability(
                    productId,
                    physicalByProduct
                        .GetValueOrDefault(
                            productId),
                    reservedByProduct
                        .GetValueOrDefault(
                            productId)));
    }
}
