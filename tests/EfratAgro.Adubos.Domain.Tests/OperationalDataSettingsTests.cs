using EfratAgro.Adubos.Domain.Operations;

namespace EfratAgro.Adubos.Domain.Tests;

public sealed class OperationalDataSettingsTests
{
    [Fact]
    public void Create_ShouldUseSingletonIdentity()
    {
        var now =
            new DateTime(
                2026,
                9,
                21,
                15,
                0,
                0,
                DateTimeKind.Utc);

        var settings =
            OperationalDataSettings.Create(
                now);

        Assert.Equal(
            OperationalDataSettings.SingletonId,
            settings.Id);

        Assert.Null(
            settings.OperationalTrustedFromUtc);

        Assert.Equal(
            now,
            settings.UpdatedAtUtc);
    }

    [Fact]
    public void SetTrustedFrom_ShouldPersistUtcBoundary()
    {
        var now =
            DateTime.SpecifyKind(
                new DateTime(
                    2026,
                    9,
                    21,
                    15,
                    0,
                    0),
                DateTimeKind.Utc);

        var trusted =
            DateTime.SpecifyKind(
                new DateTime(
                    2026,
                    9,
                    21,
                    3,
                    0,
                    0),
                DateTimeKind.Utc);

        var settings =
            OperationalDataSettings.Create(
                now);

        settings.SetOperationalTrustedFrom(
            trusted,
            now.AddMinutes(1));

        Assert.Equal(
            trusted,
            settings.OperationalTrustedFromUtc);
    }

    [Fact]
    public void TrustedFrom_ShouldBeClearable()
    {
        var now =
            DateTime.SpecifyKind(
                new DateTime(
                    2026,
                    9,
                    21,
                    15,
                    0,
                    0),
                DateTimeKind.Utc);

        var settings =
            OperationalDataSettings.Create(
                now);

        settings.SetOperationalTrustedFrom(
            now,
            now);

        settings.SetOperationalTrustedFrom(
            null,
            now.AddMinutes(1));

        Assert.Null(
            settings.OperationalTrustedFromUtc);
    }

    [Fact]
    public void NonUtcValues_ShouldBeRejected()
    {
        var local =
            new DateTime(
                2026,
                9,
                21,
                12,
                0,
                0,
                DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () =>
                OperationalDataSettings.Create(
                    local));
    }
}
