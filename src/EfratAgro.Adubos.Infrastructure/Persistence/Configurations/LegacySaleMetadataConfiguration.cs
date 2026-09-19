using EfratAgro.Adubos.Domain.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class LegacySaleMetadataConfiguration
    : IEntityTypeConfiguration<LegacySaleMetadata>
{
    public void Configure(
        EntityTypeBuilder<LegacySaleMetadata> builder)
    {
        builder.ToTable(
            "legacy_sale_metadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TransactionKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.DocumentNumber)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.TransactionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.ImportedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ImportBatch)
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.ImportBatchId,
            x.TransactionKey
        })
        .IsUnique();

        builder.HasIndex(x => x.DocumentNumber);

        builder.HasIndex(x => x.TransactionDate);
    }
}
