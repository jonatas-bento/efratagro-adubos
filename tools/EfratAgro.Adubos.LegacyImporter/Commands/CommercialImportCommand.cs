using System.Security.Cryptography;
using EfratAgro.Adubos.Domain.Legacy;
using EfratAgro.Adubos.Infrastructure;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.LegacyImporter.Commercial;
using EfratAgro.Adubos.LegacyImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EfratAgro.Adubos.LegacyImporter.Commands;

public static class CommercialImportCommand
{
    public static async Task<int> RunAsync(
        string filePath,
        bool dryRun,
        string[] args)
    {
        Console.WriteLine();
        Console.WriteLine(
            "============================================");

        Console.WriteLine(
            dryRun
                ? " EFRATAGRO - COMMERCIAL LEGACY / DRY RUN"
                : " EFRATAGRO - COMMERCIAL LEGACY / PERSIST");

        Console.WriteLine(
            "============================================");

        Console.WriteLine();

        var reader =
            new CommercialSpreadsheetReader();

        var rows =
            reader.Read(
                filePath);

        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            Console.Error.WriteLine(
                "ConnectionStrings__DefaultConnection is not set.");

            return 20;
        }

        var builder =
            Host.CreateApplicationBuilder(
                args);

        builder.Configuration[
            "ConnectionStrings:DefaultConnection"] =
            connectionString;

        builder.Services.AddInfrastructure(
            builder.Configuration);

        using var host =
            builder.Build();

        await using var scope =
            host.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    AdubosDbContext>();

        if (
            !await dbContext.Database
                .CanConnectAsync())
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

            foreach (
                var migration
                in pendingMigrations)
            {
                Console.Error.WriteLine(
                    $" - {migration}");
            }

            return 22;
        }

        var fileHash =
            await CalculateSha256Async(
                filePath);

        var alreadyImported =
            await dbContext
                .LegacyImportBatches
                .AsNoTracking()
                .AnyAsync(
                    batch =>
                        batch.SourceFileHash ==
                            fileHash
                        &&
                        batch.Scope ==
                            LegacyImportScope
                                .CommercialHistory);

        if (alreadyImported)
        {
            Console.Error.WriteLine(
                "Commercial history from this spreadsheet has already been imported.");

            return 23;
        }

        var suppliers =
            await dbContext.Suppliers
                .AsNoTracking()
                .ToListAsync();

        var products =
            await dbContext.Products
                .AsNoTracking()
                .ToListAsync();

        var customers =
            await dbContext.Customers
                .AsNoTracking()
                .ToListAsync();

        var planner =
            new CommercialImportPlanner();

        var plan =
            planner.Create(
                rows,
                suppliers,
                products,
                customers);

        PrintPlan(
            fileHash,
            plan);

        if (!plan.MatchesKnownBaseline)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                "KNOWN LEGACY BASELINE FAILED ❌");

            Console.Error.WriteLine(
                "Persistence has been blocked.");

            return 30;
        }

        Console.WriteLine();
        Console.WriteLine(
            "KNOWN LEGACY BASELINE PASSED ✅");

        if (dryRun)
        {
            Console.WriteLine();
            Console.WriteLine(
                "DRY RUN PASSED ✅");

            Console.WriteLine(
                "No database changes were made.");

            return 0;
        }

        var persistence =
            new CommercialImportPersistenceService(
                dbContext);

        var result =
            await persistence.ImportAsync(
                filePath,
                fileHash,
                plan);

        Console.WriteLine();
        Console.WriteLine(
            "===== COMMERCIAL IMPORT COMPLETED =====");

        Console.WriteLine(
            $"Batch:                     {result.BatchId}");

        Console.WriteLine(
            $"SHA-256:                   {result.FileHash}");

        Console.WriteLine(
            $"Legacy rows:               {result.LegacyRows}");

        Console.WriteLine(
            $"Safe transactions:         {result.SafeTransactions}");

        Console.WriteLine(
            $"Sale items imported:       {result.SafeItems}");

        Console.WriteLine(
            $"Review transactions:       {result.ReviewTransactions}");

        Console.WriteLine(
            $"Review rows:               {result.ReviewRows}");

        Console.WriteLine(
            $"Customers created:         {result.CustomersCreated}");

        Console.WriteLine(
            $"Legacy products created:   {result.LegacyProductsCreated}");

        Console.WriteLine(
            $"Sales created:             {result.SalesCreated}");

        Console.WriteLine(
            $"SaleItems created:         {result.SaleItemsCreated}");

        Console.WriteLine(
            "Inventory movements:       0");

        Console.WriteLine(
            "Receivables:               0");

        Console.WriteLine(
            "Payments:                  0");

        Console.WriteLine();
        Console.WriteLine(
            "PERSISTENCE PASSED ✅");

        return 0;
    }

    private static void PrintPlan(
        string fileHash,
        CommercialImportPlan plan)
    {
        Console.WriteLine(
            $"SHA-256:                     {fileHash}");

        Console.WriteLine(
            $"Commercial rows:             {plan.Rows.Count}");

        Console.WriteLine(
            $"Unkeyed rows:                 {plan.UnkeyedRows.Count}");

        Console.WriteLine(
            $"Candidate transactions:       {plan.TransactionCount}");

        Console.WriteLine(
            $"Safe transactions:            {plan.SafeTransactions.Count}");

        Console.WriteLine(
            $"Review transactions:          {plan.ReviewTransactions.Count}");

        Console.WriteLine(
            $"Safe items:                   {plan.SafeItemCount}");

        Console.WriteLine(
            $"Review transaction items:     {plan.ReviewTransactionItemCount}");

        Console.WriteLine(
            $"Total review rows:            {plan.ReviewRowCount}");

        Console.WriteLine(
            $"Legacy products to create:    {plan.LegacyProductsToCreate.Count}");

        Console.WriteLine();
        Console.WriteLine(
            "===== LEGACY PRODUCTS TO CREATE INACTIVE =====");

        foreach (
            var product
            in plan.LegacyProductsToCreate)
        {
            Console.WriteLine(
                $"{product.Supplier} :: {product.Product}");
        }

        Console.WriteLine();
        Console.WriteLine(
            "===== REVIEW TRANSACTIONS =====");

        foreach (
            var transaction
            in plan.ReviewTransactions)
        {
            Console.WriteLine(
                $"{transaction.TransactionKey} | " +
                $"{transaction.Items.Count} item(s) | " +
                $"{string.Join("|", transaction.Issues)}");
        }

        Console.WriteLine();
        Console.WriteLine(
            "===== UNKEYED ROWS =====");

        foreach (
            var row
            in plan.UnkeyedRows)
        {
            Console.WriteLine(
                $"{row.Sheet} row {row.ExcelRow} | " +
                $"doc={row.Document} | " +
                $"date={row.RawSaleDate} | " +
                $"{string.Join("|", row.Issues)}");
        }

        Console.WriteLine();
        Console.WriteLine(
            "===== WRITE GATE =====");

        Console.WriteLine(
            "InventoryMovement ........... 0");

        Console.WriteLine(
            "Receivable .................. 0");

        Console.WriteLine(
            "Payment ..................... 0");
    }

    private static async Task<string>
        CalculateSha256Async(
            string filePath)
    {
        await using var stream =
            File.OpenRead(
                filePath);

        using var sha256 =
            SHA256.Create();

        var hash =
            await sha256.ComputeHashAsync(
                stream);

        return Convert.ToHexString(
            hash);
    }
}
