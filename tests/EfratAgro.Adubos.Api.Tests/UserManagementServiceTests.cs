using EfratAgro.Adubos.Application.Users;
using EfratAgro.Adubos.Infrastructure.Identity;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class UserManagementServiceTests
{
    [Fact]
    public async Task Create_ShouldPersistUserWithSingleRole()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var service =
            fixture.Service;

        var created =
            await service.CreateAsync(
                new CreateUserRequest(
                    "Vendedor Teste",
                    "seller@efratagro.local",
                    "ValidPass1",
                    ApplicationRoles.Seller));

        Assert.Equal(
            "Vendedor Teste",
            created.DisplayName);

        Assert.Equal(
            "seller@efratagro.local",
            created.Email);

        Assert.Equal(
            ApplicationRoles.Seller,
            created.Role);

        Assert.True(
            created.IsActive);

        var stored =
            await fixture.UserManager
                .FindByEmailAsync(
                    "seller@efratagro.local");

        Assert.NotNull(stored);

        var roles =
            await fixture.UserManager
                .GetRolesAsync(
                    stored);

        Assert.Equal(
            new[]
            {
                ApplicationRoles.Seller
            },
            roles);
    }

    [Fact]
    public async Task Update_ShouldRejectDemotingLastActiveAdmin()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var admin =
            await fixture.CreateUserAsync(
                "Administrador",
                "admin@efratagro.local",
                ApplicationRoles.Admin);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    fixture.Service
                        .UpdateAsync(
                            admin.Id,
                            new UpdateUserRequest(
                                "Administrador",
                                admin.Email!,
                                ApplicationRoles.Manager,
                                true)));

        Assert.Equal(
            "O último administrador ativo não pode ser desativado ou rebaixado.",
            exception.Message);

        Assert.True(
            await fixture.UserManager
                .IsInRoleAsync(
                    admin,
                    ApplicationRoles.Admin));
    }

    [Fact]
    public async Task Update_ShouldAllowDemotionWhenAnotherActiveAdminExists()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var firstAdmin =
            await fixture.CreateUserAsync(
                "Admin Um",
                "admin1@efratagro.local",
                ApplicationRoles.Admin);

        await fixture.CreateUserAsync(
            "Admin Dois",
            "admin2@efratagro.local",
            ApplicationRoles.Admin);

        var updated =
            await fixture.Service
                .UpdateAsync(
                    firstAdmin.Id,
                    new UpdateUserRequest(
                        "Admin Um",
                        firstAdmin.Email!,
                        ApplicationRoles.Manager,
                        true));

        Assert.Equal(
            ApplicationRoles.Manager,
            updated.Role);
    }

    [Fact]
    public async Task ResetPassword_ShouldReplaceCredentialAndUnlock()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var user =
            await fixture.CreateUserAsync(
                "Gerente",
                "manager@efratagro.local",
                ApplicationRoles.Manager);

        await fixture.UserManager
            .SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow
                    .AddMinutes(15));

        await fixture.UserManager
            .AccessFailedAsync(
                user);

        await fixture.Service
            .ResetPasswordAsync(
                user.Id,
                new ResetUserPasswordRequest(
                    "NewValidPass2"));

        var reloaded =
            await fixture.UserManager
                .FindByIdAsync(
                    user.Id.ToString());

        Assert.NotNull(
            reloaded);

        Assert.True(
            await fixture.UserManager
                .CheckPasswordAsync(
                    reloaded,
                    "NewValidPass2"));

        Assert.False(
            await fixture.UserManager
                .CheckPasswordAsync(
                    reloaded,
                    "ValidPass1"));

        Assert.False(
            await fixture.UserManager
                .IsLockedOutAsync(
                    reloaded));

        Assert.Equal(
            0,
            await fixture.UserManager
                .GetAccessFailedCountAsync(
                    reloaded));
    }

    [Fact]
    public async Task Unlock_ShouldClearLockoutAndFailures()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var user =
            await fixture.CreateUserAsync(
                "Vendedor",
                "locked@efratagro.local",
                ApplicationRoles.Seller);

        await fixture.UserManager
            .SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow
                    .AddMinutes(15));

        await fixture.UserManager
            .AccessFailedAsync(
                user);

        await fixture.Service
            .UnlockAsync(
                user.Id);

        var reloaded =
            await fixture.UserManager
                .FindByIdAsync(
                    user.Id.ToString());

        Assert.NotNull(reloaded);

        Assert.False(
            await fixture.UserManager
                .IsLockedOutAsync(
                    reloaded));

        Assert.Equal(
            0,
            await fixture.UserManager
                .GetAccessFailedCountAsync(
                    reloaded));
    }

    private sealed class Fixture
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly ServiceProvider
            _provider;

        private Fixture(
            SqliteConnection connection,
            ServiceProvider provider,
            UserManager<ApplicationUser> userManager)
        {
            _connection = connection;
            _provider = provider;
            UserManager = userManager;
            Service =
                new UserManagementService(
                    userManager);
        }

        public UserManager<ApplicationUser>
            UserManager { get; }

        public IUserManagementService
            Service { get; }

        public static async Task<Fixture>
            CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:");

            await connection.OpenAsync();

            var services =
                new ServiceCollection();

            services.AddLogging();

            services.AddDbContext<
                AdubosDbContext>(
                options =>
                    options.UseSqlite(
                        connection));

            services
                .AddIdentityCore<ApplicationUser>(
                    options =>
                    {
                        options.Password
                            .RequiredLength = 8;

                        options.Password
                            .RequireDigit = true;

                        options.Password
                            .RequireLowercase = true;

                        options.Password
                            .RequireUppercase = true;

                        options.Password
                            .RequireNonAlphanumeric =
                            false;

                        options.Lockout
                            .AllowedForNewUsers =
                            true;

                        options.Lockout
                            .MaxFailedAccessAttempts =
                            5;
                    })
                .AddRoles<
                    IdentityRole<Guid>>()
                .AddEntityFrameworkStores<
                    AdubosDbContext>();

            var provider =
                services.BuildServiceProvider();

            var scope =
                provider.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        AdubosDbContext>();

            await dbContext.Database
                .EnsureCreatedAsync();

            var roleManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        RoleManager<
                            IdentityRole<Guid>>>();

            foreach (
                var role
                in ApplicationRoles.All)
            {
                var result =
                    await roleManager
                        .CreateAsync(
                            new IdentityRole<Guid>(
                                role));

                Assert.True(
                    result.Succeeded);
            }

            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<
                            ApplicationUser>>();

            return new Fixture(
                connection,
                provider,
                userManager);
        }

        public async Task<ApplicationUser>
            CreateUserAsync(
                string name,
                string email,
                string role)
        {
            var user =
                new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    DisplayName = name,
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    IsActive = true
                };

            var created =
                await UserManager
                    .CreateAsync(
                        user,
                        "ValidPass1");

            Assert.True(
                created.Succeeded);

            var roleResult =
                await UserManager
                    .AddToRoleAsync(
                        user,
                        role);

            Assert.True(
                roleResult.Succeeded);

            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
