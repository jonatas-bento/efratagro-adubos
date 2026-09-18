using EfratAgro.Adubos.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration
    : IEntityTypeConfiguration<Sale>
{
    public void Configure(
        EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(x => x.DeliveryMethod)
            .HasConversion<int>()
            .HasDefaultValue(
                DeliveryMethod.Unspecified)
            .IsRequired();

        builder.Property(x => x.DeliveryStatus)
            .HasConversion<int>()
            .HasDefaultValue(
                DeliveryStatus.Pending)
            .HasSentinel(
                DeliveryStatus.Unspecified)
            .IsRequired();

        builder.Property(x => x.DeliveredAtUtc)
            .HasColumnType("datetime(6)");

        builder.Property(x => x.Origin)
            .HasConversion<int>()
            .HasDefaultValue(
                SaleOrigin.Operational)
            .HasSentinel(
                SaleOrigin.Unspecified)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.DeliveryStatus);
    }
}
