using EfratAgro.Adubos.Domain.Legacy;

namespace EfratAgro.Adubos.Domain.Tests;

public sealed class LegacyImportRowTests
{
    [Fact]
    public void ResolveWithSaleItem_ShouldCloseReviewAndPreserveAudit()
    {
        var row =
            new LegacyImportRow(
                Guid.NewGuid(),
                "HERINGER",
                341,
                "G341",
                "{}");

        row.MarkForReview(
            "SALE_DATE_INVALID");

        var saleItemId =
            Guid.NewGuid();

        row.ResolveWithSaleItem(
            saleItemId,
            "Date recovered from matching document evidence.");

        Assert.False(
            row.RequiresReview);

        Assert.Equal(
            "SALE_DATE_INVALID",
            row.ReviewReason);

        Assert.Equal(
            saleItemId,
            row.SaleItemId);

        Assert.NotNull(
            row.ResolvedAtUtc);

        Assert.Equal(
            "Date recovered from matching document evidence.",
            row.ResolutionNote);
    }

    [Fact]
    public void ResolveWithSaleItem_ShouldRejectNonReviewRow()
    {
        var row =
            new LegacyImportRow(
                Guid.NewGuid(),
                "HERINGER",
                341,
                "G341",
                "{}");

        Assert.Throws<InvalidOperationException>(
            () =>
                row.ResolveWithSaleItem(
                    Guid.NewGuid(),
                    "Should fail."));
    }
}
