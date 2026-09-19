using EfratAgro.Adubos.Domain.Sales;

namespace EfratAgro.Adubos.Domain.Tests;

public sealed class LegacySaleTests
{
    [Fact]
    public void CreateLegacy_ShouldCreateNonTrackedLegacySale()
    {
        var customerId =
            Guid.NewGuid();

        var occurredAt =
            new DateTime(
                2026,
                9,
                2,
                0,
                0,
                0,
                DateTimeKind.Utc);

        var sale =
            Sale.CreateLegacy(
                customerId,
                occurredAt);

        Assert.NotEqual(
            Guid.Empty,
            sale.Id);

        Assert.Equal(
            customerId,
            sale.CustomerId);

        Assert.Equal(
            occurredAt,
            sale.OccurredAtUtc);

        Assert.Equal(
            SaleOrigin.Legacy,
            sale.Origin);

        Assert.Equal(
            DeliveryMethod.Unspecified,
            sale.DeliveryMethod);

        Assert.Equal(
            DeliveryStatus.NotTracked,
            sale.DeliveryStatus);

        Assert.Null(
            sale.DeliveredAtUtc);
    }

    [Fact]
    public void MarkDelivered_ShouldRejectNonTrackedLegacySale()
    {
        var sale =
            Sale.CreateLegacy(
                Guid.NewGuid(),
                DateTime.UtcNow);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    sale.MarkDelivered(
                        DateTime.UtcNow));

        Assert.Equal(
            "Delivery is not tracked for this sale.",
            exception.Message);
    }

    [Fact]
    public void OperationalSale_ShouldRemainPendingByDefault()
    {
        var sale =
            new Sale(
                Guid.NewGuid(),
                DateTime.UtcNow,
                DeliveryMethod.Delivery,
                SaleOrigin.Operational);

        Assert.Equal(
            SaleOrigin.Operational,
            sale.Origin);

        Assert.Equal(
            DeliveryStatus.Pending,
            sale.DeliveryStatus);

        Assert.Equal(
            DeliveryMethod.Delivery,
            sale.DeliveryMethod);
    }
}
