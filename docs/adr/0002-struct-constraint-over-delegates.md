# ADR-0002: Constrain to value types rather than accept a timestamp selector

## Status
Accepted.

## Context
The library must extract a timestamp from arbitrary caller types. The options
are a `Func<T, long>` selector, a plain `ITimestamped` interface constraint, or
`where T : struct, ITimestamped`.

## Decision
`where T : struct, ITimestamped`.

## Rationale
The .NET runtime shares generic code across all reference types but emits a
distinct specialisation per value type. Under `struct, ITimestamped` the JIT
knows the concrete type at the call site, so the interface call devirtualises
and the property inlines to a field load. The abstraction costs nothing at
runtime.

A `Func<T, long>` is a delegate invocation per element that will not inline, plus
the caller has to keep the instance alive. On a million-element pass this is
measurable and entirely avoidable.

An unconstrained `ITimestamped` constraint permits reference types, which brings
back shared generic code and real interface dispatch — and, worse, invites
callers to store the payload as a class, at which point the pass is chasing
pointers through the heap instead of walking an array.

## Consequences
Callers must declare their row types as structs. In practice this is what a
time-series row should be anyway — it lives in a large array, is read far more
often than it is mutated, and benefits from contiguous layout.

Types with many fields are copied on `ref readonly` returns unless the caller is
careful. The API returns `ref readonly T` from `AsOfCursor.Current` for this
reason.

The interface exists purely as a compile-time constraint. Boxing an
implementation into `ITimestamped` at runtime reintroduces everything this
decision avoids; the XML docs say so explicitly.