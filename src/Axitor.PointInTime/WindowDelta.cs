namespace Axitor.PointInTime;

/// <summary>
/// Elements that entered and left a sliding window during one advance.
/// </summary>
/// <remarks>
/// Ranges are half-open: <c>[EnteredFrom, EnteredFrom + EnteredCount)</c>.
/// Reported so that accumulators can be updated incrementally — add what
/// entered, subtract what left — rather than rescanning the window.
/// </remarks>
public readonly record struct WindowDelta(
    int EnteredFrom,
    int EnteredCount,
    int ExitedFrom,
    int ExitedCount)
{
    /// <summary>True when the window did not change.</summary>
    public bool IsEmpty => EnteredCount == 0 && ExitedCount == 0;
}