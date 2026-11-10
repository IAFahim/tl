# PhaseClock — tl 5.0 §0 experiment

Decides whether the plan's central bet pays: replacing per-entity tick advance and entry lists with a phase function (`tick = (phase + frame·dir) mod duration`, one mod per run, not per entity) plus vector lane kernels. Kernel-only prototype on identical fixture data; scalar oracle is the parity reference, not a timing arm (the JIT vectorizes it in the delegate path, so its Bench numbers are meaningless — direct-call probe gives true scalar 4.43 ns for uniform-dense).

## Run

```sh
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release -- --verify --bench
DOTNET_EnableAVX2=0 dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release --no-build -- --verify
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release --no-build -- --probe
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release --no-build -- --large-verify
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release --no-build -- --large
```

## Baseline, this machine (AMD Ryzen 5 8500G, 6c/12t, Zen4 AVX2, .NET 10.0.12)

`benchmarks/Systems --receipt results/baseline-receipt.json --reps 3 --warmup-ms 200`, N=1024, hot ns/entity: folded 14.96 (i9 receipt: 9.42), four-pair 29.42 (21.84), 2-playback 37.34 (17.26). Local CPU is ~1.6x slower than the i9 receipt; ratios below use local baseline.

## Verdict: gain — projections met or beaten on every shape

| Shape (plan §0) | Kernel | Measured ns/entity | Plan projected | vs local 4.0 |
|---|---|---:|---|---:|
| Folded, uniform phase, dense rows | `BroadcastDense` (AVX2, 8-wide) | 0.050 | 0.15–0.4 | 300x vs 14.96 |
| Folded, mixed phase, sparse rows | `RunBroadcast` (phase-sorted runs, scalar scatter) | 0.480 | 1.0–2.0 | 31x |
| Folded, uniform phase, sparse rows | `BroadcastSparse` | 0.432 | 1.0–2.0 | 35x |
| Unique phases (plan §12 risk: runs of length 1) | `LaneGatherScalar` | 1.234 | 1.0–2.0 baseline | 12x |
| Lane, dense both | `LaneDense` | 0.061 | upside | — |
| Rewound | `BroadcastDense`, dir −1 | 0.048 | same as forward | identical |
| Four pairs | 4x `BroadcastDense` | 0.205 | 4x single | 143x vs 29.42 |
| Two playbacks | 2x `BroadcastDense` | 0.094 | 2x single | — |
| 64 cohorts, 2 active | 2x `BroadcastDense` + 64-entry mask scan | 0.112 | 2x single | cost follows active |

Gates: parity PASS on all 9 shapes (scalar hash == vector hash, 97 ticks); ISA-invariance PASS (`DOTNET_EnableAVX2=0`, 4-wide, same hashes); alloc 0 B warm. Direct probe: `BroadcastDense` 0.103, `RunBroadcast` 0.647 (delegate path 0.050/0.480 — report the probe numbers as the conservative ones; either way projections hold by 2–7x).

## What this proves and does not

- Proves: the kernel floor is far below the projection. Even the §12 risk case (unique phases, no runs) lands at 1.23 ns scalar — inside the 1.0–2.0 budget without any gather intrinsics. Vector-across-rows keeps float bits identical because each row is written once per tick; no per-row sum is reordered.
- Does not prove: full-pipeline gain. Observe (binary search over stage sub-ranges), Apply, Emit, and request sorting are not in this prototype. The margin (kernel ≤1.3 ns vs baseline 15 ns) leaves room for several ns of pipeline overhead and still nets 3x+, but M1–M5 must measure the real pipeline before any claim ships.
- Kill list for M6: manual 8-wide gather (scalar loads/stores through `Vector<T>`) was 2.29 ns, slower than scalar gather, and wrong under SSE (assumes 8 lanes). Deleted. `Avx2.Gather`/`Scatter` are not on this .NET's intrinsics surface (compile error CS0117) — gather stays scalar until a hand-intrinsic shim exists.
- N=1024 is L1/L2 resident. Cold/DRAM sparse numbers will be higher; M6 needs a cold sweep and a phase-entropy sweep (duration 40 → 1024+) before locking the 1.0–2.0 claim.

## Large-scale hostile: mixed-phase sparse at 1M/4M (`--large`, `LargeBench.cs`)

