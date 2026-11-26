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

7. Lazy tables under generated systems (commits 95cd78e, 764d3da). Generated event loops used `players.View`, `players.Playing` and `world.View`, each an escape, so a table driven by a generated system stepped eagerly for good. They now read `SystemView`, `SystemPlaying` and `PlaybackView.TickOf` and keep the table lazy. A first version returned a decoded copy per slot and read through a pointer to it; that ran the system loop 2.7x slower even on an eager table (Glide, 1,152 ticks: 6.5 s against 2.1 s). Reading through the table pointer and decoding only the tick in a register brought it to 2.3 s. Glide `system` mode, 20,000 x 100, capacity 4, limit 255, one core, ABAB, median ms, hashes equal, 0 B retained:

   | Duration | base (escapes, eager) | head (lazy) |
   | ---: | ---: | ---: |
   | 120 | 26.3, 26.8 | 21.9, 23.7 |
   | 3000 | 31.5, 31.0 | 21.3, 23.6 |

   `advance` mode is unchanged (1.20-1.34 against 1.20-1.25 ms at 120, 0.09-0.13 ms at 3000).

## Per-worker scaling (KindShape, `taskset -c 0-(W-1)`, `--workers W`, ms per step, three runs)

| Cell | W=1 | W=2 | W=3 | W=4 | W=4 speedup |
| --- | --- | --- | --- | --- | ---: |
| 50k x 100 | 24.0-24.3 | 12.6-13.0 | 8.9-9.5 | 7.1-7.8 | 3.4x |
| 1M x 4 | 18.7-20.6 | 9.8-10.6 | 7.4-7.7 | 5.9-6.2 | 3.2x |
| 10k x 40 | 2.0-2.05 | 1.22-1.41 | 0.90-1.16 | 0.87-0.95 | 2.3x |

Large cells add 83-94 percent per worker. 10k x 40 is a 2 ms step where fork and join of the tick plus each system dominate.

## Rollback ring and exact backward time

Landed on main as `PlaybackImage` (#681): `PlaybackTable.Save(image)` and `Restore(image)` copy the whole mutable world (slots with their stale run tails, counts, spill and playing bits, blocks and free heads, clocks, queues, the closing list and the kind tables, pulse queues and hits). A ring is N images; exact backward time for accumulating kinds is a restore to an earlier image. `DenseOracleTests.RestoringAfterDivergentFramesRejoinsTheWorldThatNeverLeft` (this branch) runs 1 to 6 divergent frames on one arm, restores, and checks byte equality with an arm that never left, at 48 x 3, 256 x 5 (spilled) and 40 x 24; it catches dropped free heads, slow pulse masks and pulse hits that the older restore test missed.

## Tried and rejected (exact but slower)

- Four-entity interleave through a stackalloc struct: 44 vs 28 ms.
- Software prefetch of the next entity's origins: +15%.
- Ternary select for Pick: JIT keeps the branch.
- `WordSlots` 16384 (batched due map for rows up to 256 slots): +11% at 10k x 100 only, neutral elsewhere; not landed yet.

## Local build notes

- SDK 10.0.115 drops the second `ProjectReference` to Tl.Gen.CSharp in `tests/Tl.Systems.Tests` (same Include, analyzer form), so the test project does not compile. Locally the analyzer Include is spelled `..\..\src\Tl.Gen.CSharp\..\Tl.Gen.CSharp\Tl.Gen.CSharp.csproj` and the file is marked skip-worktree. CI's newer SDK is unaffected.
- `src` budget: 560,000 B (was 525,100 on main after #681). Vex allowed the raise; the open issues PR #680 uses the same number so the two merge cleanly. This branch is at 525,838 B: the non-escaping system readers did not fit under 525,100.
- glibc keeps freed arenas resident, so RSS only reflects frees after `malloc_trim(0)`; KindShape calls it on Linux.

## Unity tick job (tl.unity#56)

Measured on the self-hosted Unity runner (Unity 7000.0.0a21, Ryzen 5 8500G) with `OwnerScaleBench` at 20,000 rows x 200 playbacks; full tables are in tl.unity `docs/PERF-UNITY-LIMITS.md` section 11.

- The tick itself scales about 80 to 90 % per added worker up to 5 workers and flattens past that. The serial pulse phases were not the bottleneck: the kind jobs were 97 % of the frame.
- Why it flattens: the runner has 6 physical cores and 12 threads (SMT pairs 0/6 to 5/11). One pinned thread per CPU on a Python spin loop takes 1.33-1.54 s on cores 0 and 4 and 1.93-2.18 s on cores 1, 2, 3 and 5, so 2 cores are full Zen4 and 4 are Zen4c, about 1.4x slower; lscpu reports the same 5.08 GHz maximum for all of them. Six pinned threads on six cores finish in 1.93 s; from 7 threads on, two share a core and the slowest takes 2.1-3.1 s. Unity's main thread plus 5 workers fill the 6 cores, so a 6th to 11th worker only adds SMT siblings. This is the hardware, not tl.
- Sweep of 1 to 11 workers, head 0514525 (tl.unity main), lazy d=120, one run, total ms: 12.8, 10.8, 11.8, 8.7, 11.2, 5.2, 5.8, 5.4, 5.3, 4.8 (workers 1, 2, 3, 4, 5, 6, 7, 8, 9, 11). Hashes equal, 0 B. One run on this runner varies up to 3x, so only the trend is supported: about 2.7x at 11 workers.
- The kind jobs walked every playback slot for every kind, even kinds with no spans, and ran the event walk for timelines with no consumer pair. tl.unity#56 adds `KindIndex.SpanKinds` and a per-timeline pair filter (`KindIndex.PairBloom`) so generated jobs skip only slots the old walk already ignored.
- Base 5a15151 against head, 1 worker, median of 4 samples per arm, kind ms: eager d=120 19.9 -> 13.8, lazy d=120 17.9 -> 14.6, eager d=3000 32.4 -> 32.5, lazy d=3000 31.1 -> 29.6. Hashes are equal in every cell, allocation 0 B, EditMode and PlayMode pass.
- Samples of one build vary up to 3x between runs on this runner, so only the duration 120 gain (about 30 % eager, 18 % lazy) is supported. What is left is the Locomotion feeder, evaluated for every live span every frame.

## Next

See the thread checklist; update this file at each pushed checkpoint.
