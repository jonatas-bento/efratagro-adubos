using EfratAgro.Adubos.LegacyImporter.Commands;
using EfratAgro.Adubos.Infrastructure;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.LegacyImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


if (args.Any(
        x =>
            x.Equals(
                "--resolve-known-reviews",
                StringComparison.OrdinalIgnoreCase)))
{
    return await EfratAgro.Adubos.LegacyImporter.Commands
        .KnownCommercialReviewResolutionCommand
        .RunAsync(args);
}


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

var filePath =
    GetArgumentValue(
        args,
        "--file");

if (string.IsNullOrWhiteSpace(filePath))
{
    Console.Error.WriteLine(
        "Usage:");

    Console.Error.WriteLine(
        "dotnet run --project tools/EfratAgro.Adubos.LegacyImporter -- " +
        "--file <path-to-xlsx> --scope <warehouse|commercial> (--dry-run | --persist)");

    return 2;
}

var dryRun =
    args.Any(x =>
        x.Equals(
            "--dry-run",
            StringComparison.OrdinalIgnoreCase));

var persist =
    args.Any(x =>
        x.Equals(
            "--persist",
            StringComparison.OrdinalIgnoreCase));

if (dryRun == persist)
{
    Console.Error.WriteLine(
        "Choose exactly one mode: --dry-run or --persist.");

    return 3;
}

var importScope =
    GetArgumentValue(
        args,
        "--scope")
    ??
    "warehouse";

if (importScope.Equals(
        "commercial",
        StringComparison.OrdinalIgnoreCase))
{
    return await CommercialImportCommand.RunAsync(
        filePath,
        dryRun,
        args);
}

if (!importScope.Equals(
        "warehouse",
        StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Invalid scope. Use 'warehouse' or 'commercial'.");

    return 4;
}


try
{
    Console.WriteLine(
        "============================================");

    Console.WriteLine(
        dryRun
            ? " EFRATAGRO - LEGACY IMPORTER / DRY RUN"
            : " EFRATAGRO - LEGACY IMPORTER / PERSIST");

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
        $"Product rows found: {rows.Count}");

    Console.WriteLine(
        $"Rows with stock > 0: {rows.Count(x => x.Quantity > 0)}");

    Console.WriteLine();

    Console.WriteLine(
        "===== RECONCILIATION =====");

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
        $"Grand total: {grandTotal:N0} / {expectedGrandTotal:N0}");

    if (grandTotal != expectedGrandTotal)
    {
        validationFailed = true;
    }

    if (validationFailed)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine(
            "RECONCILIATION FAILED ❌");

        Console.Error.WriteLine(
            "Persistence has been blocked.");

        return 10;
    }

    Console.WriteLine();
    Console.WriteLine(
        "RECONCILIATION PASSED ✅");

    if (dryRun)
    {
        Console.WriteLine(
            "No database changes were made.");

        return 0;
    }

    var connectionString =
        Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine();

        Console.Error.WriteLine(
            "ConnectionStrings__DefaultConnection is not set.");

        return 20;
    }

    var builder =
        Host.CreateApplicationBuilder(args);

    builder.Configuration[
        "ConnectionStrings:DefaultConnection"] =
        connectionString;

    builder.Services.AddInfrastructure(
        builder.Configuration);

    using var host =
        builder.Build();

    await using var scope =
        host.Services.CreateAsyncScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AdubosDbContext>();

    if (!await dbContext.Database.CanConnectAsync())
    {
        Console.Error.WriteLine(
            "Unable to connect to database.");

        return 21;
    }

    var pendingMigrations =
        await dbContext.Database
            .GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        Console.Error.WriteLine(
            "Database has pending migrations.");

        foreach (var migration
                 in pendingMigrations)
        {
            Console.Error.WriteLine(
                $" - {migration}");
        }

        return 22;
    }

    var persistence =
        new WarehouseImportPersistenceService(
            dbContext);

    var result =
        await persistence.ImportAsync(
            filePath,
            rows);

    Console.WriteLine();

    Console.WriteLine(
        "===== IMPORT COMPLETED =====");

    Console.WriteLine(
        $"Batch:               {result.BatchId}");

    Console.WriteLine(
        $"SHA-256:             {result.FileHash}");

    Console.WriteLine(
        $"Legacy rows:         {result.LegacyRows}");

    Console.WriteLine(
        $"Suppliers:           {result.Suppliers}");

    Console.WriteLine(
        $"Products:            {result.Products}");

    Console.WriteLine(
        $"Opening movements:   {result.InventoryMovements}");

    Console.WriteLine(
        $"Opening quantity:    {result.TotalQuantity:N0}");

    Console.WriteLine();

    Console.WriteLine(
        "PERSISTENCE PASSED ✅");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
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
