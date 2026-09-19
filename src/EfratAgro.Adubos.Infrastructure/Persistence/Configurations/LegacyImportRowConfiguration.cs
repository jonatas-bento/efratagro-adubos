using EfratAgro.Adubos.Domain.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class LegacyImportRowConfiguration
    : IEntityTypeConfiguration<LegacyImportRow>
{
    public void Configure(
        EntityTypeBuilder<LegacyImportRow> builder)
    {
        builder.ToTable("legacy_import_rows");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SheetName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RowNumber)
            .IsRequired();

        builder.Property(x => x.SourceCell)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RawData)
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(x => x.RequiresReview)
            .IsRequired();

        builder.Property(x => x.ReviewReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SaleItem)
            .WithMany()
            .HasForeignKey(x => x.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.BatchId,
            x.SheetName,
            x.SourceCell
        })
        .IsUnique();

        builder.HasIndex(x => x.SaleItemId)
            .IsUnique();
    }
}
