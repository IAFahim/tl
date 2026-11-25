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

## Tried and rejected (exact but slower)

- Four-entity interleave through a stackalloc struct: 44 vs 28 ms.
- Software prefetch of the next entity's origins: +15%.
- Ternary select for Pick: JIT keeps the branch.
- `WordSlots` 16384 (batched due map for rows up to 256 slots): +11% at 10k x 100 only, neutral elsewhere; not landed yet.

## Local build notes

- SDK 10.0.115 drops the second `ProjectReference` to Tl.Gen.CSharp in `tests/Tl.Systems.Tests` (same Include, analyzer form), so the test project does not compile. Locally the analyzer Include is spelled `..\..\src\Tl.Gen.CSharp\..\Tl.Gen.CSharp\Tl.Gen.CSharp.csproj` and the file is marked skip-worktree. CI's newer SDK is unaffected.
- `src` budget after the Pick change: 519,947 / 520,000 B.

## Next

See the thread checklist; update this file at each pushed checkpoint.
