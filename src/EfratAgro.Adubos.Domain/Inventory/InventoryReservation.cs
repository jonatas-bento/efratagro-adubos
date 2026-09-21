using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Domain.Inventory;

public sealed class InventoryReservation
{
    private InventoryReservation()
    {
    }

    public InventoryReservation(
        Guid saleId,
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        DateTime reservedAtUtc)
    {
        if (saleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale is required.",
                nameof(saleId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product is required.",
                nameof(productId));
        }

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse is required.",
                nameof(warehouseId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (Math.Round(
                quantity,
                3,
                MidpointRounding.AwayFromZero) !=
            quantity)
        {
            throw new ArgumentException(
                "Quantity must have at most three decimal places.",
                nameof(quantity));
        }

        Id = Guid.NewGuid();
        SaleId = saleId;
        ProductId = productId;
        WarehouseId = warehouseId;
        Quantity = quantity;
        Status = InventoryReservationStatus.Active;
        ReservedAtUtc = reservedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid SaleId { get; private set; }

    public Sale Sale { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public InventoryReservationStatus Status { get; private set; }

    public DateTime ReservedAtUtc { get; private set; }

    public DateTime? FulfilledAtUtc { get; private set; }

    public void Fulfill(
        DateTime fulfilledAtUtc)
    {
        if (Status ==
            InventoryReservationStatus.Fulfilled)
        {
            return;
        }

        if (Status !=
            InventoryReservationStatus.Active)
        {
            throw new InvalidOperationException(
                "Inventory reservation is not active.");
        }

        Status =
            InventoryReservationStatus.Fulfilled;

        FulfilledAtUtc =
            fulfilledAtUtc;
    }
}