Shape: duration 1024, phase `(e*37+11)%1024` (spread across the duration, not uniform), sparse row `(e*97)%N` (permutation scatter across the full capacity), varying lane `Data.VaryingLane(1024)` for `RunBroadcast`/`LaneGatherScalar` and constant 6f lane for `BroadcastSparse`. Same (row,phase) pair set is used for kernel and World arms. Kernel calls are direct (no delegates in the timed loop); per scale the harness reports min of rounds, 0 B warm alloc over 50 ticks, and exact hash parity vs `ScalarPhase` oracle over 97 ticks. `ScalarPhase`/`LaneGatherScalar` gained `count` overloads in `Kernels.cs` — the originals hardcoded `Data.N` (1024) and silently truncated at 1M/4M; the 8-argument overloads delegate with `Data.N`, so all pre-existing cases and hashes are unchanged.

### Kernel ns/entity (measured, min of rounds; 1024: 9x20000, 1M: 5x60, 4M: 5x40 ticks/round)

| N | Kernel | ns/entity | vs 14.96 baseline | vs scalar (same N) |
|---:|---|---:|---:|---:|
| 1024 | ScalarPhase (oracle, direct) | 0.854 | 17.52x | — |
| 1024 | BroadcastSparse | 0.406 | 36.82x | 2.10x |
| 1024 | RunBroadcast | 1.444 | 10.36x | 0.59x |
| 1024 | LaneGatherScalar | 0.833 | 17.97x | 1.03x |
| 1M | ScalarPhase (oracle, direct) | 1.307 | 11.45x | — |
| 1M | BroadcastSparse | 0.688 | 21.73x | 1.90x |
| 1M | RunBroadcast | 1.571 | 9.52x | 0.83x |
| 1M | LaneGatherScalar | 1.294 | 11.56x | 1.01x |
| 4M | ScalarPhase (oracle, direct) | 4.215 | 3.55x | — |
| 4M | BroadcastSparse | 2.898 | 5.16x | 1.45x |
| 4M | RunBroadcast | 11.428 | 1.31x | 0.37x |
| 4M | LaneGatherScalar | 4.562 | 3.28x | 0.92x |

Hashes (97 ticks, oracle == actual, all PASS): 1024 const `5a7c42b6b1067325` var `82a8e96a769cf325`; 1M const `de19a84362c63025` var `a8b9a0a1dc37d025`; 4M const `f1d41c2e43525725` var `fe2cd14f08e9b725`. Alloc 0 B warm for all three kernels at all scales. Kernel data footprint is 16 B/entity (acc 4 + row 4 + phase 2 + sortedRow 4 + sortedPhase 2): 16 MB at 1M, 64 MB at 4M (plus a second acc for the oracle).

Cache effect (measured): 1024→1M is mild (LaneGatherScalar 0.83→1.29 ns, 1.6x — L2 spill, working set 16 KB→16 MB); 1M→4M is a cliff (1.29→4.56 ns, 3.5x — 40 MB of streamed row/phase/acc no longer fits L3, scalar gather/scatter goes to DRAM; BroadcastSparse 0.69→2.90 ns and scalar 1.31→4.22 ns show the same step, so it is the scatter, not the lane gather, that blows past cache). `RunBroadcast` is hostile at every scale on this shape: at 1024 runs of length ~1 it costs more than scalar gather (1.44 vs 0.85 ns); at 4M its phase-sorted rows are page-strided (~99k rows apart), so each of the 1024 run passes touches a fresh page/TLB entry across the whole 16 MB accumulator and it lands at 11.43 ns (0.37x vs scalar). The 13x threshold is 1.151 ns/entity (14.96/13): LaneGatherScalar holds it at 1024 (18.0x) but misses at 1M (11.6x) and badly at 4M (3.3x); only `BroadcastSparse` (single constant value, no per-entity phase/lane work) stays above 13x at 1M (21.7x) and still misses at 4M (5.2x). ISA-invariance measured: `DOTNET_EnableAVX2=0` (floatCount 4 vs 8) `--large-verify` gives identical hashes at all scales. Full output: `results/bench-large.txt` (this table is transcribed from that file; all numbers above are measured in that run).

### World end-to-end at 1M/4M capacity (measured, `results/bench-large.txt`)

