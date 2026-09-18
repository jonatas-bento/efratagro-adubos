using EfratAgro.Adubos.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration
    : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(
        EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Bucket)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(80);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LegacyImportRow)
            .WithMany()
            .HasForeignKey(x => x.LegacyImportRowId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new
        {
            x.ProductId,
            x.WarehouseId,
            x.OccurredAtUtc
        });

        builder.HasIndex(x => x.LegacyImportRowId);
    }
}
