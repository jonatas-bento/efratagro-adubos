using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Infrastructure.Finance;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class PaymentReversalTests
{
    [Fact]
    public async Task Reversal_ShouldRestoreOutstandingAndAllowReplacementPayment()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivable =
            new Receivable(
                Guid.NewGuid(),
                1,
                DateTime.UtcNow.Date
                    .AddDays(30),
                100m);

        database.DbContext
            .Receivables.Add(
                receivable);

        await database.DbContext
            .SaveChangesAsync();

        var originalPayment =
            new Payment(
                receivable.Id,
                100m,
                DateTime.UtcNow,
                PaymentMethod.Pix);

        database.DbContext
            .Payments.Add(
                originalPayment);

        await database.DbContext
            .SaveChangesAsync();

        var service =
            new PaymentService(
                database.DbContext);

        var userId =
            Guid.NewGuid();

        var reversal =
            await service.ReverseAsync(
                originalPayment.Id,
                userId,
                new ReversePaymentRequest(
                    "Pagamento lançado incorretamente."));

        Assert.Equal(
            originalPayment.Id,
            reversal.PaymentId);

        Assert.Equal(
            100m,
            reversal.ReversedAmount);

        Assert.Equal(
            0m,
            reversal.TotalPaid);

        Assert.Equal(
            100m,
            reversal.OutstandingAmount);

        Assert.Equal(
            userId,
            reversal.ReversedByUserId);

        var replacement =
            await service.RegisterAsync(
                receivable.Id,
                new RegisterPaymentRequest(
                    Amount: 100m,
                    Method: PaymentMethod.Pix,
                    Reference: null,
                    Notes: null));

        Assert.Equal(
            100m,
            replacement.TotalPaid);

        Assert.Equal(
            0m,
            replacement.OutstandingAmount);

        Assert.Equal(
            2,
            await database.DbContext
                .Payments.CountAsync());

        Assert.Equal(
            1,
            await database.DbContext
                .PaymentReversals.CountAsync());
    }

    [Fact]
    public async Task Reversal_ShouldRejectDuplicateReversal()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivable =
            new Receivable(
                Guid.NewGuid(),
                1,
                DateTime.UtcNow.Date
                    .AddDays(30),
                80m);

        database.DbContext
            .Receivables.Add(
                receivable);

        await database.DbContext
            .SaveChangesAsync();

        var payment =
            new Payment(
                receivable.Id,
                80m,
                DateTime.UtcNow,
                PaymentMethod.Cash);

        database.DbContext
            .Payments.Add(
                payment);

        await database.DbContext
            .SaveChangesAsync();

        var service =
            new PaymentService(
                database.DbContext);

        var userId =
            Guid.NewGuid();

        await service.ReverseAsync(
            payment.Id,
            userId,
            new ReversePaymentRequest(
                "Primeiro estorno."));

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.ReverseAsync(
                        payment.Id,
                        userId,
                        new ReversePaymentRequest(
                            "Segundo estorno.")));

        Assert.Equal(
            "Este recebimento já foi estornado.",
            exception.Message);

        Assert.Equal(
            1,
            await database.DbContext
                .PaymentReversals.CountAsync());
    }

    [Fact]
    public async Task Reversal_ShouldRequireReason()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service =
            new PaymentService(
                database.DbContext);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.ReverseAsync(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        new ReversePaymentRequest(
                            "   ")));

        Assert.Equal(
            "Informe o motivo do estorno.",
            exception.Message);
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

        public SqliteConnection Connection { get; }

        public AdubosDbContext DbContext { get; }

        public static async Task<TestDatabase>
            CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:");

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

            await dbContext.Database
                .ExecuteSqlRawAsync(
                    "PRAGMA foreign_keys = OFF;");

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
