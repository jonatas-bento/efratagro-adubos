namespace EfratAgro.Adubos.Application.Deliveries;

public interface IDeliveryService
{
    Task MarkDeliveredAsync(
        Guid saleId,
        CancellationToken cancellationToken = default);
}
