# Kernel warm-path performance (issue #85)

This records the multi-row kernel warm-path optimization and the real-world
cost model measured around it: what one call costs, what one row costs, and
how the library compares against plain ECS iteration at the shapes that
decide those numbers. All numbers come from the same machine; see
[Methodology](#methodology) before comparing elsewhere.

The optimization ledger entry lives in
[optimization-verdicts.md](optimization-verdicts.md); Alpha suite mechanics
live in [benchmarks.md](benchmarks.md).

## The change

Four moves, all inside the generated kernel and its dispatch seam:

1. **Range-batched consumers.** `PairTable.Consumer` carries a `Range`
   function pointer next to `Execute`. A single-consumer chain emits one
   `calli` per contiguous same-(tick, cycle, flags) row run instead of one
   per row. Multi-consumer chains and consumers without a registered range
   fall back to per-row dispatch unchanged.
2. **Lockstep memoization.** `TickMixed` caches one `TimelineMovement.Select`
   result for rows sharing (position, cycle); `TickUniform` handles an
   entirely uniform row set with one `Select`, one consumer run over
   `[0, rowCount)`, and constant commit words.
3. **Fused validation.** `KernelTick` no longer pre-scans row addresses in
   managed code. The generated `Tick` probes addresses and uniformity in one
   pass and returns `false` to fall back to the interpreter before any
   effects, so a mutated asset still produces interpreter-exact output.
   Registered kernels therefore return `bool` (`Register` signature change;
   generated kernels are regenerated, no hand-written caller exists).
4. **Single-row dispatch first.** `rowCount == 1` is tested before the probe
   and its body is emitted inline in `Tick`, preserving the pre-change
   single-row codegen shape. Two emitter iterations were needed: a separate
   `TickSingle` call behind the probe cost a constant ~2 ns/tick and showed
   up as +15-25% on the Alpha minimal lane before inlining removed it.

The baked asset format is unchanged: re-baked TLBs are byte-identical,
committed content hashes still bind, and the fixture receipt test regenerates
every committed kernel byte-identically (`TL_KERNEL_REGEN=1` rewrites the
fixtures from the receipt test; the default run still asserts).

## Receipts

Same-machine medians, 100,000-row movement workload (one track, one clip
covering `[0, 64)`, looping, consumer fires every tick, 200 ticks per
iteration, delta = 1, median of 15 after 5 warmups):

| Contender | before | after |
|---|---:|---:|
| tl kernel arm (hash-bound) | 790 us/tick | ~195 us/tick (**4.1x**) |
| tl interpreter arm | ~1,600 us/tick | ~1,600 us/tick (unchanged by design) |
| Friflo chunk loop (context) | — | ~125 us/tick |
| Frent chunk loop (context) | — | ~181 us/tick |

Effects are checksum-identical across all four. The kernel arm retains 0 B
warm-path allocation. The measured floor for the same consumer work with no
timeline state at all (a batch-compiled kernel walking `count` rows with one
`calli`) is ~67 us/tick; the remaining delta to 195 us is timeline state
traffic (probe scan, per-row position/cycle commit) that an ECS does not
carry.

Alpha suite, 61 benchmarks, pristine `b8bcb07` vs candidate, compared through
BenchmarkDotNet's in-run baseline-relative ratios because tiny lanes bounce
up to +/-40% between identical binaries on this machine:

- Interpreter, select-only, and no-dispatch lanes: mean +0.6%, inside the
  noise band — no interpreter code changed.
- Kernel-bound facade, 256 tracks forward: 1,251 -> 1,107 ns/tick. 256-track
  no-dispatch forward -20%, select-only 256 alternating -18% (the fused
  validation and `GetTable` early-out pay off even at one row).
- Minimal single-track write-only lane: 13 -> 15 ns/tick. The residual ~2 ns
  is the `bool` return plumbing and the larger `Tick` frame; it is constant,
  shape-independent, and only visible on one-row minimal lanes.

## The cost model

Every query-shaped call costs `entry + items x per-item`. The entry cost is
paid once per call no matter how many items are inside it.

| Call | entry | per-item |
|---|---:|---:|
| tl `Tick` kernel (uniform rows) | ~360 ns | ~1.9 ns/row |
| tl `Tick` kernel (all rows at distinct points) | ~360 ns | ~12.7 ns/row |
| tl `Timeline.Query<,>(in row)` read view | ~182 ns | scales with frames yielded |
| Frent cached chunk query | ~576 ns | ~1.8 ns/entity |
| Friflo chunk query | ~883 ns | ~1.2 ns/entity |

Consequences, measured at both extremes:

- **One item per call** (the README boss pattern, single-timeline reads):
  entry cost decides. A warm Read+Write `Tick` on one row runs 751 -> 368
  ns/tick (2x from this change); the read-only view is ~182 ns/call against
  Frent's 576 ns and Friflo's 883 ns for the same one-entity loop shape.
- **1M items in one call**: per-item cost decides and the entry cost
  vanishes (0.03-0.2%). tl in lockstep (~1.9 ms/pass), Frent plain
  `X++` (~0.25 ms/pass), tl with all 1M rows at distinct timeline points
  (~12.7 ms/pass = 12.7 ns/row).
- **The 12.7 vs 0.2 ns gap is semantics, not overhead**: Frent's plain
  increment runs at RAM speed and does nothing else; tl's mixed-path row
  cost buys the state machine (am-I-active selection, frame computation,
  cycle/commit bookkeeping, per-row dispatch with no batching possible when
  every row is at a different point). Uniform rows collapse that work back
  to 1.9 ns/row because the kernel recognizes the lockstep.

## Known residuals and follow-ups (pre-existing, unchanged by this work)

- **Cold bind per query construction: ~4.5-6.8 us.** The warm cache lives in
  the `ref struct` query, so constructing a query and ticking once pays
  hashing + resolve every time. Rebuilding 1,000 single-row queries every
  frame costs ~4.2-4.6 us per timeline-tick; building one query once and
  ticking it is the intended shape. Hoisting the cache out of the per-frame
  query construction is the highest-value follow-up found during this work.
- **Multi-column fixed cost: ~300 ns/call** over the minimal write-only
  lane, visible only at tiny row counts; identical before and after.
- **Read view at ~182 ns/call** was never on a benchmark before; kernel and
  interpreter assets measure identically because the view never dispatches
  through kernels.

## Methodology

- Machine: AMD Ryzen 5 8500G (6C/12T), Windows 11 23H2, .NET SDK 10.0.401,
  runtime 10.0.12, workstation GC, x64.
- Alpha suite: BenchmarkDotNet 0.15.8 `InProcessNoEmitToolchain`, 16 warmups,
  12 iterations, 250 ms, `TL_ALPHA_IN_PROCESS=1`.
- Cross-library scenarios: a Stopwatch harness (median of 15 after 5 warmups)
  referencing the same Tl.Core build, Frent @ `c58a601`, and a local
  Friflo.Engine.ECS @ `8e75b6b4`. Reference comparisons name their versions
  because Frent/Friflo numbers move with their upstream.
- Nothing else may run concurrently: one candidate run was discarded after
  parallel builds inflated every lane ~60-70%, including untouched baselines.
- Before/after used a pristine `git worktree` of `b8bcb07` and identical
  commands; the interpreter arm is the in-run control and did not move.
