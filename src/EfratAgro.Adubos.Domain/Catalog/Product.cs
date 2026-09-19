namespace EfratAgro.Adubos.Domain.Catalog;

public sealed class Product
{
    private Product()
    {
    }

    public Product(
        string name,
        Guid supplierId,
        string? legacyName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));
        }

        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException(
                "Supplier is required.",
                nameof(supplierId));
        }

        Id = Guid.NewGuid();

        Name = name.Trim();
        NormalizedName = Normalize(name);

        SupplierId = supplierId;

        LegacyName = string.IsNullOrWhiteSpace(legacyName)
            ? null
            : legacyName.Trim();

        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? LegacyName { get; private set; }

    public Guid SupplierId { get; private set; }

    public Supplier Supplier { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }
}
