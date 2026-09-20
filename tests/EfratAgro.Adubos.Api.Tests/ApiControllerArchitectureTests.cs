using System.Reflection;
using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class ApiControllerArchitectureTests
{
    [Fact]
    public void ApiEndpoints_ShouldBeOrganizedAsControllers()
    {
        var controllerTypes =
            new[]
            {
                typeof(HealthController),
                typeof(AuthenticationController),
                typeof(InventoryController),
                typeof(SuppliersController),
                typeof(SalesController),
                typeof(PurchasesController),
                typeof(DeliveriesController),
                typeof(CustomersController),
                typeof(ReceivablesController),
                typeof(PaymentsController)
            };

        Assert.All(
            controllerTypes,
            controllerType =>
            {
                Assert.True(
                    typeof(ControllerBase)
                        .IsAssignableFrom(
                            controllerType));

                Assert.NotNull(
                    controllerType
                        .GetCustomAttribute<
                            ApiControllerAttribute>());
            });
    }

    [Fact]
    public void AuthenticationController_ShouldPreservePublicRoutes()
    {
        var controller =
            typeof(AuthenticationController);

        var route =
            controller.GetCustomAttribute<
                RouteAttribute>();

        Assert.NotNull(route);

        Assert.Equal(
            "api/auth",
            route.Template);

        var login =
            controller.GetMethod(
                nameof(
                    AuthenticationController.Login));

        Assert.NotNull(login);

        var loginRoute =
            login.GetCustomAttribute<
                HttpPostAttribute>();

        Assert.NotNull(loginRoute);

        Assert.Equal(
            "login",
            loginRoute.Template);

        Assert.NotNull(
            login.GetCustomAttribute<
                AllowAnonymousAttribute>());

        var me =
            controller.GetMethod(
                nameof(
                    AuthenticationController.Me));

        Assert.NotNull(me);

        var meRoute =
            me.GetCustomAttribute<
                HttpGetAttribute>();

        Assert.NotNull(meRoute);

        Assert.Equal(
            "me",
            meRoute.Template);

        Assert.NotNull(
            me.GetCustomAttribute<
                AuthorizeAttribute>());
    }

    [Fact]
    public void OperationalControllers_ShouldRequireOperationalPolicy()
    {
        AssertPolicy(
            typeof(InventoryController),
            AuthorizationPolicies.Operational);

        AssertPolicy(
            typeof(SalesController),
            AuthorizationPolicies.Operational);

        AssertPolicy(
            typeof(CustomersController),
            AuthorizationPolicies.Operational);

        AssertPolicy(
            typeof(DeliveriesController),
            AuthorizationPolicies.Operational);
    }

    [Fact]
    public void ManagementControllers_ShouldRequireManagementPolicy()
    {
        AssertPolicy(
            typeof(SuppliersController),
            AuthorizationPolicies.Management);

        AssertPolicy(
            typeof(PurchasesController),
            AuthorizationPolicies.Management);

        AssertPolicy(
            typeof(ReceivablesController),
            AuthorizationPolicies.Management);

        AssertPolicy(
            typeof(PaymentsController),
            AuthorizationPolicies.Management);
    }

    private static void AssertPolicy(
        Type controllerType,
        string expectedPolicy)
    {
        var authorize =
            controllerType
                .GetCustomAttribute<
                    AuthorizeAttribute>();

        Assert.NotNull(authorize);

        Assert.Equal(
            expectedPolicy,
            authorize.Policy);
    }
}
