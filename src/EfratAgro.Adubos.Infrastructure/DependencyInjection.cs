using EfratAgro.Adubos.Application.Users;
using EfratAgro.Adubos.Application.Catalog;
using EfratAgro.Adubos.Application.Customers;
using EfratAgro.Adubos.Application.Deliveries;
using EfratAgro.Adubos.Application.Inventory;
using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Infrastructure.Catalog;
using EfratAgro.Adubos.Infrastructure.Customers;
using EfratAgro.Adubos.Infrastructure.Deliveries;
using EfratAgro.Adubos.Infrastructure.Inventory;
using EfratAgro.Adubos.Infrastructure.Finance;
using EfratAgro.Adubos.Infrastructure.Identity;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Purchases;
using EfratAgro.Adubos.Infrastructure.Sales;
using Microsoft.AspNetCore.Identity;
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

        services
            .AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);

                    options.User.RequireUniqueEmail = true;
                })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AdubosDbContext>();

        services.AddScoped<
            IIdentityBootstrapper,
            IdentityBootstrapper>();

        services.AddScoped<
            IInventoryQueryService,
            InventoryQueryService>();

        services.AddScoped<
            ISaleService,
            SaleService>();

        services.AddScoped<
            ISalesQueryService,
            SalesQueryService>();

        services.AddScoped<
            ISupplierQueryService,
            SupplierQueryService>();

        services.AddScoped<
            IPurchaseService,
            PurchaseService>();

        services.AddScoped<
            IPurchasesQueryService,
            PurchasesQueryService>();

        services.AddScoped<
            IDeliveryQueryService,
            DeliveryQueryService>();

        services.AddScoped<
            IDeliveryService,
            DeliveryService>();

        services.AddScoped<
            ICustomerQueryService,
            CustomerQueryService>();

        services.AddScoped<
            ICustomerService,
            CustomerService>();

        services.AddScoped<
            IReceivableQueryService,
            ReceivableQueryService>();

        services.AddScoped<
            IPaymentQueryService,
            PaymentQueryService>();

        services.AddScoped<
            IPaymentService,
            PaymentService>();

        services.AddScoped<
            IUserManagementService,
            UserManagementService>();

        return services;
    }
}
