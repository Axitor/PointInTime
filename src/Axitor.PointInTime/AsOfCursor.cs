using System.Runtime.CompilerServices;

namespace Axitor.PointInTime;

/// <summary>
/// Forward-only cursor over a time-ascending series. Resolves the most recent
/// observation available at a given point in time.
/// </summary>
/// <remarks>
/// <para>
/// The cursor never moves backwards, so a full pass over both series costs
/// O(N + M) with O(1) amortised work per query and zero allocations. This is
/// the whole trade: the caller promises that <c>asOf</c> is non-decreasing, and
/// in exchange gives up the log factor of a binary search.
/// </para>
/// <para>
/// The cursor holds no reference to the source. The same span must be passed to
/// every call, and it must remain sorted ascending by
/// <see cref="ITimestamped.Timestamp"/>. See ADR-0003 for why the span is a
/// parameter rather than a field.
/// </para>
/// <para>
/// <c>default(AsOfCursor&lt;T&gt;)</c> is a valid, freshly-reset cursor.
/// </para>
/// </remarks>
public struct AsOfCursor<T> where T : struct, ITimestamped
{
    // Number of elements consumed, not the current index. Encoded this way so
    // that the all-zero state means "nothing observed yet" and default(T) works.
    private int _consumed;

    /// <summary>True once at least one observation has been resolved.</summary>
    public readonly bool HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _consumed > 0;
    }

    /// <summary>
    /// Index of the current observation in the source span, or -1 if none.
    /// </summary>
    public readonly int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _consumed - 1;
    }

    /// <summary>
    /// Advances to the last element whose timestamp is at or before
    /// <paramref name="asOf"/>.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> while <paramref name="asOf"/> precedes the first
    /// observation in <paramref name="source"/>; otherwise <see langword="true"/>.
    /// </returns>
    /// <remarks>
    /// The boundary is inclusive: an observation stamped exactly
    /// <paramref name="asOf"/> is considered known. When several observations
    /// share a timestamp, the last one in source order wins.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Advance(ReadOnlySpan<T> source, long asOf)
    {
        while (_consumed < source.Length && source[_consumed].Timestamp <= asOf)
        {
            _consumed++;
        }

        return _consumed > 0;
    }

    /// <summary>
    /// The current observation. Only valid when <see cref="HasValue"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref readonly T Current(ReadOnlySpan<T> source)
        => ref source[_consumed - 1];

    /// <summary>Returns the cursor to its initial state.</summary>
    public void Reset() => _consumed = 0;
}