# Benchmarks

Live projects, receipts, and retained evidence for the tl playback engine.

## Live projects

| Project | Gate | Receipt |
| --- | --- | --- |
| `Systems` (+ `Systems21` netstandard2.1-vs-net10 ratio twin, same sources) | `benchmark.yml` (`--smoke`), `TlSystems.Tests` snapshot test | 4.0 systems throughput/correctness: NewA/B/C/D, S2, S64 shapes on hand-built views (`Systems/Generated/TlSystemsBench.g.cs` + `Systems/results/smoke-parity.txt`) |

## Standalone probes (not in `tl.slnx`)

Each owns a README that carries its verdict and cites its receipt directories under `results/`.

- `KindSystems` — K2 gates K-A to K-F through the generated path against plan 1's kernel, with a NativeAOT twin `KindSystems.Aot` (`KindSystems/README.md`).
- `KindShape` — owner-shape kinds (500 timelines × 16 tracks, 40-150 active per entity) through the generated path, hash-comparable across builds (`KindShape/README.md`, issue #652).
- `RowOrigins` — C model of playback layouts (8 B slots to 2-byte time-relative origins) and a bit-exact control oracle for origins (#673).
- `Kinds` — K0 prototype of per-tick work grouped by kind (#553): oracle, gates K-A to K-F against plan 1's kernel, Unity buffer and chunk numbers (`Kinds/README.md`).
- `Glide` — lazy slot ticks A/B on the kind-free `TimelineWorld.Advance` path against eager stepping, oracle-checked and hash-equal arms (`Glide/README.md`, `docs/PERF-GLIDE.md`).
- `ConsumerFusion` — consumer fusion inlining verdict, JIT/NativeAOT proof, and measurements.
- `LiveBatch` — retired post-cutover (harness measured the removed 3.x batch path; receipts kept, see `LiveBatch/README.md`). P0 baseline for the roadmap 4.10 gates, raw runs under `LiveBatch/results/`.
- `SystemsCompare` — pre/post fast-path gate comparison on NewA/B/C/D (plus netstandard2.1 twins), old baseline vs new receipts (`SystemsCompare/README.md`).

## Decision receipts

- `ValuePooling/results/validated` — measured rejection of per-row value pools.
- `ValueUniqueness/results` — corpus uniqueness survey behind the pool design.

## Harness

- `source_budget.py` — enforced source budgets (`src` 510,000 B, `samples` 66,000 B).
- `tools/Tl.Bake.Bench` — manual baked-asset harness with `corpus`, `parity`, `timing`, `batch`, and `batchcorpus` subcommands (#300 3.4); kept in `tl.slnx` for the compile gate.

 `Algorithms`, `AotBench`, `ConsumerChecks`, `ConsumerGenerate`, `Dispatch`, `FusionChecks`, `FusionGenerate`, `FusionHour`, `Generate`, `GenerateHooks`, `KernelCompare`, `Native`, and `Review` targeted removed surfaces, stayed outside the solution, CI, and the core gate, and were removed under issue #207 after the tracked-file necessity audit. `Alpha`, `FusedAdvance`, `InternContention`, `LaneCeiling`, `LivePlan`, `PairHandles`, `TypedPlaybackProto`, and `ValuePoolFormat` targeted the superseded 3.x playback path and were retired under issue #541; their verdicts above become historical on commit, with full receipts in `git log`. `git log` retains every receipt.

