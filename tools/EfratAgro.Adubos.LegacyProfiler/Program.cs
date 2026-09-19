using System.Globalization;
using System.Text;
using ClosedXML.Excel;

Console.OutputEncoding = Encoding.UTF8;

if (
    args.Length > 0 &&
    args[0] == "--commercial-dry-run")
{
    var input =
        args.Length > 1
            ? args[1]
            : Path.Combine(
                Directory.GetCurrentDirectory(),
                "data",
                "import",
                "ADUBOS_2025_23_import.xlsx");

    var output =
        args.Length > 2
            ? args[2]
            : Path.Combine(
                Directory.GetCurrentDirectory(),
                "artifacts");

    return CommercialDryRun.Run(
        input,
        output);
}


var workbookPath =
    args.Length > 0
        ? args[0]
        : Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "import",
            "ADUBOS_2025_23_import.xlsx");

if (!File.Exists(workbookPath))
{
    Console.Error.WriteLine(
        $"Arquivo não encontrado: {workbookPath}");

    return 1;
}

var targetSheets =
    new[]
    {
        "HERINGER",
        "FERTIPAR",
        "REAL",
        "EQUILÍBRIO",
        "DIVERSOS"
    };

var keywords =
    new[]
    {
        "CLIENTE",
        "VENDEDOR",
        "VENDA",
        "PRODUTO",
        "QUANT",
        "QTD",
        "VALOR",
        "PREÇO",
        "PRECO",
        "DATA",
        "VENC",
        "PAG",
        "PAGO",
        "STATUS",
        "ENTREGA",
        "FRETE",
        "SAFRA",
        "TRAVA",
        "PIX",
        "CARTAO",
        "CARTÃO",
        "BOLETO"
    };

Console.WriteLine(
    "===== EFRATAGRO LEGACY COMMERCIAL PROFILER =====");

Console.WriteLine(
    $"Arquivo: {workbookPath}");

Console.WriteLine();

using var workbook =
    new XLWorkbook(workbookPath);

foreach (var sheetName in targetSheets)
{
    if (!workbook.Worksheets
        .TryGetWorksheet(
            sheetName,
            out var worksheet))
    {
        Console.WriteLine(
            $"===== {sheetName} =====");

        Console.WriteLine(
            "ABA NÃO ENCONTRADA");

        Console.WriteLine();

        continue;
    }

    var usedRange =
        worksheet.RangeUsed();

    Console.WriteLine();
    Console.WriteLine(
        $"===== {sheetName} =====");

    if (usedRange is null)
    {
        Console.WriteLine(
            "Aba vazia.");

        continue;
    }

    Console.WriteLine(
        $"Used range: {usedRange.RangeAddress}");

    Console.WriteLine(
        $"Linhas: {usedRange.RowCount()}");

    Console.WriteLine(
        $"Colunas: {usedRange.ColumnCount()}");

    Console.WriteLine();
    Console.WriteLine(
        "--- PRIMEIRAS 25 LINHAS NÃO VAZIAS ---");

    var firstRow =
        usedRange.RangeAddress
            .FirstAddress.RowNumber;

    var lastRow =
        usedRange.RangeAddress
            .LastAddress.RowNumber;

    var firstColumn =
        usedRange.RangeAddress
            .FirstAddress.ColumnNumber;

    var lastColumn =
        usedRange.RangeAddress
            .LastAddress.ColumnNumber;

    var rowsPrinted = 0;

    for (
        var rowNumber = firstRow;
        rowNumber <= lastRow &&
        rowsPrinted < 25;
        rowNumber++)
    {
        var values =
            GetNonEmptyCells(
                worksheet,
                rowNumber,
                firstColumn,
                lastColumn);

        if (values.Count == 0)
        {
            continue;
        }

        Console.WriteLine(
            $"ROW {rowNumber:D3} | " +
            string.Join(
                " | ",
                values));

        rowsPrinted++;
    }

    Console.WriteLine();
    Console.WriteLine(
        "--- CÉLULAS COM PALAVRAS-CHAVE ---");

    var keywordHits =
        new List<string>();

    foreach (var cell in usedRange.CellsUsed())
    {
        var raw =
            GetDisplayValue(cell);

        if (string.IsNullOrWhiteSpace(raw))
        {
            continue;
        }

        var normalized =
            Normalize(raw);

        if (!keywords.Any(
            keyword =>
                normalized.Contains(
                    Normalize(keyword),
                    StringComparison.Ordinal)))
        {
            continue;
        }

        keywordHits.Add(
            $"{cell.Address}={Escape(raw)}");
    }

    foreach (
        var hit
        in keywordHits
            .Distinct()
            .Take(150))
    {
        Console.WriteLine(hit);
    }

    Console.WriteLine(
        $"Keyword hits: {keywordHits.Count}");

    Console.WriteLine();
    Console.WriteLine(
        "--- VALORES RECORRENTES DE STATUS/PAGAMENTO ---");

    var candidateValues =
        usedRange
            .CellsUsed()
            .Select(GetDisplayValue)
            .Where(
                value =>
                    !string.IsNullOrWhiteSpace(
                        value))
            .Select(
                value =>
                    value.Trim())
            .Where(
                LooksLikeOperationalValue)
            .GroupBy(
                value =>
                    Normalize(value))
            .Select(
                group =>
                    new
                    {
                        Value =
                            group.First(),

                        Count =
                            group.Count()
                    })
            .OrderByDescending(
                item =>
                    item.Count)
            .ThenBy(
                item =>
                    item.Value)
            .Take(80)
            .ToList();

    foreach (var item in candidateValues)
    {
        Console.WriteLine(
            $"{item.Count,5} x {item.Value}");
    }
}

