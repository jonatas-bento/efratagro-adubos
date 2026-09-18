using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Domain.Finance;

public sealed class Receivable
{
    private Receivable()
    {
    }

    public Receivable(
        Guid saleId,
        int installmentNumber,
        DateTime dueDate,
        decimal originalAmount,
        string? notes = null)
    {
        if (saleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale is required.",
                nameof(saleId));
        }

        if (installmentNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(installmentNumber),
                "Installment number must be greater than zero.");
        }

        if (originalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalAmount),
                "Original amount must be greater than zero.");
        }

        Id = Guid.NewGuid();

        SaleId = saleId;

        InstallmentNumber =
            installmentNumber;

        DueDate =
            dueDate.Date;

        OriginalAmount =
            originalAmount;

        Notes =
            string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

        CreatedAtUtc =
            DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SaleId { get; private set; }

    public Sale Sale { get; private set; } = null!;

    public int InstallmentNumber { get; private set; }

    public DateTime DueDate { get; private set; }

    public decimal OriginalAmount { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
