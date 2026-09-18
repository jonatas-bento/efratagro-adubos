using EfratAgro.Adubos.Application.Inventory;
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
    {
        var result =
            await inventory.GetInventoryAsync(
                q,
                cancellationToken);

        return Results.Ok(result);
    })
    .WithName("GetInventory")
    .WithTags("Inventory");

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
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    })
    .WithName("CreateSale")
    .WithTags("Sales");

app.Run();

public partial class Program;
