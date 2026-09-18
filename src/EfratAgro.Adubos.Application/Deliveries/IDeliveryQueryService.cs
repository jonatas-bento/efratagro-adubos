namespace EfratAgro.Adubos.Application.Deliveries;

public interface IDeliveryQueryService
{
    Task<IReadOnlyList<DeliverySummaryDto>> GetAsync(
        bool pendingOnly,
        int take,
        CancellationToken cancellationToken = default);
}
