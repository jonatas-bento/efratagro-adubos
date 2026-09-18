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
        Id = Guid.NewGuid();

        SetName(name);
        SetPhone(phone);

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

    public void Update(
        string name,
        string? phone)
    {
        SetName(name);
        SetPhone(phone);

        UpdatedAtUtc =
            DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;

        UpdatedAtUtc =
            DateTime.UtcNow;
    }

    private void SetName(
        string name)
    {
        var trimmed =
            name?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException(
                "Customer name is required.",
                nameof(name));
        }

        Name = trimmed;

        NormalizedName =
            trimmed.ToUpperInvariant();
    }

    private void SetPhone(
        string? phone)
    {
        Phone =
            string.IsNullOrWhiteSpace(phone)
                ? null
                : phone.Trim();
    }
}
