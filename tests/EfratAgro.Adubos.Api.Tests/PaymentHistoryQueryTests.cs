using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Infrastructure.Finance;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class PaymentHistoryQueryTests
{
    [Fact]
    public async Task History_ShouldExposeActiveAndReversedPayments()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var receivable =
            new Receivable(
                Guid.NewGuid(),
                1,
                DateTime.UtcNow.Date.AddDays(30),
                100m);

        database.DbContext.Receivables.Add(
            receivable);

        await database.DbContext.SaveChangesAsync();

        var reversedPayment =
            new Payment(
                receivable.Id,
                40m,
                new DateTime(
                    2026,
                    9,
                    20,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),
                PaymentMethod.Pix,
                "PIX-001",
                "Pagamento original.");

        var activePayment =
            new Payment(
                receivable.Id,
                25m,
                new DateTime(
                    2026,
                    9,
                    20,
                    11,
                    0,
                    0,
                    DateTimeKind.Utc),
                PaymentMethod.Cash);

        database.DbContext.Payments.AddRange(
            reversedPayment,
            activePayment);

        await database.DbContext.SaveChangesAsync();

        var userId =
            Guid.NewGuid();

        database.DbContext.PaymentReversals.Add(
            new PaymentReversal(
                reversedPayment.Id,
                userId,
                "Lançamento incorreto.",
                new DateTime(
                    2026,
                    9,
                    20,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc)));

        await database.DbContext.SaveChangesAsync();

        var service =
            new PaymentQueryService(
                database.DbContext);

        var result =
            await service.GetByReceivableAsync(
                receivable.Id);

        Assert.Equal(
            2,
            result.Count);

        var active =
            Assert.Single(
                result,
                x =>
                    x.Id ==
                    activePayment.Id);

        Assert.Null(
            active.Reversal);

        var reversed =
            Assert.Single(
                result,
                x =>
                    x.Id ==
                    reversedPayment.Id);

        Assert.NotNull(
            reversed.Reversal);

        Assert.Equal(
            userId,
            reversed.Reversal!
                .ReversedByUserId);

        Assert.Equal(
            "Lançamento incorreto.",
            reversed.Reversal.Reason);
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
