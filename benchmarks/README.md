# Benchmarks

Live projects, receipts, and retained evidence for the tl playback engine.

## Live projects

| Project | Gate | Receipt |
| --- | --- | --- |
| `Alpha` | `tl.slnx`, AGENTS.md core gate (`--verify`), CI | Typed lane vs `TimelineMovement` oracle, 0 B warm allocation |
| `PairHandles` | `tl.slnx`, README receipts | Pair-typed lane parity and throughput (`PairHandles/README.md`) |
| `TypedPlaybackProto` | CI | Typed lane vs hand-table parity and throughput at 100k rows |

## Standalone probes (not in `tl.slnx`)

Each owns a README that carries its verdict and cites its receipt directories under `results/`.

- `FusedAdvance` — fused vs two-call advance (#148) and the 24 B slot row A/B (#168).
- `ValuePoolFormat` — TLB1 v3 pooled format on real assets (#155) plus the #168 row repack A/B and corpus parity.
- `ConsumerFusion` — consumer fusion inlining verdict, JIT/NativeAOT proof, and measurements.

## Decision receipts

- `ValuePooling/results/validated` — measured rejection of per-row value pools.
- `ValueUniqueness/results` — corpus uniqueness survey behind the pool design.

## Harness

- `collect.py` + `test_collect.py` — BenchmarkDotNet report collection with environment fingerprinting (unit-tested in CI).
- `source_budget.py` — enforced source budgets (`src` 300,000 B, `samples` 32,000 B).

## Historical harness projects

`Algorithms`, `AotBench`, `ConsumerChecks`, `ConsumerGenerate`, `Dispatch`, `FusionChecks`, `FusionGenerate`, `FusionHour`, `Generate`, `GenerateHooks`, `KernelCompare`, `Native`, `Review`, `Routing`, plus `run.py`, `Hooks.cs`, and `Shared.cs`, target removed surfaces and are outside the solution, CI, and the core gate. Their measurement directories were removed under issue #195; `git log` retains every receipt. Their code remains only until the owner directs otherwise.
