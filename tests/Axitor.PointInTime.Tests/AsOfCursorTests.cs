using Xunit;

namespace Axitor.PointInTime.Tests;

public sealed class AsOfCursorTests
{
    [Fact]
    public void DefaultCursor_HasNoValue()
    {
        AsOfCursor<Observation> cursor = default;

        Assert.False(cursor.HasValue);
        Assert.Equal(-1, cursor.Index);
    }

    [Fact]
    public void Advance_BeforeFirstObservation_ReportsNothingKnown()
    {
        var right = TestSeries.Observations(100, 200);
        AsOfCursor<Observation> cursor = default;

        Assert.False(cursor.Advance(right, 99));
        Assert.Equal(-1, cursor.Index);
    }

    [Fact]
    public void Advance_OnExactBoundary_TreatsObservationAsKnown()
    {
        var right = TestSeries.Observations(100, 200);
        AsOfCursor<Observation> cursor = default;

        Assert.True(cursor.Advance(right, 100));
        Assert.Equal(0, cursor.Index);
    }

    [Fact]
    public void Advance_WithDuplicateTimestamps_TakesTheLastInSourceOrder()
    {
        var right = TestSeries.Observations(100, 100, 100);
        AsOfCursor<Observation> cursor = default;

        Assert.True(cursor.Advance(right, 100));
        Assert.Equal(2, cursor.Index);
    }

    [Fact]
    public void Advance_PastEndOfSeries_KeepsTheFinalObservation()
    {
        var right = TestSeries.Observations(100, 200);
        AsOfCursor<Observation> cursor = default;

        cursor.Advance(right, 10_000);

        Assert.Equal(1, cursor.Index);
        Assert.Equal(right[1], cursor.Current(right));
    }

    [Fact]
    public void Advance_OverEmptySeries_NeverResolves()
    {
        AsOfCursor<Observation> cursor = default;

        Assert.False(cursor.Advance(ReadOnlySpan<Observation>.Empty, long.MaxValue));
    }

    [Fact]
    public void Reset_ReturnsCursorToInitialState()
    {
        var right = TestSeries.Observations(100, 200);
        AsOfCursor<Observation> cursor = default;

        cursor.Advance(right, 200);
        cursor.Reset();

        Assert.False(cursor.HasValue);
        Assert.False(cursor.Advance(right, 50));
    }
}