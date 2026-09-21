using EfratAgro.Adubos.Infrastructure.DataQuality;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class DataQualityRulesTests
{
    [Fact]
    public void TrustedFrom_ShouldAcceptTechnicalBaseline()
    {
        var baseline =
            new DateOnly(
                2026,
                9,
                18);

        var today =
            new DateOnly(
                2026,
                9,
                21);

        DataQualityRules
            .ValidateOperationalTrustedFrom(
                baseline,
                baseline,
                today);
    }

    [Fact]
    public void TrustedFrom_ShouldRejectDateBeforeBaseline()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    DataQualityRules
                        .ValidateOperationalTrustedFrom(
                            new DateOnly(
                                2026,
                                9,
                                17),
                            new DateOnly(
                                2026,
                                9,
                                18),
                            new DateOnly(
                                2026,
                                9,
                                21)));

        Assert.Contains(
            "anterior",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrustedFrom_ShouldRejectFutureDate()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    DataQualityRules
                        .ValidateOperationalTrustedFrom(
                            new DateOnly(
                                2026,
                                9,
                                22),
                            new DateOnly(
                                2026,
                                9,
                                18),
                            new DateOnly(
                                2026,
                                9,
                                21)));

        Assert.Contains(
            "futuro",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrustedFrom_ShouldRequireTechnicalBaseline()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                DataQualityRules
                    .ValidateOperationalTrustedFrom(
                        new DateOnly(
                            2026,
                            9,
                            21),
                        null,
                        new DateOnly(
                            2026,
                            9,
                            21)));
    }
}
