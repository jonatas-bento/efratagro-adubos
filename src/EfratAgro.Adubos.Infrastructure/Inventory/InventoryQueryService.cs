using EfratAgro.Adubos.Infrastructure.Common;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Application.Inventory;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Inventory;

public sealed class InventoryQueryService
    : IInventoryQueryService
{
    private readonly AdubosDbContext _dbContext;

    public InventoryQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InventoryItemDto>>
        GetInventoryAsync(
            string? search,
            DateOnly? asOf = null,
            CancellationToken cancellationToken = default)
    {
        var productsQuery =
            _dbContext.Products
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch =
                search
                    .Trim()
                    .ToUpperInvariant();

            productsQuery =
                productsQuery.Where(x =>
                    x.NormalizedName.Contains(normalizedSearch) ||
                    x.Supplier.NormalizedName.Contains(normalizedSearch));
        }

        var products =
            await productsQuery
                .OrderBy(x => x.Supplier.Name)
                .ThenBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    ProductName = x.Name,
                    SupplierName = x.Supplier.Name,
                    x.IsActive
                })
                .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return [];
        }

        var productIds =
            products
                .Select(
                    x => x.Id)
                .ToArray();

        DateTime? asOfExclusiveUtc =
            null;

        if (asOf.HasValue)
        {
            var baselineAtUtc =
                await _dbContext
                    .InventoryMovements
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.Type ==
                            InventoryMovementType.OpeningBalance)
                    .MinAsync(
                        x =>
                            (DateTime?)
                            x.OccurredAtUtc,
                        cancellationToken);

            if (baselineAtUtc.HasValue)
            {
                var baselineDate =
                    BusinessDate.ToLocalDate(
                        baselineAtUtc.Value);

                if (
                    asOf.Value <
                    baselineDate)
                {
                    throw new InvalidOperationException(
                        "O histórico físico do estoque está " +
                        $"disponível a partir de {baselineDate:dd/MM/yyyy}. " +
                        "Datas anteriores pertencem ao período legado " +
                        "sem baseline físico reconstruível.");
                }
            }

            asOfExclusiveUtc =
                BusinessDate.ExclusiveEndUtc(
                    asOf.Value);
        }

        var availabilityByProduct =
            await InventoryStockCoordinator
                .GetAvailabilityAsync(
                    _dbContext,
                    productIds,
                    asOfExclusiveUtc,
                    cancellationToken);

        return products
            .Select(product =>
            {
                var availability =
                    availabilityByProduct[
                        product.Id];

                return new InventoryItemDto(
                    product.Id,
                    product.ProductName,
                    product.SupplierName,
                    product.IsActive,
                    availability.PhysicalQuantity)
                {
                    ReservedQuantity =
                        availability.ReservedQuantity,
                    AvailableQuantity =
                        availability.AvailableQuantity
                };
            })
            .ToList();
    }
}
