using EfratAgro.Adubos.Infrastructure;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.LegacyImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EfratAgro.Adubos.LegacyImporter.Commands;

public static class KnownCommercialReviewResolutionCommand
{
    public static async Task<int> RunAsync(
        string[] args)
    {
        var dryRun =
            args.Any(
                x =>
                    x.Equals(
                        "--dry-run",
                        StringComparison.OrdinalIgnoreCase));

        var persist =
            args.Any(
                x =>
                    x.Equals(
                        "--persist",
                        StringComparison.OrdinalIgnoreCase));

        if (dryRun == persist)
        {
            Console.Error.WriteLine(
                "Choose exactly one mode: " +
                "--dry-run or --persist.");

            return 3;
        }

        try
        {
            Console.WriteLine(
                "============================================");

            Console.WriteLine(
                dryRun
                    ? " EFRATAGRO - REVIEW RESOLUTION / DRY RUN"
                    : " EFRATAGRO - REVIEW RESOLUTION / PERSIST");

            Console.WriteLine(
                "============================================");

            var builder =
                Host.CreateApplicationBuilder();

            builder.Services.AddInfrastructure(
                builder.Configuration);

            using var host =
                builder.Build();

            await using var scope =
                host.Services
                    .CreateAsyncScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AdubosDbContext>();

            if (!await dbContext.Database
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

                foreach (var migration
                         in pendingMigrations)
                {
                    Console.Error.WriteLine(
                        $" - {migration}");
                }

                return 22;
            }

            var service =
                new KnownCommercialReviewResolutionService(
                    dbContext);

            var result =
                await service.ExecuteAsync(
                    persist);

            Console.WriteLine();
            Console.WriteLine(
                "===== RESOLUTION GATE =====");

            Console.WriteLine(
                $"Candidates:         {result.Candidates}");

            Console.WriteLine(
                $"Already resolved:   {result.AlreadyResolved}");

            Console.WriteLine(
                $"SaleItems created:  {result.SaleItemsCreated}");

            Console.WriteLine(
                $"Review rows before: {result.ReviewRowsBefore}");

            Console.WriteLine(
                $"Review rows after:  {result.ReviewRowsAfter}");

            Console.WriteLine();
            Console.WriteLine(
                dryRun
                    ? "DRY RUN PASSED ✅"
                    : "REVIEW RESOLUTION PASSED ✅");

            if (dryRun)
            {
                Console.WriteLine(
                    "No database changes were made.");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"RESOLUTION ERROR ❌ {ex.Message}");

            return 99;
        }
    }
}
