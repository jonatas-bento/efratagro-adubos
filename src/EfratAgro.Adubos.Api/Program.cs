using EfratAgro.Adubos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "EfratAgro.Adubos.Api",
        timestampUtc = DateTime.UtcNow
    });
});

app.Run();

public partial class Program;
