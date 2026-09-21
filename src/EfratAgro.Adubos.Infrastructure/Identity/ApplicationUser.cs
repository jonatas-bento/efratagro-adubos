using Microsoft.AspNetCore.Identity;

namespace EfratAgro.Adubos.Infrastructure.Identity;

public sealed class ApplicationUser
    : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        LockoutEnabled = true;
        IsActive = true;
    }

    public string? DisplayName { get; set; }

    public bool IsActive { get; set; }
}
