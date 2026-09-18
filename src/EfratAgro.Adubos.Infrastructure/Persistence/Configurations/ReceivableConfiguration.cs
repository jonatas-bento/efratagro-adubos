using EfratAgro.Adubos.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class ReceivableConfiguration
    : IEntityTypeConfiguration<Receivable>
{
    public void Configure(
        EntityTypeBuilder<Receivable> builder)
    {
        builder.ToTable("receivables");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.InstallmentNumber)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(x => x.OriginalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleId);

        builder.HasIndex(
            x => new
            {
                x.SaleId,
                x.InstallmentNumber
            })
            .IsUnique();

        builder.HasIndex(x => x.DueDate);
    }
}
