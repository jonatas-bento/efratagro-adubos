using EfratAgro.Adubos.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EfratAgro.Adubos.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddEfratAgroAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(
                configuration.GetSection(
                    JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Issuer),
                "JWT issuer must be configured.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Audience),
                "JWT audience must be configured.")
            .Validate(
                options =>
                    Encoding.UTF8.GetByteCount(
                        options.SigningKey) >= 32,
                "JWT signing key must contain at least 256 bits.")
            .Validate(
                options =>
                    options.AccessTokenMinutes
                        is >= 1 and <= 60,
                "JWT access token lifetime must be between 1 and 60 minutes.")
            .ValidateOnStart();

        services.AddSingleton<
            IJwtTokenService,
            JwtTokenService>();

        services
            .AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
            .AddJwtBearer(
                options =>
                {
                    var jwtOptions =
                        configuration
                            .GetSection(
                                JwtOptions.SectionName)
                            .Get<JwtOptions>()
                        ?? new JwtOptions();

                    var signingKey =
                        JwtSecurity.CreateSigningKey(
                            jwtOptions);

                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        new()
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwtOptions.Issuer,

                            ValidateAudience = true,
                            ValidAudience = jwtOptions.Audience,

                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = signingKey,

                            ValidAlgorithms =
                            [
                                SecurityAlgorithms.HmacSha256
                            ],

                            ValidateLifetime = true,

                            ClockSkew =
                                TimeSpan.FromSeconds(30),

                            NameClaimType =
                                JwtRegisteredClaimNames.UniqueName,

                            RoleClaimType =
                                JwtClaimNames.Role
                        };
                });

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    AuthorizationPolicies.Operational,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.RequireRole(
                            ApplicationRoles.Admin,
                            ApplicationRoles.Manager,
                            ApplicationRoles.Seller);
                    });

                options.AddPolicy(
                    AuthorizationPolicies.Management,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.RequireRole(
                            ApplicationRoles.Admin,
                            ApplicationRoles.Manager);
                    });
            });

        return services;
    }
}
