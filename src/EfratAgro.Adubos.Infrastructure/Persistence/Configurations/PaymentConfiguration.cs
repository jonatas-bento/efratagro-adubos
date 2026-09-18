using EfratAgro.Adubos.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(
        EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaidAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(x => x.Method)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasOne(x => x.Receivable)
            .WithMany()
            .HasForeignKey(x => x.ReceivableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ReceivableId);
        builder.HasIndex(x => x.PaidAtUtc);
    }
}
