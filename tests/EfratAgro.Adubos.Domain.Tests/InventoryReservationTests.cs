using EfratAgro.Adubos.Domain.Inventory;

namespace EfratAgro.Adubos.Domain.Tests;

public sealed class InventoryReservationTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveReservation()
    {
        var reservedAtUtc =
            new DateTime(
                2026,
                9,
                21,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var reservation =
            new InventoryReservation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                12.5m,
                reservedAtUtc);

        Assert.Equal(
            12.5m,
            reservation.Quantity);

        Assert.Equal(
            InventoryReservationStatus.Active,
            reservation.Status);

        Assert.Equal(
            reservedAtUtc,
            reservation.ReservedAtUtc);

        Assert.Null(
            reservation.FulfilledAtUtc);
    }

    [Fact]
    public void Fulfill_ShouldBeIdempotent()
    {
        var reservation =
            new InventoryReservation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                10m,
                DateTime.UtcNow);

        var fulfilledAtUtc =
            new DateTime(
                2026,
                9,
                21,
                13,
                0,
                0,
                DateTimeKind.Utc);

        reservation.Fulfill(
            fulfilledAtUtc);

        reservation.Fulfill(
            fulfilledAtUtc.AddHours(1));

        Assert.Equal(
            InventoryReservationStatus.Fulfilled,
            reservation.Status);

        Assert.Equal(
            fulfilledAtUtc,
            reservation.FulfilledAtUtc);
    }
}
