using EfratAgro.Adubos.Application.Catalog;
using EfratAgro.Adubos.Application.Inventory;
using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app =
    builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
    .WithTags("Inventory");

app.MapGet(
    "/api/suppliers",
    async (
        ISupplierQueryService suppliers,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await suppliers.GetAllAsync(
                cancellationToken)))
    .WithTags("Catalog");

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
    .WithTags("Sales");

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
    .WithTags("Sales");

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
    .WithTags("Purchases");

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
    .WithTags("Purchases");

app.Run();

public partial class Program;
