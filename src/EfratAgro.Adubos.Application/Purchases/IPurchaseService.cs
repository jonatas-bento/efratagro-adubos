namespace EfratAgro.Adubos.Application.Purchases;

public interface IPurchaseService
{
    Task<CreatePurchaseResult> CreateAsync(
        CreatePurchaseRequest request,
        CancellationToken cancellationToken = default);
}
