using Xunit;

namespace Axitor.PointInTime.Tests;

public sealed class TimeWindowCursorTests
{
    [Fact]
    public void Advance_KeepsOnlyObservationsInsideTheWindow()
    {
        var series = TestSeries.Observations(0, 10, 20, 30, 40);
        TimeWindowCursor cursor = default;

        cursor.Advance(series, asOf: 40, window: 25);

        Assert.Equal(2, cursor.Tail);   // 0 and 10 are older than the cutoff
        Assert.Equal(5, cursor.Head);
        Assert.Equal(3, cursor.Count);
    }

    [Fact]
    public void Advance_UsesHalfOpenBounds()
    {
        var series = TestSeries.Observations(0, 10);
        TimeWindowCursor cursor = default;

        // cutoff == 0, so the element stamped 0 leaves; the one at 10 stays.
        cursor.Advance(series, asOf: 10, window: 10);

        Assert.Equal(1, cursor.Count);
        Assert.Equal(1, cursor.Tail);
    }

    [Fact]
    public void Advance_WithZeroWindow_HoldsNothing()
    {
        var series = TestSeries.Observations(0, 10, 20);
        TimeWindowCursor cursor = default;

        cursor.Advance(series, asOf: 20, window: 0);

        Assert.Equal(0, cursor.Count);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(55)]
    [InlineData(555)]
    public void Advance_DeltasAccountForEveryElementExactlyOnce(int seed)
    {
        var (left, right) = TestSeries.RandomPair(seed, 300, 300, span: 6_000);
        const long Window = 500;

        TimeWindowCursor cursor = default;
        int entered = 0;
        int exited = 0;

        foreach (var tick in left)
        {
            var delta = cursor.Advance(right, tick.Timestamp, Window);

            entered += delta.EnteredCount;
            exited += delta.ExitedCount;

            Assert.Equal(entered - exited, cursor.Count);
        }

        Assert.True(entered <= right.Length);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(22)]
    [InlineData(222)]
    public void Advance_MatchesNaiveRescan(int seed)
    {
        var (left, right) = TestSeries.RandomPair(seed, 200, 400, span: 5_000);
        const long Window = 700;

        TimeWindowCursor cursor = default;

        foreach (var tick in left)
        {
            cursor.Advance(right, tick.Timestamp, Window);

            int expected = right.Count(o =>
                o.Timestamp <= tick.Timestamp &&
                o.Timestamp > tick.Timestamp - Window);

            Assert.Equal(expected, cursor.Count);
        }
    }

    [Fact]
    public void Advance_WithNegativeWindow_Throws()
    {
        var series = TestSeries.Observations(0);
        TimeWindowCursor cursor = default;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cursor.Advance(series, asOf: 0, window: -1));
    }
}