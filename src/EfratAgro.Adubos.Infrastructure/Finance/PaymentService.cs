using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Finance;

public sealed class PaymentService
    : IPaymentService
{
    private readonly AdubosDbContext _dbContext;

    public PaymentService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterPaymentResult> RegisterAsync(
        Guid receivableId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (receivableId == Guid.Empty)
        {
            throw new ArgumentException(
                "Recebível inválido.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException(
                "O valor recebido deve ser maior que zero.");
        }

        if (Math.Round(
                request.Amount,
                2) != request.Amount)
        {
            throw new ArgumentException(
                "O valor recebido deve ter no máximo duas casas decimais.");
        }

        if (request.Method ==
            PaymentMethod.Unspecified)
        {
            throw new ArgumentException(
                "Informe o meio de pagamento.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var receivable =
                await GetReceivableForUpdateAsync(
                    receivableId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Recebível não encontrado.");

            var totalPaid =
                await GetEffectivePaidTotalAsync(
                    receivableId,
                    cancellationToken);

            var outstanding =
                Math.Max(
                    0m,
                    receivable.OriginalAmount -
                    totalPaid);

            if (outstanding <= 0m)
            {
                throw new InvalidOperationException(
                    "Esta parcela já está quitada.");
            }

            if (request.Amount > outstanding)
            {
                throw new InvalidOperationException(
                    $"O valor informado excede o saldo " +
                    $"da parcela ({outstanding:C}).");
            }

            var paidAtUtc =
                DateTime.UtcNow;

            var payment =
                new Payment(
                    receivable.Id,
                    request.Amount,
                    paidAtUtc,
                    request.Method,
                    request.Reference,
                    request.Notes);

            _dbContext.Payments.Add(
                payment);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var newTotalPaid =
                totalPaid +
                request.Amount;

            var newOutstanding =
                Math.Max(
                    0m,
                    receivable.OriginalAmount -
                    newTotalPaid);

            return new RegisterPaymentResult(
                payment.Id,
                receivable.Id,
                request.Amount,
                newTotalPaid,
                newOutstanding,
                paidAtUtc);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<ReversePaymentResult> ReverseAsync(
        Guid paymentId,
        Guid reversedByUserId,
        ReversePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Recebimento inválido.");
        }

        if (reversedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Usuário responsável inválido.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Reason))
        {
            throw new ArgumentException(
                "Informe o motivo do estorno.");
        }

        var reason =
            request.Reason.Trim();

        if (reason.Length > 500)
        {
            throw new ArgumentException(
                "O motivo do estorno deve ter no máximo 500 caracteres.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var payment =
                await _dbContext.Payments
                    .SingleOrDefaultAsync(
                        x =>
                            x.Id ==
                            paymentId,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "Recebimento não encontrado.");

            var receivable =
                await GetReceivableForUpdateAsync(
                    payment.ReceivableId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Recebível não encontrado.");

            var alreadyReversed =
                await _dbContext.PaymentReversals
                    .AnyAsync(
                        x =>
                            x.PaymentId ==
                            payment.Id,
                        cancellationToken);

            if (alreadyReversed)
            {
                throw new InvalidOperationException(
                    "Este recebimento já foi estornado.");
            }

            var totalPaid =
                await GetEffectivePaidTotalAsync(
                    receivable.Id,
                    cancellationToken);

            if (payment.Amount > totalPaid)
            {
                throw new InvalidOperationException(
                    "O saldo financeiro está inconsistente para este estorno.");
            }

            var reversedAtUtc =
                DateTime.UtcNow;

            var reversal =
                new PaymentReversal(
                    payment.Id,
                    reversedByUserId,
                    reason,
                    reversedAtUtc);

            _dbContext.PaymentReversals.Add(
                reversal);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var newTotalPaid =
                Math.Max(
                    0m,
                    totalPaid -
                    payment.Amount);

            var newOutstanding =
                Math.Max(
                    0m,
                    receivable.OriginalAmount -
                    newTotalPaid);

            return new ReversePaymentResult(
                reversal.Id,
                payment.Id,
                receivable.Id,
                payment.Amount,
                newTotalPaid,
                newOutstanding,
                reversedAtUtc,
                reversedByUserId);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task<decimal>
        GetEffectivePaidTotalAsync(
            Guid receivableId,
            CancellationToken cancellationToken)
    {
        return
            await _dbContext.Payments
                .Where(
                    x =>
                        x.ReceivableId ==
                            receivableId &&
                        x.Reversal == null)
                .Select(
                    x =>
                        (decimal?)x.Amount)
                .SumAsync(
                    cancellationToken)
            ?? 0m;
    }

    private async Task<Receivable?>
        GetReceivableForUpdateAsync(
            Guid receivableId,
            CancellationToken cancellationToken)
    {
        var providerName =
            _dbContext.Database.ProviderName;

        var isMySql =
            providerName?.Contains(
                "MySql",
                StringComparison.OrdinalIgnoreCase) ==
            true;

        if (isMySql)
        {
            var lockedReceivables =
                await _dbContext.Receivables
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM `receivables`
                        WHERE `Id` = {receivableId}
                        FOR UPDATE
                        """)
                    .ToListAsync(
                        cancellationToken);

            return lockedReceivables
                .SingleOrDefault();
        }

        return
            await _dbContext.Receivables
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        receivableId,
                    cancellationToken);
    }
}
