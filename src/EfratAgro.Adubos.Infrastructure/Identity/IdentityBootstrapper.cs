using Microsoft.AspNetCore.Identity;

namespace EfratAgro.Adubos.Infrastructure.Identity;

public sealed class IdentityBootstrapper(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
    : IIdentityBootstrapper
{
    public async Task BootstrapAsync(
        string adminEmail,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new ArgumentException(
                "Bootstrap admin email is required.",
                nameof(adminEmail));
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new ArgumentException(
                "Bootstrap admin password is required.",
                nameof(adminPassword));
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var roleName in ApplicationRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role =
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName
                };

            var roleResult =
                await roleManager.CreateAsync(
                    role);

            EnsureSucceeded(
                roleResult,
                $"creating role '{roleName}'");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail =
            adminEmail.Trim();

        var admin =
            await userManager.FindByEmailAsync(
                normalizedEmail);

        if (admin is null)
        {
            admin =
                new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    UserName = normalizedEmail,
                    EmailConfirmed = true
                };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    adminPassword);

            EnsureSucceeded(
                createResult,
                "creating bootstrap admin");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!await userManager.IsInRoleAsync(
                admin,
                ApplicationRoles.Admin))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    ApplicationRoles.Admin);

            EnsureSucceeded(
                roleResult,
                "assigning bootstrap admin role");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors =
            string.Join(
                "; ",
                result.Errors.Select(
                    error =>
                        $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"Identity operation failed while {operation}: {errors}");
    }
}
