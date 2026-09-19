using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

public static class CommercialDryRun
{
    private static readonly string[] TargetSheets =
    [
        "HERINGER",
        "FERTIPAR",
        "REAL",
        "EQUILÍBRIO",
        "DIVERSOS"
    ];

    public static int Run(
        string workbookPath,
        string outputDirectory)
    {
        if (!File.Exists(workbookPath))
        {
            Console.Error.WriteLine(
                $"Arquivo não encontrado: {workbookPath}");

            return 1;
        }

        Directory.CreateDirectory(
            outputDirectory);

        using var workbook =
            new XLWorkbook(workbookPath);

        var rows =
            new List<CommercialRow>();

        foreach (var sheetName in TargetSheets)
        {
            if (!workbook.Worksheets
                .TryGetWorksheet(
                    sheetName,
                    out var worksheet))
            {
                continue;
            }

            ReadWorksheet(
                worksheet,
                rows);
        }

        ApplyDocumentValidation(
            rows);

        WriteRowsCsv(
            rows,
            Path.Combine(
                outputDirectory,
                "legacy-commercial-rows.csv"));

        WriteReviewCsv(
            rows,
            Path.Combine(
                outputDirectory,
                "legacy-commercial-review.csv"));

        WriteDocumentCsv(
            rows,
            Path.Combine(
                outputDirectory,
                "legacy-commercial-documents.csv"));

        PrintSummary(rows);

        return 0;
    }

