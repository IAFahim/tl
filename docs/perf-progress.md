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

## Next

See the thread checklist; update this file at each pushed checkpoint.
