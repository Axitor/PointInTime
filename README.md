# Axitor.PointInTime

[![build](https://github.com/Axitor/PointInTime/actions/workflows/ci-net.yml/badge.svg)](https://github.com/Axitor/PointInTime/actions/workflows/ci-net.yml)

Allocation-free point-in-time alignment of irregular time series for .NET 10.

Joining a 5-minute price series against funding rates that update every 8 hours,
macro indicators that update weekly, and an economic calendar that updates
whenever it feels like it — where getting the boundary wrong by one row silently
leaks the future into your training data.

```csharp
var aligned = new int[candles.Length];
AsOfJoin.Align<Candle, MacroPoint>(candles, macro, aligned);
// aligned[i] is the index of the latest macro observation known at candles[i],
// or -1 if none had been published yet.
```

## Why

Three things go wrong when series of different frequencies are joined by hand.

**Lookahead bias.** The naive join picks the *nearest* observation rather than
the latest *prior* one. A weekly macro print published on Friday ends up
attached to Tuesday's candles. Backtests look excellent and live trading does
not. Everything here resolves strictly backwards, and
[the test suite asserts it as a property](tests/Axitor.PointInTime.Tests/NoLookaheadTests.cs):
rewriting every observation after time *T* must not change any result at or
before *T*.

**Cost.** Rescanning the right-hand series per element is O(N·M). At a million
candles this is the dominant cost of feature generation.

**Silent warm-up.** A 30-day rolling statistic computed on day 3 is a 3-day
statistic wearing a 30-day label. `WarmupMask` makes that explicit instead of
leaving it to be discovered later.

## Benchmarks

### All four approaches

20,000 left elements against 400 right elements — small, because the quadratic
variants are unusable at realistic sizes, which is the point.

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Linq | 42,432.03 | 16.36 | 8,000,000 B |
| Rescan | 2,593.51 | 1.00 | – |
| BinarySearch | 228.36 | 0.09 | – |
| **Cursor** | **30.17** | **0.01** | **–** |

The gap between `Linq` and `Rescan` is the same algorithm written two ways:
16× slower and 8 MB of enumerators and closures per operation, versus nothing.
The gap between `Rescan` and the rest is the algorithm itself.

### At realistic sizes

Only the two viable implementations. `BinarySearch` is the baseline here — it
is what most implementations settle on, and the only comparison worth making.

| Left | Right | BinarySearch | Cursor | Ratio |
|---:|---:|---:|---:|---:|
| 105,000 | 400 | 1,254.3 | 137.4 | **0.11** |
| 105,000 | 50,000 | 5,364.5 | 204.9 | **0.04** |
| 1,000,000 | 400 | 11,996.7 | 1,859.9 | **0.16** |
| 1,000,000 | 50,000 | 23,602.2 | 2,170.7 | **0.09** |

Neither allocates. The difference is entirely in work per element.

The more telling number is the response to a denser right-hand series. Growing
it from 400 to 50,000 observations costs binary search roughly 2× more time at
either size — the log factor plus a cache miss per lookup, on a working set that
no longer fits. The cursor moves by 10–17%, because it still touches each
observation exactly once no matter how many there are.

## Design

**A monotonic contract instead of a search.** `AsOfCursor<T>` never moves
backwards. The caller promises non-decreasing `asOf`; in exchange a full pass
costs O(N + M) with O(1) amortised work per query. Feature generation walks
forward anyway, so the constraint costs nothing in practice — see
[ADR-0001](docs/adr/0001-cursor-over-binary-search.md) for when it is the wrong
call.

**Generic over value types, not over an interface.** Every API constrains to
`where T : struct, ITimestamped`. The JIT emits a separate specialisation per
value type, so `Timestamp` devirtualises and inlines to a field load — no
interface dispatch inside the loop. Boxing an implementation into `ITimestamped`
gives that back; don't.
[ADR-0002](docs/adr/0002-struct-constraint-over-delegates.md).

**Index maps, not materialised joins.** `Align` writes indices into a caller-owned
`Span<int>`; the library allocates nothing and never takes ownership of memory.
`ForwardFill` exists for when copying is genuinely more convenient, but the index
map keeps the original series as the single source of truth.
[ADR-0003](docs/adr/0003-caller-owned-buffers.md).

**Deltas, not rescans.** `TimeWindowCursor` reports which elements entered and
left the window, so accumulators update incrementally. Without the delta the
caller has to rescan and an O(N) pass silently becomes O(N·W).

## API

| Type | Purpose |
|---|---|
| `AsOfCursor<T>` | Forward-only cursor. The primitive everything else is built on. |
| `AsOfJoin.Align` | Batch join producing an index map. |
| `AsOfJoin.ForwardFill` | Batch join producing materialised values. |
| `AsOfJoin.WarmupMask` | Flags positions with insufficient history. |
| `TimeWindowCursor` | Time-bounded sliding window with enter/exit deltas. |

## Contracts

- Both series sorted ascending by timestamp. `AsOfJoin.IsSortedAscending`
  is provided for ingestion-time guards; the hot paths do not re-check.
- `asOf` non-decreasing across calls on a given cursor.
- Boundaries inclusive: an observation stamped exactly `asOf` is known.
- Duplicate timestamps: the last in source order wins.
- `TimeWindowCursor` retains `(asOf - window, asOf]`. A zero window holds nothing.
- Timestamp units are the caller's choice, but must match across both series.

## Install

```
dotnet add package Axitor.PointInTime
```

Targets .NET 10. No dependencies. Native AOT compatible.

## Licence

MIT.