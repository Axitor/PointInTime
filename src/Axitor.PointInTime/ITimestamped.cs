namespace Axitor.PointInTime;

/// <summary>
/// A value carrying an observation time on a monotonic timeline.
/// </summary>
/// <remarks>
/// Implemented by value types only. Every API in this library constrains its
/// type parameters to <c>struct, ITimestamped</c>, which lets the JIT specialise
/// the generic instantiation and inline the property access — no interface
/// dispatch on the hot path. Boxing an implementation into
/// <see cref="ITimestamped"/> defeats this and should never happen inside a loop.
/// </remarks>
public interface ITimestamped
{
    /// <summary>
    /// Observation time. Units are the caller's choice (Unix milliseconds,
    /// nanoseconds, ticks) but must be consistent across both sides of a join.
    /// </summary>
    long Timestamp { get; }
}