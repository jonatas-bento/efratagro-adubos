using EfratAgro.Adubos.Domain.Customers;

namespace EfratAgro.Adubos.Domain.Sales;

public sealed class Sale
{
    private Sale()
    {
    }

    public Sale(
        Guid customerId,
        DateTime occurredAtUtc,
        DeliveryMethod deliveryMethod = DeliveryMethod.Delivery,
        SaleOrigin origin = SaleOrigin.Operational)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer is required.",
                nameof(customerId));
        }

        if (deliveryMethod == DeliveryMethod.Unspecified)
        {
            throw new ArgumentException(
                "Delivery method is required.",
                nameof(deliveryMethod));
        }

        if (origin == SaleOrigin.Unspecified)
        {
            throw new ArgumentException(
                "Sale origin is required.",
                nameof(origin));
        }

        Id = Guid.NewGuid();

        CustomerId = customerId;
        OccurredAtUtc = occurredAtUtc;

        DeliveryMethod = deliveryMethod;
        DeliveryStatus = DeliveryStatus.Pending;

        Origin = origin;

        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DeliveryMethod DeliveryMethod { get; private set; }

    public DeliveryStatus DeliveryStatus { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public SaleOrigin Origin { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void MarkDelivered(
        DateTime deliveredAtUtc)
    {
        if (DeliveryStatus ==
            DeliveryStatus.Delivered)
        {
            return;
        }

        DeliveryStatus =
            DeliveryStatus.Delivered;

        DeliveredAtUtc =
            deliveredAtUtc;
    }
}
