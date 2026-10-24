# InternContention

Contention probe for #485: dispatch latency while a loader thread churns the intern
table. Eight threads run the effect-less typed dispatch (`Timeline<,>.Apply<ushort,
ushort>`, one `TimelineTable.Reference` per call) against one anchor timeline; the
`loaded` scenario adds one thread looping `TimelineAsset.Of(TimelineAsset.Load(bytes))`
over 256 distinct contents — full pin/dispose lifecycles, so the loader holds and
releases the process-wide intern gate continuously, including graveyard drains.

Verdict (#485): **fixed.** On main @ `1cc42e41` the loader collapses dispatch
throughput to 0.10x (24.2M/s quiet -> 2.7M/s loaded) and inflates per-call p99 by
5.5x-90x across runs, because every dispatch pays the spin gate the loader holds.
After the fix (fixed 65,536-slot arena, gate-free `Reference`/`IsLive` reads,
validation hoisted before the gate) dispatch is decoupled: throughput 0.87-0.96x,
p99 0.9-1.3x across runs — within run-to-run noise on this host. Raw runs under
`results/main/` (pre-fix) and `results/fixed/` (post-fix, including `perf stat`
counters for cycles, instructions, branches, branch-misses).

## Reproduction

```sh
dotnet run --project benchmarks/InternContention -c Release -- run.json
dotnet run --project benchmarks/InternContention -c Release -- --require verify.json
```

`--require` fails (exit 1) when loaded p99 exceeds 1.5x quiet or loaded throughput
falls below 0.8x quiet; it exits 0 on the fixed runtime. Each run prints its own
line; the receipt JSON lands in `bin/Release/net10.0/results/`.
