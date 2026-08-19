using Xunit;

namespace Axitor.PointInTime.Tests;

/// <summary>
/// The property this library exists to guarantee: a result at time T is a
/// function of observations at or before T, and of nothing else.
/// </summary>
/// <remarks>
/// The test states it directly rather than checking a fixed expected array —
/// it recomputes the alignment after rewriting every observation in the future
/// and asserts the past did not move.
/// </remarks>
public sealed class NoLookaheadTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(123)]
    [InlineData(4242)]
    [InlineData(999_983)]
    public void MutatingFutureObservations_DoesNotChangeAlignedPast(int seed)
    {
        var random = new Random(seed);
        var (left, right) = TestSeries.RandomPair(seed, 600, 120, span: 20_000);

        long cutoff = left[random.Next(left.Length)].Timestamp;

        var baseline = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, right, baseline);

        // Rewrite the payload of everything strictly after the cutoff. Timestamps
        // are left alone so the series stays sorted; only the future changes.
        var tampered = (Observation[])right.Clone();
        for (int i = 0; i < tampered.Length; i++)
        {
            if (tampered[i].Timestamp > cutoff)
            {
                tampered[i] = tampered[i] with { Value = random.NextDouble() * 1e9 };
            }
        }

        var filledBefore = new Observation[left.Length];
        var filledAfter = new Observation[left.Length];
        AsOfJoin.ForwardFill(left, right, filledBefore);
        AsOfJoin.ForwardFill(left, tampered, filledAfter);

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i].Timestamp > cutoff)
            {
                continue;
            }

            Assert.Equal(filledBefore[i], filledAfter[i]);
        }

        var recomputed = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, tampered, recomputed);
        Assert.Equal(baseline, recomputed);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(64)]
    [InlineData(7_777)]
    public void TruncatingTheFuture_DoesNotChangeAlignedPast(int seed)
    {
        var (left, right) = TestSeries.RandomPair(seed, 400, 100, span: 8_000);

        long cutoff = right[right.Length / 2].Timestamp;
        var truncated = right.Where(o => o.Timestamp <= cutoff).ToArray();

        var full = new int[left.Length];
        var partial = new int[left.Length];
        AsOfJoin.Align<Tick, Observation>(left, right, full);
        AsOfJoin.Align<Tick, Observation>(left, truncated, partial);

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i].Timestamp > cutoff)
            {
                continue;
            }

            Assert.Equal(full[i], partial[i]);
        }
    }
}