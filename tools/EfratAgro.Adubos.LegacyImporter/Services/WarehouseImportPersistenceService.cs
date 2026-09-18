using System.Security.Cryptography;
using System.Text.Json;
using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Legacy;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.LegacyImporter.Models;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.LegacyImporter.Services;

public sealed class WarehouseImportPersistenceService
{
    private readonly AdubosDbContext _dbContext;

    public WarehouseImportPersistenceService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarehouseImportResult> ImportAsync(
        string filePath,
        IReadOnlyList<WarehouseImportRow> rows,
        CancellationToken cancellationToken = default)
    {
        var fileHash =
            await CalculateSha256Async(
                filePath,
                cancellationToken);

        var alreadyImported =
            await _dbContext.LegacyImportBatches
                .AnyAsync(
                    x => x.SourceFileHash == fileHash,
                    cancellationToken);

        if (alreadyImported)
        {
            throw new InvalidOperationException(
                "This spreadsheet content has already been imported.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var batch =
                new LegacyImportBatch(
                    Path.GetFileName(filePath),
                    fileHash);

            _dbContext.LegacyImportBatches.Add(batch);

            var warehouse =
                await _dbContext.Warehouses
                    .SingleOrDefaultAsync(
                        x => x.Name == "Armazém Principal",
                        cancellationToken);

            if (warehouse is null)
            {
                warehouse =
                    new Warehouse(
                        "Armazém Principal");

                _dbContext.Warehouses.Add(
                    warehouse);
            }

            var existingSuppliers =
                await _dbContext.Suppliers
                    .ToListAsync(
                        cancellationToken);

            var suppliers =
                existingSuppliers.ToDictionary(
                    x => x.NormalizedName,
                    x => x,
                    StringComparer.OrdinalIgnoreCase);

            var existingProducts =
                await _dbContext.Products
                    .ToListAsync(
                        cancellationToken);

            var productIndex =
                existingProducts.ToDictionary(
                    x => BuildProductKey(
                        x.SupplierId,
                        x.NormalizedName),
                    x => x,
                    StringComparer.OrdinalIgnoreCase);

            var movementCount = 0;

            foreach (var row in rows)
            {
                var normalizedSupplier =
                    Normalize(
                        row.Supplier);

                if (!suppliers.TryGetValue(
                        normalizedSupplier,
                        out var supplier))
                {
                    supplier =
                        new Supplier(
                            row.Supplier);

                    suppliers.Add(
                        normalizedSupplier,
                        supplier);

                    _dbContext.Suppliers.Add(
                        supplier);
                }

                var normalizedProduct =
                    Normalize(
                        row.Product);

                var productKey =
                    BuildProductKey(
                        supplier.Id,
                        normalizedProduct);

                if (!productIndex.TryGetValue(
                        productKey,
                        out var product))
                {
                    product =
                        new Product(
                            row.Product,
                            supplier.Id,
                            row.Product);

                    productIndex.Add(
                        productKey,
                        product);

                    _dbContext.Products.Add(
                        product);
                }

                var rawData =
                    JsonSerializer.Serialize(
                        new
                        {
                            sheet = "ARMAZÉM",
                            row = row.ExcelRow,
                            sourceCell = row.SourceCell,
                            supplier = row.Supplier,
                            product = row.Product,
                            quantity = row.Quantity,
                            safraRaw = row.SafraRaw,
                            priceRaw = row.PriceRaw
                        });

                var legacyRow =
                    new LegacyImportRow(
                        batch.Id,
                        "ARMAZÉM",
                        row.ExcelRow,
                        row.SourceCell,
                        rawData);

                _dbContext.LegacyImportRows.Add(
                    legacyRow);

                if (row.Quantity <= 0)
                {
                    continue;
                }

                var movement =
                    new InventoryMovement(
                        product.Id,
                        warehouse.Id,
                        InventoryMovementType.OpeningBalance,
                        StockBucket.UnclassifiedLegacy,
                        row.Quantity,
                        batch.StartedAtUtc,
                        legacyRow.Id,
                        "LEGACY_IMPORT",
                        batch.Id,
                        "Saldo inicial importado da planilha legada.");

                _dbContext.InventoryMovements.Add(
                    movement);

                movementCount++;
            }

            batch.Complete(
                rows.Count,
                reviewRows: 0);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new WarehouseImportResult(
                batch.Id,
                fileHash,
                rows.Count,
                suppliers.Count,
                productIndex.Count,
                movementCount,
                rows.Sum(x => x.Quantity));
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static string Normalize(
        string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }

    private static string BuildProductKey(
        Guid supplierId,
        string normalizedProduct)
    {
        return
            $"{supplierId:N}:{normalizedProduct}";
    }

    private static async Task<string>
        CalculateSha256Async(
            string filePath,
            CancellationToken cancellationToken)
    {
        await using var stream =
            File.OpenRead(filePath);

        using var sha256 =
            SHA256.Create();

        var hash =
            await sha256.ComputeHashAsync(
                stream,
                cancellationToken);

        return Convert.ToHexString(
            hash);
    }
}

public sealed record WarehouseImportResult(
    Guid BatchId,
    string FileHash,
    int LegacyRows,
    int Suppliers,
    int Products,
    int InventoryMovements,
    decimal TotalQuantity);
