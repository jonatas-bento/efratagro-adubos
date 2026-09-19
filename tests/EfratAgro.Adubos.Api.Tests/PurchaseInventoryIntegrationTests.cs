using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Purchases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class PurchaseInventoryIntegrationTests
{
    [Fact]
    public async Task OperationalPurchase_ShouldCreateOneAuditableStockEntryPerItem()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var supplier =
            new Supplier(
                "FORNECEDOR TESTE");

        var warehouse =
            new Warehouse(
                "Armazém Principal");

        database.DbContext.Suppliers.Add(
            supplier);

        database.DbContext.Warehouses.Add(
            warehouse);

        await database.DbContext
            .SaveChangesAsync();

        var productA =
            new Product(
                "PRODUTO A",
                supplier.Id);

        var productB =
            new Product(
                "PRODUTO B",
                supplier.Id);

        database.DbContext.Products.AddRange(
            productA,
            productB);

        await database.DbContext
            .SaveChangesAsync();

        var openingAtUtc =
            new DateTime(
                2026,
                9,
                19,
                10,
                0,
                0,
                DateTimeKind.Utc);

        database.DbContext.InventoryMovements
            .AddRange(
                new InventoryMovement(
                    productA.Id,
                    warehouse.Id,
                    InventoryMovementType.OpeningBalance,
                    StockBucket.UnclassifiedLegacy,
                    30m,
                    openingAtUtc,
                    null,
                    "TEST_OPENING",
                    null,
                    "Saldo inicial de teste."),
                new InventoryMovement(
                    productB.Id,
                    warehouse.Id,
                    InventoryMovementType.OpeningBalance,
                    StockBucket.UnclassifiedLegacy,
                    40m,
                    openingAtUtc,
                    null,
                    "TEST_OPENING",
                    null,
                    "Saldo inicial de teste."));

        await database.DbContext
            .SaveChangesAsync();

        var service =
            new PurchaseService(
                database.DbContext);

        var request =
            new CreatePurchaseRequest(
                SupplierId:
                    supplier.Id,
                Items:
                [
                    new CreatePurchaseItemRequest(
                        productA.Id,
                        Quantity: 10m,
                        UnitCost: 12.50m),
                    new CreatePurchaseItemRequest(
                        productB.Id,
                        Quantity: 5m,
                        UnitCost: 20m)
                ]);

        var result =
            await service.CreateAsync(
                request);

        Assert.Equal(
            supplier.Id,
            result.SupplierId);

        Assert.Equal(
            2,
            result.Items);

        Assert.Equal(
            15m,
            result.TotalQuantity);

        Assert.Equal(
            225m,
            result.TotalValue);

        var purchase =
            await database.DbContext.Purchases
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        result.PurchaseId);

        Assert.Equal(
            supplier.Id,
            purchase.SupplierId);

        var purchaseItems =
            await database.DbContext
                .PurchaseItems
                .AsNoTracking()
                .Where(
                    x =>
                        x.PurchaseId ==
                        result.PurchaseId)
                .ToListAsync();

        Assert.Equal(
            2,
            purchaseItems.Count);

        var movements =
            await database.DbContext
                .InventoryMovements
                .AsNoTracking()
                .Where(
                    x =>
                        x.ReferenceType ==
                            "PURCHASE" &&
                        x.ReferenceId ==
                            result.PurchaseId)
                .ToListAsync();

        Assert.Equal(
            2,
            movements.Count);

        Assert.All(
            movements,
            movement =>
            {
                Assert.Equal(
                    InventoryMovementType.Purchase,
                    movement.Type);

                Assert.Equal(
                    StockBucket.Normal,
                    movement.Bucket);

                Assert.Equal(
                    warehouse.Id,
                    movement.WarehouseId);

                Assert.Equal(
                    "PURCHASE",
                    movement.ReferenceType);

                Assert.Equal(
                    result.PurchaseId,
                    movement.ReferenceId);

                Assert.True(
                    movement.Quantity > 0);
            });

        var movementByProduct =
            movements.ToDictionary(
                x => x.ProductId);

        Assert.Equal(
            10m,
            movementByProduct[
                productA.Id]
                .Quantity);

        Assert.Equal(
            5m,
            movementByProduct[
                productB.Id]
                .Quantity);

        var productABalance =
            await database.DbContext
                .InventoryMovements
                .Where(
                    x =>
                        x.ProductId ==
                        productA.Id)
                .SumAsync(
                    x => x.Quantity);

        var productBBalance =
            await database.DbContext
                .InventoryMovements
                .Where(
                    x =>
                        x.ProductId ==
                        productB.Id)
                .SumAsync(
                    x => x.Quantity);

        Assert.Equal(
            40m,
            productABalance);

        Assert.Equal(
            45m,
            productBBalance);
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
