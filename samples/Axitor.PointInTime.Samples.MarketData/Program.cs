using Axitor.PointInTime;

// A five-minute candle series joined against a funding-rate series that updates
// every eight hours. The two grids never line up, and the funding series has
// gaps — the exact situation as-of joins exist for.

var candles = BuildCandles(count: 12, stepMs: 300_000);
var funding = new FundingPoint[]
{
    new(Timestamp: 0, Rate: 0.0001),
    new(Timestamp: 900_000, Rate: 0.0004),
    new(Timestamp: 2_400_000, Rate: -0.0002),
};

var aligned = new FundingPoint[candles.Length];
var warmed = new bool[candles.Length];

AsOfJoin.ForwardFill(candles, funding, aligned, fallback: new FundingPoint(0, double.NaN));
AsOfJoin.WarmupMask(candles, funding, warmupPeriod: 1_800_000, warmed);

Console.WriteLine($"{"candle",-10}{"close",-10}{"funding",-12}{"warm"}");

for (int i = 0; i < candles.Length; i++)
{
    string rate = double.IsNaN(aligned[i].Rate) ? "-" : aligned[i].Rate.ToString("F5");
    Console.WriteLine($"{candles[i].Timestamp,-10}{candles[i].Close,-10:F2}{rate,-12}{warmed[i]}");
}

// The window cursor drives incremental accumulators: add what entered, subtract
// what left, never rescan. Here it just counts, but the delta is what lets a
// rolling mean or z-score run in O(N) over the whole series.
TimeWindowCursor window = default;

Console.WriteLine();
Console.WriteLine("rolling 30-minute funding observation count:");

foreach (var candle in candles)
{
    window.Advance(funding, candle.Timestamp, window: 1_800_000);
    Console.WriteLine($"  at {candle.Timestamp,-10} -> {window.Count}");
}

static Candle[] BuildCandles(int count, long stepMs)
{
    var result = new Candle[count];
    for (int i = 0; i < count; i++)
    {
        result[i] = new Candle(i * stepMs, 100 + i * 0.5);
    }

    return result;
}

internal readonly record struct Candle(long Timestamp, double Close) : ITimestamped;

internal readonly record struct FundingPoint(long Timestamp, double Rate) : ITimestamped;