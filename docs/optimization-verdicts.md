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
