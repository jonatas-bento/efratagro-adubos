using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.LegacyImporter.Commercial;
using EfratAgro.Adubos.LegacyImporter.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class LegacyCommercialInventoryIsolationTests
{
    [Fact]
    public async Task CommercialLegacyImport_ShouldPreservePhysicalAndFinancialLedgers()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var supplier =
            new Supplier(
                "HERINGER");

        var warehouse =
            new Warehouse(
                "Armazém Principal");

        database.DbContext.Suppliers.Add(
            supplier);

        database.DbContext.Warehouses.Add(
            warehouse);

        await database.DbContext
            .SaveChangesAsync();

        var product =
            new Product(
                "PRODUTO TESTE",
                supplier.Id);

        database.DbContext.Products.Add(
            product);

        await database.DbContext
            .SaveChangesAsync();

        var openingMovement =
            new InventoryMovement(
                product.Id,
                warehouse.Id,
                InventoryMovementType.OpeningBalance,
                StockBucket.UnclassifiedLegacy,
                25m,
                new DateTime(
                    2026,
                    9,
                    1,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc),
                null,
                "TEST_OPENING",
                null,
                "Saldo físico existente antes do histórico comercial.");

        database.DbContext.InventoryMovements.Add(
            openingMovement);

        await database.DbContext
            .SaveChangesAsync();

        var inventoryCountBefore =
            await database.DbContext
                .InventoryMovements
                .CountAsync();

        var inventoryQuantityBefore =
            await database.DbContext
                .InventoryMovements
                .SumAsync(
                    x => x.Quantity);

        var receivablesBefore =
            await database.DbContext
                .Receivables
                .CountAsync();

        var paymentsBefore =
            await database.DbContext
                .Payments
                .CountAsync();

        var saleDate =
            new DateTime(
                2026,
                8,
                15);

        var row =
            new CommercialImportRow(
                Sheet:
                    "HERINGER",
                ExcelRow:
                    10,
                Product:
                    "PRODUTO TESTE",
                Document:
                    "DOC-001",
                RawSaleDate:
                    "15/08/2026",
                SaleDate:
                    saleDate,
                DateQuality:
                    "EXACT",
                Seller:
                    "VENDEDOR TESTE",
                Address:
                    "ENDEREÇO TESTE",
                Customer:
                    "CLIENTE LEGADO TESTE",
                CanonicalCustomer:
                    "CLIENTE LEGADO TESTE",
                Quantity:
                    2m,
                UnitPrice:
                    50m,
                FinancialRaw:
                    "PENDENTE",
                TravaRaw:
                    string.Empty,
                DeliveryStatusRaw:
                    "PENDENTE",
                ObservationRaw:
                    string.Empty,
                Issues:
                    []);

        var transactionKey =
            CommercialImportPlanner
                .BuildTransactionKey(
                    saleDate,
                    row.Document);

        var transaction =
            new CommercialTransactionPlan(
                TransactionKey:
                    transactionKey,
                DocumentNumber:
                    row.Document,
                TransactionDate:
                    saleDate,
                CustomerName:
                    row.Customer,
                CanonicalCustomer:
                    row.CanonicalCustomer,
                Items:
                    [row],
                Issues:
                    []);

        var plan =
            new CommercialImportPlan(
                Rows:
                    [row],
                UnkeyedRows:
                    [],
                SafeTransactions:
                    [transaction],
                ReviewTransactions:
                    [],
                LegacyProductsToCreate:
                    []);

        var service =
            new CommercialImportPersistenceService(
                database.DbContext);

        var result =
            await service.ImportAsync(
                filePath:
                    "commercial-history-test.xlsx",
                fileHash:
                    new string(
                        'A',
                        64),
                plan:
                    plan);

        Assert.Equal(
            1,
            result.LegacyRows);

        Assert.Equal(
            1,
            result.SafeTransactions);

        Assert.Equal(
            1,
            result.SafeItems);

        Assert.Equal(
            1,
            result.SalesCreated);

        Assert.Equal(
            1,
            result.SaleItemsCreated);

        var sale =
            await database.DbContext.Sales
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            SaleOrigin.Legacy,
            sale.Origin);

        Assert.Equal(
            DeliveryStatus.NotTracked,
            sale.DeliveryStatus);

        var saleItem =
            await database.DbContext.SaleItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            sale.Id,
            saleItem.SaleId);

        Assert.Equal(
            product.Id,
            saleItem.ProductId);

        Assert.Equal(
            2m,
            saleItem.Quantity);

        Assert.Equal(
            50m,
            saleItem.UnitPrice);

        var metadata =
            await database.DbContext
                .LegacySaleMetadataEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            sale.Id,
            metadata.SaleId);

        Assert.Equal(
            transactionKey,
            metadata.TransactionKey);

        var legacyRow =
            await database.DbContext
                .LegacyImportRows
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            saleItem.Id,
            legacyRow.SaleItemId);

        var inventoryCountAfter =
            await database.DbContext
                .InventoryMovements
                .CountAsync();

        var inventoryQuantityAfter =
            await database.DbContext
                .InventoryMovements
                .SumAsync(
                    x => x.Quantity);

        var receivablesAfter =
            await database.DbContext
                .Receivables
                .CountAsync();

        var paymentsAfter =
            await database.DbContext
                .Payments
                .CountAsync();

        Assert.Equal(
            inventoryCountBefore,
            inventoryCountAfter);

        Assert.Equal(
            inventoryQuantityBefore,
            inventoryQuantityAfter);

        Assert.Equal(
            25m,
            inventoryQuantityAfter);

        Assert.Equal(
            receivablesBefore,
            receivablesAfter);

        Assert.Equal(
            paymentsBefore,
            paymentsAfter);

        Assert.Equal(
            0,
            receivablesAfter);

        Assert.Equal(
            0,
            paymentsAfter);
    }

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private TestDatabase(
            SqliteConnection connection,
            AdubosDbContext dbContext)
        {
            _connection =
                connection;

            DbContext =
                dbContext;
        }

        public AdubosDbContext DbContext
        {
            get;
        }

        public static async Task<TestDatabase>
            CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:;Foreign Keys=True");

            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<
                    AdubosDbContext>()
                    .UseSqlite(
                        connection)
                    .Options;

            var dbContext =
                new AdubosDbContext(
                    options);

            await dbContext.Database
                .EnsureCreatedAsync();

            return new TestDatabase(
                connection,
                dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext
                .DisposeAsync();

            await _connection
                .DisposeAsync();
        }
    }
}
