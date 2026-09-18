using EfratAgro.Adubos.Domain.Catalog;

namespace EfratAgro.Adubos.Domain.Purchases;

public sealed class PurchaseItem
{
    private PurchaseItem()
    {
    }

    public PurchaseItem(
        Guid purchaseId,
        Guid productId,
        decimal quantity,
        decimal unitCost)
    {
        if (purchaseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Purchase is required.",
                nameof(purchaseId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product is required.",
                nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (unitCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitCost),
                "Unit cost cannot be negative.");
        }

        Id = Guid.NewGuid();
        PurchaseId = purchaseId;
        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PurchaseId { get; private set; }

    public Purchase Purchase { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public decimal UnitCost { get; private set; }

    public decimal Total =>
        Quantity * UnitCost;

    public DateTime CreatedAtUtc { get; private set; }
}
