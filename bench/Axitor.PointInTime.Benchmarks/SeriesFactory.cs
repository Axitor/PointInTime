namespace Axitor.PointInTime.Benchmarks;

public readonly record struct Candle(long Timestamp, double Close) : ITimestamped;

public readonly record struct MacroPoint(long Timestamp, double Value) : ITimestamped;

internal static class SeriesFactory
{
    private const long FiveMinutesMs = 300_000;

    /// <summary>
    /// Two evenly spaced series over the same span: candles on a fixed grid,
    /// and a right-hand series spread across the whole range.
    /// </summary>
    public static (Candle[] Left, MacroPoint[] Right) Build(int leftCount, int rightCount)
    {
        var left = new Candle[leftCount];
        for (int i = 0; i < leftCount; i++)
        {
            left[i] = new Candle(i * FiveMinutesMs, i);
        }

        long span = (long)leftCount * FiveMinutesMs;
        long rightStep = Math.Max(1, span / rightCount);

        var right = new MacroPoint[rightCount];
        for (int i = 0; i < rightCount; i++)
        {
            right[i] = new MacroPoint(i * rightStep, i * 1.5);
        }

        return (left, right);
    }
}

internal static class Bounds
{
    /// <summary>
    /// Index of the last element at or before <paramref name="asOf"/>, or -1.
    /// </summary>
    public static int LowerBound(MacroPoint[] source, long asOf)
    {
        int low = 0;
        int high = source.Length - 1;
        int result = -1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);

            if (source[mid].Timestamp <= asOf)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }
}