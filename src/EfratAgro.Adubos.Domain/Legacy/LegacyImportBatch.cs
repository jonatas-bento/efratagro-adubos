namespace EfratAgro.Adubos.Domain.Legacy;

public sealed class LegacyImportBatch
{
    private LegacyImportBatch()
    {
    }

    public LegacyImportBatch(string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            throw new ArgumentException(
                "Source file name is required.",
                nameof(sourceFileName));
        }

        Id = Guid.NewGuid();
        SourceFileName = sourceFileName.Trim();
        StartedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string SourceFileName { get; private set; } = null!;

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public int ImportedRows { get; private set; }

    public int ReviewRows { get; private set; }
}
