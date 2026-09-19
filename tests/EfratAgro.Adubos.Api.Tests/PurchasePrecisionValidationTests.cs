using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Purchases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class PurchasePrecisionValidationTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectQuantityWithMoreThanThreeDecimalPlaces()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service =
            new PurchaseService(
                database.DbContext);

        var request =
            new CreatePurchaseRequest(
                SupplierId:
                    Guid.NewGuid(),
                Items:
                [
                    new CreatePurchaseItemRequest(
                        ProductId:
                            Guid.NewGuid(),
                        Quantity:
                            1.0001m,
                        UnitCost:
                            10.00m)
                ]);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.CreateAsync(
                    request));

        Assert.Empty(
            await database.DbContext.Purchases
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await database.DbContext.PurchaseItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await database.DbContext.InventoryMovements
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnitCostWithMoreThanTwoDecimalPlaces()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service =
            new PurchaseService(
                database.DbContext);

        var request =
            new CreatePurchaseRequest(
                SupplierId:
                    Guid.NewGuid(),
                Items:
                [
                    new CreatePurchaseItemRequest(
                        ProductId:
                            Guid.NewGuid(),
                        Quantity:
                            1.000m,
                        UnitCost:
                            10.001m)
                ]);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.CreateAsync(
                    request));

        Assert.Empty(
            await database.DbContext.Purchases
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await database.DbContext.PurchaseItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await database.DbContext.InventoryMovements
                .AsNoTracking()
                .ToListAsync());
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
