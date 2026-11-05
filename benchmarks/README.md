# Benchmarks

Live projects, receipts, and retained evidence for the tl playback engine.

## Live projects

| Project | Gate | Receipt |
| --- | --- | --- |
| `Numbers` | `tl.slnx`, CI consistency test (`tests/test_readme_numbers.py`) | README Numbers generator: seven 1M-row scenarios plus a 19.3 MB corpus bake/load; `eng/refresh-numbers` re-measures and re-renders the README from `Numbers/results/numbers.json` |
| `Systems` (+ `Systems21` netstandard2.1-vs-net10 ratio twin, same sources) | `benchmark.yml` (`--smoke`), `TlSystems.Tests` snapshot test | 4.0 systems throughput/correctness: NewA/B/C/D, S2, S64 shapes on hand-built views (`Systems/Generated/TlSystemsBench.g.cs` + `Systems/results/smoke-parity.txt`) |

## Standalone probes (not in `tl.slnx`)

Each owns a README that carries its verdict and cites its receipt directories under `results/`.

- `ConsumerFusion` — consumer fusion inlining verdict, JIT/NativeAOT proof, and measurements.
- `InternContention` — #485 dispatch-vs-loader contention verdict (`InternContention/README.md`, raw runs under `InternContention/results/`): pre-fix main collapses dispatch to 0.10x under one loading thread; the fixed-arena, gate-free-reader runtime measures 0.87-0.96x across runs.
- `LivePlan` — #486 live-plan cache verdict (`LivePlan/README.md`, raw runs under `LivePlan/results/`): alternating-id ColumnSet apply drops 25% (5,062 to 3,804 us at 100k rows) once id changes stop re-running key thunks and column scans; grouped and single-id arms hold parity.
- `LiveBatch` — P0 batch-playback baseline for the roadmap 4.10 gates (`LiveBatch/README.md`, raw runs under `LiveBatch/results/`): shapes A (folded), B (fold-fed compose via `ColumnSet` `Apply` + `Prepare`/`Play` + per-128-chunk), C (frame consumer), D (four pairs) through the real generator, with a 97-frame bit-exact `--verify` parity arm.

## Decision receipts

- `ValuePooling/results/validated` — measured rejection of per-row value pools.
- `ValueUniqueness/results` — corpus uniqueness survey behind the pool design.

## Harness

- `source_budget.py` — enforced source budgets (`src` 510,000 B, `samples` 66,000 B).
- `tools/Tl.Bake.Bench` — manual baked-asset harness with `corpus`, `parity`, `timing`, `batch`, and `batchcorpus` subcommands (#300 3.4); kept in `tl.slnx` for the compile gate, and its `tools/Tl.Bake.Bench/Fixtures.cs` is shared into `benchmarks/Numbers`.

 `Algorithms`, `AotBench`, `ConsumerChecks`, `ConsumerGenerate`, `Dispatch`, `FusionChecks`, `FusionGenerate`, `FusionHour`, `Generate`, `GenerateHooks`, `KernelCompare`, `Native`, and `Review` targeted removed surfaces, stayed outside the solution, CI, and the core gate, and were removed under issue #207 after the tracked-file necessity audit. `Alpha`, `FusedAdvance`, `InternContention`, `LaneCeiling`, `LivePlan`, `PairHandles`, `TypedPlaybackProto`, and `ValuePoolFormat` targeted the superseded 3.x playback path and were retired under issue #541; their verdicts above become historical on commit, with full receipts in `git log`. `git log` retains every receipt.

