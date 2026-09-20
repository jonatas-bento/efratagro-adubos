using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Application.Catalog;
using EfratAgro.Adubos.Application.Customers;
using EfratAgro.Adubos.Application.Deliveries;
using EfratAgro.Adubos.Application.Inventory;
using EfratAgro.Adubos.Application.Finance;
using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Infrastructure;
using EfratAgro.Adubos.Infrastructure.Identity;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddEfratAgroAuthentication(
    builder.Configuration);

var app =
    builder.Build();



if (app.Configuration.GetValue<bool>(
        "BootstrapAdmin:Enabled"))
{
    var adminEmail =
        app.Configuration[
            "BootstrapAdmin:Email"];

    var adminPassword =
        app.Configuration[
            "BootstrapAdmin:Password"];

    if (string.IsNullOrWhiteSpace(
            adminEmail) ||
        string.IsNullOrWhiteSpace(
            adminPassword))
    {
        throw new InvalidOperationException(
            "Bootstrap admin credentials were not configured.");
    }

    await using var scope =
        app.Services.CreateAsyncScope();

    var bootstrapper =
        scope.ServiceProvider
            .GetRequiredService<
                IIdentityBootstrapper>();

    await bootstrapper.BootstrapAsync(
        adminEmail,
        adminPassword);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapEfratAgroAuthenticationEndpoints();

app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "healthy",
            service = "EfratAgro.Adubos.Api",
            timestampUtc = DateTime.UtcNow
        }));

app.MapGet(
    "/api/inventory",
    async (
        string? q,
        IInventoryQueryService inventory,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await inventory.GetInventoryAsync(
                q,
                cancellationToken)))
    .WithTags("Inventory")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapGet(
    "/api/suppliers",
    async (
        ISupplierQueryService suppliers,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await suppliers.GetAllAsync(
                cancellationToken)))
    .WithTags("Catalog")
    .RequireAuthorization(AuthorizationPolicies.Management);

app.MapGet(
    "/api/sales",
    async (
        int? take,
        ISalesQueryService sales,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sales.GetRecentAsync(
                take ?? 20,
                cancellationToken)))
    .WithTags("Sales")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapPost(
    "/api/sales",
    async (
        CreateSaleRequest request,
        ISaleService sales,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result =
                await sales.CreateAsync(
                    request,
                    cancellationToken);

            return Results.Created(
                $"/api/sales/{result.SaleId}",
                result);
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .WithTags("Sales")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapGet(
    "/api/purchases",
    async (
        int? take,
        IPurchasesQueryService purchases,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await purchases.GetRecentAsync(
                take ?? 20,
                cancellationToken)))
    .WithTags("Purchases")
    .RequireAuthorization(AuthorizationPolicies.Management);

app.MapPost(
    "/api/purchases",
    async (
        CreatePurchaseRequest request,
        IPurchaseService purchases,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result =
                await purchases.CreateAsync(
                    request,
                    cancellationToken);

            return Results.Created(
                $"/api/purchases/{result.PurchaseId}",
                result);
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .WithTags("Purchases")
    .RequireAuthorization(AuthorizationPolicies.Management);

app.MapGet(
    "/api/deliveries",
    async (
        bool? pendingOnly,
        int? take,
        IDeliveryQueryService deliveries,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await deliveries.GetAsync(
                pendingOnly ?? true,
                take ?? 100,
                cancellationToken)))
    .WithTags("Deliveries")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapPatch(
    "/api/deliveries/{saleId:guid}/complete",
    async (
        Guid saleId,
        IDeliveryService deliveries,
        CancellationToken cancellationToken) =>
    {
        try
        {
            await deliveries.MarkDeliveredAsync(
                saleId,
                cancellationToken);

            return Results.NoContent();
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .WithTags("Deliveries")
    .RequireAuthorization(AuthorizationPolicies.Operational);


app.MapGet(
    "/api/customers",
    async (
        string? q,
        int? take,
        ICustomerQueryService customers,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await customers.GetAsync(
                q,
                take ?? 100,
                cancellationToken)))
    .WithTags("Customers")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapGet(
    "/api/customers/{customerId:guid}",
    async (
        Guid customerId,
        ICustomerQueryService customers,
        CancellationToken cancellationToken) =>
    {
        var customer =
            await customers.GetByIdAsync(
                customerId,
                cancellationToken);

        return customer is null
            ? Results.NotFound()
            : Results.Ok(customer);
    })
    .WithTags("Customers")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapPost(
    "/api/customers",
    async (
        CreateCustomerRequest request,
        ICustomerService customers,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var id =
                await customers.CreateAsync(
                    request,
                    cancellationToken);

            return Results.Created(
                $"/api/customers/{id}",
                new { id });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new { error = ex.Message });
        }
    })
    .WithTags("Customers")
    .RequireAuthorization(AuthorizationPolicies.Operational);

app.MapPut(
    "/api/customers/{customerId:guid}",
    async (
        Guid customerId,
        UpdateCustomerRequest request,
        ICustomerService customers,
        CancellationToken cancellationToken) =>
    {
        try
        {
            await customers.UpdateAsync(
                customerId,
                request,
                cancellationToken);

            return Results.NoContent();
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return Results.BadRequest(
                new { error = ex.Message });
        }
    })
    .WithTags("Customers")
    .RequireAuthorization(AuthorizationPolicies.Operational);


app.MapGet(
    "/api/receivables",
    async (
        bool? openOnly,
        int? take,
        IReceivableQueryService receivables,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await receivables.GetAsync(
                openOnly ?? true,
                take ?? 200,
                cancellationToken)))
    .WithTags("Finance")
    .RequireAuthorization(AuthorizationPolicies.Management);

app.MapPost(
    "/api/receivables/{receivableId:guid}/payments",
    async (
        Guid receivableId,
        RegisterPaymentRequest request,
        IPaymentService payments,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result =
                await payments.RegisterAsync(
                    receivableId,
                    request,
                    cancellationToken);

            return Results.Created(
                $"/api/receivables/{receivableId}/payments/{result.PaymentId}",
                result);
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .WithTags("Finance")
    .RequireAuthorization(AuthorizationPolicies.Management);


app.MapPost(
    "/api/payments/{paymentId:guid}/reversal",
    async (
        Guid paymentId,
        ReversePaymentRequest request,
        HttpContext httpContext,
        IPaymentService payments,
        CancellationToken cancellationToken) =>
    {
        var subject =
            httpContext.User
                .FindFirst("sub")
                ?.Value;

        if (!Guid.TryParse(
                subject,
                out var reversedByUserId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await payments.ReverseAsync(
                    paymentId,
                    reversedByUserId,
                    request,
                    cancellationToken);

            return Results.Ok(
                result);
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .WithTags("Finance")
    .RequireAuthorization(
        AuthorizationPolicies.Management);

app.Run();

public partial class Program;
