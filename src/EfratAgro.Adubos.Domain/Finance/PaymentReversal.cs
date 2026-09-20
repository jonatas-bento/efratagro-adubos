namespace EfratAgro.Adubos.Domain.Finance;

public sealed class PaymentReversal
{
    private PaymentReversal()
    {
    }

    public PaymentReversal(
        Guid paymentId,
        Guid reversedByUserId,
        string reason,
        DateTime reversedAtUtc)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment is required.",
                nameof(paymentId));
        }

        if (reversedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User is required.",
                nameof(reversedByUserId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Reversal reason is required.",
                nameof(reason));
        }

        var normalizedReason =
            reason.Trim();

        if (normalizedReason.Length > 500)
        {
            throw new ArgumentException(
                "Reversal reason cannot exceed 500 characters.",
                nameof(reason));
        }

        Id = Guid.NewGuid();
        PaymentId = paymentId;
        ReversedByUserId = reversedByUserId;
        Reason = normalizedReason;
        ReversedAtUtc = reversedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PaymentId { get; private set; }

    public Payment Payment { get; private set; } = null!;

    public Guid ReversedByUserId { get; private set; }

    public string Reason { get; private set; } = null!;

    public DateTime ReversedAtUtc { get; private set; }
}
