namespace EfratAgro.Adubos.Application.Sales;

public interface ISaleService
{
    Task<CreateSaleResult> CreateAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default);
}
