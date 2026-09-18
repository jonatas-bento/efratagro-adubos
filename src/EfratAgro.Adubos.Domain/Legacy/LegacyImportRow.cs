namespace EfratAgro.Adubos.Domain.Legacy;

public sealed class LegacyImportRow
{
    private LegacyImportRow()
    {
    }

    public LegacyImportRow(
        Guid batchId,
        string sheetName,
        int rowNumber,
        string sourceCell,
        string rawData)
    {
        if (batchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Import batch is required.",
                nameof(batchId));
        }

        if (string.IsNullOrWhiteSpace(sheetName))
        {
            throw new ArgumentException(
                "Sheet name is required.",
                nameof(sheetName));
        }

        if (rowNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rowNumber),
                "Row number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(sourceCell))
        {
            throw new ArgumentException(
                "Source cell is required.",
                nameof(sourceCell));
        }

        if (string.IsNullOrWhiteSpace(rawData))
        {
            throw new ArgumentException(
                "Raw data is required.",
                nameof(rawData));
        }

        Id = Guid.NewGuid();

        BatchId = batchId;

        SheetName =
            sheetName.Trim();

        RowNumber =
            rowNumber;

        SourceCell =
            sourceCell
                .Trim()
                .ToUpperInvariant();

        RawData =
            rawData;

        CreatedAtUtc =
            DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid BatchId { get; private set; }

    public LegacyImportBatch Batch { get; private set; } = null!;

    public string SheetName { get; private set; } = null!;

    public int RowNumber { get; private set; }

    public string SourceCell { get; private set; } = null!;

    public string RawData { get; private set; } = null!;

    public bool RequiresReview { get; private set; }

    public string? ReviewReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
