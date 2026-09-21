using System.Reflection;
using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class DataQualityAuthorizationTests
{
    [Fact]
    public void Controller_ShouldRequireOperationalPolicy()
    {
        var authorize =
            typeof(DataQualityController)
                .GetCustomAttributes<
                    AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            AuthorizationPolicies.Operational,
            authorize.Policy);
    }

    [Fact]
    public void TrustedFromUpdate_ShouldRequireManagementPolicy()
    {
        var method =
            typeof(DataQualityController)
                .GetMethod(
                    nameof(
                        DataQualityController
                            .SetOperationalTrustedFrom));

        Assert.NotNull(method);

        var authorize =
            method!
                .GetCustomAttributes<
                    AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            AuthorizationPolicies.Management,
            authorize.Policy);
    }
}
