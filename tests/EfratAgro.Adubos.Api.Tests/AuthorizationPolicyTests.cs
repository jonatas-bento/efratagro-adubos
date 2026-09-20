using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public async Task OperationalPolicy_ShouldAllowAllApplicationRoles()
    {
        await AssertPolicyRolesAsync(
            AuthorizationPolicies.Operational,
            ApplicationRoles.Admin,
            ApplicationRoles.Manager,
            ApplicationRoles.Seller);
    }

    [Fact]
    public async Task ManagementPolicy_ShouldAllowOnlyAdminAndManager()
    {
        await AssertPolicyRolesAsync(
            AuthorizationPolicies.Management,
            ApplicationRoles.Admin,
            ApplicationRoles.Manager);
    }

    private static async Task AssertPolicyRolesAsync(
        string policyName,
        params string[] expectedRoles)
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] =
                            "EfratAgro.Tests",

                        ["Jwt:Audience"] =
                            "EfratAgro.Tests.Web",

                        ["Jwt:SigningKey"] =
                            "0123456789abcdef0123456789abcdef0123456789abcdef",

                        ["Jwt:AccessTokenMinutes"] =
                            "15"
                    })
                .Build();

        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddEfratAgroAuthentication(
            configuration);

        await using var provider =
            services.BuildServiceProvider();

        var policyProvider =
            provider.GetRequiredService<
                IAuthorizationPolicyProvider>();

        var policy =
            await policyProvider.GetPolicyAsync(
                policyName);

        Assert.NotNull(policy);

        Assert.Contains(
            policy.Requirements,
            requirement =>
                requirement is
                    DenyAnonymousAuthorizationRequirement);

        var roleRequirement =
            Assert.Single(
                policy.Requirements
                    .OfType<
                        RolesAuthorizationRequirement>());

        Assert.Equal(
            expectedRoles
                .OrderBy(
                    role => role,
                    StringComparer.Ordinal),
            roleRequirement
                .AllowedRoles
                .OrderBy(
                    role => role,
                    StringComparer.Ordinal));
    }
}
