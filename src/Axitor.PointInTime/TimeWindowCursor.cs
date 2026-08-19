namespace Axitor.PointInTime;

/// <summary>
/// Two-pointer sliding window bounded by elapsed time rather than by element
/// count, over a time-ascending series.
/// </summary>
/// <remarks>
/// <para>
/// The window is half-open: it retains elements with timestamp in
/// <c>(asOf - window, asOf]</c>. A window of zero therefore holds nothing, and
/// an element leaves exactly when it becomes older than the span.
/// </para>
/// <para>
/// Both pointers only move forward, so a full pass is O(N) regardless of window
/// size — every element is admitted once and evicted once. The returned
/// <see cref="WindowDelta"/> is what makes incremental accumulators possible;
/// without it the caller would have to rescan and the pass would degrade to
/// O(N·W).
/// </para>
/// <para>Calls must be made with non-decreasing <c>asOf</c>.</para>
/// </remarks>
public struct TimeWindowCursor
{
    private int _head; // exclusive
    private int _tail; // inclusive

    /// <summary>Index one past the newest element in the window.</summary>
    public readonly int Head => _head;

    /// <summary>Index of the oldest element in the window.</summary>
    public readonly int Tail => _tail;

    /// <summary>Number of elements currently inside the window.</summary>
    public readonly int Count => _head - _tail;

    /// <summary>
    /// Moves the window so that it covers <c>(asOf - window, asOf]</c>.
    /// </summary>
    /// <returns>Which elements entered and which left as a result.</returns>
    public WindowDelta Advance<T>(ReadOnlySpan<T> source, long asOf, long window)
        where T : struct, ITimestamped
    {
        ArgumentOutOfRangeException.ThrowIfNegative(window);

        int enteredFrom = _head;

        while (_head < source.Length && source[_head].Timestamp <= asOf)
        {
            _head++;
        }

        int exitedFrom = _tail;
        long cutoff = asOf - window;

        while (_tail < _head && source[_tail].Timestamp <= cutoff)
        {
            _tail++;
        }

        return new WindowDelta(
            enteredFrom, _head - enteredFrom,
            exitedFrom, _tail - exitedFrom);
    }

    /// <summary>Returns the cursor to its initial state.</summary>
    public void Reset()
    {
        _head = 0;
        _tail = 0;
    }
}