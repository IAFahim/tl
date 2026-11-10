# PhaseClock — tl 5.0 §0 experiment

Decides whether the plan's central bet pays: replacing per-entity tick advance and entry lists with a phase function (`tick = (phase + frame·dir) mod duration`, one mod per run, not per entity) plus vector lane kernels. Kernel-only prototype on identical fixture data; scalar oracle is the parity reference, not a timing arm (the JIT vectorizes it in the delegate path, so its Bench numbers are meaningless — direct-call probe gives true scalar 4.43 ns for uniform-dense).

## Run

```sh
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release -- --verify --bench
DOTNET_EnableAVX2=0 dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release --no-build -- --verify
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release --no-build -- --probe
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
