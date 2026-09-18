using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Legacy;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Persistence;

public sealed class AdubosDbContext : DbContext
{
    public AdubosDbContext(
        DbContextOptions<AdubosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supplier> Suppliers =>
        Set<Supplier>();

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<Warehouse> Warehouses =>
        Set<Warehouse>();

    public DbSet<InventoryMovement> InventoryMovements =>
        Set<InventoryMovement>();

    public DbSet<LegacyImportBatch> LegacyImportBatches =>
        Set<LegacyImportBatch>();

    public DbSet<LegacyImportRow> LegacyImportRows =>
        Set<LegacyImportRow>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AdubosDbContext).Assembly);
    }
}
