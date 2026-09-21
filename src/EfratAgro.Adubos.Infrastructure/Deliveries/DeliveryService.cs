using EfratAgro.Adubos.Application.Deliveries;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Inventory;
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

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var sale =
                await GetSaleForUpdateAsync(
                    saleId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Venda não encontrada.");

            if (
                sale.DeliveryStatus ==
                DeliveryStatus.Delivered)
            {
                await transaction.CommitAsync(
                    cancellationToken);

                return;
            }

            var reservations =
                await _dbContext.InventoryReservations
                    .Where(
                        x =>
                            x.SaleId ==
                            sale.Id)
                    .OrderBy(
                        x => x.ProductId)
                    .ToListAsync(
                        cancellationToken);

            if (reservations.Count > 0)
            {
                if (
                    reservations.Any(
                        x =>
                            x.Status !=
                            InventoryReservationStatus.Active))
                {
                    throw new InvalidOperationException(
                        "A reserva de estoque desta venda " +
                        "não está íntegra.");
                }

                var productIds =
                    reservations
                        .Select(
                            x => x.ProductId)
                        .ToArray();

                await InventoryStockCoordinator
                    .LockProductsAsync(
                        _dbContext,
                        productIds,
                        requireActive: false,
                        cancellationToken);

                var availabilityByProduct =
                    await InventoryStockCoordinator
                        .GetAvailabilityAsync(
                            _dbContext,
                            productIds,
                            cancellationToken);

                var deliveredAtUtc =
                    DateTime.UtcNow;

                foreach (
                    var reservation
                    in reservations)
                {
                    var availability =
                        availabilityByProduct[
                            reservation.ProductId];

                    if (
                        availability.PhysicalQuantity <
                        reservation.Quantity)
                    {
                        throw new InvalidOperationException(
                            "O estoque físico não é suficiente " +
                            "para concluir esta entrega.");
                    }

                    var movement =
                        new InventoryMovement(
                            reservation.ProductId,
                            reservation.WarehouseId,
                            InventoryMovementType.Sale,
                            StockBucket.Normal,
                            -reservation.Quantity,
                            deliveredAtUtc,
                            referenceType: "SALE",
                            referenceId: sale.Id,
                            notes:
                                "Saída de estoque por entrega " +
                                "de venda reservada.");

                    _dbContext.InventoryMovements.Add(
                        movement);

                    reservation.Fulfill(
                        deliveredAtUtc);
                }

                sale.MarkDelivered(
                    deliveredAtUtc);
            }
            else
            {
                // Venda normal já baixou o físico
                // quando foi registrada.
                sale.MarkDelivered(
                    DateTime.UtcNow);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task<Sale?>
        GetSaleForUpdateAsync(
            Guid saleId,
            CancellationToken cancellationToken)
    {
        var providerName =
            _dbContext.Database.ProviderName;

        var isMySql =
            providerName?.Contains(
                "MySql",
                StringComparison.OrdinalIgnoreCase) ==
            true;

        if (isMySql)
        {
            var rows =
                await _dbContext.Sales
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM `sales`
                        WHERE `Id` = {saleId}
                        FOR UPDATE
                        """)
                    .ToListAsync(
                        cancellationToken);

            return rows.SingleOrDefault();
        }

        return
            await _dbContext.Sales
                .SingleOrDefaultAsync(
                    x => x.Id == saleId,
                    cancellationToken);
    }
}
