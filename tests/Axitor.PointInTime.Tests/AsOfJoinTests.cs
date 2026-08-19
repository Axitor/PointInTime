using Xunit;

namespace Axitor.PointInTime.Tests;

public sealed class AsOfJoinTests
{
    [Fact]
    public void Align_MatchesNaiveReference_OnHandBuiltSeries()
    {
        var left = TestSeries.Ticks(50, 100, 150, 200, 250);
        var right = TestSeries.Observations(100, 200);

        var actual = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, right, actual);

        Assert.Equal(new[] { -1, 0, 0, 1, 1 }, actual);
        Assert.Equal(TestSeries.AlignNaive(left, right), actual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(1337)]
    [InlineData(90210)]
    public void Align_MatchesNaiveReference_OnRandomSeries(int seed)
    {
        var (left, right) = TestSeries.RandomPair(seed, 500, 80, span: 10_000);

        var actual = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, right, actual);

        Assert.Equal(TestSeries.AlignNaive(left, right), actual);
    }

    [Fact]
    public void Align_WhenRightStartsLater_ReportsNothingKnownThroughout()
    {
        var left = TestSeries.Ticks(1, 2, 3);
        var right = TestSeries.Observations(10, 20);

        var actual = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, right, actual);

        Assert.All(actual, index => Assert.Equal(-1, index));
    }

    [Fact]
    public void Align_WithEmptyRight_ReportsNothingKnownThroughout()
    {
        var left = TestSeries.Ticks(1, 2, 3);

        var actual = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, Array.Empty<Observation>(), actual);

        Assert.All(actual, index => Assert.Equal(-1, index));
    }

    [Fact]
    public void Align_WithUndersizedDestination_Throws()
    {
        var left = TestSeries.Ticks(1, 2, 3);
        var right = TestSeries.Observations(1);

        Assert.Throws<ArgumentException>(() =>
        {
            var tooSmall = new int[2];
            AsOfJoin.Align<Tick, Observation>(left, right, tooSmall);
        });
    }

    [Fact]
    public void ForwardFill_SubstitutesFallbackBeforeFirstObservation()
    {
        var left = TestSeries.Ticks(50, 150);
        var right = TestSeries.Observations(100);
        var fallback = new Observation(0, double.NaN);

        var actual = new Observation[left.Length];
        AsOfJoin.ForwardFill(left, right, actual, fallback);

        Assert.Equal(fallback, actual[0]);
        Assert.Equal(right[0], actual[1]);
    }

    [Fact]
    public void ForwardFill_AgreesWithAlign()
    {
        var (left, right) = TestSeries.RandomPair(11, 400, 60, span: 5_000);

        var indices = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, right, indices);

        var filled = new Observation[left.Length];
        AsOfJoin.ForwardFill(left, right, filled);

        for (int i = 0; i < left.Length; i++)
        {
            var expected = indices[i] < 0 ? default : right[indices[i]];
            Assert.Equal(expected, filled[i]);
        }
    }

    [Fact]
    public void WarmupMask_TurnsOnOnceEnoughHistoryHasAccumulated()
    {
        var left = TestSeries.Ticks(100, 150, 200, 250);
        var right = TestSeries.Observations(100);

        var actual = new bool[left.Length];
        AsOfJoin.WarmupMask(left, right, warmupPeriod: 100, actual);

        Assert.Equal(new[] { false, false, true, true }, actual);
    }

    [Fact]
    public void WarmupMask_WithEmptyRight_IsAlwaysOff()
    {
        var left = TestSeries.Ticks(1, 2, 3);

        var actual = new bool[left.Length];
        AsOfJoin.WarmupMask(left, Array.Empty<Observation>(), 10, actual);

        Assert.All(actual, Assert.False);
    }

    [Fact]
    public void WarmupMask_WithNegativePeriod_Throws()
    {
        var left = TestSeries.Ticks(1);
        var right = TestSeries.Observations(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var mask = new bool[1];
            AsOfJoin.WarmupMask(left, right, -1, mask);
        });
    }

    [Fact]
    public void IsSortedAscending_AcceptsDuplicatesAndRejectsInversions()
    {
        Assert.True(AsOfJoin.IsSortedAscending<Tick>(TestSeries.Ticks(1, 1, 2)));
        Assert.False(AsOfJoin.IsSortedAscending<Tick>(TestSeries.Ticks(1, 3, 2)));
    }
}