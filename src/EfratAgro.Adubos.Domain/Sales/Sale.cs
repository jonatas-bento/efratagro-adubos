using EfratAgro.Adubos.Domain.Customers;

namespace EfratAgro.Adubos.Domain.Sales;

public sealed class Sale
{
    private Sale()
    {
    }

    public Sale(
        Guid customerId,
        DateTime occurredAtUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer is required.",
                nameof(customerId));
        }

        Id = Guid.NewGuid();
        CustomerId = customerId;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
