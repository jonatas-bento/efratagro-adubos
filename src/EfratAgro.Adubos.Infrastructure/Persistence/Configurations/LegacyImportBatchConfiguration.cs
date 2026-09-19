using EfratAgro.Adubos.Domain.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class LegacyImportBatchConfiguration
    : IEntityTypeConfiguration<LegacyImportBatch>
{
    public void Configure(
        EntityTypeBuilder<LegacyImportBatch> builder)
    {
        builder.ToTable("legacy_import_batches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SourceFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.SourceFileHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<int>()
            .HasDefaultValue(
                LegacyImportScope.WarehouseOpeningStock)
            .HasSentinel(
                LegacyImportScope.Unspecified)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(x => x.FinishedAtUtc)
            .HasColumnType("datetime(6)");

        builder.Property(x => x.ImportedRows)
            .IsRequired();

        builder.Property(x => x.ReviewRows)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SourceFileHash,
            x.Scope
        })
        .IsUnique();
    }
}
