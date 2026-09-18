using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Legacy;

namespace EfratAgro.Adubos.Domain.Inventory;

public sealed class InventoryMovement
{
    private InventoryMovement()
    {
    }

    public InventoryMovement(
        Guid productId,
        Guid warehouseId,
        InventoryMovementType type,
        StockBucket bucket,
        decimal quantity,
        DateTime occurredAtUtc,
        string? notes = null)
    {
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

        if (quantity == 0)
        {
            throw new ArgumentException(
                "Inventory movement quantity cannot be zero.",
                nameof(quantity));
        }

        Id = Guid.NewGuid();

        ProductId = productId;
        WarehouseId = warehouseId;

        Type = type;
        Bucket = bucket;

        Quantity = quantity;

        OccurredAtUtc = occurredAtUtc;

        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();

        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; } = null!;

    public InventoryMovementType Type { get; private set; }

    public StockBucket Bucket { get; private set; }

    public decimal Quantity { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string? ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public Guid? LegacyImportRowId { get; private set; }

    public LegacyImportRow? LegacyImportRow { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
