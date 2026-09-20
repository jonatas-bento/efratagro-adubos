using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Finance;

public sealed class PaymentQueryService
    : IPaymentQueryService
{
    private readonly AdubosDbContext _dbContext;

    public PaymentQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        IReadOnlyList<PaymentHistoryItemDto>>
        GetByReceivableAsync(
            Guid receivableId,
            CancellationToken cancellationToken = default)
    {
        if (receivableId == Guid.Empty)
        {
            throw new ArgumentException(
                "Recebível inválido.");
        }

        var payments =
            await _dbContext.Payments
                .AsNoTracking()
                .Where(
                    x =>
                        x.ReceivableId ==
                        receivableId)
                .OrderByDescending(
                    x => x.PaidAtUtc)
                .Select(
                    x => new
                    {
                        x.Id,
                        x.ReceivableId,
                        x.Amount,
                        x.PaidAtUtc,
                        x.Method,
                        x.Reference,
                        x.Notes
                    })
                .ToListAsync(
                    cancellationToken);

        if (payments.Count == 0)
        {
            return [];
        }

        /*
         * Mantemos o filtro dos GUIDs em memória.
         * Isso evita depender de tradução de Guid.Contains
         * pelo provider Oracle MySQL.
         */
        var paymentIds =
            payments
                .Select(x => x.Id)
                .ToHashSet();

        var allReversals =
            await _dbContext.PaymentReversals
                .AsNoTracking()
                .Select(
                    x => new
                    {
                        x.Id,
                        x.PaymentId,
                        x.ReversedByUserId,
                        x.Reason,
                        x.ReversedAtUtc
                    })
                .ToListAsync(
                    cancellationToken);

        var reversals =
            allReversals
                .Where(
                    x =>
                        paymentIds.Contains(
                            x.PaymentId))
                .ToDictionary(
                    x => x.PaymentId);

        var users =
            await _dbContext.Users
                .AsNoTracking()
                .Select(
                    x => new
                    {
                        x.Id,
                        x.Email
                    })
                .ToListAsync(
                    cancellationToken);

        var emailsByUser =
            users.ToDictionary(
                x => x.Id,
                x => x.Email);

        return payments
            .Select(payment =>
            {
                PaymentReversalHistoryDto?
                    reversalDto = null;

                if (reversals.TryGetValue(
                        payment.Id,
                        out var reversal))
                {
                    reversalDto =
                        new PaymentReversalHistoryDto(
                            reversal.Id,
                            reversal.ReversedByUserId,
                            emailsByUser.GetValueOrDefault(
                                reversal.ReversedByUserId),
                            reversal.Reason,
                            reversal.ReversedAtUtc);
                }

                return new PaymentHistoryItemDto(
                    payment.Id,
                    payment.ReceivableId,
                    payment.Amount,
                    payment.PaidAtUtc,
                    payment.Method,
                    payment.Reference,
                    payment.Notes,
                    reversalDto);
            })
            .ToList();
    }
}
