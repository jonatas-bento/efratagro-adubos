using EfratAgro.Adubos.Application.Common;
using EfratAgro.Adubos.Infrastructure.Common;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class TemporalFoundationTests
{
    [Fact]
    public void BusinessDay_ShouldUseSaoPauloBoundaries()
    {
        var date =
            new DateOnly(
                2026,
                9,
                10);

        var start =
            BusinessDate.StartOfDayUtc(
                date);

        var end =
            BusinessDate.ExclusiveEndUtc(
                date);

        Assert.Equal(
            new DateTime(
                2026,
                9,
                10,
                3,
                0,
                0,
                DateTimeKind.Utc),
            start);

        Assert.Equal(
            new DateTime(
                2026,
                9,
                11,
                3,
                0,
                0,
                DateTimeKind.Utc),
            end);
    }

    [Fact]
    public void ToLocalDate_ShouldRespectBusinessBoundary()
    {
        var beforeMidnight =
            new DateTime(
                2026,
                9,
                10,
                2,
                59,
                59,
                DateTimeKind.Utc);

        var midnight =
            new DateTime(
                2026,
                9,
                10,
                3,
                0,
                0,
                DateTimeKind.Utc);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                9),
            BusinessDate.ToLocalDate(
                beforeMidnight));

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                10),
            BusinessDate.ToLocalDate(
                midnight));
    }

    [Fact]
    public void ValidateRange_ShouldRejectInvertedPeriod()
    {
        var from =
            new DateOnly(
                2026,
                9,
                10);

        var to =
            new DateOnly(
                2026,
                9,
                1);

        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    BusinessDate.ValidateRange(
                        from,
                        to));

        Assert.Contains(
            "data inicial",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PagedResult_ShouldCalculateNavigation()
    {
        var result =
            new PagedResult<int>(
                [21, 22],
                Page: 2,
                PageSize: 20,
                TotalItems: 45);

        Assert.Equal(
            3,
            result.TotalPages);

        Assert.True(
            result.HasPreviousPage);

        Assert.True(
            result.HasNextPage);
    }

    [Fact]
    public void EmptyPagedResult_ShouldHaveZeroPages()
    {
        var result =
            new PagedResult<int>(
                [],
                Page: 1,
                PageSize: 20,
                TotalItems: 0);

        Assert.Equal(
            0,
            result.TotalPages);

        Assert.False(
            result.HasPreviousPage);

        Assert.False(
            result.HasNextPage);
    }
}
