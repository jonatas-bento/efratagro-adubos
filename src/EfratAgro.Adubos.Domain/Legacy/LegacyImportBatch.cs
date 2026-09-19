namespace EfratAgro.Adubos.Domain.Legacy;

public sealed class LegacyImportBatch
{
    private LegacyImportBatch()
    {
    }

    public LegacyImportBatch(
        string sourceFileName,
        string sourceFileHash)
        : this(
            sourceFileName,
            sourceFileHash,
            LegacyImportScope.WarehouseOpeningStock)
    {
    }

    public LegacyImportBatch(
        string sourceFileName,
        string sourceFileHash,
        LegacyImportScope scope)
    {
        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            throw new ArgumentException(
                "Source file name is required.",
                nameof(sourceFileName));
        }

        if (string.IsNullOrWhiteSpace(sourceFileHash))
        {
            throw new ArgumentException(
                "Source file hash is required.",
                nameof(sourceFileHash));
        }

        if (scope == LegacyImportScope.Unspecified)
        {
            throw new ArgumentException(
                "Import scope is required.",
                nameof(scope));
        }

        Id = Guid.NewGuid();

        SourceFileName =
            sourceFileName.Trim();

        SourceFileHash =
            sourceFileHash
                .Trim()
                .ToUpperInvariant();

        Scope = scope;

        StartedAtUtc =
            DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string SourceFileName { get; private set; } = null!;

    public string SourceFileHash { get; private set; } = null!;

    public LegacyImportScope Scope { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public int ImportedRows { get; private set; }

    public int ReviewRows { get; private set; }

    public void Complete(
        int importedRows,
        int reviewRows)
    {
        if (importedRows < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(importedRows));
        }

        if (reviewRows < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reviewRows));
        }

        ImportedRows = importedRows;
        ReviewRows = reviewRows;
        FinishedAtUtc = DateTime.UtcNow;
    }
}
