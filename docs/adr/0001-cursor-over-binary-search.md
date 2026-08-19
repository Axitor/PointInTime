# ADR-0001: Forward-only cursor rather than per-element binary search

## Status
Accepted.

## Context
For each element of a left series we need the latest element of a right series
at or before its timestamp. Three shapes are available.

A rescan per element is O(N·M). At a million candles it dominates the entire
feature-generation pass, so it is only a reference implementation.

A binary search per element is O(N log M), allocation-free, and imposes no
ordering requirement on the queries. It is the obvious choice and it is what
most implementations settle on.

A forward-only cursor is O(N + M) overall — every element of both series is
visited once — but requires that queries arrive in non-decreasing time order.

## Decision
Forward-only cursor, with the monotonicity requirement stated as a contract
rather than enforced.

## Rationale
Feature generation is a forward walk by nature: bars are processed oldest to
newest because the features themselves depend on prior state. The constraint the
cursor imposes is one the caller was already satisfying.

Beyond the asymptotics, the cursor touches the right series sequentially. The
binary search jumps across it per element, and at realistic sizes the resulting
cache behaviour is a larger effect than the log factor itself.

Enforcement was considered and rejected. Checking monotonicity per call means
carrying the previous `asOf` and branching on it in the hottest loop in the
library, to catch a caller error that shows up immediately and unmistakably in
any test. The contract is documented instead.

## Consequences
Random-access queries are not supported. A caller that needs them should use
`LowerBound` directly — it is a dozen lines and there is no reason to wrap it.

Out-of-order `asOf` degrades silently: the cursor stays where it is and returns a
stale observation rather than throwing. This is the cost of not enforcing, and
it is why `IsSortedAscending` exists for ingestion boundaries.

Because the cursor holds no reference to its source, passing a *different* span
to a subsequent call is undetectable and produces meaningless results. See
ADR-0003 for why the span is a parameter anyway.