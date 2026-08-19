namespace Axitor.PointInTime.Tests;

internal readonly record struct Tick(long Timestamp, double Value) : ITimestamped;

internal readonly record struct Observation(long Timestamp, double Value) : ITimestamped;

internal static class TestSeries
{
    public static Tick[] Ticks(params long[] timestamps)
    {
        var result = new Tick[timestamps.Length];
        for (int i = 0; i < timestamps.Length; i++)
        {
            result[i] = new Tick(timestamps[i], i);
        }

        return result;
    }

    public static Observation[] Observations(params long[] timestamps)
    {
        var result = new Observation[timestamps.Length];
        for (int i = 0; i < timestamps.Length; i++)
        {
            result[i] = new Observation(timestamps[i], i * 10.0);
        }

        return result;
    }

    /// <summary>
    /// Reference implementation: obvious, quadratic, and obviously correct.
    /// Everything else in the suite is checked against this.
    /// </summary>
    public static int[] AlignNaive<TLeft, TRight>(TLeft[] left, TRight[] right)
        where TLeft : struct, ITimestamped
        where TRight : struct, ITimestamped
    {
        var result = new int[left.Length];

        for (int i = 0; i < left.Length; i++)
        {
            result[i] = -1;

            for (int j = 0; j < right.Length; j++)
            {
                if (right[j].Timestamp <= left[i].Timestamp)
                {
                    result[i] = j;
                }
            }
        }

        return result;
    }

    public static (Tick[] Left, Observation[] Right) RandomPair(
        int seed, int leftCount, int rightCount, long span)
    {
        var random = new Random(seed);

        var leftStamps = new long[leftCount];
        for (int i = 0; i < leftCount; i++)
        {
            leftStamps[i] = random.NextInt64(0, span);
        }

        var rightStamps = new long[rightCount];
        for (int i = 0; i < rightCount; i++)
        {
            rightStamps[i] = random.NextInt64(0, span);
        }

        Array.Sort(leftStamps);
        Array.Sort(rightStamps);

        return (Ticks(leftStamps), Observations(rightStamps));
    }
}