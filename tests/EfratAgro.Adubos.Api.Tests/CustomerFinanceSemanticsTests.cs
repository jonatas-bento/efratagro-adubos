using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Customers;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class CustomerFinanceSemanticsTests
{
    [Fact]
    public async Task CustomerHistory_ShouldKeepUnscheduledSaleSeparateFromFinancialBalance()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var customerId =
            Guid.NewGuid();

        await SeedScenarioAsync(
            database.DbContext,
            customerId);

        var service =
            new CustomerQueryService(
                database.DbContext);

        var customers =
            await service.GetAsync(
                search: null,
                take: 100);

        var customer =
            Assert.Single(
                customers);

        Assert.Equal(
            customerId,
            customer.Id);

        Assert.Equal(
            2,
            customer.SalesCount);

        Assert.Equal(
            1300m,
            customer.TotalPurchased);

        Assert.Equal(
            100m,
            customer.TotalReceived);

        Assert.Equal(
            200m,
            customer.OutstandingAmount);

        Assert.Equal(
            1,
            customer.SalesWithoutFinancialSchedule);

        var details =
            await service.GetByIdAsync(
                customerId);

        Assert.NotNull(
            details);

        Assert.Equal(
            2,
            details.SalesCount);

        Assert.Equal(
            1300m,
            details.TotalPurchased);

        Assert.Equal(
            300m,
            details.Financial.TotalReceivables);

        Assert.Equal(
            100m,
            details.Financial.TotalReceived);

        Assert.Equal(
            200m,
            details.Financial.OutstandingAmount);

        Assert.Equal(
            1,
            details.Financial
                .SalesWithoutFinancialSchedule);

        Assert.Equal(
            2,
            details.Purchases.Count);
    }

    private static async Task SeedScenarioAsync(
        AdubosDbContext dbContext,
        Guid customerId)
    {
        var supplierId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        var unscheduledSaleId =
            Guid.NewGuid();

        var scheduledSaleId =
            Guid.NewGuid();

        var firstReceivableId =
            Guid.NewGuid();

        var secondReceivableId =
            Guid.NewGuid();

        var thirdReceivableId =
            Guid.NewGuid();

        var createdAtUtc =
            new DateTime(
                2026,
                9,
                19,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var firstSaleAtUtc =
            new DateTime(
                2026,
                9,
                18,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var secondSaleAtUtc =
            new DateTime(
                2026,
                9,
                19,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var deliveryMethod =
            (int)DeliveryMethod.Delivery;

        var deliveryStatus =
            (int)DeliveryStatus.Pending;

        var saleOrigin =
            (int)SaleOrigin.Operational;

        var paymentMethod =
            (int)PaymentMethod.Pix;

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO suppliers
                (
                    Id,
                    Name,
                    NormalizedName,
                    IsActive,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    {supplierId},
                    {"FORNECEDOR TESTE"},
                    {"FORNECEDOR TESTE"},
                    {true},
                    {createdAtUtc},
                    {null}
                )
                """);

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO products
                (
                    Id,
                    Name,
                    NormalizedName,
                    LegacyName,
                    SupplierId,
                    IsActive,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    {productId},
                    {"BIOFOSFATO TESTE"},
                    {"BIOFOSFATO TESTE"},
                    {null},
                    {supplierId},
                    {true},
                    {createdAtUtc},
                    {null}
                )
                """);

        await dbContext.Database
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
                    {"CLIENTE TESTE FINANCEIRO"},
                    {"CLIENTE TESTE FINANCEIRO"},
                    {null},
                    {true},
                    {createdAtUtc},
                    {null}
                )
                """);

        await InsertSaleAsync(
            dbContext,
            unscheduledSaleId,
            customerId,
            firstSaleAtUtc,
            deliveryMethod,
            deliveryStatus,
            saleOrigin,
            createdAtUtc);

        await InsertSaleAsync(
            dbContext,
            scheduledSaleId,
            customerId,
            secondSaleAtUtc,
            deliveryMethod,
            deliveryStatus,
            saleOrigin,
            createdAtUtc);

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO sale_items
                (
                    Id,
                    SaleId,
                    ProductId,
                    Quantity,
                    UnitPrice,
                    CreatedAtUtc
                )
                VALUES
                (
                    {Guid.NewGuid()},
                    {unscheduledSaleId},
                    {productId},
                    {10m},
                    {100m},
                    {createdAtUtc}
                )
                """);

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO sale_items
                (
                    Id,
                    SaleId,
                    ProductId,
                    Quantity,
                    UnitPrice,
                    CreatedAtUtc
                )
                VALUES
                (
                    {Guid.NewGuid()},
                    {scheduledSaleId},
                    {productId},
                    {1m},
                    {300m},
                    {createdAtUtc}
                )
                """);

        await InsertReceivableAsync(
            dbContext,
            firstReceivableId,
            scheduledSaleId,
            installmentNumber: 1,
            new DateTime(
                2099,
                1,
                10),
            100m,
            createdAtUtc);

        await InsertReceivableAsync(
            dbContext,
            secondReceivableId,
            scheduledSaleId,
            installmentNumber: 2,
            new DateTime(
                2099,
                2,
                10),
            100m,
            createdAtUtc);

        await InsertReceivableAsync(
            dbContext,
            thirdReceivableId,
            scheduledSaleId,
            installmentNumber: 3,
            new DateTime(
                2099,
                3,
                10),
            100m,
            createdAtUtc);

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO payments
                (
                    Id,
                    ReceivableId,
                    Amount,
                    PaidAtUtc,
                    Method,
                    Reference,
                    Notes,
                    CreatedAtUtc
                )
                VALUES
                (
                    {Guid.NewGuid()},
                    {firstReceivableId},
                    {100m},
                    {createdAtUtc},
                    {paymentMethod},
                    {null},
                    {null},
                    {createdAtUtc}
                )
                """);
    }

    private static Task<int> InsertSaleAsync(
        AdubosDbContext dbContext,
        Guid saleId,
        Guid customerId,
        DateTime occurredAtUtc,
        int deliveryMethod,
        int deliveryStatus,
        int origin,
        DateTime createdAtUtc)
    {
        return dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO sales
                (
                    Id,
                    CustomerId,
                    OccurredAtUtc,
                    DeliveryMethod,
                    DeliveryStatus,
                    DeliveredAtUtc,
                    Origin,
                    CreatedAtUtc
                )
                VALUES
                (
                    {saleId},
                    {customerId},
                    {occurredAtUtc},
                    {deliveryMethod},
                    {deliveryStatus},
                    {null},
                    {origin},
                    {createdAtUtc}
                )
                """);
    }

    private static Task<int> InsertReceivableAsync(
        AdubosDbContext dbContext,
        Guid receivableId,
        Guid saleId,
        int installmentNumber,
        DateTime dueDate,
        decimal originalAmount,
        DateTime createdAtUtc)
    {
        return dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO receivables
                (
                    Id,
                    SaleId,
                    InstallmentNumber,
                    DueDate,
                    OriginalAmount,
                    Notes,
                    CreatedAtUtc
                )
                VALUES
                (
                    {receivableId},
                    {saleId},
                    {installmentNumber},
                    {dueDate},
                    {originalAmount},
                    {null},
                    {createdAtUtc}
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
