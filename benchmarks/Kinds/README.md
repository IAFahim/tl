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

Correctness: oracle 0 mismatches on 5 seeds x 10,000 steps x 64 entities, replay hash equal on every seed and unchanged by the tuning. Planted bugs are caught (freeing a stopped slot early: 5/5 seeds; skipping wrap removal: 3/5 seeds at 1,000 steps). `KindsA` and `KindsC` match plan 1's `SA` system bit for bit.

Tuned run (`results/k0-tuned`), with the first run (`results/k0`) for comparison:

| Gate | Shape | First run | Tuned | Target | Result |
|---|---|---|---|---|---|
| K-A | `KindsA` vs `PlanOneA` (one walk entry each, one timeline) | 0.69x | 20.09 vs 33.62 ms = 0.60x | <= 1.2x | pass |
| K-B | `KindsB` (10% active) vs `KindsA` | 0.12x | 2.43 ms = 0.12x | <= 0.15x | pass |
| K-C | `KindsC` (100 timelines, rows unsorted) vs `KindsA` | 1.11x | 21.34 ms = 1.06x | <= 1.3x | pass |
| K-D | `KindsDTickOnly`, Tick with no boundary events | 3.17 ns | 7.41 ms / 3.2M = 2.31 ns, 0.37x `KindsA` | <= 0.5 ns as written; <= 0.5x K-A restated | miss as written, pass restated |
| K-E | `KindsE64` vs `KindsE2`, both 32 timelines | 1.13x (unequal timelines) | 38.63 vs 31.88 ms = 1.21x; blocked layout 34.26 ms = 1.07x | storage 2 kinds only; time <= 1.1x | storage pass (48 B = 48 B); time miss interleaved, pass blocked |
| K-F | `KindsF8` vs `KindsF1` (game damage pulses) | 5.19x | 64.66 vs 20.11 ms = 3.21x | <= 2x | miss |
| Alloc | all warm steps | 0 B | 0 B over 20 warm ops per shape (`smoke.txt`) | 0 B | pass |

Tuning (one variable each, all bit-identical under the oracle):
- Tick takes a fast path for a settled slot (no flags but `Reverse`), hoists library pointers, and no longer writes a `Played` flag every step.
- Consecutive game pulses for the same target fold at merge for kinds that declare an associative fold (`IPulseFold`, here `Damage` int sum), so 8 sends become one entry. Kinds without it keep one entry per pulse.
- A row whose entries are all pulses is cleared instead of compacted.
- K-E's 2-kind arm now plays 32 timelines like the 64-kind arm.

Notes:
- K-D: the remaining cost is the playing-bitset scan plus one slot read, one duration read and one event-bit read per playback, scattered by timeline. 0.5 ns is about three cycles at this clock.
- K-E: `KindsE64` interleaves kind sets (entity e reaches kinds 2(e%32), 2(e%32)+1), so each kind loop strides 32 entities through the columns. `KindsE64Blocked` puts each kind set in a contiguous entity block, as Unity's archetype chunks do, and lands at 1.07x. The cost is the access pattern, not the kind count.
- K-F: after folding, the remaining cost is the 8 game `Send` calls and 8 merge iterations per entity, about 2.8 ns per extra pulse.
- BenchmarkDotNet's in-process toolchain reports 35 to 996 B on some shapes and none on others, varying between runs; the direct count on the benchmark thread over 20 warm ops is 0 B for every shape.

Unity numbers (Unity 7000.0.0a12, Entities 6.7.0) are in `results/unity/k0-unity-numbers.md` with the eval sources beside them: 16-byte buffer header, `SizeInChunk = 16 + capacity x element`, 16,384-byte chunks with 16,320 usable, 128 entities per chunk at most, and `TrimExcess` returns overflowed data inline.

Receipts: `results/k0` and `results/k0-tuned` (BenchmarkDotNet JSON, CSV, Markdown, log; `k0-tuned/smoke.txt`), `results/unity`. Profile a shape with `DOTNET_EnableWriteXorExecute=0 DOTNET_PerfMapEnabled=1 perf record dotnet bin/Release/net10.0/Kinds.dll --profile <Shape>`.
