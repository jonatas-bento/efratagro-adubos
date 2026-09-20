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

        var stockRows =
            await _dbContext.InventoryMovements
                .AsNoTracking()
                .GroupBy(x => x.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(x => x.Quantity)
                })
                .ToListAsync(cancellationToken);

        var stockByProduct =
            stockRows.ToDictionary(
                x => x.ProductId,
                x => x.Quantity);

        return products
            .Select(product =>
                new InventoryItemDto(
                    product.Id,
                    product.ProductName,
                    product.SupplierName,
                    product.IsActive,
                    stockByProduct.GetValueOrDefault(
                        product.Id,
                        0m)))
            .ToList();
    }
}
