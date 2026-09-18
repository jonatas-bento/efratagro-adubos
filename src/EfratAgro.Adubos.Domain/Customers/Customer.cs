namespace EfratAgro.Adubos.Domain.Customers;

public sealed class Customer
{
    private Customer()
    {
    }

    public Customer(
        string name,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Customer name is required.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        NormalizedName = Normalize(name);

        Phone =
            string.IsNullOrWhiteSpace(phone)
                ? null
                : phone.Trim();

        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    private static string Normalize(
        string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }
}
