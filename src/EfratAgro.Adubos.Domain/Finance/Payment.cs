namespace EfratAgro.Adubos.Domain.Finance;

public sealed class Payment
{
    private Payment()
    {
    }

    public Payment(
        Guid receivableId,
        decimal amount,
        DateTime paidAtUtc,
        PaymentMethod method,
        string? reference = null,
        string? notes = null)
    {
        if (receivableId == Guid.Empty)
        {
            throw new ArgumentException(
                "Receivable is required.",
                nameof(receivableId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        if (method == PaymentMethod.Unspecified)
        {
            throw new ArgumentException(
                "Payment method is required.",
                nameof(method));
        }

        Id = Guid.NewGuid();

        ReceivableId =
            receivableId;

        Amount =
            amount;

        PaidAtUtc =
            paidAtUtc;

        Method =
            method;

        Reference =
            string.IsNullOrWhiteSpace(reference)
                ? null
                : reference.Trim();

        Notes =
            string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

        CreatedAtUtc =
            DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ReceivableId { get; private set; }

    public Receivable Receivable { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public DateTime PaidAtUtc { get; private set; }

    public PaymentMethod Method { get; private set; }

    public string? Reference { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public PaymentReversal? Reversal { get; private set; }
}
