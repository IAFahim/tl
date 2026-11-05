# Kinds

K0 prototype for the kind model (issue #553, plan #552): per-tick timeline work grouped by kind instead of dispatched per entity. Hand-written, no generator, standalone (not in `tl.slnx`).

- `Library.cs`: baked timelines with clip windows, fold lanes, per-tick event bits, per-direction event lists (remove, insert, start pulse, end pulse), wrap lists and a kind mask per timeline.
- `Playbacks.cs`: stable slot indices, lowest free slot reused, growth past the inline count, `Stopping` slot held until the next Tick.
- `KindTable.cs`: one table per kind, rows only for entities that reach the kind, inline entries with heap overflow, `Active` and `Enabled` bitsets.
- `World.cs`: requests, the Tick pass (boundaries, pulses, loop, next, hold, end), deferred game pulses merged at the next Tick, and the generic kind loop (`Combine` or `Each`).
- `Oracle.cs`: rebuilds every kind's entries from the playbacks alone each step and compares entries and applied columns with the incremental state, on random scripts of starts, stops, reverses, pauses, loops, next, hold, channels and game pulses.

## Run

```sh
dotnet run --project benchmarks/Kinds/Kinds.csproj -c Release -- --oracle
dotnet run --project benchmarks/Kinds/Kinds.csproj -c Release -- --smoke
dotnet run --project benchmarks/Kinds/Kinds.csproj -c Release -- --filter '*' --artifacts benchmarks/Kinds/results/k0
```

## Verdict (i9-14900K, .NET 10, 100,000 entities, 32 steps per op)

Correctness: oracle 0 mismatches on 5 seeds x 10,000 steps x 64 entities, replay hash equal on every seed. Planted bugs are caught (freeing a stopped slot early: 5/5 seeds; skipping wrap removal: 3/5 seeds at 1,000 steps). `KindsA` and `KindsC` match plan 1's `SA` system bit for bit.

| Gate | Shape | Measured | Target | Result |
|---|---|---|---|---|
| K-A | `KindsA` vs `PlanOneA` (one walk entry each, one timeline) | 22.06 ms vs 31.89 ms = 0.69x | <= 1.2x | pass |
| K-B | `KindsB` (10% active) vs `KindsA` | 2.70 ms = 0.12x | <= 0.15x | pass |
| K-C | `KindsC` (100 timelines, rows unsorted) vs `KindsA` | 24.49 ms = 1.11x | <= 1.3x | pass |
| K-D | `KindsDTickOnly`, Tick with no boundary events | 10.14 ms / 3.2M playbacks = 3.17 ns | <= 0.5 ns | miss |
| K-E | `KindsE64` vs `KindsE2` | storage 48 B vs 48 B per entity; time 41.50 ms vs 36.57 ms = 1.13x | storage 2 kinds only; time <= 1.1x | storage pass, time miss |
| K-F | `KindsF8` vs `KindsF1` (game damage pulses) | 119.80 ms vs 23.10 ms = 5.19x | <= 2x | miss |
| Alloc | all warm steps | 0 B in `--smoke`; BDN reports 61 B/op for `KindsF1` only | 0 B | see note |

Notes:
- K-D includes the empty request pass and the playing-bitset scan. Every playback reads its slot, flags, duration and event bit through scattered memory; 0.5 ns is about three cycles at this clock.
- K-E's 64-kind shape plays 32 different timelines with rows unsorted, the 2-kind shape plays one. K-C shows unsorted timelines alone cost 1.11x, so most of the 1.13x is not the kind count.
- K-F is dominated by per-pulse work: 8 sends, 8 merges (each with a row lookup) and 8 entries folded per entity per step, about 4.3 ns per extra pulse.
- The 61 B/op on `KindsF1` does not reproduce under `--smoke` (0 B); unexplained.

Unity numbers (Unity 7000.0.0a12, Entities 6.7.0) are in `results/unity/k0-unity-numbers.md` with the eval sources beside them: 16-byte buffer header, `SizeInChunk = 16 + capacity x element`, 16,384-byte chunks with 16,320 usable, 128 entities per chunk at most, and `TrimExcess` returns overflowed data inline.

Receipts: `results/k0` (BenchmarkDotNet JSON, CSV, Markdown, log), `results/unity`.