    private static void ReadWorksheet(
        IXLWorksheet worksheet,
        List<CommercialRow> output)
    {
        var used =
            worksheet.RangeUsed();

        if (used is null)
        {
            return;
        }

        string? currentProduct = null;

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

            var row =
                BuildRow(
                    worksheet,
                    rowNumber,
                    currentProduct);

            output.Add(row);
        }
    }

    private static bool IsSaleHeader(
        IXLWorksheet worksheet,
        int row)
    {
        return
            Normalize(
                Text(
                    worksheet.Cell(
                        row,
                        "G"))) == "DATA2"
            &&
            Normalize(
                Text(
                    worksheet.Cell(
                        row,
                        "K"))) == "CLIENTE"
            &&
            Normalize(
                Text(
                    worksheet.Cell(
                        row,
                        "L")))
                .StartsWith(
                    "QUANT")
            &&
            Normalize(
                Text(
                    worksheet.Cell(
                        row,
                        "M"))) == "VALOR2";
    }

    private static string? FindProductName(
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
                Normalize(raw);

            if (
                normalized is
                    "COMPRA" or
                    "DATA" or
                    "TOTAL" or
                    "CONTROLE DE ADUBOS")
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

        return null;
    }

    private static bool LooksLikeSaleRow(
        IXLWorksheet worksheet,
        int row)
    {
        var a =
            Normalize(
                Text(
                    worksheet.Cell(
                        row,
                        "A")));

        if (a == "TOTAL")
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

    private static CommercialRow BuildRow(
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

        var customer =
            Text(
                worksheet.Cell(
                    rowNumber,
                    "K"));

        var document =
            Text(
                worksheet.Cell(
                    rowNumber,
                    "H"));

        var financialRaw =
            Text(
                worksheet.Cell(
                    rowNumber,
                    "N"));

        var observation =
            Text(
                worksheet.Cell(
                    rowNumber,
                    "Q"));

        var result =
            new CommercialRow
            {
                Sheet =
                    worksheet.Name,

                RowNumber =
                    rowNumber,

                Product =
                    product?.Trim() ??
                    string.Empty,

                Document =
                    document.Trim(),

                RawSaleDate =
                    rawSaleDate,

                SaleDate =
                    dateResult.Value,

                DateQuality =
                    dateResult.Quality,

                Seller =
                    Text(
                        worksheet.Cell(
                            rowNumber,
                            "I")),

                Address =
                    Text(
                        worksheet.Cell(
                            rowNumber,
                            "J")),

                Customer =
                    customer.Trim(),

                CanonicalCustomer =
                    CanonicalName(
                        customer),

                Quantity =
                    quantity,

                UnitPrice =
                    unitPrice,

                FinancialRaw =
                    financialRaw,

                FinancialClass =
                    ClassifyFinancialRaw(
                        financialRaw),

                TravaRaw =
                    Text(
                        worksheet.Cell(
                            rowNumber,
                            "O")),

                DeliveryStatusRaw =
                    Text(
                        worksheet.Cell(
                            rowNumber,
                            "P")),

                ObservationRaw =
                    observation,

                DeliveryHint =
                    ClassifyDeliveryHint(
                        observation)
            };

        if (string.IsNullOrWhiteSpace(
                result.Product))
        {
            result.Issues.Add(
                "PRODUCT_NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(
                result.Document))
        {
            result.Issues.Add(
                "DOCUMENT_MISSING");
        }

        if (string.IsNullOrWhiteSpace(
                result.Customer))
        {
            result.Issues.Add(
                "CUSTOMER_MISSING");
        }

        if (!result.SaleDate.HasValue)
        {
            result.Issues.Add(
                "SALE_DATE_INVALID");
        }
        else if (
            result.DateQuality ==
            "RECOVERED")
        {
            result.Issues.Add(
                "SALE_DATE_RECOVERED");
        }

        if (
            !result.Quantity.HasValue ||
            result.Quantity <= 0)
        {
            result.Issues.Add(
                "QUANTITY_INVALID");
        }

        if (
            !result.UnitPrice.HasValue ||
            result.UnitPrice <= 0)
        {
            result.Issues.Add(
                "UNIT_PRICE_INVALID");
        }

        return result;
    }

    private static void ApplyDocumentValidation(
        List<CommercialRow> rows)
    {
        var groups =
            rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.Document))
                .GroupBy(
                    row =>
                        NormalizeDocument(
                            row.Document));

        foreach (var group in groups)
        {
            var customers =
                group
                    .Select(
                        row =>
                            row.CanonicalCustomer)
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Distinct()
                    .ToList();

            var dates =
                group
                    .Where(
                        row =>
                            row.SaleDate.HasValue)
                    .Select(
                        row =>
                            row.SaleDate!.Value.Date)
                    .Distinct()
                    .ToList();

            if (customers.Count > 1)
            {
                foreach (var row in group)
                {
                    row.Issues.Add(
                        "DOCUMENT_CUSTOMER_CONFLICT");
                }
            }

            if (dates.Count > 1)
            {
                foreach (var row in group)
                {
                    row.Issues.Add(
                        "DOCUMENT_DATE_CONFLICT");
                }
            }
        }
    }

    private static void PrintSummary(
        List<CommercialRow> rows)
    {
        var ready =
            rows.Count(
                row =>
                    row.Issues.Count == 0);

        var review =
            rows.Count - ready;

        var documents =
            rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.Document))
                .GroupBy(
                    row =>
                        NormalizeDocument(
                            row.Document))
                .ToList();

        var multiItemDocuments =
            documents.Count(
                group =>
                    group.Count() > 1);

        var customerConflictDocuments =
            documents.Count(
                group =>
                    group
                        .Select(
                            row =>
                                row.CanonicalCustomer)
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x))
                        .Distinct()
                        .Count() > 1);

        var dateConflictDocuments =
            documents.Count(
                group =>
                    group
                        .Where(
                            row =>
                                row.SaleDate.HasValue)
                        .Select(
                            row =>
                                row.SaleDate!.Value.Date)
                        .Distinct()
                        .Count() > 1);

        var distinctCustomers =
            rows
                .Select(
                    row =>
                        row.CanonicalCustomer)
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(
                            x))
                .Distinct()
                .Count();

        var sellers =
            rows
                .Select(
                    row =>
                        CanonicalName(
                            row.Seller))
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(
                            x))
                .Distinct()
                .Count();

        var commercialValue =
            rows
                .Where(
                    row =>
                        row.Quantity.HasValue &&
                        row.UnitPrice.HasValue)
                .Sum(
                    row =>
                        row.Quantity!.Value *
                        row.UnitPrice!.Value);

        var financialClasses =
            rows
                .GroupBy(
                    row =>
                        row.FinancialClass)
                .OrderByDescending(
                    group =>
                        group.Count())
                .ToList();

        Console.WriteLine();
        Console.WriteLine(
            "===== COMMERCIAL LEGACY DRY-RUN =====");

        Console.WriteLine(
            $"Linhas candidatas ............ {rows.Count}");

        Console.WriteLine(
            $"Linhas comerciais seguras .... {ready}");

        Console.WriteLine(
            $"Linhas para revisão .......... {review}");

        Console.WriteLine(
            $"Documentos/NOTA2 distintos ... {documents.Count}");

        Console.WriteLine(
            $"Documentos multi-item ........ {multiItemDocuments}");

        Console.WriteLine(
            $"Conflitos de cliente ......... {customerConflictDocuments}");

        Console.WriteLine(
            $"Conflitos de data ............ {dateConflictDocuments}");

        Console.WriteLine(
            $"Clientes canônicos ........... {distinctCustomers}");

        Console.WriteLine(
            $"Vendedores distintos ......... {sellers}");

        Console.WriteLine(
            $"Valor comercial legível ...... {commercialValue:N2}");

        Console.WriteLine();
        Console.WriteLine(
            "Financeiro bruto:");

        foreach (
            var group
            in financialClasses)
        {
            Console.WriteLine(
                $"  {group.Key,-28} {group.Count(),5}");
        }

        Console.WriteLine();
        Console.WriteLine(
            "Entidades que este dry-run grava:");

        Console.WriteLine(
            "  Customer .................... 0");

        Console.WriteLine(
            "  Sale ........................ 0");

        Console.WriteLine(
            "  SaleItem .................... 0");

        Console.WriteLine(
            "  Receivable .................. 0");

        Console.WriteLine(
            "  Payment ..................... 0");

        Console.WriteLine(
            "  InventoryMovement ........... 0");

        Console.WriteLine();
        Console.WriteLine(
            "Arquivos:");

        Console.WriteLine(
            "  artifacts/legacy-commercial-rows.csv");

        Console.WriteLine(
            "  artifacts/legacy-commercial-review.csv");

        Console.WriteLine(
            "  artifacts/legacy-commercial-documents.csv");
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

    private static string ClassifyFinancialRaw(
        string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "EMPTY";
        }

        var normalized =
            Normalize(raw)
                .Replace(
                    " ",
                    string.Empty);

        if (
            normalized is
                "AVISTA" or
                "AVISTA.")
        {
            return "CASH_CONDITION_ONLY";
        }

        if (normalized.Contains(
                "PAGO"))
        {
            return "PAID_MARKER_ONLY";
        }

        if (normalized.Contains(
                "CARTAO"))
        {
            return "PAYMENT_METHOD_ONLY";
        }

        if (Regex.IsMatch(
                raw,
                @"\d{1,2}/\d{1,2}/\d{4}") ||
            Regex.IsMatch(
                raw,
                @"\d{4}-\d{2}-\d{2}"))
        {
            return "DUE_DATE_CANDIDATE";
        }

        return "UNKNOWN";
    }

    private static string ClassifyDeliveryHint(
        string raw)
    {
        var normalized =
            Normalize(raw);

        if (normalized.Contains(
                "VEM BUSCAR"))
        {
            return "PICKUP_HINT";
        }

        if (normalized.Contains(
                "ENTREGAS"))
        {
            return "DELIVERY_HINT";
        }

        return "UNKNOWN";
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

    private static string NormalizeDocument(
        string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace(
                " ",
                string.Empty);
    }

    private static string CanonicalName(
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
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (
                char.IsLetterOrDigit(
                    character) ||
                char.IsWhiteSpace(
                    character))
            {
                builder.Append(
                    character);
            }
        }

        return Regex.Replace(
                builder.ToString(),
                @"\s+",
                " ")
            .Trim();
    }

    private static string Normalize(
        string value)
    {
        return CanonicalName(value);
    }

    private static void WriteRowsCsv(
        IReadOnlyList<CommercialRow> rows,
        string path)
    {
        using var writer =
            new StreamWriter(
                path,
                false,
                new UTF8Encoding(true));

        writer.WriteLine(
            "Sheet;Row;Product;Document;SaleDateRaw;" +
            "SaleDate;DateQuality;Seller;Address;Customer;" +
            "CanonicalCustomer;Quantity;UnitPrice;LineTotal;" +
            "FinancialRaw;FinancialClass;TravaRaw;" +
            "DeliveryStatusRaw;ObservationRaw;DeliveryHint;" +
            "Status;Issues");

        foreach (var row in rows)
        {
            WriteCsvRow(
                writer,
                row);
        }
    }

    private static void WriteReviewCsv(
        IReadOnlyList<CommercialRow> rows,
        string path)
    {
        using var writer =
            new StreamWriter(
                path,
                false,
                new UTF8Encoding(true));

        writer.WriteLine(
            "Sheet;Row;Product;Document;SaleDateRaw;" +
            "Customer;Quantity;UnitPrice;FinancialRaw;" +
            "Issues");

        foreach (
            var row
            in rows.Where(
                x =>
                    x.Issues.Count > 0))
        {
            writer.WriteLine(
                string.Join(
                    ";",
                    Csv(row.Sheet),
                    Csv(
                        row.RowNumber
                            .ToString()),
                    Csv(row.Product),
                    Csv(row.Document),
                    Csv(row.RawSaleDate),
                    Csv(row.Customer),
                    Csv(
                        row.Quantity?
                            .ToString(
                                CultureInfo.InvariantCulture)
                        ?? string.Empty),
                    Csv(
                        row.UnitPrice?
                            .ToString(
                                CultureInfo.InvariantCulture)
                        ?? string.Empty),
                    Csv(row.FinancialRaw),
                    Csv(
                        string.Join(
                            "|",
                            row.Issues))));
        }
    }

    private static void WriteDocumentCsv(
        IReadOnlyList<CommercialRow> rows,
        string path)
    {
        using var writer =
            new StreamWriter(
                path,
                false,
                new UTF8Encoding(true));

        writer.WriteLine(
            "Document;Items;Sheets;Dates;Customers;" +
            "Products;Issues");

        foreach (
            var group
            in rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.Document))
                .GroupBy(
                    row =>
                        NormalizeDocument(
                            row.Document))
                .OrderBy(
                    group =>
                        group.Key))
        {
            var issues =
                group
                    .SelectMany(
                        row =>
                            row.Issues)
                    .Where(
                        issue =>
                            issue.StartsWith(
                                "DOCUMENT_"))
                    .Distinct()
                    .ToList();

            writer.WriteLine(
                string.Join(
                    ";",
                    Csv(group.Key),
                    Csv(
                        group.Count()
                            .ToString()),
                    Csv(
                        string.Join(
                            "|",
                            group
                                .Select(
                                    row =>
                                        row.Sheet)
                                .Distinct())),
                    Csv(
                        string.Join(
                            "|",
                            group
                                .Where(
                                    row =>
                                        row.SaleDate.HasValue)
                                .Select(
                                    row =>
                                        row.SaleDate!.Value
                                            .ToString(
                                                "yyyy-MM-dd"))
                                .Distinct())),
                    Csv(
                        string.Join(
                            "|",
                            group
                                .Select(
                                    row =>
                                        row.Customer)
                                .Distinct())),
                    Csv(
                        string.Join(
                            "|",
                            group
                                .Select(
                                    row =>
                                        row.Product)
                                .Distinct())),
                    Csv(
                        string.Join(
                            "|",
                            issues))));
        }
    }

    private static void WriteCsvRow(
        StreamWriter writer,
        CommercialRow row)
    {
        var lineTotal =
            row.Quantity.HasValue &&
            row.UnitPrice.HasValue
                ? row.Quantity.Value *
                  row.UnitPrice.Value
                : (decimal?)null;

        writer.WriteLine(
            string.Join(
                ";",
                Csv(row.Sheet),
                Csv(
                    row.RowNumber
                        .ToString()),
                Csv(row.Product),
                Csv(row.Document),
                Csv(row.RawSaleDate),
                Csv(
                    row.SaleDate?
                        .ToString(
                            "yyyy-MM-dd")
                    ?? string.Empty),
                Csv(row.DateQuality),
                Csv(row.Seller),
                Csv(row.Address),
                Csv(row.Customer),
                Csv(row.CanonicalCustomer),
                Csv(
                    row.Quantity?
                        .ToString(
                            CultureInfo.InvariantCulture)
                    ?? string.Empty),
                Csv(
                    row.UnitPrice?
                        .ToString(
                            CultureInfo.InvariantCulture)
                    ?? string.Empty),
                Csv(
                    lineTotal?
                        .ToString(
                            CultureInfo.InvariantCulture)
                    ?? string.Empty),
                Csv(row.FinancialRaw),
                Csv(row.FinancialClass),
                Csv(row.TravaRaw),
                Csv(row.DeliveryStatusRaw),
                Csv(row.ObservationRaw),
                Csv(row.DeliveryHint),
                Csv(
                    row.Issues.Count == 0
                        ? "READY"
                        : "REVIEW"),
                Csv(
                    string.Join(
                        "|",
                        row.Issues))));
    }

    private static string Csv(
        string value)
    {
        return "\"" +
               value.Replace(
                   "\"",
                   "\"\"") +
               "\"";
    }

    private sealed record DateParseResult(
        DateTime? Value,
        string Quality);

    private sealed class CommercialRow
    {
        public string Sheet { get; init; } = "";
        public int RowNumber { get; init; }
        public string Product { get; init; } = "";
        public string Document { get; init; } = "";
        public string RawSaleDate { get; init; } = "";
        public DateTime? SaleDate { get; init; }
        public string DateQuality { get; init; } = "";
        public string Seller { get; init; } = "";
        public string Address { get; init; } = "";
        public string Customer { get; init; } = "";
        public string CanonicalCustomer { get; init; } = "";
        public decimal? Quantity { get; init; }
        public decimal? UnitPrice { get; init; }
        public string FinancialRaw { get; init; } = "";
        public string FinancialClass { get; init; } = "";
        public string TravaRaw { get; init; } = "";
        public string DeliveryStatusRaw { get; init; } = "";
        public string ObservationRaw { get; init; } = "";
        public string DeliveryHint { get; init; } = "";

        public HashSet<string> Issues { get; } =
            new(
                StringComparer.Ordinal);
    }
}
