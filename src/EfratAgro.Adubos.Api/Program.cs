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

builder.Services.AddControllers();

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

app.MapControllers();

app.Run();

public partial class Program;
