# Performance work in progress (branch claude/project-thread-tuyrnt)

Resumable notes for the next session. Measurements are on a 4-core cloud VM (Intel Xeon, Emerald Rapids, 2.1 GHz, AVX-512, no PMU counters), .NET 10.0.115 from Ubuntu (Microsoft's SDK download host is blocked there; `global.json` is overridden locally and never committed).

## Baseline, main d87d199, KindShape `--domain pure`, one core (`taskset -c 1`), 5 runs

| Cell | ms per step | ns per active | hash |
| --- | ---: | ---: | --- |
| 10k x 40 | 3.18 | 7.95 | 9752CB7E647D8CFB |
| 50k x 100 | 27.1 | 5.42 | D0A3C452A2732780 |
| 1M x 2 | 10.7 | 5.35 | AD61E245D6C37375 |
| 1M x 4 | 18.9 | 4.73 | 57EC4E7C35FF011D |

The VM is noisy: the same 50k x 100 binary ranges 27-44 ms across processes. Compare arms ABAB and many runs.

## Profile (perf cpu-clock, DOTNET_PerfMapEnabled=1, DOTNET_EnableWriteXorExecute=0)

- 50k x 100: generated `Movement.FusedDense` about 60% of a step, `KindStore.Exact` about 30% (stride 100 > 64 disables the batched due map, so every entity goes through `Exact`).
- 1M x 4: generated `Movement.FusedSmall` about 58%, `KindStore.DenseRange` about 24%.

## Test shapes

- KindShape: 500 timelines x 16 tracks, 22 clips (6 blend tracks with 2 clips), 30-60 ticks looping, `--live 8`. Realistic, but short timelines keep the 1.44 MB span table in L2.
- Glide: 1000 identical timelines, one track, one clip covering the whole timeline, no kinds. Best case for lazy ticks; not a clip-evaluation benchmark.

## Landed on this branch

1. Branch-free Pick (commit 9aaba8f). Declared `KindCombine.Pick` kinds in the generated `FusedDense` loop keep a running row offset and priority and select with an arithmetic mask (`KindStore.Pick`); the value is read once after the slot loop. The JIT does not if-convert the ternary form, so the mask form is required. `FusedSmall` keeps the branchy form (the mask form was 25% slower at 1M x 2). `DenseOracleTests` fails when the tie rule is mutated.

   ABAB, one core, `--domain pure --runs 3 --measure 24`, hashes unchanged:

   | Cell | before ms | after ms |
   | --- | ---: | ---: |
   | 10k x 40 | 3.15 | 2.02 |
   | 50k x 100 | 32.5 | 26.1 |
   | 1M x 2 | 11.0 | 11.3 |
   | 1M x 4 | 19.4 | 19.5 |

2. Kind tick on TlWorkers (commit 5bbc69e). `TimelineWorld.Tick(players, forward, workers)` splits the dense kind pass into word chunks; each chunk pushes pulse hits into its own buffers, entities that need the general `Tick` path are deferred to a per-chunk list and ticked serially afterwards (steady state defers about one per entity per run), then lane hits merge into the kind buffers by entity so consumers still see sorted hits. `--workers N` in KindShape passes the same workers to the tick and the systems. One core is unchanged.

   ABAB, `taskset -c 0-3`, hashes unchanged:

   | Cell | 4 workers, serial tick ms | 4 workers, parallel tick ms | vs main one core |
   | --- | ---: | ---: | ---: |
   | 10k x 40 | 1.22 | 0.82 | 3.9x |
   | 50k x 100 | 12.5 | 7.5 | 3.6x |
   | 1M x 4 | 9.6 | 7.1 | 2.7x |
   | 1M x 2 | 5.3 | 3.9 | 2.7x |

   Systems scale 3.41x on 4 workers; the kind tick alone about 2.5x (serial deferred ticks, merge and request apply remain).

3. Start-burst memory (commit de6f5d1). Request queue, pulse-hit buffers and spilled kind rows are released once steps are far lighter than the burst. Live memory after the burst (RssAnon after `malloc_trim`): 50k x 100 5491 -> 2229 B/row (2.46x less), 10k x 40 5116 -> 3334, 1M x 4 617 -> 572. Speed within noise.

4. Clock pack (commit 1a61732). `packs/Clock` adds `FixedSteps(hz, maxStepsPerAdvance)`: exact integer remainder, capped catch-up with a `Dropped` count, `Alpha` for interpolation. No Tl.Core change.

5. Lazy-tick fallback. A lazy (horizon) table measures how many slots it steps per tick after 8 settling ticks; above one slot in 16 per tick (tables of 64+ slots) it steps eagerly for 32 ticks, doubling to 4,096 on repeated failed probes. `benchmarks/Glide` (Tl.Bake.Bench), 20,000 x 100, capacity 4, limit 255, netstandard2.1 asset, one core, median ms, hashes and oracle unchanged:

   | Duration | eager (limit 0) | lazy before | lazy after |
   | ---: | ---: | ---: | ---: |
   | 1 | 9.9 | 41.8-43.8 | 9.7-10.8 |
   | 4 | 6.3 | 12.8-14.3 | 7.3-8.0 |
   | 8 | 6.3 | 8.0-8.2 | 5.9-6.2 |
   | 16 | | 5.8 | 5.7 |
   | 120 | 7.2 | 2.85-3.15 | 2.83-2.97 |
   | 600 | | 0.68-0.93 | 0.71-0.97 (noise) |

   Calling `Judge` from inside `AdvanceLazy` cost 15 percent at duration 120 through JIT code layout; it is called from `Advance` after the lazy pass instead, and `Land` counts the stepped slots. The budget room came from primary constructors on 12 plain structs (same public API; `PlaybackView` keeps its internal constructor, `Frame` keeps its attribute).

6. netstandard2.1 lazy compare. `LazyTicks.Scan` uses `Vector<ushort>` when `Vector.IsHardwareAccelerated` on the netstandard2.1 asset (the .NET 10 asset keeps `Vector128`). Glide, 20,000 x 100, netstandard2.1 asset on .NET 10, one core, hashes unchanged: duration 120 3.0 -> 1.08 ms, duration 600 0.69 -> 0.37 ms. Core tests pass with `-p:TlCoreTfm=netstandard2.1`, and fail when the vector hit test is mutated. Unity's Mono may not accelerate `Vector<T>`; it then takes the scalar loop as before.

## Per-worker scaling (KindShape, `taskset -c 0-(W-1)`, `--workers W`, ms per step, three runs)

| Cell | W=1 | W=2 | W=3 | W=4 | W=4 speedup |
| --- | --- | --- | --- | --- | ---: |
| 50k x 100 | 24.0-24.3 | 12.6-13.0 | 8.9-9.5 | 7.1-7.8 | 3.4x |
| 1M x 4 | 18.7-20.6 | 9.8-10.6 | 7.4-7.7 | 5.9-6.2 | 3.2x |
| 10k x 40 | 2.0-2.05 | 1.22-1.41 | 0.90-1.16 | 0.87-0.95 | 2.3x |

Large cells add 83-94 percent per worker. 10k x 40 is a 2 ms step where fork and join of the tick plus each system dominate.

## Open design: rollback ring and exact backward time

Exact backward time for accumulating kinds (`+=` per tick on floats) and a 32-frame rollback ring both need a full copy of mutable world state: `PlaybackTable` (slots, counts, spill blocks, playing bits, dense origins and dues, lazy clock, request queues) and `KindStore` (kind tables with their spilled heaps, pulse queues and hits, dense records). Re-deriving kind state from slots is not exact: a library reload marks spans ReenterPending (see `PlaybackRebindTests`). The clean design is one arena per table and store so a snapshot is one `memcpy` of a fixed-size region and restore copies it back; that is several KB of core code and needs the owner's budget decision.

## Tried and rejected (exact but slower)

- Four-entity interleave through a stackalloc struct: 44 vs 28 ms.
- Software prefetch of the next entity's origins: +15%.
- Ternary select for Pick: JIT keeps the branch.
- `WordSlots` 16384 (batched due map for rows up to 256 slots): +11% at 10k x 100 only, neutral elsewhere; not landed yet.

## Local build notes

- SDK 10.0.115 drops the second `ProjectReference` to Tl.Gen.CSharp in `tests/Tl.Systems.Tests` (same Include, analyzer form), so the test project does not compile. Locally the analyzer Include is spelled `..\..\src\Tl.Gen.CSharp\..\Tl.Gen.CSharp\Tl.Gen.CSharp.csproj` and the file is marked skip-worktree. CI's newer SDK is unaffected.
- `src` budget: 519,951 / 520,000 B after de6f5d1. Commit 5bbc69e compacted about 3 KB (global usings, `Free4`, shared `Inline` constant, default `StructLayout` removed) to fit the parallel tick without a waiver. Anything larger needs more compaction or the owner's waiver.
- glibc keeps freed arenas resident, so RSS only reflects frees after `malloc_trim(0)`; KindShape calls it on Linux.

## Unity tick job (tl.unity 4b23471, read only)

This session can clone tl.unity but cannot push to it, so the self-hosted Unity runner was not used and nothing there was measured. From reading `com.tl.unity/Runtime/KindSystems.cs`, the kind step schedules three phases: `KindPulseClearJob` (an `IJob`), `KindTickJob` (`ScheduleParallel` over chunks) and one `KindMergeJob` (`IJob`) per pulse sender. The two `IJob` phases run on one worker and their work grows with the pulse count: `KindPulseClearJob.FlushKind` resizes and memmoves each target row for every pending lane item, and `KindMergeJob` appends every streamed pulse. With pulse-heavy worlds these serial phases bound scaling (Amdahl), which matches a low per-worker gain. Candidate fixes, in order: move the lane flush into `KindTickJob` so each chunk flushes its own rows (the same split as `KindStore.Lanes` here), then sort merge input by chunk and run it as an `IJobParallelForDefer` over chunks. The core copy in `com.tl.unity/Core` picks up this branch's dense parallel tick only once it is synced with `eng/sync-tl-core.sh`.

## Next

See the thread checklist; update this file at each pushed checkpoint.
