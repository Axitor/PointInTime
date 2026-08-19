namespace Axitor.PointInTime;

/// <summary>
/// Batch point-in-time joins between two time-ascending series.
/// </summary>
/// <remarks>
/// Every method here answers the same question for each element of the left
/// series: <em>what was known about the right series at that moment?</em>
/// Nothing observed after a left element's timestamp can influence its result,
/// which is what makes the output safe to use as model input.
/// </remarks>
public static class AsOfJoin
{
    /// <summary>
    /// For each element of <paramref name="left"/>, writes the index of the most
    /// recent element of <paramref name="right"/> observable at that time, or -1
    /// when nothing had been observed yet.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="destination"/> is shorter than <paramref name="left"/>.
    /// </exception>
    public static void Align<TLeft, TRight>(
        ReadOnlySpan<TLeft> left,
        ReadOnlySpan<TRight> right,
        Span<int> destination)
        where TLeft : struct, ITimestamped
        where TRight : struct, ITimestamped
    {
        if (destination.Length < left.Length)
        {
            throw new ArgumentException(
                "Destination is shorter than the left series.", nameof(destination));
        }

        AsOfCursor<TRight> cursor = default;

        for (int i = 0; i < left.Length; i++)
        {
            destination[i] = cursor.Advance(right, left[i].Timestamp)
                ? cursor.Index
                : -1;
        }
    }

    /// <summary>
    /// Materialises the aligned right-hand values, substituting
    /// <paramref name="fallback"/> where nothing had been observed yet.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="Align{TLeft,TRight}"/> when the caller can work with an
    /// index map: it avoids copying the payload and keeps the original series as
    /// the single source of truth.
    /// </remarks>
    public static void ForwardFill<TLeft, TRight>(
        ReadOnlySpan<TLeft> left,
        ReadOnlySpan<TRight> right,
        Span<TRight> destination,
        TRight fallback = default)
        where TLeft : struct, ITimestamped
        where TRight : struct, ITimestamped
    {
        if (destination.Length < left.Length)
        {
            throw new ArgumentException(
                "Destination is shorter than the left series.", nameof(destination));
        }

        AsOfCursor<TRight> cursor = default;

        for (int i = 0; i < left.Length; i++)
        {
            destination[i] = cursor.Advance(right, left[i].Timestamp)
                ? cursor.Current(right)
                : fallback;
        }
    }

    /// <summary>
    /// Marks the positions at which <paramref name="right"/> has accumulated at
    /// least <paramref name="warmupPeriod"/> of history.
    /// </summary>
    /// <remarks>
    /// Window statistics computed before this point are arithmetically valid but
    /// describe a shorter window than requested. Carrying the mask alongside the
    /// values lets downstream code discard or flag them explicitly instead of
    /// silently treating a three-day average as a thirty-day one.
    /// </remarks>
    public static void WarmupMask<TLeft, TRight>(
        ReadOnlySpan<TLeft> left,
        ReadOnlySpan<TRight> right,
        long warmupPeriod,
        Span<bool> destination)
        where TLeft : struct, ITimestamped
        where TRight : struct, ITimestamped
    {
        ArgumentOutOfRangeException.ThrowIfNegative(warmupPeriod);

        if (destination.Length < left.Length)
        {
            throw new ArgumentException(
                "Destination is shorter than the left series.", nameof(destination));
        }

        if (right.IsEmpty)
        {
            destination[..left.Length].Clear();
            return;
        }

        long warmedAt = right[0].Timestamp + warmupPeriod;

        for (int i = 0; i < left.Length; i++)
        {
            destination[i] = left[i].Timestamp >= warmedAt;
        }
    }

    /// <summary>
    /// Verifies that a series is sorted ascending by timestamp. Intended for
    /// guard clauses at ingestion boundaries and for tests, not for hot paths.
    /// </summary>
    public static bool IsSortedAscending<T>(ReadOnlySpan<T> source)
        where T : struct, ITimestamped
    {
        for (int i = 1; i < source.Length; i++)
        {
            if (source[i].Timestamp < source[i - 1].Timestamp)
            {
                return false;
            }
        }

        return true;
    }
}