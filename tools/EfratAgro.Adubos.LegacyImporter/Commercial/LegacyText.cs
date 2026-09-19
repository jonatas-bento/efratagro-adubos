using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EfratAgro.Adubos.LegacyImporter.Commercial;

public static class LegacyText
{
    public static string NormalizeExact(
        string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant();
    }

    public static string NormalizeDocument(
        string value)
    {
        return NormalizeExact(value)
            .Replace(
                " ",
                string.Empty);
    }

    public static string CanonicalName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed =
            value
                .Trim()
                .ToUpperInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in decomposed)
        {
            if (
                CharUnicodeInfo.GetUnicodeCategory(
                    character)
                ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (
                char.IsLetterOrDigit(character)
                ||
                char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return Regex.Replace(
                builder.ToString(),
                @"\s+",
                " ")
            .Trim();
    }
}
