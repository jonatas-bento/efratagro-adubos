using EfratAgro.Adubos.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class InventoryReservationConfiguration
    : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(
        EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable(
            "inventory_reservations");

        builder.HasKey(
            x => x.Id);

        builder.Property(
                x => x.Id)
            .ValueGeneratedNever();

        builder.Property(
                x => x.Quantity)
            .HasPrecision(
                18,
                3)
            .IsRequired();

        builder.Property(
                x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                x => x.ReservedAtUtc)
            .HasColumnType(
                "datetime(6)")
            .IsRequired();

        builder.Property(
                x => x.FulfilledAtUtc)
            .HasColumnType(
                "datetime(6)");

        builder.HasOne(
                x => x.Sale)
            .WithMany()
            .HasForeignKey(
                x => x.SaleId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne(
                x => x.Product)
            .WithMany()
            .HasForeignKey(
                x => x.ProductId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne(
                x => x.Warehouse)
            .WithMany()
            .HasForeignKey(
                x => x.WarehouseId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasIndex(
                x => new
                {
                    x.SaleId,
                    x.ProductId
                })
            .IsUnique();

        builder.HasIndex(
            x => new
            {
                x.ProductId,
                x.Status
            });

        builder.HasIndex(
            x => x.WarehouseId);
    }
}
