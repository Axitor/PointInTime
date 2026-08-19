using BenchmarkDotNet.Attributes;

namespace Axitor.PointInTime.Benchmarks;

/// <summary>
/// The two viable implementations at realistic sizes. The naive variants are
/// excluded deliberately: at a million elements they run for tens of seconds
/// each and drown the comparison that actually matters.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class ScalingBenchmarks
{
    private Candle[] _left = [];
    private MacroPoint[] _right = [];
    private int[] _destination = [];

    /// <summary>Five-minute candles. 105k is roughly one year.</summary>
    [Params(105_000, 1_000_000)]
    public int LeftCount { get; set; }

    /// <summary>Observations on the right-hand series: daily, then near-tick.</summary>
    [Params(400, 50_000)]
    public int RightCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_left, _right) = SeriesFactory.Build(LeftCount, RightCount);
        _destination = new int[LeftCount];
    }

    [Benchmark(Baseline = true)]
    public int BinarySearch()
    {
        int checksum = 0;

        foreach (var candle in _left)
        {
            checksum += Bounds.LowerBound(_right, candle.Timestamp);
        }

        return checksum;
    }

    [Benchmark]
    public int Cursor()
    {
        AsOfJoin.Align<Candle, MacroPoint>(_left, _right, _destination);

        int checksum = 0;
        foreach (int index in _destination)
        {
            checksum += index;
        }

        return checksum;
    }
}