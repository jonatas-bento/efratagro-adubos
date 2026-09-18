using EfratAgro.Adubos.Application.Deliveries;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Deliveries;

public sealed class DeliveryService
    : IDeliveryService
{
    private readonly AdubosDbContext _dbContext;

    public DeliveryService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task MarkDeliveredAsync(
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Venda inválida.",
                nameof(saleId));
        }

        var sale =
            await _dbContext.Sales
                .SingleOrDefaultAsync(
                    x => x.Id == saleId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Venda não encontrada.");

        sale.MarkDelivered(
            DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
