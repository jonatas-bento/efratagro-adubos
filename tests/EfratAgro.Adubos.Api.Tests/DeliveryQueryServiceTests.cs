using System.Text.Json;
using EfratAgro.Adubos.Infrastructure.Deliveries;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class DeliveryQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldNeverExposeLegacyNotTrackedSales()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AdubosDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AdubosDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var pendingCustomerId =
            Guid.NewGuid();

        var deliveredCustomerId =
            Guid.NewGuid();

        var legacyCustomerId =
            Guid.NewGuid();

        var pendingSaleId =
            Guid.NewGuid();

        var deliveredSaleId =
            Guid.NewGuid();

        var legacySaleId =
            Guid.NewGuid();

        var now =
            new DateTime(
                2026,
                9,
                19,
                12,
                0,
                0,
                DateTimeKind.Utc);

        await InsertCustomerAsync(
            dbContext,
            pendingCustomerId,
            "CLIENTE OPERACIONAL PENDENTE",
            now);

        await InsertCustomerAsync(
            dbContext,
            deliveredCustomerId,
            "CLIENTE OPERACIONAL ENTREGUE",
            now);

        await InsertCustomerAsync(
            dbContext,
            legacyCustomerId,
            "CLIENTE LEGADO NÃO RASTREADO",
            now);

        await InsertSaleAsync(
            dbContext,
            pendingSaleId,
            pendingCustomerId,
            now.AddHours(-3),
            deliveryMethod: 1,
            deliveryStatus: 1,
            origin: 1,
            deliveredAtUtc: null,
            createdAtUtc: now);

        await InsertSaleAsync(
            dbContext,
            deliveredSaleId,
            deliveredCustomerId,
            now.AddHours(-2),
            deliveryMethod: 1,
            deliveryStatus: 2,
            origin: 1,
            deliveredAtUtc: now.AddHours(-1),
            createdAtUtc: now);

        await InsertSaleAsync(
            dbContext,
            legacySaleId,
            legacyCustomerId,
            now.AddHours(-1),
            deliveryMethod: 0,
            deliveryStatus: 3,
            origin: 2,
            deliveredAtUtc: null,
            createdAtUtc: now);

        var service =
            new DeliveryQueryService(
                dbContext);

        var allTracked =
            await service.GetAsync(
                pendingOnly: false,
                take: 100);

        var pendingOnly =
            await service.GetAsync(
                pendingOnly: true,
                take: 100);

        Assert.Equal(
            2,
            allTracked.Count);

        Assert.Single(
            pendingOnly);

        var allTrackedJson =
            JsonSerializer.Serialize(
                allTracked);

        var pendingOnlyJson =
            JsonSerializer.Serialize(
                pendingOnly);

        Assert.Contains(
            "CLIENTE OPERACIONAL PENDENTE",
            allTrackedJson);

        Assert.Contains(
            "CLIENTE OPERACIONAL ENTREGUE",
            allTrackedJson);

        Assert.DoesNotContain(
            "CLIENTE LEGADO NÃO RASTREADO",
            allTrackedJson);

        Assert.Contains(
            "CLIENTE OPERACIONAL PENDENTE",
            pendingOnlyJson);

        Assert.DoesNotContain(
            "CLIENTE OPERACIONAL ENTREGUE",
            pendingOnlyJson);

        Assert.DoesNotContain(
            "CLIENTE LEGADO NÃO RASTREADO",
            pendingOnlyJson);
    }

    [Fact]
    public async Task GetAsync_ShouldExposeDeliveryDatesAsUtc()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AdubosDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AdubosDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var customerId =
            Guid.NewGuid();

        var saleId =
            Guid.NewGuid();

        var occurredAt =
            new DateTime(
                2026,
                9,
                18,
                22,
                50,
                18,
                DateTimeKind.Unspecified);

        var deliveredAt =
            new DateTime(
                2026,
                9,
                19,
                16,
                3,
                7,
                DateTimeKind.Unspecified);

        var createdAt =
            new DateTime(
                2026,
                9,
                19,
                16,
                0,
                0,
                DateTimeKind.Unspecified);

        await InsertCustomerAsync(
            dbContext,
            customerId,
            "CLIENTE TESTE UTC",
            createdAt);

        await InsertSaleAsync(
            dbContext,
            saleId,
            customerId,
            occurredAt,
            deliveryMethod: 1,
            deliveryStatus: 2,
            origin: 1,
            deliveredAtUtc: deliveredAt,
            createdAtUtc: createdAt);

        var service =
            new DeliveryQueryService(
                dbContext);

        var deliveries =
            await service.GetAsync(
                pendingOnly: false,
                take: 100);

        var delivery =
            Assert.Single(
                deliveries);

        Assert.Equal(
            DateTimeKind.Utc,
            delivery.OccurredAtUtc.Kind);

        Assert.NotNull(
            delivery.DeliveredAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            delivery.DeliveredAtUtc.Value.Kind);

        var occurredJson =
            JsonSerializer.Serialize(
                delivery.OccurredAtUtc);

        var deliveredJson =
            JsonSerializer.Serialize(
                delivery.DeliveredAtUtc.Value);

        Assert.EndsWith(
            "Z\"",
            occurredJson);

        Assert.EndsWith(
            "Z\"",
            deliveredJson);
    }

    private static Task<int> InsertCustomerAsync(
        AdubosDbContext dbContext,
        Guid id,
        string name,
        DateTime createdAtUtc)
    {
        var normalizedName =
            name.ToUpperInvariant();

        return dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO customers
                (
                    Id,
                    Name,
                    NormalizedName,
                    IsActive,
                    CreatedAtUtc
                )
                VALUES
                (
                    {id},
                    {name},
                    {normalizedName},
                    {true},
                    {createdAtUtc}
                );
                """);
    }

    private static Task<int> InsertSaleAsync(
        AdubosDbContext dbContext,
        Guid id,
        Guid customerId,
        DateTime occurredAtUtc,
        int deliveryMethod,
        int deliveryStatus,
        int origin,
        DateTime? deliveredAtUtc,
        DateTime createdAtUtc)
    {
        return dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO sales
                (
                    Id,
                    CustomerId,
                    OccurredAtUtc,
                    DeliveryMethod,
                    DeliveryStatus,
                    DeliveredAtUtc,
                    Origin,
                    CreatedAtUtc
                )
                VALUES
                (
                    {id},
                    {customerId},
                    {occurredAtUtc},
                    {deliveryMethod},
                    {deliveryStatus},
                    {deliveredAtUtc},
                    {origin},
                    {createdAtUtc}
                );
                """);
    }
}