Dense-active World at 1M/4M active playbacks cannot be set up through the public API: `World.StartPlayback` duplicate-checks with a linear `FindPlain` scan over all playbacks, so starting N playbacks is O(N^2). Measured start-cost probe (capacity == active, duration 1024 varying lane, first Tick applies all Starts): active 1024 setup 0.008 s tick 8.249 ns/entity; 8192 setup 0.024 s tick 9.765 ns; 32768 setup 0.193 s tick 6.214 ns. Setup is already 0.193 s at 32k and growing quadratically; dense-active 1M/4M setup was not attempted (projected ~180 s / ~2900 s from the quadratic trend — that projection is labelled, not measured). Even the steady Tick at 32k dense is 6.21 ns/entity (2.4x vs 14.96), far from the kernel floor, before any 1M cache cliff.

The feasible 1M/4M end-to-end measurement is therefore sparse capacity: `World(Library, capacity)` with 4096 active playbacks spread across the capacity (rows `(i*7919)%capacity`, phases from the kernel pair set), duration 1024. Two windows: dense `[0,1024)` (visits 4096/tick) and sparse `[0,40)` (visits ~165-169/tick, ~4% of active). Hash parity PASS (97 ticks, frame 1..97): 1M dense `1303e1f036d97025`, 1M sparse `a0dbd5b6c77b7025`, 4M dense `99a3fae8a39bb725`, 4M sparse `04d39dfebb59d725` (oracle == world in all four).

| World (sparse capacity, 4096 active) | ns/capacity-row | ns/active-visit | visits/tick | heap (measured GC delta) | alloc/tick (calling thread) |
|---|---:|---:|---:|---:|---:|
| 1M dense-window | 0.124 | 30.392 | 4096 | 69,558,960 B | 2162 B |
| 1M sparse-window 40/1024 | 0.086 | 509.361 | 169 | 69,542,512 B | 2280 B |
| 4M dense-window | 0.124 | 121.003 | 4096 | 276,534,288 B | 2177 B |
| 4M sparse-window 40/1024 | 0.107 | 2587.414 | 165 | 276,484,944 B | 2299 B |

World heap is ~69 B/row (acc+applied, playback/work/order/scratch/radix/group/run and solo arrays, all capacity-sized) + a small pending buffer for 4096 Starts: 69.6 MB resident at 1M, 276.5 MB at 4M, plus kernel data when both are live. World alloc is not 0 B: ~2.2 KB/tick on the calling thread from `Parallel.For` dispatch (kernels above are 0 B; World alloc is reported, not gated). Tick cost is O(capacity) regardless of visits — `Array.Copy(_acc,_applied,capacity)` plus parallel chunk dispatch over the full capacity — so ns/capacity-row looks like 120-170x vs baseline while ns/active-visit explodes with sparsity and capacity (30 ns dense at 1M → 509 ns sparse at 1M → 2587 ns sparse at 4M). Sparsity does not save World Tick; it makes per-visit cost worse. World Tick at 1M/4M is also parallel wall-clock (≥4096 rows uses `Parallel.For` over row chunks), unlike the single-threaded kernel arms — the 0.124 ns/capacity-row is copy bandwidth (~32 GB/s for 4 MB and 16 MB copies), not a single-core gain.

### Verdict: 13x does NOT hold at scale on the hostile shape (all measured unless labelled projected)

- Kernel: 13x (≤1.151 ns/entity) holds only at 1024 for `LaneGatherScalar`/`BroadcastSparse` and at 1M only for `BroadcastSparse`. At 4M every kernel misses: `LaneGatherScalar` 3.3x, `BroadcastSparse` 5.2x, `RunBroadcast` 1.3x (slower than scalar gather, 0.37x). The scalar gather itself blows past cache between 1M and 4M (3.5x step), and `RunBroadcast` with 1024 short/page-strided runs is the worst arm at every scale.
- World: dense Tick is already only ~2.4x vs baseline at 32k (6.21 ns, measured) and dense 1M/4M setup is O(N^2) via `FindPlain` (measured to 32k; 1M/4M setup projected, not measured, ~minutes). Sparse-capacity Tick is copy-bound O(capacity): per-capacity-row gain is meaningless, per-visit cost is 30-2587 ns and grows with capacity. No World number at 1M/4M supports a 13x pipeline claim.
- What survives: hash parity exact + ISA-invariant + 0 B kernel alloc at 1M/4M, and `BroadcastSparse` at 1M (21.7x). What does not: any 13x claim for mixed-phase sparse at 4M, for `RunBroadcast` at any scale on duration 1024, or for World end-to-end at scale.
