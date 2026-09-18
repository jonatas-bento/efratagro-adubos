using EfratAgro.Adubos.LegacyImporter.Services;

const decimal expectedGrandTotal = 19174m;

var expectedSupplierTotals =
    new Dictionary<string, decimal>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["HERINGER"] = 4409m,
        ["FERTIPAR"] = 1186m,
        ["REAL"] = 438m,
        ["DIVERSOS"] = 11465m,
        ["EQUILÍBRIO"] = 1676m
    };

var filePath = GetArgumentValue(args, "--file");

if (string.IsNullOrWhiteSpace(filePath))
{
    Console.Error.WriteLine(
        "Usage:");
    Console.Error.WriteLine(
        "dotnet run --project tools/EfratAgro.Adubos.LegacyImporter -- " +
        "--file <path-to-xlsx> --dry-run");

    return 2;
}

var dryRun =
    args.Any(x =>
        x.Equals(
            "--dry-run",
            StringComparison.OrdinalIgnoreCase));

if (!dryRun)
{
    Console.Error.WriteLine(
        "Persistence is intentionally disabled at this stage.");

    Console.Error.WriteLine(
        "Run the importer with --dry-run.");

    return 3;
}

try
{
    Console.WriteLine(
        "============================================");

    Console.WriteLine(
        " EFRATAGRO - LEGACY IMPORTER / DRY RUN");

    Console.WriteLine(
        "============================================");

    Console.WriteLine();

    Console.WriteLine(
        $"Source: {Path.GetFullPath(filePath)}");

    Console.WriteLine(
        "Worksheet: ARMAZÉM");

    Console.WriteLine();

    var reader =
        new WarehouseSpreadsheetReader();

    var rows =
        reader.Read(filePath);

    Console.WriteLine(
        $"Product rows found: {rows.Count}");

    Console.WriteLine(
        $"Rows with stock > 0: {rows.Count(x => x.Quantity > 0)}");

    Console.WriteLine();

    var supplierTotals =
        rows
            .GroupBy(
                x => x.Supplier,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Quantity),
                StringComparer.OrdinalIgnoreCase);

    var validationFailed = false;

    Console.WriteLine(
        "===== TOTAL BY SUPPLIER =====");

    foreach (var expected in expectedSupplierTotals)
    {
        supplierTotals.TryGetValue(
            expected.Key,
            out var actual);

        var ok =
            actual == expected.Value;

        Console.WriteLine(
            $"{expected.Key,-12} " +
            $"Actual={actual,8:N0}  " +
            $"Expected={expected.Value,8:N0}  " +
            $"{(ok ? "OK" : "MISMATCH")}");

        if (!ok)
        {
            validationFailed = true;
        }
    }

    var grandTotal =
        rows.Sum(x => x.Quantity);

    Console.WriteLine();
    Console.WriteLine(
        "===== GRAND TOTAL =====");

    Console.WriteLine(
        $"Actual:   {grandTotal:N0}");

    Console.WriteLine(
        $"Expected: {expectedGrandTotal:N0}");

    if (grandTotal != expectedGrandTotal)
    {
        validationFailed = true;
    }

    Console.WriteLine();

    Console.WriteLine(
        "===== SAMPLE NON-ZERO STOCK =====");

    foreach (var row in rows
                 .Where(x => x.Quantity > 0)
                 .Take(15))
    {
        Console.WriteLine(
            $"{row.Supplier,-12} " +
            $"{row.Product,-35} " +
            $"{row.Quantity,8:N0} " +
            $"[row {row.ExcelRow}]");
    }

    Console.WriteLine();

    if (validationFailed)
    {
        Console.Error.WriteLine(
            "DRY RUN FAILED ❌");

        Console.Error.WriteLine(
            "Spreadsheet totals do not match the migration baseline.");

        return 10;
    }

    Console.WriteLine(
        "DRY RUN PASSED ✅");

    Console.WriteLine(
        "The ARMAZÉM snapshot reconciles with the legacy baseline.");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(
        $"IMPORT ERROR ❌ {ex.Message}");

    return 99;
}

static string? GetArgumentValue(
    string[] args,
    string name)
{
    for (var i = 0;
         i < args.Length - 1;
         i++)
    {
        if (args[i].Equals(
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}
