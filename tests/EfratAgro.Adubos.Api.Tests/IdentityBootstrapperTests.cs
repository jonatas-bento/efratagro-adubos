using EfratAgro.Adubos.Infrastructure.Identity;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class IdentityBootstrapperTests
{
    [Fact]
    public async Task Bootstrap_ShouldCreateOfficialRolesAndAdmin()
    {
        await using var fixture =
            await IdentityFixture.CreateAsync();

        using var scope =
            fixture.Services.CreateScope();

        var bootstrapper =
            scope.ServiceProvider
                .GetRequiredService<
                    IIdentityBootstrapper>();

        await bootstrapper.BootstrapAsync(
            "admin@efratagro.local",
            "ValidPass1");

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    AdubosDbContext>();

        var roleNames =
            await dbContext.Roles
                .Select(role => role.Name)
                .OrderBy(name => name)
                .ToListAsync();

        Assert.Equal(
            new[]
            {
                "Admin",
                "Manager",
                "Seller"
            },
            roleNames);

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var admin =
            await userManager.FindByEmailAsync(
                "admin@efratagro.local");

        Assert.NotNull(
            admin);

        Assert.Equal(
            "admin@efratagro.local",
            admin.UserName);

        Assert.True(
            admin.EmailConfirmed);

        Assert.True(
            await userManager.IsInRoleAsync(
                admin,
                ApplicationRoles.Admin));
    }

    [Fact]
    public async Task Bootstrap_ShouldBeIdempotent_AndMustNotResetExistingPassword()
    {
        await using var fixture =
            await IdentityFixture.CreateAsync();

        using var scope =
            fixture.Services.CreateScope();

        var bootstrapper =
            scope.ServiceProvider
                .GetRequiredService<
                    IIdentityBootstrapper>();

        await bootstrapper.BootstrapAsync(
            "admin@efratagro.local",
            "FirstPass1");

        await bootstrapper.BootstrapAsync(
            "admin@efratagro.local",
            "SecondPass2");

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    AdubosDbContext>();

        Assert.Equal(
            3,
            await dbContext.Roles.CountAsync());

        Assert.Equal(
            1,
            await dbContext.Users.CountAsync());

        Assert.Equal(
            1,
            await dbContext.UserRoles.CountAsync());

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var admin =
            await userManager.FindByEmailAsync(
                "admin@efratagro.local");

        Assert.NotNull(
            admin);

        Assert.True(
            await userManager.CheckPasswordAsync(
                admin,
                "FirstPass1"));

        Assert.False(
            await userManager.CheckPasswordAsync(
                admin,
                "SecondPass2"));

        Assert.True(
            await userManager.IsInRoleAsync(
                admin,
                ApplicationRoles.Admin));
    }

    [Theory]
    [InlineData("", "ValidPass1")]
    [InlineData("   ", "ValidPass1")]
    [InlineData("admin@efratagro.local", "")]
    [InlineData("admin@efratagro.local", "   ")]
    public async Task Bootstrap_ShouldRejectMissingCredentials(
        string email,
        string password)
    {
        await using var fixture =
            await IdentityFixture.CreateAsync();

        using var scope =
            fixture.Services.CreateScope();

        var bootstrapper =
            scope.ServiceProvider
                .GetRequiredService<
                    IIdentityBootstrapper>();

        await Assert.ThrowsAsync<
            ArgumentException>(
            () =>
                bootstrapper.BootstrapAsync(
                    email,
                    password));
    }

    private sealed class IdentityFixture
        : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private IdentityFixture(
            SqliteConnection connection,
            ServiceProvider services)
        {
            _connection = connection;
            Services = services;
        }

        public ServiceProvider Services { get; }

        public static async Task<IdentityFixture> CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:");

            await connection.OpenAsync();

            var services =
                new ServiceCollection();

            services.AddLogging();

            services.AddDbContext<AdubosDbContext>(
                options =>
                    options.UseSqlite(
                        connection));

            services
                .AddIdentityCore<ApplicationUser>(
                    options =>
                    {
                        options.Password.RequiredLength = 8;
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = false;

                        options.Lockout.AllowedForNewUsers = true;
                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.DefaultLockoutTimeSpan =
                            TimeSpan.FromMinutes(15);

                        options.User.RequireUniqueEmail = true;
                    })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AdubosDbContext>();

            services.AddScoped<
                IIdentityBootstrapper,
                IdentityBootstrapper>();

            var provider =
                services.BuildServiceProvider();

            using (var scope =
                   provider.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<
                            AdubosDbContext>();

                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            return new IdentityFixture(
                connection,
                provider);
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
