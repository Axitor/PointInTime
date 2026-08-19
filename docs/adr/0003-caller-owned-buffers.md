# ADR-0003: Caller-owned buffers and spans as parameters

## Status
Accepted.

## Context
Two related questions. Should batch operations allocate and return their result,
or write into a caller-supplied buffer? And should `AsOfCursor<T>` hold the
source span as a field or take it as a parameter on every call?

## Decision
Batch operations write into a caller-supplied `Span<T>`. The cursor takes the
source as a parameter.

## Rationale
Returning `int[]` from `Align` would allocate one array per call. In a pass over
hundreds of symbols that is hundreds of large-object-heap allocations for
results the caller almost always wants to reuse. Writing into a caller-owned
span lets the buffer be allocated once per worker and reused across symbols, and
makes the ownership obvious rather than implied.

For the cursor, holding a `ReadOnlySpan<T>` field would make it a `ref struct`.
That forbids storing it in a class field, in an array, or across an `await` —
which rules out the natural usage of keeping one cursor per macro series inside
a per-symbol context object. Passing the span per call keeps the cursor a plain
struct that can live anywhere.

## Consequences
The API is more verbose: every call repeats the source span.

Passing a different span to a cursor mid-pass is undetectable and silently
wrong. This is a real sharp edge, accepted because the alternative rules out the
primary usage pattern.

`ForwardFill` exists as a convenience for callers who genuinely want values
rather than indices, but it copies the payload — for wide row structs the index
map is cheaper and keeps one source of truth.