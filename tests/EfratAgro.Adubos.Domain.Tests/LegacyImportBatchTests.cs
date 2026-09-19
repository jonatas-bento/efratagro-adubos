using EfratAgro.Adubos.Domain.Legacy;

namespace EfratAgro.Adubos.Domain.Tests;

public sealed class LegacyImportBatchTests
{
    [Fact]
    public void LegacyConstructor_ShouldDefaultToWarehouseOpeningStock()
    {
        var batch =
            new LegacyImportBatch(
                "ADUBOS.xlsx",
                "abcdef123456");

        Assert.Equal(
            LegacyImportScope.WarehouseOpeningStock,
            batch.Scope);

        Assert.Equal(
            "ABCDEF123456",
            batch.SourceFileHash);
    }

    [Fact]
    public void CommercialHistory_ShouldBeExplicitlySupported()
    {
        var batch =
            new LegacyImportBatch(
                "ADUBOS.xlsx",
                "abcdef123456",
                LegacyImportScope.CommercialHistory);

        Assert.Equal(
            LegacyImportScope.CommercialHistory,
            batch.Scope);
    }

    [Fact]
    public void UnspecifiedScope_ShouldBeRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LegacyImportBatch(
                    "ADUBOS.xlsx",
                    "abcdef123456",
                    LegacyImportScope.Unspecified));
    }
}
