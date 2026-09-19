using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Domain.Legacy;

public sealed class LegacySaleMetadata
{
    private LegacySaleMetadata()
    {
    }

    public LegacySaleMetadata(
        Guid saleId,
        Guid importBatchId,
        string transactionKey,
        string documentNumber,
        DateTime transactionDate)
    {
        if (saleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale is required.",
                nameof(saleId));
        }

        if (importBatchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Import batch is required.",
                nameof(importBatchId));
        }

        if (string.IsNullOrWhiteSpace(transactionKey))
        {
            throw new ArgumentException(
                "Transaction key is required.",
                nameof(transactionKey));
        }

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new ArgumentException(
                "Document number is required.",
                nameof(documentNumber));
        }

        Id = Guid.NewGuid();

        SaleId = saleId;

        ImportBatchId =
            importBatchId;

        TransactionKey =
            transactionKey.Trim();

        DocumentNumber =
            documentNumber.Trim();

        TransactionDate =
            transactionDate.Date;

        ImportedAtUtc =
            DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SaleId { get; private set; }

    public Sale Sale { get; private set; } = null!;

    public Guid ImportBatchId { get; private set; }

    public LegacyImportBatch ImportBatch { get; private set; } = null!;

    public string TransactionKey { get; private set; } = null!;

    public string DocumentNumber { get; private set; } = null!;

    public DateTime TransactionDate { get; private set; }

    public DateTime ImportedAtUtc { get; private set; }
}
