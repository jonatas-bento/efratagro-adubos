using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Customers;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Legacy;
using EfratAgro.Adubos.Domain.Purchases;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Persistence;

public sealed class AdubosDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AdubosDbContext(
        DbContextOptions<AdubosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<InventoryMovement> InventoryMovements =>
        Set<InventoryMovement>();

    public DbSet<LegacyImportBatch> LegacyImportBatches =>
        Set<LegacyImportBatch>();

    public DbSet<LegacyImportRow> LegacyImportRows =>
        Set<LegacyImportRow>();

    public DbSet<LegacySaleMetadata> LegacySaleMetadataEntries =>
        Set<LegacySaleMetadata>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<Receivable> Receivables =>
        Set<Receivable>();

    public DbSet<Payment> Payments =>
        Set<Payment>();

    public DbSet<PaymentReversal> PaymentReversals =>
        Set<PaymentReversal>();


    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseItem> PurchaseItems =>
        Set<PurchaseItem>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AdubosDbContext).Assembly);
    }
}
