using EfratAgro.Adubos.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class OperationalDataSettingsConfiguration
    : IEntityTypeConfiguration<
        OperationalDataSettings>
{
    public void Configure(
        EntityTypeBuilder<
            OperationalDataSettings> builder)
    {
        builder.ToTable(
            "operational_data_settings");

        builder.HasKey(
            x => x.Id);

        builder.Property(
                x => x.Id)
            .ValueGeneratedNever();

        builder.Property(
                x =>
                    x.OperationalTrustedFromUtc)
            .HasColumnType(
                "datetime(6)");

        builder.Property(
                x => x.UpdatedAtUtc)
            .HasColumnType(
                "datetime(6)")
            .IsRequired();
    }
}
