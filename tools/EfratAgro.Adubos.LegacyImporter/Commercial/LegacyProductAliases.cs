namespace EfratAgro.Adubos.LegacyImporter.Commercial;

public static class LegacyProductAliases
{
    private static readonly IReadOnlyDictionary<string, string>
        Aliases =
            new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                [
                    BuildKey(
                        "FERTIPAR",
                        "18-05-15 SN")
                ] =
                    "18-05-15 SN."
            };

    public static string ResolveProductName(
        string supplier,
        string product)
    {
        var key =
            BuildKey(
                supplier,
                product);

        return Aliases.TryGetValue(
                key,
                out var resolved)
            ? resolved
            : product.Trim();
    }

    private static string BuildKey(
        string supplier,
        string product)
    {
        return
            $"{LegacyText.CanonicalName(supplier)}|" +
            $"{LegacyText.NormalizeExact(product)}";
    }
}
