# Optimization verdicts

This ledger records which `tl` optimization moves won, which died, and which are conditional, so that dead ends are not repeated and wins are not silently abandoned. It was mined from repository history through `0c0a167` and extended with the first data-authored gate measurements. Every entry names its evidence. Per the repository rules, a hot-loop change is only kept when exact receipts pass and timing evidence supports it; when timing and instruction counts disagree, timing wins (B14), and a redesign that passes semantic parity but regresses timing is still a failure (B19).

Detailed narratives with full tables live in [benchmarks.md](benchmarks.md), [v0.4-unmanaged.md](v0.4-unmanaged.md), [v1.0-alpha.3.md](v1.0-alpha.3.md), and the retained results directories under `benchmarks/Alpha/results/`.

## Won, with numbers

- **Generated direct and constrained static calls.** Byte-identical to a hand-written direct implementation at 1.00x. Delegates cost about 4x, interface variables 4-4.5x, and boxing 4-11.4x while silently losing mutation semantics. ([benchmarks.md — Dispatch: how hook calls reach user code](benchmarks.md#dispatch-how-hook-calls-reach-user-code); boxing measured 10.9-11.4x at 24 B/call.)
- **One 16-aligned native block per timeline.** Replacing managed entry-plus-array storage with a single native block measured -26% on `PlaybackSingle` (23.53 to 17.32 ns) and -16% on `PlaybackParamsFour` (17.46 to 14.77 ns), with zero steady-state heap growth. ([v0.4-unmanaged.md](v0.4-unmanaged.md))
- **Dense `delegate*` tables for sparse random dispatch.** 6.0 ns at 256 keys and 11.2 ns at 4,096 keys, versus 24.1 and 36.4 ns for binary search. A switch index that is constant per call site is free. ([benchmarks.md — Sparse index dispatch](benchmarks.md#sparse-index-dispatch))
- **Monotone cursor plus rank bitvector for navigation.** A monotone cursor serves sequential movement at 0.87-1.23 ns independent of timeline size. The rank bitvector serves arbitrary seeks at 4.18-5.79 ns from about 1.25 bits per tick (10.2 KB) where a dense table needs 2 B/tick (131.1 KB). ([benchmarks.md — Algorithms: region navigation](benchmarks.md#algorithms-region-navigation))
- **Region-stable materialization of work slots.** The active work set is constant inside a region; materializing it once on region entry removed the enumerator and per-iteration bundle tax that consumed 57% (15.3 of 26.92 ns) of a consuming tick, taking full consumption from 26.92 to 18.09 ns. ([benchmarks.md — TrackViewDecomp](benchmarks.md#trackviewdecomp-what-one-tick-actually-spends-v02-receipts), [v0.3](benchmarks.md#v03-region-stable-materialization--the-view-tax-paid-once-per-region))
- **Hoisting immutable facts once per call.** Generic-static table fetches and other per-iteration reloads hoisted to one load per call; leaving them per-iteration costs about +0.2x.
- **Frozen span collection-expression tables.** `static ReadOnlySpan` collection expressions remove bounds guards and GC-static reloads and erased the entire NativeAOT toll to within +/-0.07 ns. ([benchmarks.md — NativeAOT: the no-tiering, no-PGO column](benchmarks.md#nativeaot-the-no-tiering-no-pgo-column))
- **Branchless flag arithmetic through sign masks.** Ternaries over flags compile to real branches; sign-mask arithmetic reaches exactly 2 conditional branches per tick. ([benchmarks.md — Bake v2](benchmarks.md#bake-v2-branchless-movement--interleaved-single-load-tables), [Bake v3](benchmarks.md#bake-v3-branchless-under-the-per-work-contract))
- **Pairing jointly-consumed scalars in one load.** One 24 B struct load beats bit-packed primitives that need three bounds-checked loads and unpacking; reordering fields to remove padding is free and removed 14.3% of slot bytes. ([benchmarks.md — Bake v3](benchmarks.md#bake-v3-branchless-under-the-per-work-contract))
- **Runtime-authored instance playback near generated-static parity.** 1.27x versus 1.20x against direct at four ticks. ([benchmarks.md — Runtime-authored instances](benchmarks.md#runtime-authored-instances))

## Dead, with reasons

- **B1 — delegates, boxing, or interface variables in playback.** Slow and semantically wrong; forbidden outright.
- **B2 — default interface hook bodies.** Interface dispatch cost without any authoring benefit.
- **B3 — generated switch state machines.** Slower than binary search over the same keys.
- **B4 — generated comparison trees for seek.** Lost to the rank bitvector at every measured size.
- **B5 — flat sparse switch relying on JIT lowering.** The JIT does not lower huge sparse switches into jumps.
- **B6 — radix switch 4x4x4x4.** 16.2/25.2 ns against 6.0/11.2 ns for the dense `delegate*` table.
- **B7 — fused megaswitch over sparse keys.** 3.3-3.8x slower than delegate-table-then-call.
- **B8 — prefix cut counts.** Superseded by rank packing that carries movement facts for free.
- **B9 — skew/width-ordered branch trees.** Branch outcome entropy, not expected compare count, is what a misprediction costs. ([benchmarks.md — Skew-aware tree ordering](benchmarks.md#skew-aware-tree-ordering-experiment))
- **B10 — maximal same-region unrolling.** +2.4x generated bytes and slower everywhere.
- **B11 — bit-packed primitive work slots.** See the pairing win above; packing separately-consumed fields always lost.
- **B12 — generic direct-span reshaping.** Generic reshaping costs more than the layout it fixes.
- **B13 — monolithic signed scalar body.** One signed body serving both directions is slower than direction-shaped paths.
- **B14 — late specialization without measurement.** Fewer instructions is not lower latency; timing receipts decide.
- **B15 — generic movement with specialized flags.** The split reintroduced the branch the specialization removed.
- **B16 — per-call region materialization.** +8 ns per tick; materialization pays only once per region entry.
- **B17 — deduplication on by default.** Taxed hot arms 5-15%; it stays opt-in.
- **B18 — movement lookup tables sized O(duration^2).** Size grows past any budget long before it wins.
- **B19 — redesigns that pass parity gates but regress timing 2-3x.** Reintroduced data-dependent branches smuggled the cost back in. Every hot-loop re-emission must re-run timing receipts. ([benchmarks.md — The redesigned contract on the frozen path](benchmarks.md#the-redesigned-contract-on-the-frozen-path--and-the-regression-it-smuggled-in))
- **B20 — unpinned CI benchmark identities.** Moving identities produce false alerts.
- **B21 — cross-repository or sampling-only nanosecond claims.** They never transfer; only same-machine receipts compare.

## Conditional

- **Direction-specialized kernels.** -6 to -12% serial, about +2% same-tick; accepted because serial playback is the scalar latency contract.
- **Deduplication.** Opt-in only; default-on is a measured tax (B17).
- **Pre-blending clip pairs.** Only when a bit-exact fold is provable; otherwise resolve lazily once per visit into one local with factor `(tick - FactorStart) / (float)(FactorSpan - 1)` and `0.5f` when the span is at most 1.
- **Inline versus staged scheduling.** Inline fused bodies win for narrow rows; a non-inlined staged scheduler is required for wide rows and batches, where an inline megabody collapsed a 10,000-row batch from 8.3 to 38.2 ns per row. The 32 KiB fusion budget implements this cutover; exceeding it on the 256-track asset is the measured alpha.3 cliff.
- **Seek strategy by start count.** Linear at most 16 starts, binary above, hybrid overall.
- **SIMD.** Only where lanes, layout, and effect order permit; float order is parity-binding, so serial accumulation never vectorizes.

## Laws

- Receipts precede timing; scalar latency is never labeled throughput; zero warm allocation is proven by receipts.
- The physical floor is about 0.6-0.95 ns per serially-dependent active track (3-5 cycles at 4-6 GHz); 1-2 ns is 4-12 cycles; a misprediction is 17-20 cycles; random dispatch floors near 6 ns.
- Cost is `hot_cycles + random_seek_weight * branch_miss + code_byte_weight * emitted + data_byte_weight * retained`, weighted per build profile.
- The optimization order is binding: delete redundant work, then hoist immutable facts, then specialize scalar single-steps, then compact schedules and payloads, then direct typed calls, then SIMD.
- Float evaluation order is parity-binding: no reassociation, FMA, reciprocals, or vectorized serial accumulation. Batch `Tick` is a fold over steps.
- Keep separate byte ledgers for source, generated, plan, static data, state, JIT, and AOT; AOT is not JIT and each is gated separately.
- The alpha.3 production floor to beat or match is 1.804 ns one-track, 1.997 ns gap, 2.555 ns blend, 3.754 ns A-B-A, 21.230 ns 16-track, 34,250.868 ns 256-track (the issue #10 cliff), 34.650 ns per entity-tick mixed, and 18.006 ns per entity-step Unity. The hand-written direct oracle floor is 2.062 ns for three effects plus state. ([v1.0-alpha.3.md](v1.0-alpha.3.md), [alpha.3 query floor](../benchmarks/Alpha/results/v1.0.0-alpha.3-query-floor/README.md))

## Bindings for the data-authored runtime

1. The single native block layout is the measured winner and is retained; see [memory-and-performance.md](memory-and-performance.md).
2. Function-pointer consumer thunks are free when the target stream is stable per call site and cost about 6 ns when randomized. Stage programs with stable per-stage consumers sit on the right side of this line; a chain-walk through a process-global consumer table does not.
3. Frame slots are plain structs with inline payloads: pair what is consumed together, never bit-pack separately-consumed fields.
4. `TimelineMovement.Select` remains the selection oracle; do not late-specialize it without new receipts (B14, B15).
5. The largest available win is deleting data-dependent branches; the largest risk is reintroducing them through redesign (B19). Every hot-loop change re-runs timing receipts.
6. No delegates, boxing, or interface-variable dispatch anywhere warm; generated static calls or `delegate*` only.
7. First gate-7 pass (2026-09-12, i9-14900K, one process per job): the facade decisively removes the 256-track cliff — 1,655.134 ns median against 34,646.702 ns for the alpha.3 generated kernel and 339.299 ns direct — at 0 B warm allocation with receipts identical across all three arms. Small shapes did not reach the compiled-kernel tier in this first pass: 15.118 ns one-track, 16.318 ns blend, 27.302 ns A-B-A, 110.868 ns 16-track, against 1.898/2.531/3.829/22.343 ns generated. The measured structure is a fixed per-tick facade overhead near 9 ns plus about 6.4 ns per dispatched step (per-tick consumer bind over the process-global table, binary pair search, and per-step column lookup inside the consumer thunk); reducing either is optimization work that requires its own atom and receipts, not a silent tune. ([first data-authored shape comparison](../benchmarks/Alpha/results/data-authored-first-pass/README.md))
8. Kernel lane final verdict (2026-09-13, i9-14900K, .NET 10.0.12 X64 RyuJIT x86-64-v3, InProcessNoEmit, 16 warmup + 12 × 250 ms, OperationsPerInvoke=4096, 0 B allocated on every arm). Kernel-bound facade vs interpreter medians (ns), two independent full runs of the 60-case `*DataAuthoredQueryBenchmarks*` suite (501.5 s / 501.2 s):

   | Arm | run 1 kernel / interp (ratio) | run 2 kernel / interp (ratio) |
   |---|---|---|
   | OneTrack Forward | 12.392 / 11.572 (1.071) | 8.354 / 11.664 (0.716) |
   | OneTrack Alternating | 9.544 / 10.459 (0.913) | 9.591 / 10.663 (0.900) |
   | ThreeTracks Forward | 16.283 / 18.969 (0.858) | 16.684 / 21.889 (0.762) |
   | ThreeTracks Alternating | 18.033 / 21.288 (0.847) | 17.715 / 21.279 (0.832) |
   | SixteenTracks Forward | 61.218 / 71.953 (0.851) | 62.378 / 72.662 (0.859) |
   | SixteenTracks Alternating | 66.809 / 77.681 (0.860) | 65.484 / 75.930 (0.862) |
   | 256 Forward | 966.304 / 1,103.106 (0.876) | 994.264 / 1,107.869 (0.897) |
   | 256 Alternating | 1,061.370 / 1,133.075 (0.937) | 1,087.848 / 1,121.369 (0.970) |

   The single run-1 miss is the OneTrack/Forward lottery below; every other arm beats the interpreter in both runs; the Blend identity control (no committed kernel) read 10.766/10.824 and 10.556/10.552 across the two runs. Pre-fix, the committed-kernel lane inverted with scale — kernel/interpreter 1.41-1.47 at 16/256 tracks (kernel-scale probe, same machine and job; raw tables recorded on issue #56, probe wave 2026-09-13). The fix chain: hoisted `Chain` scratch + aggressive inlining (`323c85a`), then dedicated `F`/`R` direction chunk bodies (`739bbc8`). Variant record at 256 tracks (filtered in-process medians): baseline 1,050.3/1,090.0 F and 1,101.4/1,111.2 A; Variant A (`AggressiveInlining` on chunks) 1,007.2/1,078.7 F and 1,103.6/1,105.9 A — rejected, the A-leg did not improve; Variant B (direction bodies, kept) 1,012.8/1,117.2 F and 1,118.7/1,142.2 A. Kernel SHA-256 hashes never moved; `--verify` receipts stayed component-equal across arms.

   OneTrack/Forward is a per-process JIT layout lottery, not a regression: across 13 processes the kernel median is 8.35-8.74 ns in 12 and 12.39-12.67 ns in 1, against a stable 10.5-11.8 ns interpreter (required 3-process run: 8.567/11.470, 8.395/11.693, 12.432/11.574; five child-toolchain disasm processes 8.687/8.509/8.677/8.499/8.739; four `DOTNET_JitDisasm` in-process runs 8.708/8.472/8.498/8.441). The fast mode's `TimelineKernel_65a912…::Tick` assembly is byte-identical across processes after address stripping, and the slow mode never drew under observation (0/9). Documented, deliberately not chased; an optional emission-only follow-up is recorded on issue #56.

   Cross-process variance on the untouched shared path exceeds the old noise law: interpreter ThreeTracks Forward read 18.969 then 21.889 ns for the same binary (+13.1%). Treat ±13% as the cross-process band on ~20 ns arms before flagging; the per-process ratio remains the decision statistic.
