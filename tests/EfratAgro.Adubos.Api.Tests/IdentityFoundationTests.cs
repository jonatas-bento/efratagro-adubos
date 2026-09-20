using System.Reflection;
using EfratAgro.Adubos.Infrastructure;
using EfratAgro.Adubos.Infrastructure.Identity;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class IdentityFoundationTests
{
    private const string DummyConnectionString =
        "Server=localhost;Database=identity_foundation_tests;Uid=not_used;Pwd=not_used;";

    private static ServiceProvider BuildServiceProvider()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            DummyConnectionString
                    })
                .Build();

        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void ApplicationUser_ShouldUseGuidAsKey()
    {
        Assert.Equal(
            typeof(IdentityUser<Guid>),
            typeof(ApplicationUser).BaseType);

        Assert.Equal(
            typeof(Guid),
            typeof(ApplicationUser)
                .GetProperty(nameof(IdentityUser<Guid>.Id))!
                .PropertyType);
    }

    [Fact]
    public void AdubosDbContext_ShouldDeriveFromIdentityDbContextOfApplicationUserAndGuid()
    {
        Assert.Equal(
            typeof(IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>),
            typeof(AdubosDbContext).BaseType);
    }

    [Fact]
    public void AdminRole_ShouldHaveStableName()
        => Assert.Equal(
            "Admin",
            ApplicationRoles.Admin);

    [Fact]
    public void ManagerRole_ShouldHaveStableName()
        => Assert.Equal(
            "Manager",
            ApplicationRoles.Manager);

    [Fact]
    public void SellerRole_ShouldHaveStableName()
        => Assert.Equal(
            "Seller",
            ApplicationRoles.Seller);

    [Fact]
    public void OfficialRoles_ShouldContainExactlyTheThreeUniqueRoles()
    {
        Assert.Equal(
            3,
            ApplicationRoles.All.Distinct().Count());

        Assert.Equal(
            new[] { "Admin", "Manager", "Seller" },
            ApplicationRoles.All.ToArray());
    }

    [Fact]
    public void OfficialRoles_ShouldBeDeclaredAsConstStrings()
    {
        var constFieldNames =
            typeof(ApplicationRoles)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => field.Name)
                .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(ApplicationRoles.Admin),
                nameof(ApplicationRoles.Manager),
                nameof(ApplicationRoles.Seller)
            },
            constFieldNames);
    }

    [Fact]
    public void Identity_ShouldBeRegisteredInServiceContainer()
    {
        using var provider =
            BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        Assert.NotNull(
            scope.ServiceProvider.GetService<
                UserManager<ApplicationUser>>());

        Assert.NotNull(
            scope.ServiceProvider.GetService<
                RoleManager<IdentityRole<Guid>>>());
    }

    [Fact]
    public void UserManager_ShouldBeResolvableFromContainer()
    {
        using var provider =
            BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var userManager =
            scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>>();

        Assert.NotNull(
            userManager);

        Assert.NotNull(
            scope.ServiceProvider.GetService<
                IUserStore<ApplicationUser>>());
    }

    [Fact]
    public void RoleManager_ShouldBeResolvableFromContainer()
    {
        using var provider =
            BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<
                RoleManager<IdentityRole<Guid>>>();

        Assert.NotNull(
            roleManager);

        Assert.NotNull(
            scope.ServiceProvider.GetService<
                IRoleStore<IdentityRole<Guid>>>());
    }

    [Fact]
    public void IdentityStores_ShouldBeRoleAwareEntityFrameworkStores()
    {
        using var provider =
            BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var userManager =
            scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<
                RoleManager<IdentityRole<Guid>>>();

        var userStore =
            scope.ServiceProvider.GetRequiredService<
                IUserStore<ApplicationUser>>();

        Assert.IsAssignableFrom<IUserRoleStore<ApplicationUser>>(
            userStore);

        Assert.True(
            userManager.SupportsUserEmail);

        Assert.True(
            userManager.SupportsUserLockout);

        Assert.True(
            roleManager.SupportsRoleClaims);
    }

    [Fact]
    public void PasswordOptions_ShouldMatchAuth001Rules()
    {
        using var provider =
            BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                IOptions<IdentityOptions>>()
                .Value;

        Assert.Equal(
            8,
            options.Password.RequiredLength);

        Assert.True(
            options.Password.RequireDigit);

        Assert.True(
            options.Password.RequireLowercase);

        Assert.True(
            options.Password.RequireUppercase);

        Assert.False(
            options.Password.RequireNonAlphanumeric);
    }

    [Fact]
    public void LockoutOptions_ShouldMatchAuth001Rules()
    {
        using var provider =
            BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                IOptions<IdentityOptions>>()
                .Value;

        Assert.True(
            options.Lockout.AllowedForNewUsers);

        Assert.Equal(
            5,
            options.Lockout.MaxFailedAccessAttempts);

        Assert.Equal(
            TimeSpan.FromMinutes(15),
            options.Lockout.DefaultLockoutTimeSpan);
    }

    [Fact]
    public void UserOptions_ShouldRequireUniqueEmail()
    {
        using var provider =
            BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                IOptions<IdentityOptions>>()
                .Value;

        Assert.True(
            options.User.RequireUniqueEmail);
    }

    [Fact]
    public async Task AdubosDbContext_WithSqlite_ShouldCreateSchemaIncludingIdentityTables()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AdubosDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AdubosDbContext(options);

        await dbContext.Database
            .EnsureCreatedAsync();

        Assert.False(
            await dbContext.Users.AnyAsync());

        Assert.False(
            await dbContext.Roles.AnyAsync());
    }

    [Fact]
    public async Task RoleManager_WithProductionRegistration_ShouldCreateOfficialRolesOnSqlite()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<AdubosDbContext>(
            options => options.UseSqlite(connection));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AdubosDbContext>();

        using var provider =
            services.BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<
                AdubosDbContext>();

        await dbContext.Database
            .EnsureCreatedAsync();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<
                RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in ApplicationRoles.All)
        {
            var result =
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>(roleName));

            Assert.True(
                result.Succeeded,
                string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description)));
        }

        var storedRoleNames =
            await dbContext.Roles
                .Select(role => role.Name)
                .OrderBy(name => name)
                .ToListAsync();

        Assert.Equal(
            new[] { "Admin", "Manager", "Seller" },
            storedRoleNames);
    }
}