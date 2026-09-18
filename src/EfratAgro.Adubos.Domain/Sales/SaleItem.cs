using EfratAgro.Adubos.Domain.Catalog;

namespace EfratAgro.Adubos.Domain.Sales;

public sealed class SaleItem
{
    private SaleItem()
    {
    }

    public SaleItem(
        Guid saleId,
        Guid productId,
        decimal quantity,
        decimal unitPrice)
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

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");
        }

        Id = Guid.NewGuid();
        SaleId = saleId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SaleId { get; private set; }

    public Sale Sale { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Total =>
        Quantity * UnitPrice;

    public DateTime CreatedAtUtc { get; private set; }
}
