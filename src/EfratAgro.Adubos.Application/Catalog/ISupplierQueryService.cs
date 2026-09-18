namespace EfratAgro.Adubos.Application.Catalog;

public interface ISupplierQueryService
{
    Task<IReadOnlyList<SupplierOptionDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
