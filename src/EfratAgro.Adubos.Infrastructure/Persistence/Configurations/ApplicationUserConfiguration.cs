using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfratAgro.Adubos.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(
        EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(
                x => x.DisplayName)
            .HasMaxLength(120);

        builder.Property(
                x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
