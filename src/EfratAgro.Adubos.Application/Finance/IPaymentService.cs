namespace EfratAgro.Adubos.Application.Finance;

public interface IPaymentService
{
    Task<RegisterPaymentResult> RegisterAsync(
        Guid receivableId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ReversePaymentResult> ReverseAsync(
        Guid paymentId,
        Guid reversedByUserId,
        ReversePaymentRequest request,
        CancellationToken cancellationToken = default);
}
