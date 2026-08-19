# ADR-0004: Inclusive as-of boundary, half-open window, last duplicate wins

## Status
Accepted.

## Context
Three boundary questions have to be answered identically everywhere, because
inconsistency between them is exactly how a one-row lookahead leak appears.

Is an observation stamped exactly `asOf` known? Which of several observations
sharing a timestamp is current? Does an element leave a time-bounded window when
it reaches the window length or after?

## Decision
The as-of boundary is inclusive: `Timestamp <= asOf`. Among duplicates, the last
in source order wins. The sliding window is half-open: `(asOf - window, asOf]`.

## Rationale
Inclusive as-of matches how observations are published. A funding rate stamped
12:00:00 is known at 12:00:00. Excluding it would mean a value is unknown at the
instant it exists, which is not conservative — it is just wrong in the other
direction, and it produces off-by-one artefacts at every publication boundary.

Last-duplicate-wins matches append-only sources, where a corrected value is
appended after the one it supersedes. Taking the first would mean ignoring
corrections.

The half-open window makes an element's lifetime exactly `window` long and makes
consecutive windows partition the timeline without overlap. It also makes a zero
window hold nothing, which is the only self-consistent answer, if a surprising
one.

## Consequences
A zero-length window returns an empty window rather than the single current
element. Documented on the type and asserted in tests, because it will surprise
someone.

Callers whose data uses exclusive publication semantics — where a value stamped
T only becomes actionable at T+1 — must shift their timestamps at ingestion.
The library does not offer a switch for this; one consistent rule is worth more
than a configurable one.