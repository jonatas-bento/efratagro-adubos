using System.Data.Common;
using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Infrastructure.Finance;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySql.EntityFrameworkCore.Extensions;
using Xunit;

namespace EfratAgro.Adubos.MySqlIntegrationTests;

public sealed class PaymentConcurrencyTests
{
    [Fact]
    public async Task ConcurrentPayments_MustNotOverpaySameReceivable()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__ConcurrencyTest");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "ConnectionStrings__ConcurrencyTest is required.");

        var receivableId =
            await SeedReceivableAsync(
                connectionString!);

        var raceBarrier =
            new AsyncBarrier(
                participants: 2);

        await using var contextA =
            CreateContext(
                connectionString!,
                new PaymentRaceInterceptor(
                    raceBarrier));

        await using var contextB =
            CreateContext(
                connectionString!,
                new PaymentRaceInterceptor(
                    raceBarrier));

        var serviceA =
            new PaymentService(
                contextA);

        var serviceB =
            new PaymentService(
                contextB);

        var startGate =
            new TaskCompletionSource<bool>(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(20));

        var attemptA =
            RunPaymentAsync(
                serviceA,
                receivableId,
                startGate.Task,
                timeout.Token);

        var attemptB =
            RunPaymentAsync(
                serviceB,
                receivableId,
                startGate.Task,
                timeout.Token);

        startGate.SetResult(true);

        var attempts =
            await Task.WhenAll(
                attemptA,
                attemptB);

        var successful =
            attempts
                .Where(x => x.Exception is null)
                .ToList();

        var failed =
            attempts
                .Where(x => x.Exception is not null)
                .ToList();

        Assert.Single(
            successful);

        var rejected =
            Assert.Single(
                failed);

        var exception =
            Assert.IsType<InvalidOperationException>(
                rejected.Exception);

        Assert.Contains(
            "excede o saldo",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        await using var verificationContext =
            CreateContext(
                connectionString!);

        var payments =
            await verificationContext.Payments
                .AsNoTracking()
                .Where(
                    x =>
                        x.ReceivableId ==
                        receivableId)
                .ToListAsync();

        Assert.Single(
            payments);

        Assert.Equal(
            80.00m,
            payments.Sum(
                x => x.Amount));
    }

    private static async Task<PaymentAttempt>
        RunPaymentAsync(
            PaymentService service,
            Guid receivableId,
            Task startGate,
            CancellationToken cancellationToken)
    {
        await startGate;

        try
        {
            var result =
                await service.RegisterAsync(
                    receivableId,
                    new RegisterPaymentRequest(
                        Amount: 80.00m,
                        Method:
                            PaymentMethod.Pix,
                        Reference: null,
                        Notes:
                            "Teste concorrente"),
                    cancellationToken);

            return new PaymentAttempt(
                result,
                null);
        }
        catch (Exception ex)
        {
            return new PaymentAttempt(
                null,
                ex);
        }
    }

    private static async Task<Guid>
        SeedReceivableAsync(
            string connectionString)
    {
        await using var dbContext =
            CreateContext(
                connectionString);

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
                    {"CLIENTE TESTE CONCORRENCIA"},
                    {"CLIENTE TESTE CONCORRENCIA"},
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
                    now.Date.AddDays(30),
                originalAmount: 100.00m);

        dbContext.Receivables.Add(
            receivable);

        await dbContext.SaveChangesAsync();

        return receivable.Id;
    }

    private static AdubosDbContext
        CreateContext(
            string connectionString,
            DbCommandInterceptor? interceptor = null)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<AdubosDbContext>()
                .UseMySQL(
                    connectionString);

        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(
                interceptor);
        }

        return new AdubosDbContext(
            optionsBuilder.Options);
    }

    private sealed record PaymentAttempt(
        RegisterPaymentResult? Result,
        Exception? Exception);

    private sealed class AsyncBarrier
    {
        private readonly int _participants;

        private readonly
            TaskCompletionSource<bool> _release =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private int _arrivals;

        public AsyncBarrier(
            int participants)
        {
            _participants =
                participants;
        }

        public async Task SignalAndWaitAsync(
            CancellationToken cancellationToken)
        {
            var arrivals =
                Interlocked.Increment(
                    ref _arrivals);

            if (arrivals >=
                _participants)
            {
                _release.TrySetResult(
                    true);
            }

            await _release.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                cancellationToken);
        }
    }

    private sealed class PaymentRaceInterceptor
        : DbCommandInterceptor
    {
        private readonly AsyncBarrier _barrier;

        private bool _pessimisticLockObserved;

        public PaymentRaceInterceptor(
            AsyncBarrier barrier)
        {
            _barrier =
                barrier;
        }

        public override ValueTask<
            InterceptionResult<DbDataReader>>
            ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(
                    "FOR UPDATE",
                    StringComparison.OrdinalIgnoreCase))
            {
                _pessimisticLockObserved =
                    true;
            }

            return base.ReaderExecutingAsync(
                command,
                eventData,
                result,
                cancellationToken);
        }

        public override async ValueTask<DbDataReader>
            ReaderExecutedAsync(
                DbCommand command,
                CommandExecutedEventData eventData,
                DbDataReader result,
                CancellationToken cancellationToken = default)
        {
            if (
                !_pessimisticLockObserved &&
                IsPaymentSum(
                    command.CommandText))
            {
                await _barrier
                    .SignalAndWaitAsync(
                        cancellationToken);
            }

            return result;
        }

        private static bool IsPaymentSum(
            string commandText)
        {
            return
                commandText.Contains(
                    "SUM(",
                    StringComparison.OrdinalIgnoreCase) &&
                commandText.Contains(
                    "payments",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
