using BenchmarkDotNet.Attributes;

namespace Axitor.PointInTime.Benchmarks;

/// <summary>
/// All four implementations on one small shape. Small because the quadratic
/// ones are unusable at realistic sizes — which is the point being made.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class NaiveComparisonBenchmarks
{
    private const int LeftCount = 20_000;
    private const int RightCount = 400;

    private Candle[] _left = [];
    private MacroPoint[] _right = [];
    private int[] _destination = [];

    [GlobalSetup]
    public void Setup()
    {
        (_left, _right) = SeriesFactory.Build(LeftCount, RightCount);
        _destination = new int[LeftCount];
    }

    /// <summary>
    /// What this looks like before anyone profiles it. On top of the quadratic
    /// scan, each element allocates a closure and a chain of enumerators.
    /// </summary>
    [Benchmark]
    public int Linq()
    {
        int checksum = 0;

        foreach (var candle in _left)
        {
            long asOf = candle.Timestamp;

            int match = _right
                .Select((point, index) => (point, index))
                .Where(entry => entry.point.Timestamp <= asOf)
                .Select(entry => entry.index)
                .DefaultIfEmpty(-1)
                .Last();

            checksum += match;
        }

        return checksum;
    }

    /// <summary>
    /// The same algorithm written by hand. Allocation-free, still quadratic —
    /// which separates the cost of LINQ from the cost of the approach.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int Rescan()
    {
        int checksum = 0;

        foreach (var candle in _left)
        {
            long asOf = candle.Timestamp;

            int index = -1;
            for (int j = _right.Length - 1; j >= 0; j--)
            {
                if (_right[j].Timestamp <= asOf)
                {
                    index = j;
                    break;
                }
            }

            checksum += index;
        }

        return checksum;
    }

    [Benchmark]
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