using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Finance;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Sales;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class FinancialIntegrityTests
{
    [Fact]
    public async Task Sale_ShouldRejectScheduleOneCentBelowTotal()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service =
            new SaleService(
                database.DbContext);

        var request =
            CreateSaleRequest(
                scheduledAmount: 299.99m);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));
    }

    [Fact]
    public async Task Sale_ShouldRejectScheduleOneCentAboveTotal()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service =
            new SaleService(
                database.DbContext);

        var request =
            CreateSaleRequest(
                scheduledAmount: 300.01m);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));
    }

    [Fact]
    public async Task ReceivableQuery_ShouldKeepOneCentOpen()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivableId =
            await SeedReceivableAsync(
                database.DbContext,
                originalAmount: 100.00m);

        database.DbContext.Payments.Add(
            new Payment(
                receivableId,
                99.99m,
                DateTime.UtcNow,
                PaymentMethod.Pix));

        await database.DbContext
            .SaveChangesAsync();

        var service =
            new ReceivableQueryService(
                database.DbContext);

        var result =
            await service.GetAsync(
                openOnly: true,
                take: 100);

        var item =
            Assert.Single(
                result.Items);

        Assert.Equal(
            0.01m,
            item.OutstandingAmount);

        Assert.Equal(
            "Parcial",
            item.Status);

        Assert.Equal(
            0.01m,
            result.Summary.TotalOutstanding);
    }

    [Fact]
    public async Task Payment_ShouldAllowExactRemainingCent()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivableId =
            await SeedReceivableAsync(
                database.DbContext,
                originalAmount: 100.00m);

        database.DbContext.Payments.Add(
            new Payment(
                receivableId,
                99.99m,
                DateTime.UtcNow.AddMinutes(-1),
                PaymentMethod.Pix));

        await database.DbContext
            .SaveChangesAsync();

        var service =
            new PaymentService(
                database.DbContext);

        var result =
            await service.RegisterAsync(
                receivableId,
                new RegisterPaymentRequest(
                    Amount: 0.01m,
                    Method: PaymentMethod.Pix,
                    Reference: null,
                    Notes: null));

        Assert.Equal(
            100.00m,
            result.TotalPaid);

        Assert.Equal(
            0m,
            result.OutstandingAmount);

        Assert.Equal(
            2,
            await database.DbContext.Payments.CountAsync());
    }

    [Fact]
    public async Task Payment_ShouldRejectOverpaymentByOneCent()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivableId =
            await SeedReceivableAsync(
                database.DbContext,
                originalAmount: 100.00m);

        var service =
            new PaymentService(
                database.DbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                service.RegisterAsync(
                    receivableId,
                    new RegisterPaymentRequest(
                        Amount: 100.01m,
                        Method: PaymentMethod.Pix,
                        Reference: null,
                        Notes: null)));

        Assert.Equal(
            0,
            await database.DbContext.Payments.CountAsync());
    }

    [Fact]
    public async Task Payment_ShouldPreservePartialOutstandingAmount()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivableId =
            await SeedReceivableAsync(
                database.DbContext,
                originalAmount: 100.00m);

        var service =
            new PaymentService(
                database.DbContext);

        var result =
            await service.RegisterAsync(
                receivableId,
                new RegisterPaymentRequest(
                    Amount: 40.00m,
                    Method: PaymentMethod.Pix,
                    Reference: null,
                    Notes: null));

        Assert.Equal(
            40.00m,
            result.TotalPaid);

        Assert.Equal(
            60.00m,
            result.OutstandingAmount);
    }

    [Fact]
    public async Task Payment_ShouldAllowExactFullPayment()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivableId =
            await SeedReceivableAsync(
                database.DbContext,
                originalAmount: 100.00m);

        var service =
            new PaymentService(
                database.DbContext);

        var result =
            await service.RegisterAsync(
                receivableId,
                new RegisterPaymentRequest(
                    Amount: 100.00m,
                    Method: PaymentMethod.Pix,
                    Reference: null,
                    Notes: null));

        Assert.Equal(
            100.00m,
            result.TotalPaid);

        Assert.Equal(
            0m,
            result.OutstandingAmount);
    }

    [Fact]
    public async Task Sale_ShouldPassFinancialValidation_WhenRoundedCentTotalMatchesSchedule()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service =
            new SaleService(
                database.DbContext);

        var request =
            new CreateSaleRequest(
                CustomerId: Guid.NewGuid(),
                Items:
                [
                    new CreateSaleItemRequest(
                        ProductId: Guid.NewGuid(),
                        Quantity: 1.001m,
                        UnitPrice: 10.01m)
                ],
                DeliveryMethod:
                    DeliveryMethod.Delivery,
                Receivables:
                [
                    new CreateSaleReceivableRequest(
                        InstallmentNumber: 1,
                        DueDate:
                            DateTime.UtcNow.Date
                                .AddDays(30),
                        Amount: 10.02m)
                ]);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateAsync(request));

        Assert.Equal(
            "Cliente não encontrado.",
            exception.Message);
    }

    [Fact]
    public async Task Payment_ShouldRejectAdditionalPayment_WhenReceivableIsFullyPaid()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivableId =
            await SeedReceivableAsync(
                database.DbContext,
                originalAmount: 100.00m);

        database.DbContext.Payments.Add(
            new Payment(
                receivableId,
                100.00m,
                DateTime.UtcNow.AddMinutes(-1),
                PaymentMethod.Pix));

        await database.DbContext
            .SaveChangesAsync();

        var service =
            new PaymentService(
                database.DbContext);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    service.RegisterAsync(
                        receivableId,
                        new RegisterPaymentRequest(
                            Amount: 0.01m,
                            Method: PaymentMethod.Pix,
                            Reference: null,
                            Notes: null)));

        Assert.Equal(
            "Esta parcela já está quitada.",
            exception.Message);

        Assert.Equal(
            1,
            await database.DbContext.Payments.CountAsync());
    }

    private static CreateSaleRequest CreateSaleRequest(
        decimal scheduledAmount)
    {
        return new CreateSaleRequest(
            CustomerId: Guid.NewGuid(),
            Items:
            [
                new CreateSaleItemRequest(
                    ProductId: Guid.NewGuid(),
                    Quantity: 1m,
                    UnitPrice: 300.00m)
            ],
            DeliveryMethod:
                DeliveryMethod.Delivery,
            Receivables:
            [
                new CreateSaleReceivableRequest(
                    InstallmentNumber: 1,
                    DueDate:
                        DateTime.UtcNow.Date
                            .AddDays(30),
                    Amount: scheduledAmount)
            ]);
    }

    private static async Task<Guid> SeedReceivableAsync(
        AdubosDbContext dbContext,
        decimal originalAmount)
    {
        var customerId =
            Guid.NewGuid();

        var saleId =
            Guid.NewGuid();

        var now =
            DateTime.UtcNow;

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO customers
                (
                    Id,
                    Name,
                    NormalizedName,
                    IsActive,
                    CreatedAtUtc
                )
                VALUES
                (
                    {customerId},
                    {"CLIENTE TESTE FINANCEIRO"},
                    {"CLIENTE TESTE FINANCEIRO"},
                    {true},
                    {now}
                );
                """);

        await dbContext.Database
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
                    {now},
                    {1},
                    {1},
                    {null},
                    {1},
                    {now}
                );
                """);

        var receivable =
            new Receivable(
                saleId,
                installmentNumber: 1,
                dueDate:
                    DateTime.UtcNow.Date
                        .AddDays(30),
                originalAmount);

        dbContext.Receivables.Add(
            receivable);

        await dbContext.SaveChangesAsync();

        return receivable.Id;
    }

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private TestDatabase(
            SqliteConnection connection,
            AdubosDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
        }

        private SqliteConnection Connection { get; }

        public AdubosDbContext DbContext { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:");

            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<AdubosDbContext>()
                    .UseSqlite(connection)
                    .Options;

            var dbContext =
                new AdubosDbContext(options);

            await dbContext.Database
                .EnsureCreatedAsync();

            return new TestDatabase(
                connection,
                dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
