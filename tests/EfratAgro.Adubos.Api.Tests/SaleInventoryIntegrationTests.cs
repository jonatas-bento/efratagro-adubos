using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Sales;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class SaleInventoryIntegrationTests
{
    [Fact]
    public async Task OperationalSale_ShouldCreateOneAuditableStockExitPerItem()
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
                    100m,
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
                    100m,
                    openingAtUtc,
                    null,
                    "TEST_OPENING",
                    null,
                    "Saldo inicial de teste."));

        await database.DbContext
            .SaveChangesAsync();

        var customerId =
            Guid.NewGuid();

        await InsertCustomerAsync(
            database.DbContext,
            customerId);

        var service =
            new SaleService(
                database.DbContext);

        var request =
            new CreateSaleRequest(
                CustomerId:
                    customerId,
                Items:
                [
                    new CreateSaleItemRequest(
                        productA.Id,
                        Quantity: 10m,
                        UnitPrice: 10m),
                    new CreateSaleItemRequest(
                        productB.Id,
                        Quantity: 5m,
                        UnitPrice: 20m)
                ],
                DeliveryMethod:
                    DeliveryMethod.Delivery,
                Receivables:
                [
                    new CreateSaleReceivableRequest(
                        InstallmentNumber: 1,
                        DueDate:
                            new DateTime(
                                2026,
                                10,
                                19),
                        Amount: 200m)
                ]);

        var result =
            await service.CreateAsync(
                request);

        var sale =
            await database.DbContext.Sales
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        result.SaleId);

        Assert.Equal(
            SaleOrigin.Operational,
            sale.Origin);

        var saleItems =
            await database.DbContext.SaleItems
                .AsNoTracking()
                .Where(
                    x =>
                        x.SaleId ==
                        result.SaleId)
                .ToListAsync();

        Assert.Equal(
            2,
            saleItems.Count);

        var movements =
            await database.DbContext
                .InventoryMovements
                .AsNoTracking()
                .Where(
                    x =>
                        x.ReferenceType ==
                            "SALE" &&
                        x.ReferenceId ==
                            result.SaleId)
                .ToListAsync();

        Assert.Equal(
            2,
            movements.Count);

        Assert.All(
            movements,
            movement =>
            {
                Assert.Equal(
                    InventoryMovementType.Sale,
                    movement.Type);

                Assert.Equal(
                    StockBucket.Normal,
                    movement.Bucket);

                Assert.Equal(
                    warehouse.Id,
                    movement.WarehouseId);

                Assert.Equal(
                    "SALE",
                    movement.ReferenceType);

                Assert.Equal(
                    result.SaleId,
                    movement.ReferenceId);

                Assert.True(
                    movement.Quantity < 0);
            });

        var movementByProduct =
            movements.ToDictionary(
                x => x.ProductId);

        Assert.Equal(
            -10m,
            movementByProduct[
                productA.Id]
                .Quantity);

        Assert.Equal(
            -5m,
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
            90m,
            productABalance);

        Assert.Equal(
            95m,
            productBBalance);
    }

    private static Task<int>
        InsertCustomerAsync(
            AdubosDbContext dbContext,
            Guid customerId)
    {
        var createdAtUtc =
            new DateTime(
                2026,
                9,
                19,
                10,
                0,
                0,
                DateTimeKind.Utc);

        string? phone =
            null;

        DateTime? updatedAtUtc =
            null;

        return dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO customers
                (
                    Id,
                    Name,
                    NormalizedName,
                    Phone,
                    IsActive,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    {customerId},
                    {"CLIENTE TESTE ESTOQUE"},
                    {"CLIENTE TESTE ESTOQUE"},
                    {phone},
                    {true},
                    {createdAtUtc},
                    {updatedAtUtc}
                )
                """);
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
