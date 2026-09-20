namespace EfratAgro.Adubos.Application.Finance;

public interface IPaymentQueryService
{
    Task<IReadOnlyList<PaymentHistoryItemDto>>
        GetByReceivableAsync(
            Guid receivableId,
            CancellationToken cancellationToken = default);
}
