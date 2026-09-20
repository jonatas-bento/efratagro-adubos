using System.IdentityModel.Tokens.Jwt;
using System.Text;
using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class JwtAuthenticationFoundationTests
{
    private const string TestSigningKey =
        "0123456789abcdef0123456789abcdef0123456789abcdef";

    private static IConfiguration BuildConfiguration(
        string? signingKey = TestSigningKey)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] =
                        "EfratAgro.Tests",

                    ["Jwt:Audience"] =
                        "EfratAgro.Tests.Web",

                    ["Jwt:SigningKey"] =
                        signingKey,

                    ["Jwt:AccessTokenMinutes"] =
                        "15"
                })
            .Build();
    }

    private static ServiceProvider BuildProvider(
        IConfiguration configuration)
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddEfratAgroAuthentication(
            configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void JwtOptions_ShouldBindExpectedValues()
    {
        using var provider =
            BuildProvider(
                BuildConfiguration());

        var options =
            provider
                .GetRequiredService<
                    IOptions<JwtOptions>>()
                .Value;

        Assert.Equal(
            "EfratAgro.Tests",
            options.Issuer);

        Assert.Equal(
            "EfratAgro.Tests.Web",
            options.Audience);

        Assert.Equal(
            15,
            options.AccessTokenMinutes);

        Assert.Equal(
            TestSigningKey,
            options.SigningKey);
    }

    [Fact]
    public void JwtBearer_ShouldUseExplicitClaimTypesAndStrictValidation()
    {
        using var provider =
            BuildProvider(
                BuildConfiguration());

        var options =
            provider
                .GetRequiredService<
                    IOptionsMonitor<JwtBearerOptions>>()
                .Get(
                    JwtBearerDefaults.AuthenticationScheme);

        Assert.False(
            options.MapInboundClaims);

        Assert.True(
            options.TokenValidationParameters.ValidateIssuer);

        Assert.True(
            options.TokenValidationParameters.ValidateAudience);

        Assert.True(
            options.TokenValidationParameters.ValidateIssuerSigningKey);

        Assert.True(
            options.TokenValidationParameters.ValidateLifetime);

        Assert.Contains(
            SecurityAlgorithms.HmacSha256,
            options.TokenValidationParameters.ValidAlgorithms!);

        Assert.Equal(
            JwtRegisteredClaimNames.UniqueName,
            options.TokenValidationParameters.NameClaimType);

        Assert.Equal(
            JwtClaimNames.Role,
            options.TokenValidationParameters.RoleClaimType);

        Assert.Equal(
            TimeSpan.FromSeconds(30),
            options.TokenValidationParameters.ClockSkew);
    }

    [Fact]
    public void TokenService_ShouldGenerateValidSignedTokenWithIdentityClaims()
    {
        using var provider =
            BuildProvider(
                BuildConfiguration());

        var service =
            provider.GetRequiredService<
                IJwtTokenService>();

        var user =
            new ApplicationUser
            {
                Id =
                    Guid.Parse(
                        "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),

                Email =
                    "manager@efratagro.local",

                UserName =
                    "manager@efratagro.local"
            };

        var before =
            DateTime.UtcNow;

        var result =
            service.CreateToken(
                user,
                new[]
                {
                    ApplicationRoles.Manager,
                    ApplicationRoles.Seller
                });

        var after =
            DateTime.UtcNow;

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.InRange(
            result.ExpiresAtUtc,
            before.AddMinutes(15),
            after.AddMinutes(15));

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidIssuer =
                    "EfratAgro.Tests",

                ValidateAudience = true,

                ValidAudience =
                    "EfratAgro.Tests.Web",

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            TestSigningKey)),

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromSeconds(30),

                NameClaimType =
                    JwtRegisteredClaimNames.UniqueName,

                RoleClaimType =
                    JwtClaimNames.Role
            };

        var handler =
            new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

        var principal =
            handler.ValidateToken(
                result.AccessToken,
                validationParameters,
                out var validatedToken);

        Assert.NotNull(
            validatedToken);

        Assert.Equal(
            user.Id.ToString(),
            principal
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)!
                .Value);

        Assert.Equal(
            user.Email,
            principal
                .FindFirst(
                    JwtRegisteredClaimNames.Email)!
                .Value);

        Assert.Equal(
            user.UserName,
            principal
                .FindFirst(
                    JwtRegisteredClaimNames.UniqueName)!
                .Value);

        var roles =
            principal
                .FindAll(
                    JwtClaimNames.Role)
                .Select(
                    claim => claim.Value)
                .OrderBy(
                    role => role)
                .ToArray();

        Assert.Equal(
            new[]
            {
                ApplicationRoles.Manager,
                ApplicationRoles.Seller
            },
            roles);

        Assert.True(
            principal.IsInRole(
                ApplicationRoles.Manager));

        Assert.True(
            principal.IsInRole(
                ApplicationRoles.Seller));
    }

    [Fact]
    public void JwtOptions_ShouldRejectMissingSigningKey()
    {
        using var provider =
            BuildProvider(
                BuildConfiguration(
                    string.Empty));

        var exception =
            Assert.Throws<
                OptionsValidationException>(
                () =>
                    provider
                        .GetRequiredService<
                            IOptions<JwtOptions>>()
                        .Value);

        Assert.Contains(
            exception.Failures,
            failure =>
                failure.Contains(
                    "256 bits",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void JwtConfiguration_ShouldRejectSigningKeyShorterThan256Bits()
    {
        using var provider =
            BuildProvider(
                BuildConfiguration(
                    "too-short"));

        var exception =
            Assert.Throws<
                OptionsValidationException>(
                () =>
                    provider.GetRequiredService<
                        IJwtTokenService>());

        Assert.Contains(
            exception.Failures,
            failure =>
                failure.Contains(
                    "256 bits",
                    StringComparison.OrdinalIgnoreCase));
    }
}