return 0;

static List<string> GetNonEmptyCells(
    IXLWorksheet worksheet,
    int rowNumber,
    int firstColumn,
    int lastColumn)
{
    var result =
        new List<string>();

    for (
        var columnNumber = firstColumn;
        columnNumber <= lastColumn;
        columnNumber++)
    {
        var cell =
            worksheet.Cell(
                rowNumber,
                columnNumber);

        if (cell.IsEmpty())
        {
            continue;
        }

        var value =
            GetDisplayValue(cell);

        if (string.IsNullOrWhiteSpace(value))
        {
            continue;
        }

        result.Add(
            $"{cell.Address}={Escape(value)}");
    }

    return result;
}

static string GetDisplayValue(
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

        if (cell.DataType ==
            XLDataType.Boolean)
        {
            return cell
                .GetBoolean()
                .ToString();
        }

        return cell
            .GetFormattedString()
            .Trim();
    }
    catch
    {
        return cell
            .Value
            .ToString()
            .Trim();
    }
}

static bool LooksLikeOperationalValue(
    string value)
{
    var normalized =
        Normalize(value);

    if (normalized.Length > 40)
    {
        return false;
    }

    var terms =
        new[]
        {
            "PENDENTE",
            "PAGO",
            "PAGA",
            "ENTREGUE",
            "ENTREGA",
            "RETIRADA",
            "A VISTA",
            "AVISTA",
            "CARTAO",
            "CARTÃO",
            "PIX",
            "BOLETO",
            "DINHEIRO",
            "TRANSFERENCIA",
            "TRANSFERÊNCIA",
            "SIM",
            "NAO",
            "NÃO",
            "TRAVA",
            "SAFRA"
        };

    return terms.Any(
        term =>
            normalized.Contains(
                Normalize(term),
                StringComparison.Ordinal));
}

static string Normalize(
    string value)
{
    var normalized =
        value
            .Trim()
            .ToUpperInvariant()
            .Normalize(
                NormalizationForm.FormD);

    var builder =
        new StringBuilder();

    foreach (var character in normalized)
    {
        if (
            CharUnicodeInfo
                .GetUnicodeCategory(
                    character) !=
            UnicodeCategory
                .NonSpacingMark)
        {
            builder.Append(
                character);
        }
    }

    return builder
        .ToString()
        .Normalize(
            NormalizationForm.FormC);
}

static string Escape(
    string value)
{
    return value
        .Replace(
            "\r",
            " ")
        .Replace(
            "\n",
            " ")
        .Trim();
}
