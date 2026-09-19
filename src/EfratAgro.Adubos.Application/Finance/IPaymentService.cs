namespace EfratAgro.Adubos.Application.Finance;

public interface IPaymentService
{
    Task<RegisterPaymentResult> RegisterAsync(
        Guid receivableId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default);
}
