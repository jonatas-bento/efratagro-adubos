using EfratAgro.Adubos.Application.Inventory;
using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Infrastructure.Inventory;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Extensions;

namespace EfratAgro.Adubos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AdubosDbContext>(
            options =>
            {
                options.UseMySQL(
                    connectionString);
            });

        services.AddScoped<
            IInventoryQueryService,
            InventoryQueryService>();

        services.AddScoped<
            ISaleService,
            SaleService>();

        services.AddScoped<
            ISalesQueryService,
            SalesQueryService>();

        return services;
    }
}
