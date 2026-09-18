using EfratAgro.Adubos.Domain.Catalog;

namespace EfratAgro.Adubos.Domain.Purchases;

public sealed class Purchase
{
    private Purchase()
    {
    }

    public Purchase(
        Guid supplierId,
        DateTime occurredAtUtc)
    {
        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException(
                "Supplier is required.",
                nameof(supplierId));
        }

        Id = Guid.NewGuid();
        SupplierId = supplierId;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SupplierId { get; private set; }

    public Supplier Supplier { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
