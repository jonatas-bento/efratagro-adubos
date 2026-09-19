using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace EfratAgro.Adubos.LegacyImporter.Commercial;

public sealed class CommercialSpreadsheetReader
{
    private static readonly string[] TargetSheets =
    [
        "HERINGER",
        "FERTIPAR",
        "REAL",
        "EQUILÍBRIO",
        "DIVERSOS"
    ];

    public IReadOnlyList<CommercialImportRow> Read(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Spreadsheet was not found.",
                filePath);
        }

        using var workbook =
            new XLWorkbook(filePath);

        var rows =
            new List<CommercialImportRow>();

        foreach (var sheetName in TargetSheets)
        {
            if (!workbook.Worksheets.TryGetWorksheet(
                    sheetName,
                    out var worksheet))
            {
                throw new InvalidOperationException(
                    $"Required worksheet '{sheetName}' was not found.");
            }

            ReadWorksheet(
                worksheet,
                rows);
        }

        return rows;
    }

    private static void ReadWorksheet(
        IXLWorksheet worksheet,
        ICollection<CommercialImportRow> output)
    {
        var used =
            worksheet.RangeUsed();

        if (used is null)
        {
            return;
        }

        string? currentProduct =
            null;

        var firstRow =
            used.RangeAddress
                .FirstAddress.RowNumber;

        var lastRow =
            used.RangeAddress
                .LastAddress.RowNumber;

        for (
            var rowNumber = firstRow;
            rowNumber <= lastRow;
            rowNumber++)
        {
            if (IsSaleHeader(
                    worksheet,
                    rowNumber))
            {
                currentProduct =
                    FindProductName(
                        worksheet,
                        rowNumber);

                continue;
            }

            if (!LooksLikeSaleRow(
                    worksheet,
                    rowNumber))
            {
                continue;
            }

            output.Add(
                BuildRow(
                    worksheet,
                    rowNumber,
                    currentProduct));
        }
    }

    private static bool IsSaleHeader(
        IXLWorksheet worksheet,
        int row)
    {
        return
            LegacyText.CanonicalName(
                Text(
                    worksheet.Cell(
                        row,
                        "G"))) == "DATA2"
            &&
            LegacyText.CanonicalName(
                Text(
                    worksheet.Cell(
                        row,
                        "K"))) == "CLIENTE"
            &&
            LegacyText.CanonicalName(
                    Text(
                        worksheet.Cell(
                            row,
                            "L")))
                .StartsWith(
                    "QUANT",
                    StringComparison.Ordinal)
            &&
            LegacyText.CanonicalName(
                Text(
                    worksheet.Cell(
                        row,
                        "M"))) == "VALOR2";
    }

    private static string FindProductName(
        IXLWorksheet worksheet,
        int headerRow)
    {
        for (
            var row = headerRow - 1;
            row >= Math.Max(
                1,
                headerRow - 8);
            row--)
        {
            var cell =
                worksheet.Cell(
                    row,
                    "A");

            var raw =
                Text(cell);

            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var normalized =
                LegacyText.CanonicalName(raw);

            if (
                normalized is
                    "COMPRA"
                    or "DATA"
                    or "TOTAL"
                    or "CONTROLE DE ADUBOS")
            {
                continue;
            }

            if (cell.DataType ==
                XLDataType.DateTime)
            {
                continue;
            }

            if (Regex.IsMatch(
                    raw,
                    @"^\d{4}-\d{2}-\d{2}"))
            {
                continue;
            }

            return raw.Trim();
        }

        return string.Empty;
    }

    private static bool LooksLikeSaleRow(
        IXLWorksheet worksheet,
        int row)
    {
        var firstColumn =
            LegacyText.CanonicalName(
                Text(
                    worksheet.Cell(
                        row,
                        "A")));

        if (firstColumn == "TOTAL")
        {
            return false;
        }

        var saleDate =
            Text(
                worksheet.Cell(
                    row,
                    "G"));

        var document =
            Text(
                worksheet.Cell(
                    row,
                    "H"));

        var customer =
            Text(
                worksheet.Cell(
                    row,
                    "K"));

        return
            !string.IsNullOrWhiteSpace(
                customer)
            ||
            (
                !string.IsNullOrWhiteSpace(
                    saleDate)
                &&
                !string.IsNullOrWhiteSpace(
                    document)
            );
    }

    private static CommercialImportRow BuildRow(
        IXLWorksheet worksheet,
        int rowNumber,
        string? product)
    {
        var saleDateCell =
            worksheet.Cell(
                rowNumber,
                "G");

        var rawSaleDate =
            Text(saleDateCell);

        var dateResult =
            ParseSaleDate(
                saleDateCell);

        var document =
            Text(
                worksheet.Cell(
                    rowNumber,
                    "H"))
                .Trim();

        var customer =
            Text(
                worksheet.Cell(
                    rowNumber,
                    "K"))
                .Trim();

        var quantity =
            DecimalValue(
                worksheet.Cell(
                    rowNumber,
                    "L"));

        var unitPrice =
            DecimalValue(
                worksheet.Cell(
                    rowNumber,
                    "M"));

        var issues =
            new HashSet<string>(
                StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(product))
        {
            issues.Add(
                "PRODUCT_NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            issues.Add(
                "DOCUMENT_MISSING");
        }

        if (string.IsNullOrWhiteSpace(customer))
        {
            issues.Add(
                "CUSTOMER_MISSING");
        }

        if (!dateResult.Value.HasValue)
        {
            issues.Add(
                "SALE_DATE_INVALID");
        }
        else if (
            dateResult.Quality ==
            "RECOVERED")
        {
            issues.Add(
                "SALE_DATE_RECOVERED");
        }

        if (
            !quantity.HasValue ||
            quantity.Value <= 0)
        {
            issues.Add(
                "QUANTITY_INVALID");
        }

        if (
            !unitPrice.HasValue ||
            unitPrice.Value <= 0)
        {
            issues.Add(
                "UNIT_PRICE_INVALID");
        }

        return new CommercialImportRow(
            worksheet.Name,
            rowNumber,
            product?.Trim() ??
                string.Empty,
            document,
            rawSaleDate,
            dateResult.Value,
            dateResult.Quality,
            Text(
                worksheet.Cell(
                    rowNumber,
                    "I")),
            Text(
                worksheet.Cell(
                    rowNumber,
                    "J")),
            customer,
            LegacyText.CanonicalName(
                customer),
            quantity,
            unitPrice,
            Text(
                worksheet.Cell(
                    rowNumber,
                    "N")),
            Text(
                worksheet.Cell(
                    rowNumber,
                    "O")),
            Text(
                worksheet.Cell(
                    rowNumber,
                    "P")),
            Text(
                worksheet.Cell(
                    rowNumber,
                    "Q")),
            issues
                .OrderBy(x => x)
                .ToArray());
    }

    private static DateParseResult ParseSaleDate(
        IXLCell cell)
    {
        if (cell.DataType ==
            XLDataType.DateTime)
        {
            return new DateParseResult(
                cell.GetDateTime(),
                "EXACT");
        }

        var raw =
            Text(cell)
                .Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new DateParseResult(
                null,
                "EMPTY");
        }

        var formats =
            new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "d/M/yyyy"
            };

        if (DateTime.TryParseExact(
                raw,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return new DateParseResult(
                parsed,
                "EXACT");
        }

        var compact =
            Regex.Match(
                raw,
                @"^(\d{2})(\d{2})/(\d{4})$");

        if (compact.Success)
        {
            var reconstructed =
                $"{compact.Groups[1].Value}/" +
                $"{compact.Groups[2].Value}/" +
                $"{compact.Groups[3].Value}";

            if (DateTime.TryParseExact(
                    reconstructed,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return new DateParseResult(
                    parsed,
                    "RECOVERED");
            }
        }

        return new DateParseResult(
            null,
            "INVALID");
    }

    private static decimal? DecimalValue(
        IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType ==
            XLDataType.Number)
        {
            return Convert.ToDecimal(
                cell.GetDouble(),
                CultureInfo.InvariantCulture);
        }

        var raw =
            Text(cell);

        if (decimal.TryParse(
                raw,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(
                raw,
                NumberStyles.Any,
                new CultureInfo("pt-BR"),
                out var brazilian))
        {
            return brazilian;
        }

        return null;
    }

    private static string Text(
        IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        try
        {
            if (cell.DataType ==
                XLDataType.DateTime)
            {
                return cell
                    .GetDateTime()
                    .ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture);
            }

            if (cell.DataType ==
                XLDataType.Number)
            {
                return cell
                    .GetDouble()
                    .ToString(
                        "0.##########",
                        CultureInfo.InvariantCulture);
            }

            return cell
                .GetFormattedString()
                .Trim();
        }
        catch
        {
            return cell.Value
                .ToString()
                .Trim();
        }
    }

    private sealed record DateParseResult(
        DateTime? Value,
        string Quality);
}
