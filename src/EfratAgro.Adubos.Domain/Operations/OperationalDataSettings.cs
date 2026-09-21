namespace EfratAgro.Adubos.Domain.Operations;

public sealed class OperationalDataSettings
{
    public static readonly Guid SingletonId =
        Guid.Parse(
            "98c89ef8-a143-4314-90e9-f8ca77b77649");

    private OperationalDataSettings()
    {
    }

    private OperationalDataSettings(
        DateTime updatedAtUtc)
    {
        Id = SingletonId;
        UpdatedAtUtc =
            RequireUtc(
                updatedAtUtc,
                nameof(updatedAtUtc));
    }

    public Guid Id { get; private set; }

    public DateTime?
        OperationalTrustedFromUtc
        { get; private set; }

    public DateTime UpdatedAtUtc
        { get; private set; }

    public static OperationalDataSettings Create(
        DateTime createdAtUtc)
    {
        return new OperationalDataSettings(
            createdAtUtc);
    }

    public void SetOperationalTrustedFrom(
        DateTime? trustedFromUtc,
        DateTime updatedAtUtc)
    {
        OperationalTrustedFromUtc =
            trustedFromUtc.HasValue
                ? RequireUtc(
                    trustedFromUtc.Value,
                    nameof(trustedFromUtc))
                : null;

        UpdatedAtUtc =
            RequireUtc(
                updatedAtUtc,
                nameof(updatedAtUtc));
    }

    private static DateTime RequireUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data deve estar em UTC.",
                parameterName);
        }

        return value;
    }
}
