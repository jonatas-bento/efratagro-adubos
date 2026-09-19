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
                2) !=
            request.Amount)
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
                await _dbContext.Payments
                    .Where(
                        x =>
                            x.ReceivableId ==
                            receivableId)
                    .Select(
                        x =>
                            (decimal?)x.Amount)
                    .SumAsync(
                        cancellationToken)
                ?? 0m;

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
