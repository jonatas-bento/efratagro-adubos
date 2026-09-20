using EfratAgro.Adubos.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class PaymentReversalConfiguration
    : IEntityTypeConfiguration<PaymentReversal>
{
    public void Configure(
        EntityTypeBuilder<PaymentReversal> builder)
    {
        builder.ToTable(
            "payment_reversals");

        builder.HasKey(
            x => x.Id);

        builder.Property(
                x => x.Id)
            .ValueGeneratedNever();

        builder.Property(
                x => x.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(
                x => x.ReversedAtUtc)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(
                x => x.ReversedByUserId)
            .IsRequired();

        builder.HasOne(
                x => x.Payment)
            .WithOne(
                x => x.Reversal)
            .HasForeignKey<PaymentReversal>(
                x => x.PaymentId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasIndex(
                x => x.PaymentId)
            .IsUnique();

        builder.HasIndex(
            x => x.ReversedAtUtc);

        builder.HasIndex(
            x => x.ReversedByUserId);
    }
}
