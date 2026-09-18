namespace EfratAgro.Adubos.Domain.Inventory;

public sealed class Warehouse
{
    private Warehouse()
    {
    }

    public Warehouse(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Warehouse name is required.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
