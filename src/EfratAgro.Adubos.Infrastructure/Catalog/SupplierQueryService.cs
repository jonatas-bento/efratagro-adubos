using EfratAgro.Adubos.Application.Catalog;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Catalog;

public sealed class SupplierQueryService
    : ISupplierQueryService
{
    private readonly AdubosDbContext _dbContext;

    public SupplierQueryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SupplierOptionDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x =>
                new SupplierOptionDto(
                    x.Id,
                    x.Name))
            .ToListAsync(cancellationToken);
    }
}
