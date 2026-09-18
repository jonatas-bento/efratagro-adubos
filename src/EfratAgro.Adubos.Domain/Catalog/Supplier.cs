namespace EfratAgro.Adubos.Domain.Catalog;

public sealed class Supplier
{
    private Supplier()
    {
    }

    public Supplier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Supplier name is required.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        NormalizedName = Normalize(name);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }
}
