# Algorithm and dispatch verdicts

The latest correctness and playback performance review is in [review.md](review.md).
The tables below record the earlier experiments. The generated fixture's duration
has since been corrected from 515 to 600 ticks, and playback now handles leading
gaps and terminal ticks. Use the new review results for the updated movement path.

Two questions, two benchmark suites, both with verified-equivalent results
(812,544 exact trace comparisons against an oracle; dispatch receipts checked
including mutation semantics):

1. Which region-navigation algorithm should `tl` use? (`benchmarks/Algorithms`)
2. Does the interface/hook consumption route cost anything? (`benchmarks/Dispatch`)

Environment: i9-14900K pinned to one core, .NET 10.0.11, BenchmarkDotNet 0.15.8,
16 warmups / 12 iterations / 250 ms. Algorithm artifacts live under
`benchmarks/Algorithms/results/validated/` (`environment.json` records the exact
source hashes and machine state); the dispatch suites' tables live under
`benchmarks/Dispatch/results/validated/` and `benchmarks/Dispatch/results/design/`
(same config and core pinning, run during design iterations).

## Dispatch: how hook calls reach user code

A `Receiver` struct implements `ITimelineForward.OnForward(in Frame)`. The
per-call cost of getting that hook invoked, six ways:

| Method              | Ratio (tiered) | Ratio (no tiering) | Allocated      |
|---------------------|---------------:|-------------------:|----------------|
| Direct              |         1.00 × |             1.00 × | -              |
| GeneratedLink       |         1.00 × |             1.00 × | -              |
| Constrained         |         1.00 × |             1.00 × | -              |
| ExplicitConstrained |         1.00 × |             1.00 × | -              |
| CachedDelegate      |         4.01 × |             4.00 × | -              |
| BoxedOnce           |         4.01 × |             6.49 × | one-time box   |
| MixedBoxes          |         4.51 × |             7.51 × | -              |
| EscapingBoxPerCall  |        11.38 × |            16.88 × | 40 B per call  |

- **GeneratedLink** is a generated static method calling the hook; **Constrained**
  is a generic `where T : struct, ITimelineForward` call. The disassembly
  diagnosser shows `Direct`, `GeneratedLink`, and `Constrained` emit **byte-identical
  71 B code** — fully inlined, no dispatch.
- **Boxing silently changes semantics**: an interface call on a boxed struct
  mutates the box copy, not the caller's instance (verified by receipt). Any
  "invisible" wiring that stores receivers as interface references is wrong
  even before it is slow.

**Verdict:** the marker-interface route is free *if and only if* the generator
discovers hooks at generation time and emits direct (or struct-constrained)
calls through `ref` on the caller-owned instance. Delegates, interface
variables, and per-call boxing are forbidden in the playback path.

## Algorithms: region navigation

All six methods produce identical callback sequences and float weights
(including gaps, one-tick overlaps, skipped ticks, and loop resets).
Sizes: Small 8 clips / 15 regions, Medium 64 / 113, Large 512 / 897.

Sequential playback (ns per update, ratio vs binary search):

| Method         | Small step 1 | Medium step 1 | Large step 1 | Large step 7 |
|----------------|-------------:|--------------:|-------------:|-------------:|
| Binary         |   3.17 (1.00) |    4.13 (1.00) |   6.06 (1.00) |  10.73 (1.00) |
| Dense          |   1.02 (0.32) |    1.20 (0.29) |   1.18 (0.19) |   1.36 (0.13) |
| Rank           |   1.44 (0.46) |    1.51 (0.37) |   1.54 (0.25) |   1.76 (0.16) |
| GeneratedTree  |   1.50 (0.47) |    1.96 (0.47) |   3.04 (0.50) |   3.85 (0.36) |
| Cursor         | **0.87 (0.27)** | **1.05 (0.25)** | **0.98 (0.16)** | **1.23 (0.11)** |
| GeneratedState |   0.85 (0.27) |    1.79 (0.43) |   9.05 (1.49) |  15.39 (1.43) |

Random seeking (ns per lookup):

| Method         | Small        | Medium       | Large          |
|----------------|-------------:|-------------:|---------------:|
| Binary         |  13.80 (1.00) |  47.39 (1.00) |  71.36 (1.00)   |
| Dense          | **2.56 (0.19)** | **3.68 (0.08)** | **4.08 (0.06)** |
| Rank           |   4.18 (0.30) |   5.79 (0.12) |   5.39 (0.08)   |
| GeneratedTree  |  14.39 (1.04) |  24.75 (0.52) |  46.55 (0.65)   |

Memory for the Large fixture (512 clips, 65,536 ticks, 897 regions):
regions 21.5 KB · dense 131.1 KB (2 B/tick) · rank 10.2 KB (~1.25 bits/tick).

Readings:

- **Cursor wins playback at every size** (~0.9–1.2 ns) and is immune to size.
  The generated switch state machine ties it only at Small and is *worse than
  binary search* at Large (9–15 ns): the `goto case` skip chain plus 654 KB of
  generated code defeat the I-cache and branch predictor.
- **Branch-free lookup wins seeking decisively.** Binary search collapses from
  6 ns (predictable, sequential) to 71 ns (random) purely through branch
  misprediction — comparison searches are the wrong shape for arbitrary seeks.
- **Generated comparison trees lose everywhere** — slower than a 10 KB
  bitvector at every size and they scale with timeline length. Dropped.
- **Rank is the compact default** for seeks: within ~1.3 ns of dense at 13× less
  memory. Dense (2 B/tick) is justified only for small, hot timelines.

## Recommended architecture

Specialize *computation*, keep *navigation* in data:

- Per-region bodies stay generated (fixed payload references, fixed blend
  formula, direct hook calls) — partial evaluation pays here and the dispatch
  suite proves the calls are free.
- The playback cursor stays a data-driven region index advanced monotonically —
  it *is* the state machine, without generated dispatch.
- Arbitrary seeks resolve through the rank bitvector (or a dense table when the
  timeline is small and hot).

## Consumption surface

The validated shape is a generic shell — `Timeline<TTrack, TClip, TData>` —
with **one callback per tick** and **blending resolved before the consumer can
observe it**. Each active track exposes exactly one clip: an original, or the
`IBlend` collapse of an overlapping pair:

```text
Timeline determines active clips and the blend factor
        → IBlend<TClip>.Blend(in a, in b, factor, out C')
        → IForwardTracks sees exactly ONE C per active track
        → ref D
```

```cs
public interface IBlend<TClip>
    where TClip : struct
{
    void Blend(in TClip first, in TClip second, float factor, out TClip result);
}

public interface IForwardTracks<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Forward(uint tick, in Tracks<TTrack, TClip> tracks, ref TData data);
}

Timeline<HealthTrack, HealthClip, Player>.Forward(ref player, t0, t1, t2, t3);
```

The consumer is blend-ignorant by construction:

```cs
foreach (var item in tracks)
{
    data.Ticks += item.Track.Offset;
    data.Health += item.Clip.Amount;   // exactly one resolved clip — no weights
}
```

`Tracks` is a small `readonly ref struct` view over the region's slice
of generated CSR tables (`RegionRow → TrackRow → ClipRow`). `item.Track` and
`item.Clip` are `ref readonly`; standalone clips point straight into the
payload table (never copied), blend results into one hoisted
`stackalloc TClip[MaxActiveTracks]` buffer per call. Two implementation rules
came out of measuring the naive version (1.61×):

1. **Fetch static-abstract tables once per call.** `TTrack.TrackRows` etc. are
   generic-dictionary indirections; reading them per track cost 0.2×.
2. **Fuse resolution into traversal.** Blend inside `MoveNext`, not in a
   separate eager pass — the rows are walked once, and items the consumer
   never visits are never blended.

Final numbers vs a hand-written fused pipeline (identical receipts, 12 regions
incl. gaps and two blend pairs):

| Path                       | Tiered | No tiering | Allocated |
|----------------------------|-------:|-----------:|-----------|
| Direct pipeline (baseline) |  1.00 × |     1.00 × | -         |
| Shell, one tick per call   |  1.42 × |     1.15 × | -         |
| Shell, four ticks per call |  1.16 × |     1.06 × | -         |

Multi-tick calls (the primary usage) sit at 1.06–1.16×; the single-tick path
pays per-call machinery the batching amortizes — the generator's concrete
emission covers that if it ever matters. Ticks are `uint`; `params
ReadOnlySpan<uint>` stackallocs the tick list. The `stackalloc` resolution
buffer requires `TClip : unmanaged`; managed payload types can use generated
`[InlineArray]` locals instead.

### Runtime-authored instances

`Timeline<TTrack, TClip, TData>` also exists as a runtime builder:
`new Timeline<...>()`, `AddTrack(in TTrack)`, `AddClip(track, in TClip, start,
end)`, `Build()` — an event sweep turns clip edges into region boundaries and
emits the same CSR tables the generator would. Each closed generic type
assigns sequential `ushort Index` values, so one player can hold several
timeline instances. The static flavor is `GeneratedTimeline<...>`.

Measured against the same baseline (identical receipts — the built tables are
byte-equivalent in content to the static fixture):

| Path                       | Tiered | No tiering | Allocated |
|----------------------------|-------:|-----------:|-----------|
| Runtime instance, 1 tick   |  1.42 × |     1.23 × | -         |
| Runtime instance, 4 ticks  |  1.27 × |     1.09 × | -         |
| Generated static, 4 ticks  |  1.20 × |     1.10 × | -         |

Runtime authoring costs essentially nothing at playback time — instance array
loads hoist the same way static-abstract fetches do. Index assignment is
construction order (fine for runtime use; use explicit indices if save/replay
determinism ever requires stable ids).

### Playback state and direction

Direction is a method, not a subtype. Four hooks cover everything; the
enter/exit/looped/start/stop callback family is replaced by bits:

```cs
public interface IForward<TClip, TData>      // clip-level, simple consumers
    { void Forward(in TClip clip, uint tick, ref TData data); }
public interface IBackward<TClip, TData>     // its mirror
    { void Backward(in TClip clip, uint tick, ref TData data); }
public interface IForwardTracks<TTrack, TClip, TData>
    { void Forward(uint tick, in Tracks<TTrack, TClip> tracks, ref TData data); }
public interface IBackwardTracks<TTrack, TClip, TData>
    { void Backward(uint tick, in Tracks<TTrack, TClip> tracks, ref TData data); }
```

`Playback` is an 8-byte blittable value: `uint Tick` plus one packed word of
26-bit `Cycles` and six flags — `Enter`, `First`, `Active`, `Last`,
`Complete`, `Exit`. It flows `in` and comes back by value, so state is data
you can snapshot: a rewind ring of `(Playback, Player)` pairs *is* the
Prince-of-Persia save format, and replay is the initial value plus logged
ticks. Nothing hides inside a timeline object.

```cs
var pb = Playback.Start();                                    // positions silently
pb = timeline.Forward(in pb, ref player, t0, t1, t2);         // each tick = one step
pb = timeline.Backward(in pb, ref player, t);
if (pb.Has(PlaybackFlags.Complete)) ...
```

Semantics (all receipt-verified, including a forward-walk-then-rewind that
restores the player exactly):

- `First`/`Last`/`Active` are positional: facts about the destination tick.
  `Enter`/`Exit` are facts about the step; a jump that fully skips a clip
  shows `Enter | Exit` on the same word — nothing crosses silently.
- Backward mirrors: enter through the clip's right edge, exit through its
  left. A repeated tick re-samples without re-firing movement flags.
- Looping timelines (`Loops`/`IsLooping`) wrap ticks modulo duration; each
  crossed boundary moves `Cycles` (forward adds — multi-cycle jumps count
  exactly; backward subtracts and saturates at zero). Non-looping timelines
  set `Complete` at their far end instead.
- `Tracks.Status` carries the same bits inside the per-tick callback.

The physics of the 8 bytes: `in`, by-value, and `ref` passing of `Playback`
are indistinguishable (0.527/0.528/0.527 ns for a step-and-return micro;
the tiered JIT deletes the call outright). The real cost is computing
movement facts. The first implementation scanned the authored `ClipEdge`
table per step and cost 3.9× direct. The fix falls out of the table shape:
clip starts and ends are always region cuts, so three precomputed bits per
region (`starts on a clip start`, `starts on a clip end`, `ends on a clip
end`) answer every flag — positional facts from the destination region,
movement facts from the boundaries crossed between the two positions. Only
wraps touch the edge table.

| Path                                | Tiered | No tiering | Allocated |
|-------------------------------------|-------:|-----------:|-----------|
| Stateless shell, 1 tick             |  1.37 × |     1.13 × | -         |
| Playback + flags, 1 tick            |  1.97 × |     2.41 × | -         |
| Playback + flags, 4 ticks           |  1.77 × |     2.15 × | -         |
| Playback backward, 1 tick           |  2.01 × |     2.45 × | -         |
| Clip-level hooks, 1 tick            |  1.86 × |     2.33 × | -         |

Measured on random jump queries — the worst case for movement facts (every
step binary-searches the previous region and walks the crossed boundaries);
sequential playback crosses at most one boundary per step. The gap over the
stateless shell (~6.5 ns/tick) is the flag machinery plus the `Playback`
churn; receipts confirm sampling is bit-identical to the stateless path.

## Sparse index dispatch

`Timeline.Index` is a sparse `ushort`. Six dispatch strategies over random hit
queries (`benchmarks/Dispatch/Generated/Sparse*.g.cs`, all arms receipt-verified
against each other):

| Method                       | 256 indices | 4096 indices |
|------------------------------|------------:|-------------:|
| Binary search                |    24.1 ns  |     36.4 ns  |
| Linear chain                 |    18.4 ns  |  1,031.3 ns  |
| Flat sparse switch (JIT pick)|    17.9 ns  |     49.3 ns  |
| Radix switch 8+8 (generated) |     9.6 ns  |     19.9 ns  |
| Radix switch 4×4×4×4         |    16.2 ns  |     25.2 ns  |
| Dense `delegate*` table      |     6.0 ns  |     11.2 ns  |

Readings:

- **The generated radix idea works and 8+8 is the shape**: fixed depth, immune
  to count, 1.8–2.6× faster than binary search at both scales. 4×4 loses —
  every extra level is another mispredictable indirect branch.
- **The flat sparse switch is not reliable**: the JIT's own lowering beat
  binary at 256 entries and then fell *behind* it at 4096. Compiler choice is
  not a strategy; generate the structure.
- **The floor for random dispatch is ~6 ns, not <1 ns**: even the raw
  function-pointer jump table pays an indirect-branch misprediction per
  random hit (~26 cycles at 4.4 GHz). Sub-2 ns dispatch exists only for
  predictable streams (same timeline, sequential ticks), where predictors and
  caches are warm — which needs a separate end-to-end timeline-dispatch benchmark; the cursor suite measures region selection and sampling.

Recommended thresholds: a handful of timelines → plain comparisons; sparse and
safe-code-only → generated radix 8+8; maximum speed where unsafe is acceptable
→ dense `delegate*` table (512 KB for the full `ushort` space, or two-level
256-page tables for sparse footprint). Region lookup (tick → region) is a
*dense* problem and keeps the cursor/rank answers from the algorithm suite.

### One huge switch over all timelines?

The same query with a per-timeline body (three range comparisons + a visit),
comparing dispatch-then-call against fused megaswitches
(`benchmarks/Dispatch/Generated/Fused*.g.cs`):

| Method                        | 256 timelines | 4096 timelines |
|-------------------------------|--------------:|---------------:|
| `delegate*` table + call      |      8.13 ns  |       13.86 ns  |
| Huge switch, **sparse** keys  |     26.90 ns   |       52.23 ns  |
| Huge switch, **dense** keys   |      7.39 ns  |       16.09 ns  |

Sparse keys are 3.3–3.8× *slower* than dispatch-then-call — the JIT lowers a
huge sparse switch to comparison trees that mispredict (third independent
measurement of this). Dense keys + inline bodies win modestly at ~256
timelines (jump table straight into inline code, no call) but lose at
thousands: the multi-megabyte method's I-cache footprint outweighs the saved
call. The `delegate*` table keeps hot code compact at any scale.

Where the JIT genuinely delivers: constrained-call devirtualization, jump
tables over dense ranges, inlining small methods, and PGO. Where it does not:
huge sparse switches and megamethods. Generate the structure; keep bodies
small.

Hard rules from the dispatch suite still apply:

1. Hooks must be abstract — **no default interface bodies**. A non-overridden
   default boxes per call (measured 10.9–11.4×, 24 B/call) and silently loses
   mutations (the default body writes to the box). The generator emits calls
   only for hooks a type actually declares, so opting out costs nothing.
2. No delegates, no interface variables, no boxing in the playback path
   (4.0–4.5× and copy semantics).

Static-abstract polymorphism over timelines (`interface ITimeline` implemented
by generated tracks) remains available for utilities that are generic over
timelines — same devirtualization machinery, zero cost.

### Frozen path (work in progress)

The `Generate` project now emits two **frozen** timelines into
`benchmarks/Dispatch/Generated/` — per-timeline playback code specialized at
emission time against tables the generator reads directly (`Generate.csproj`
compiles `Hooks.cs`, so the fixture tables have one source of truth):

- `VitalsFrozen.g.cs` — the `VitalsTrack` fixture (duration 600, 13 region
  starts = 12 regions plus the empty sentinel, 4 tracks, blends and gaps).
- `Fused16Frozen.g.cs` — a synthesized single-track, 16-clip fixture in the
  Review/Movement shape: clip `i` covers `[i*4, i*4+3)` with value `i+1`,
  duration 63, the 4th tick of each group is the gap. The sub-1 ns gate arm.

What the frozen form specializes away from `PlaybackCore`: in the
**full-bake dense LUT mode** (picked when duration is ≤ 1024) the entire
per-tick leaf is precomputed into ONE interleaved `ulong` word per tick per
direction — `s_tickF`/`s_tickB` for single-slot fixtures, `s_wordF`/
`s_wordB` plus the float slot tables for multi-slot ones. The word packs
bits 40..47 = the positional flag byte (`Active | First | Last | Complete`,
shifted `<< 27` into the `PlaybackFlags` word — two tables per fixture
because `Complete` is direction-dependent: forward fires from duration−1 on
and past the duration, backward only exactly at tick 0), bits 48..55 =
`startRank` and bits 56..63 = `endRank` (the cut counts at or below the
tick — the movement-fact ranks ride along for free), and, single-slot
fixtures only, bits 0..31 = the blend-resolved track value's float bits,
unpacked with `BitConverter.Int32BitsToSingle((int)word)` (a JIT
intrinsic). Multi-slot fixtures keep `s_slot0..s_slotN` (one table per
active-track slot in track-row order, `0f` where the slot is inactive — an
exact no-op in the addition order the oracle performs) as independent
stream loads beside the word. The generator folds the exact runtime
expressions — `(tick - factorStart) / (float)(factorLength - 1)` (`0.5f`
for a one-tick window) run through `first * (1f - f) + second * f`, all in
`float` — so parity holds bit-for-bit. Per tick the loop does: a clamped
index (out-of-range ticks clamp to the sentinel word at the duration — the
empty region, whose packed ranks are the total cut counts, asserted at
emission), ONE word load, a shift unpack, **branchless movement** — the
step fact `tick > prev ? 1u : 0u` materializes as a `setcc` that `0u -
moved` broadcasts to a mask, and both rank diffs are `-1/0` sign masks
(`(prevRank - rank) >> 31`, exact because the packed ranks are 0..255 so
the subtraction cannot overflow) AND-ed with the `Enter`/`Exit` bits — with
the previous tick's ranks carried in locals (seeded by one clamped load
before the loop), so there are no rank loads at all; and a branchless
`Count` (the `Active` bit `>> 28`, not a conditional increment). No
per-tick blend math, no flag-assembly branches, **no data-dependent
branches at all** (disassembly receipt below), and no region switch
(`s_region`, the jump-table switch, and the leaf bodies all died); exactly
one `Playback` construction remains, after the loop — the loop carries
`prev` / `cycles` / `flags` / the two prev-rank locals, valid because
frozen timelines never loop, which makes the ctor's cycle-capacity throw
unreachable (commented in the generated files). Timelines longer than
1024 ticks keep the binary branch trees over the starts and cut
boundaries, rank trees for movement, and immediate-payload leaves. The
frozen form is bound to one exact non-looping timeline: no wraps, no
cycle arithmetic (`Cycles` passes through), no duration-0/empty handling.

Parity receipts (`Dispatch --verify`, oracle = `PlaybackCore` through the real
tables: `OracleVitals`, and hand-set `Fused16Track` literals cross-checked
tick-by-tick against the raw clip list): sequential walks 0..duration+10 with
exact float sink equality, a 24-pair deterministic jump battery in and out of
range (forward and backward), forward/backward mirrors that land both sinks
back on their initial values (`Count` and `Flags` exactly zero, `Sum` within
float cancellation), one 8-tick batch call vs eight single calls, and
flag-count totals over the full walk (frozen vs oracle, nothing hardcoded).

Accumulation contract mirrored bit-for-bit by both sides, per tick, in order:
`Sum +=` one blend-resolved clip value per active track in track-row order,
`Flags += (uint)status`, then `Count++` for ticks with at least one active
track; `Backward` subtracts in the same order (the exact inverse — which is
why the mirror receipt can demand exact restoration of `Count` and `Flags`).

Bake v1 measurement (`--filter '*Frozen*'`, pinned to one core, medians,
ns per tick, old = the dense-LUT switch shape before the full bake,
new = the full-bake tables + batch `Playback` skip; the ApiShape
reference `PlaybackSingle` on the same Vitals fixture is 21.5 ns with
random ticks):

| Method                 | Random Jit    | Random NoTiering | Sequential Jit | Sequential NoTiering |
|------------------------|--------------:|-----------------:|---------------:|---------------------:|
| `FrozenVitalsSingle`   |  9.62 →  5.08 |   11.57 →  6.20   |  3.40 →  2.22  |   4.73 →  3.26        |
| `FrozenVitalsBatch8`   |  8.45 →  4.93 |   10.59 →  5.87   |       —        |         —             |
| `Fused16Single`        | 12.60 →  5.07 |   14.33 →  6.21   |  3.07 →  1.89  |   4.96 →  3.28        |
| `Fused16Batch8`        | 11.42 →  4.50 |   13.11 →  5.52   |       —        |         —             |

Table cost: Vitals bakes 3 slot tables (601 floats each, 7.2 KB) plus
two 601-byte flag tables and the two rank tables — ≈ 9.4 KB of runtime
data; the generated source grew 23.0 → 29.7 KB. Fused16 needs one slot
table and two 64-byte flag tables (0.5 KB); its source shrank
17.3 → 6.4 KB because the 32-case switches died.

Readings, honestly:

- The full bake plus the batch `Playback` skip moved everything:
  `Fused16Batch8` 11.42 → 4.50 ns/tick (2.5x), `Fused16Single`
  12.60 → 5.07, Vitals random 9.62 → 5.08 — now 4.2x over the generic
  engine on random jumps and 9.7x on sequential streams (was 2.3x /
  6.2x), still 0 B allocated. The sequential arms dropped to
  1.89 (Fused16) / 2.22 (Vitals) ns/tick.
- The sub-1 ns gate (`Fused16Batch8` < 1.0 ns/tick tiered) was **missed**:
  it landed at 4.50 ns/tick. The remaining per-tick work, in loop order:
  span iteration (pointer bump + bounds check), the clamp cmov, the
  flag-byte load + shift, the movement gate `tick > prev` — a coin toss
  on a random tick stream, mispredicted about half the time at ~15–20
  cycles a piece — four guarded rank loads and two rank-diff branches
  inside that gate, the float slot add(s), and the `Active` branch for
  `Count`. The sequential arms price the memory path itself: 1.89 ns/tick
  with the movement gate perfectly predicted and always taken. The gap
  (4.50 − 1.89 ≈ 2.6 ns/tick) is branch mispredicts on the movement
  facts, not table traffic; the next lever is a branchless movement
  encoding (setcc/cmov rank-diff to flag bits instead of
  compare-and-OR), deliberately untouched here.
- Tiered PGO matters more now, and `AggressiveInlining` on the frozen
  entry points is load-bearing: without the hint the shrunken bodies
  stopped folding through callers (the hub arms paid +2.6…+4.0 ns/tick
  and the NoTiering sequential arms regressed ~1.5–1.9 ns); with it,
  every arm on both jobs improves over the old shape.
- The sub-1 ns frozen receipts from the sibling LUT repo (0.165 ns
  sampling, 1.33 ns one-tick traverse) measured a repeated single tick
  and pure sampling, without per-tick `Playback` movement facts on
  varied ticks; this path is not there yet. 1.9–2.2 ns/tick sequential
  and ~4.5–5.1 ns/tick random (Fused16/Vitals) with full movement
  semantics was the proven floor at the full bake; the branchless +
  interleaved pass below moved it.

#### Bake v2: branchless movement + interleaved single-load tables

The v1 receipts left one branch class standing, and it was the whole gap:
the movement gate `tick > prev` — a ~50% mispredicted coin toss on random
streams — around four guarded rank loads and two rank-diff branches.
v2 deletes every data-dependent conditional from the hot loop, two ways:

1. **Interleaved single-load bake.** Everything the tick needs now lives
   in one `ulong` (the flag byte, both ranks, and — single-slot fixtures —
   the value's float bits), and the previous tick's ranks ride in locals
   seeded by one clamped load before the loop. Six loads per tick became
   one (multi-slot: one plus one per slot).
2. **Branchless movement arithmetic.** `moved` is the `setcc` 0/1 that
   `0u - moved` broadcasts to a mask; each rank diff is
   `(prevRank - rank) >> 31`, a `-1/0` sign mask — exact because the
   packed ranks are 0..255, so the subtraction cannot overflow and bit 31
   *is* the comparison — AND-ed with the `Enter`/`Exit` bits and OR-ed
   over the shifted flag byte. `Count` is the `Active` bit shifted out
   (`>> 28`). (A first cut kept the rank diffs as ternaries;
   `cond ? (uint)PlaybackFlags.Enter : 0u` compiled as `jle` diamonds —
   real branches — which is why the sign-mask form shipped. The `? 1u :
   0u` step mask *did* compile to `setcc`+`movzx`, as predicted.)

Disassembly receipt (FullOpts, no tiering, direct-call probe around
`Fused16Frozen.Forward`): per tick — one table load, six unpack
shifts/`movzx`s, `seta`+`neg` for the step mask, two `sub`/`sar`/`and`
pairs for the rank diffs, the three sink read-modify-writes (`add` qword,
`vaddss`, `add` dword), the rank carries as register moves — and exactly
two conditional branches: the constant clamp (`tick < D`, never taken on
in-range streams) and the loop's own increment. Zero data-dependent
branches per tick.

Bake v2 measurement (same `--filter '*Frozen*'`, core 4, medians, ns per
tick, old = bake v1 above; artifacts under `/tmp/bake2`):

| Method                 | Random Jit    | Random NoTiering | Sequential Jit | Sequential NoTiering |
|------------------------|--------------:|-----------------:|---------------:|---------------------:|
| `FrozenVitalsSingle`   |  5.08 →  2.65 |    6.20 →  3.20   |  2.22 →  2.49  |   3.26 →  3.21        |
| `FrozenVitalsBatch8`   |  4.93 →  1.96 |    5.87 →  2.21   |       —        |         —             |
| `Fused16Single`        |  5.07 →  2.12 |    6.21 →  2.51   |  1.89 →  2.16  |   3.28 →  2.51        |
| `Fused16Batch8`        |  4.50 →  1.54 |    5.52 →  1.70   |       —        |         —             |

Table cost: Fused16 now bakes two 64-entry `ulong` tables (1.0 KB runtime
data, was 0.5 KB; source 6.4 → 8.4 KB). Vitals bakes two 601-entry words
(9.6 KB) plus the unchanged 3 slot tables (7.2 KB) = 16.8 KB (was 9.4 KB;
the rank and flag tables folded into the words); source 29.7 → 43.2 KB.

Readings, honestly:

- The gate (`Fused16Batch8` < 1.0 ns/tick tiered) was **missed** again:
  1.54. But the v1 diagnosis proved out exactly — the random-vs-sequential
  gap, the mispredict signature, collapsed from 2.61 ns (4.50 vs 1.89) to
  zero (2.12 vs 2.16; statistically identical streams). Random
  `Fused16Batch8` at 1.54 now beats v1's *best* arm, sequential 1.89.
  Every random arm improved 2.5–3.0×: Fused16Batch8 4.50 → 1.54,
  VitalsBatch8 4.93 → 1.96, singles ~5.1 → ~2.1–2.6.
- The sequential arms paid a small toll (Fused16 1.89 → 2.16, Vitals
  2.22 → 2.49, Jit): v1's perfectly-predicted branches were cheaper than
  mask arithmetic on ordered streams. That is the trade — the worst case
  (random) now equals the best case; no stream shape is left that
  mispredicts.
- Where the remaining 1.54 ns lives: the loop is instruction-throughput
  bound. Roughly fifty uops per tick (tick load, clamp pair, bounds
  check, word load, unpack, `setcc`+`neg`, two `sub`/`sar`/`and`/`and`
  chains, three sink RMWs, three carry moves, increment/compare/branch)
  at ~6 uops/cycle sustained ≈ 8–9 cycles ≈ 1.5 ns at ~5.3 GHz. The next
  instruction-level costs, in order: the per-tick bounds check and
  GC-static base reload (removable only via unsafe bounds-free
  indexing), the branchy clamp diamond (the JIT emitted `cmov` for the
  pre-loop clamp but not the in-loop one), and the movement mask
  arithmetic itself (~14 uops; a `(prev, tick)` movement LUT would delete
  it but is O(duration²) — 360 KB for Vitals).
- The parity-pinned floor: `sink.Sum`'s accumulation order is exact float
  order — one serial `addss` per active track per tick; splitting into
  multiple accumulators changes float ORDER, which parity forbids. For
  Fused16 (one active track) that chain is 3–5 cycles ≈ 0.6–0.95 ns at
  ~5.3 GHz: the hard floor, unreachable until the instruction stream
  sheds another ~35–40% of its uops, and hard-capped there. For Vitals
  (three chained `addss` per tick ≈ 1.7–2.3 ns depending on FADD
  latency) the chain is *already* the binding constraint —
  `FrozenVitalsBatch8` at 1.96/2.21 sits on it, and no instruction-cut
  can move Vitals at all. Net: ~0.9 ns/tick is the hard parity floor of
  this contract for a single active track; 1.54 is the current proven
  point, throughput-bound ~0.6 ns above the floor.

#### NativeAOT: the no-tiering, no-PGO column

Does the frozen story survive a compilation model with no tiering and no
PGO, ever? BenchmarkDotNet cannot answer that: `dotnet publish
benchmarks/Dispatch -c Release -r linux-x64 /p:PublishAot=true` dies in ILC
on the BDN dependency tree (load-bearing subset, verbatim):

```
BenchmarkDotNet.dll : error IL2104: Assembly 'BenchmarkDotNet' produced trim warnings.
CommandLine.dll : error IL2104: Assembly 'CommandLine' produced trim warnings.
ILC : error IL3000: BenchmarkDotNet.Running.BenchmarkPartitioner...: 'System.Reflection.Assembly.Location.get' always returns an empty string for assemblies embedded in a single-file app.
ILC : error IL3053: Assembly 'BenchmarkDotNet' produced AOT analysis warnings.
Microsoft.CodeAnalysis.dll : error IL2104: Assembly 'Microsoft.CodeAnalysis' produced trim warnings.
```

and BDN's default toolchain would only spawn generated JIT child projects
anyway. The AOT column therefore comes from a **manual harness**:
`benchmarks/AotBench`, a package-free console project compiling the same
`Hooks.cs` and the same generated frozen fixtures, published with
`dotnet publish -c Release -r linux-x64 /p:PublishAot=true /p:IlcInstructionSet=native`
(.NET SDK 10.0.400, ILC 10.0.11, this machine's ISA — like the JIT sees).
Every arm body and the tick generation are copied verbatim from the Frozen
benchmark class (seed `0x6D2B79F5`, 65536 ticks per sample, durations
63/600); a sample times one full pass with `Stopwatch`, the number is the
median of 512 samples per round, median across 5 rounds, 2 s warmup first.
The harness runs unchanged under the JIT, so its JIT columns cross-check
the BDN medians before any AOT comparison: Fused16Batch8 1.512 vs 1.54,
Fused16Single 2.110 vs 2.12, Fused16Sequential 2.155 vs 2.16,
VitalsBatch8 1.921 vs 1.96 (VitalsSingle 2.470 vs 2.65 — the harness reads
that one 0.18 ns faster than the older BDN run; everything else within
0.05). Correctness under AOT: all six arm checksums are bit-identical to
the JIT output, batch-vs-single stays bit-exact, the forward/backward
mirror still restores the sink exactly — the `params ReadOnlySpan<uint>`
shape and the static-abstract `IFrozen` face are no trouble on .NET 10
NativeAOT.

Measurement (core 10, ns per tick, medians; two AOT process runs agreed
within 0.03 on every arm):

| Method                  | BDN Jit (bake v2, core 4) | Harness Jit tiered | Harness Jit NoTiering | NativeAOT |
|-------------------------|--------------------------:|-------------------:|----------------------:|----------:|
| `Fused16Batch8`         |                      1.54  |              1.512 |                 1.526 |     1.622 |
| `Fused16Single`         |                      2.12  |              2.110 |                 2.103 |     2.605 |
| `Fused16Sequential`     |                      2.16  |              2.155 |                 2.168 |     2.596 |
| `FrozenVitalsBatch8`    |                      1.96  |              1.921 |                 1.920 |     2.235 |
| `FrozenVitalsSingle`    |                      2.65  |              2.470 |                 2.637 |     3.101 |
| `FrozenVitalsSequential`|                      2.49  |              2.432 |                 2.570 |     3.101 |

Readings, honestly:

- The design held. The arm ordering survives intact (batch arms fastest,
  Fused16 under Vitals, everything under the float-chain floor plus toll),
  the batch `Playback` amortization pays exactly the same way (1.62 vs
  2.60 — a 1.0 ns/tick gap, same as the JIT's 1.51 vs 2.11), and the
  branchless movement arithmetic is fully present in the published binary
  (`seta`+`neg`, the `sub`/`sar`/`and` sign-mask chains are all in the
  objdump). The mispredict-immunity held too: AOT random and sequential
  arms are statistically identical (2.605 vs 2.596 Fused16, 3.101 vs 3.101
  Vitals) — no stream shape regressed.
- The toll is uniform and codegen-shaped, not design-shaped: +0.11 ns/tick
  on Fused16Batch8 (+7%) and +0.44…+0.67 on the per-tick-call arms
  (+20–27%). objdump of the published binary shows where it lives: ILC did
  NOT inline `Playback.__ctor` — the single arms pay a real `call` (with
  its own frame) once per tick, plus the per-call prev-word seed reload
  (`s_tickF[clamp(prev)]` re-loaded and re-unpacked every tick instead of
  carried in locals), the params ticks staged through stack slots, the
  `Sum` float accumulator spilled/reloaded per tick (`vmovss` against the
  frame), the branchy clamp diamond, and a per-tick rip-relative `lea` for
  the frozen struct's static base (the tables themselves sit in writable
  BSS as `__FrozenObj_*`, not frozen read-only pages — placement note, not
  a latency claim). The tick-array bounds checks ARE eliminated. The batch
  arms amortize the ctor/seed over 8 ticks, which is why they only pay
  +0.1–0.3.
- This is not "no PGO" biting. The harness's NoTiering JIT column is also
  PGO-less, single-pass FullOpts — and it lands on the tiered numbers
  (1.526 vs 1.512 Fused16Batch8; bake v2 deleted the mispredicts PGO used
  to fix). NativeAOT sits above *both* JIT columns by the same margin, so
  the driver is ILC's inline/register-allocation decisions for this
  call-shaped source, not tiering-forever effects.
- Layout sensitivity is real on these arms: an earlier publish of
  byte-identical hot-loop source (only the harness warmup constants
  differed, shifting code layout) measured 0.08–0.19 ns/tick slower on
  every arm. The numbers above are the final binary, run twice.
- Net: the frozen path is NativeAOT-viable at 1.62/2.24 ns/tick batch and
  2.60/3.10 single — a +7…27% codegen toll, zero allocation (the 48 B in
  the harness log is the six cached delegate thunks), bit-exact parity,
  and no stream-shape cliff. The ".NET x64 JIT only" hedge is retired for
  the frozen path; IL2CPP/WASM stay unmeasured.

**Re-measured after the redesign + bake v3 (be9aa92).** Two things
changed since the table above. First, the redesigned global registry
broke the *publish*: the one-time (entry, consumer) bridge bind closes
`BindData<TData>` via `MakeGenericMethod`, and ILC fails IL2060/IL3050
on it. The frozen arms never bind, so the fix is scoped and honest —
`Timeline<TTrack,TClip>.Bind<TData>(index)` is the new static,
AOT-compiler-visible instantiation path, and the reflection path guards
on `RuntimeFeature.IsDynamicCodeSupported` (a named throw under AOT)
behind scoped suppressions. Second, bake v3's emission — span
collection-expression tables (frozen blobs, no per-tick GC-static base
reload) and pure-integer branchless movement — turns out to be exactly
the shape ILC likes. Back-to-back runs, same core, same harness:

| Arm                  | Harness Jit | NativeAOT | delta  |
|----------------------|------------:|----------:|-------:|
| `Fused16Batch8`      |       1.064 |     1.094 | +0.030 |
| `Fused16Single`      |       1.205 |     1.138 | −0.067 |
| `Fused16Sequential`  |       1.205 |     1.146 | −0.059 |
| `FrozenVitalsBatch8` |       2.502 |     2.528 | +0.026 |
| `FrozenVitalsSingle` |       2.957 |     3.012 | +0.055 |
| `FrozenVitalsSequential` |    2.922 |     2.913 | −0.009 |

All six arm checksums are bit-identical to the JIT output (verified by
direct diff of full checksum sections, not just the harness's inline
checks). **The +7…27% ILC toll is gone** — every delta is within ±0.07
ns with mixed signs, i.e. run noise, and the AOT binary now sits at
1.09 ns/tick on the gate arm. The pre-v3 toll traced to ILC's codegen
for the old emission's shape (stack spills around guarded loads,
GC-static base re-derivation); v3's arithmetic form has nothing left
for ILC to decide differently.

#### The frozen hub: one dispatch call site

`FrozenHub.g.cs` closes the loop the dispatch verdicts opened: ONE call
site dispatching a timeline index into the right generated frozen
timeline. `FrozenHub` exposes `Count` plus `Start` / `Forward` /
`Backward(ushort index, …)`; every method bounds-checks first
(`index >= Count` throws `ArgumentOutOfRangeException` before any work)
and then runs one dense `switch` over contiguous cases `0..Count-1` —
the jump-table shape the dense-switch verdict established holds up to
256 indices and beyond. The index space is the generator's frozen hub
registry (a plain index → type-name list in `Generate/Program.cs`, also
documented in the generated file header):

| Index | Timeline        | Fixture                                            |
|------:|-----------------|----------------------------------------------------|
| 0     | `VitalsFrozen`  | VitalsTrack (duration 600, 4 tracks, blends, gaps) |
| 1     | `Fused16Frozen` | Movement shape (1 track, 16 clips, duration 63)    |

A third frozen timeline joins the registry and the switch grows by one
contiguous case; nothing else changes.

Hub receipts (`--verify`): for every registered index, hub `Start`,
`Forward`, and `Backward` must be identical to the direct static calls
on one shared deterministic walk (ascending runs plus jumps, some past
the duration) — per-step and final `Playback` equality, sink equality
with exact floats, over a forward walk, the reversed backward mirror,
and one 128-tick batch. Out-of-range indices (`Count` and
`ushort.MaxValue`) must throw `ArgumentOutOfRangeException` on all three
methods before touching the sink, proven by a sentinel sink left
bit-identical after the rejected call.

Measured hub-vs-direct deltas (same `--filter '*Frozen*'` run as the bake
v1 table above, medians, ns per tick; negative = hub faster, and anything
under ~0.2 ns is run-to-run noise on these arms):

| Arm pair (Jit / NoTiering medians)                  | Direct              | Via hub             | Delta               |
|-----------------------------------------------------|--------------------:|--------------------:|--------------------:|
| `Fused16Single` ↔ `HubFused16Single` (random ticks) |  5.065 /  6.209 ns  |  4.930 /  6.157 ns  | −0.14 / −0.05 ns    |
| `Fused16Sequential` ↔ `HubFused16Sequential`        |  1.893 /  3.276 ns  |  1.841 /  3.386 ns  | −0.05 / +0.11 ns    |
| `Fused16Batch8` ↔ `HubFused16Batch8`                |  4.496 /  5.517 ns  |  4.498 /  5.507 ns  | +0.00 / −0.01 ns    |
| `FrozenVitalsSingle` ↔ `HubVitalsSingle` (random)   |  5.083 /  6.196 ns  |  5.062 /  6.418 ns  | −0.02 / +0.22 ns    |

Readings:

- One structural note from the full bake: when the frozen bodies
  shrank to table loads, the hub's forwarder layer first *stopped*
  inlining under tiered PGO and cost +2.6…+4.0 ns/tick
  (`HubFused16Sequential` 3.25 → 5.19 in that intermediate state).
  `AggressiveInlining` on the frozen entry points and the hub methods
  restored it — the deltas above are all noise-level again, on both
  jobs, including the tightest 1.8 ns sequential arm.
- The hub's switch sees a per-call-site *constant* index (only the
  ticks randomize), so there is no second random indirect branch to
  mispredict; the mispredict those arms paid lives in the timeline's own
  movement gate, not in dispatch.
- Re-measured after bake v2 (same run as the v2 table, Jit/NoTiering
  medians, ns per tick): `HubFused16Single` 2.154/2.524 vs direct
  2.122/2.509, `HubFused16Sequential` 2.124/2.519 vs 2.161/2.514,
  `HubFused16Batch8` 1.547/1.694 vs 1.542/1.704, `HubVitalsSingle`
  2.641/3.371 vs 2.647/3.195. All |delta| ≤ 0.05 ns except the
  NoTiering `HubVitalsSingle` pair (+0.18 ns, the largest seen on these
  arms) — the forwarder stays effectively free at the new speeds.
- Net: one call site dispatching a dense ≤ 256 index into the frozen
  timelines costs effectively nothing — the frozen path now runs ~8×
  the generic engine on random jumps (21.5 → 2.65 ns on the same Vitals
  fixture) with a registry in front, and the hub gives every consumer a
  single, receipt-verified entry point.

### Skew-aware tree ordering (experiment)

**Verdict: DROP.** Width-ordering the branch tree regresses the arm that
matters (random ticks, the frozen path's design target and every prior
receipt's worst case) because it minimizes the wrong quantity: compare
*count*, when compare *entropy* is what costs. Measured on the
pre-redesign contract (base `2f7fb52`, aggregate flag bits — the
branch-entropy finding is contract-independent); the emitter flags,
fixtures, and benchmark class live on the `exp/skew` branch so the
receipt stays reproducible there.

The question: timelines longer than the 1024-tick dense-LUT threshold keep
the binary branch-tree fallback; on strongly skewed timelines, does
ordering the tree so the widest regions sit shallowest (fewest compares
for the most-visited ticks) beat the midpoint split?

The fixture (`Generated/SkewNaive.g.cs` / `SkewOrdered.g.cs`, from
`Generate/Program.cs` block (c) on the branch): duration 4096 — one idle
region `[0, 2048)` covering half the timeline, then 32 thin clip/gap
regions (clip `i` = `[2048 + i*60, 2048 + i*60 + 50)` with value `i+1`,
10-tick gaps, empty tail, zero-width sentinel at 4096; 66 region starts,
single track, single slot, Fused16's value conventions). Two emitter
flags drive it: `forceTree` pins a fixture to the tree path regardless
of the dense threshold (the duration already exceeds it — the flag
documents intent and survives a threshold change), and `skewWide` swaps
the region tree's split. Both default off; VitalsFrozen/Fused16Frozen
regenerate byte-identically with the flags in place (diffed against a
pre-change generation). The cut-rank trees stay midpoint in both arms,
so the A/B isolates region lookup.

The heuristic (`SkewOrdered`): split each node at the **width-weighted
median** — a region's weight is its tick count, the split lands where the
remaining tick mass divides most evenly. On this fixture the fat idle
region resolves with ONE comparison (`tick < 2048u` at the root) instead
of the midpoint tree's six (`tick < 3008u` root, then 2518/2278/2158/
2098/2048 down the left spine). Analytically (width-weighted leaf depths
under the same split rule): expected compares on a uniform random tick
drop 6.05 → 4.01, paid for by the thin regions sitting ~1 compare deeper
(mean depth 6.10 → 7.03, max 7 → 8; source 53.6 → 57.9 KB). On a uniform
fixture the rule degenerates to the midpoint shape.

Parity: both arms pass the same walk-oracle battery as Vitals/Fused16
(`Dispatch --verify`). The oracle-side `SkewTrack` tables are *computed*
from the authored clip list by the same event sweep the generator runs
(no hand-set literals), brute-force cross-checked tick-by-tick against
the raw clip list, and then each arm runs the full battery against
PlaybackCore: forward walk 0..duration+10 (per-step `Playback` equality,
exact-float sink equality, flag-count totals), a 24-pair deterministic
jump battery in and out of range both directions, the backward mirror
(sink restored), and an 8-tick batch vs singles. Each arm matches the
oracle bit-for-bit, so naive ≡ ordered transitively.

Measurement (`SkewTree` class, Frozen-style config — Jit + NoTiering,
16 warmups / 12 iterations / 250 ms — pinned to core 14 via
`taskset -c 14`, two runs, artifacts under `/tmp/skew-run1` and
`/tmp/skew-run2`; ns per tick, medians; standard errors ≤ 0.05 ns):

| Arm                | Random Jit (run 1 / run 2) | Random NoTiering (1 / 2) | Sequential Jit (1 / 2) | Sequential NoTiering (1 / 2) |
|--------------------|---------------------------:|------------------------:|----------------------:|-----------------------------:|
| `SkewNaiveSingle`   |           14.052 / 13.801 |          13.985 / 13.930 |        6.900 / 6.602 |               9.693 / 9.720 |
| `SkewOrderedSingle` |           15.555 / 14.847 |          15.709 / 15.732 |        6.385 / 6.399 |               9.070 / 9.017 |

Readings, honestly:

- **Random ticks: ordering regresses 1.0–1.8 ns/tick (7–12%), both jobs,
  both runs.** The mechanism is the experiment's real finding:
  width-balancing the tree makes every compare's outcome a fair coin
  (~50/50 tick mass each side — that is what the split optimizes), the
  worst possible input for branch prediction, while the midpoint tree's
  compares skew along the fat spine (root 73% taken, deeper 84/90/95%)
  and resolve at better-than-coin rates. A bimodal model (per-compare
  mispredict ≈ `min(p, 1−p)` of the taken-mass) predicts 1.31
  mispredicts per random tick for naive vs 1.78 for ordered; the +0.47
  difference at ~17–20 cycles ≈ +1.6–1.9 ns at ~5.3 GHz — matching the
  measured regression. Buying −2 compares with +0.5 mispredicts is a
  bad trade: a correct compare costs ~1 cycle, a mispredict ~17–20.
- **Sequential ticks: ordering wins 0.2–0.7 ns (~4–8%).** With the
  per-region path stable for runs of ticks, compares stay predicted and
  count is (almost) pure throughput — the fat half of the walk genuinely
  executes 1 compare instead of 6. But predictable streams are not the
  frozen path's target workload, and even here ordering recovers only a
  fraction of the gap to the LUT mode below.
- **The random baseline itself (~14 ns/tick) indicts trees, not their
  order.** Bake v2's LUT mode runs the *same full movement semantics* at
  2.1–2.7 ns/tick on random ticks for ≤ 1024-tick timelines; the tree
  fallback is ~5–7× that. A 4096-tick single-slot LUT needs ~32 KB of
  words per direction — the same order as these trees' code footprint —
  so the natural evolution for > 1024 timelines is bigger (or two-level)
  LUTs, which deletes the branches outright and makes tree-ordering
  moot. (Part of the ~14 ns is the shared movement/rank machinery both
  arms pay equally — the delta, not the absolute, is the A/B.)

Net: **DROP the width-weighted split from the default path.** It is a
loss on the arm the frozen receipts optimize (random seeks), a small win
only where the LUT already dominates, and — the general lesson worth
keeping — reordering comparison trees against measured skew must be
judged by branch-outcome entropy, not by expected comparison count: the
two objectives move in opposite directions precisely on skewed data. The
`forceTree`/`skewWide` emitter flags and the `SkewTree` benchmark class
stay on the `exp/skew` branch so this receipt can be re-run; neither is
registered in `FrozenHub` and neither changes any default emission.

### The redesigned contract on the frozen path — and the regression it smuggled in

The Tracks redesign (15ce099) deleted the aggregate movement flag bits,
and the frozen kernels were re-emitted for the new sink contract
(emitter ported in b82c60d behind a byte-identity gate against a
ground-truth generation; all parity receipts green):

- **Per-slot ClipState codes, not flag bits.** `sink.Flags` accumulates
  Enter=0 / Stay=1 / Exit=2 per **active** slot: Exit is positional (the
  window's End−1 forward, Start backward) and baked as a per-slot bit in
  word bits 32..39; Enter fires when the step crossed the slot's entry
  edge (window Start forward, End backward), carried by per-slot
  `s_enterFk`/`s_enterBk` uint **entry-reference tables** so the test is
  one `prev` compare.
- **Lifecycle bits.** `Start` mints `Started`; the per-tick flags word is
  `Started | (Completed × bit 40)` — Completed direction-dependent and
  positional, assembled branchlessly (a 0/1 multiply).
- **Empty ticks do no sink work**: bits 44..47 carry the active-slot
  count and `n == 0` skips the tick entirely.
- `Cycles` passes through; `Count++` per tick with at least one active
  slot; `Backward` stays the exact subtractive mirror; the clamp still
  targets the empty sentinel word at the duration.

Parity held everywhere — but nothing had **timed** the new kernels (the
redesign receipts covered the API-shape and hub arms only). First
measurement (`--filter '*Frozen*'`, core 4, medians, ns per tick; old =
the bake v2 receipts; a package-free JIT harness on core 14 reproduced
the same picture within 0.15 ns):

| Method                 | Random Jit     | Random NoTiering | Sequential Jit  | Sequential NoTiering |
|------------------------|---------------:|-----------------:|----------------:|---------------------:|
| `FrozenVitalsSingle`   | 2.65 → **5.58** | 3.20 → **6.91**  | 2.49 → 2.13     | 3.21 → 3.19          |
| `FrozenVitalsBatch8`   | 1.96 → **5.65** | 2.21 → **6.95**  |       —         |         —            |
| `Fused16Single`        | 2.12 → **4.35** | 2.51 → **5.16**  | 2.16 → **1.08** | 2.51 → 1.86          |
| `Fused16Batch8`        | 1.54 → **4.44** | 1.70 → **5.20**  |       —         |         —            |

Random arms regressed 2.3–2.9× while sequential arms *improved* — the
exact mispredict signature of the bake v1 era, back for the same class
of reason: the redesigned emission reintroduced data-dependent branches
bake v2 had engineered out. Two sources: the per-slot nested ternary
`(exits & 1) != 0 ? 2 : prev < s_enterFk[i] ? 0 : 1` (variable-arm
ternaries compile as branches, and the enter compare is a coin toss on
random streams), and the active-slot guards `if (n != 0)` / `if (n > k)`
flipping per tick. Sequential streams predict everything and profit from
the gap skip (`Fused16Sequential` 1.08 — a quarter of its ticks are
gaps and now cost nearly nothing); random streams pay a mispredict per
unpredicted branch, up to four deep on Vitals. The hub deltas stayed
noise-level (3.87 vs 4.44 on `HubFused16Batch8` tiered) — the kernel,
not the dispatch, regressed. Lesson recorded: **byte-identity and parity
gates do not catch a branchiness regression; every re-emission of a
hot loop needs its timing receipt re-run.**

#### Bake v3: branchless under the per-work contract

The fix deletes all three branch classes the redesign introduced,
keeping the new semantics bit-exact:

1. **Per-slot ClipState is pure integer arithmetic.** The exit bit comes
   from the word; Enter is one constant-arm ternary `prev < ref ? 0 : 1`
   (compiles to `setcc`, as bake v2 proved); the code is
   `exit + (exit | enter)` — 2 uops, exit set gives 1 + (1|e) = 2, exit
   clear gives 0 + e. Multi-slot codes are summed into **one `Flags`
   RMW per tick** (integer addition is associative, so the single
   accumulate is bit-exact).
2. **Inactive slots are baked to contribute exactly nothing.** Forward
   entry refs bake `uint.MaxValue` (`prev < MaxValue` always holds →
   Enter code 0), backward bake `0u` (`prev >= 0u` always holds); value
   bits bake zero. The `if (n > k)` guards are gone entirely.
3. **`Count` is an add.** Word bit 41 = any-active; `Count += bit`. The
   `if (n != 0)` guard is gone; "empty ticks do no sink work" now holds
   arithmetically (zero contributions) instead of by branch.
4. **Multi-slot pair tables.** Each slot's value bits (low half) and
   entry reference (high half) ride in ONE `ulong` per tick per
   direction — value and movement in a single load, restoring bake v2's
   one-word-plus-one-load-per-slot access pattern.
5. **Span collection-expression tables** (`static ReadOnlySpan<T> =>
   [...]`): Roslyn lowers them to frozen read-only blobs with constant
   lengths — every per-access bounds guard AND the per-tick GC-static
   base reload die, the latter even the v2 receipt still carried.

Disassembly receipt (FullOpts, probes around `Fused16Frozen.Forward`):
per tick — tick load, the constant clamp diamond, ONE word load, the
`Started | Completed` shift/and/or/mul unpack, `Count` bit-add, one
`cmp`/`setae` enter, the `or`+`add` code, one Flags RMW, one
`vaddss`+store pair, the `prev = tick` move, increment/compare/branch.
**Exactly two conditional branches: the constant clamp and the loop
increment. Zero data-dependent branches.** Vitals is the identical
shape ×3 slots; Backward is the exact subtractive mirror
(`sub`/`setb`/`vsubss`).

Measurement — cross-checked three ways (BDN core 10 dev run, BDN core 4
receipt-of-record, package-free Stopwatch harness core 14; all agree
within 0.06 ns on Fused16 and 0.14 on Vitals; ns per tick, medians):

| Method                 | bake v2 Jit | redesigned Jit | v3 Jit  | v3 NoTiering |
|------------------------|------------:|---------------:|--------:|-------------:|
| `FrozenVitalsSingle`   |        2.65 |           5.58 |   2.942 |        2.722 |
| `FrozenVitalsBatch8`   |        1.96 |           5.65 |   2.587 |        2.478 |
| `Fused16Single`        |        2.12 |           4.35 |   1.236 |        1.107 |
| `Fused16Batch8`        |        1.54 |           4.44 |   1.088 |        1.061 |
| `FrozenVitalsSequential` |      2.49 |           2.13 |   2.721 |        2.733 |
| `Fused16Sequential`    |        2.16 |           1.08 |   1.190 |        1.105 |

Readings, honestly:

- **Random ≡ sequential again** (1.236 vs 1.190, 2.942 vs 2.721): the
  mispredict cliff is gone, and this time there was no sequential toll
  to pay for it — v3 sequential beats bake v2's sequential too.
- **Fused16 beats bake v2 on every arm**: batch 1.54 → 1.06–1.09
  (−30%), single 2.12 → 1.11–1.24, sequential 2.16 → 1.11–1.19. The
  richer per-work semantics cost *less* than the old aggregate-flag
  machinery once both are branchless.
- **VitalsBatch8 missed its 2.0–2.3 bar** (2.46–2.59): the per-slot
  contract intrinsically adds ~25 uops/tick over v2's aggregate word —
  three entry-ref consumes and three code computations — on top of the
  3-chained-`addss` float floor (~1.7–2.3) that parity pins. The
  disassembly shows no guard or branch fat left; the remaining levers
  (unsafe bounds-free pointer walks) are outside the established
  emission shape. This is the new honest floor of the per-work
  contract on a 3-track fixture.
- **The sub-1 ns gate** (`Fused16Batch8` < 1.0 tiered): 1.088/1.061
  Jit, 1.094 NativeAOT, 1.05–1.09 across every harness and core. Still
  not crossed — but the gap to the ~0.6–0.95 parity floor of the
  serial `addss` chain is now 0.1–0.4 ns, and the loop is proven
  branch-free on JIT, NoTiering, and AOT alike.
- Hub stays free at the new speeds: `HubFused16Batch8` 1.050/1.058 vs
  direct 1.088/1.061 — noise-level, both jobs.

### TrackViewDecomp: what one tick actually spends (v0.2 receipts)

The suspicion: `Tracks<T,C>` view machinery dominates the per-tick cost.
Decomposition ladder (`TrackViewDecomp` class, v0.2 API, core 4, medians,
ns per single-tick call; fixture = ONE region, 4 tracks live every tick,
duration 64 — region lookup and movement constant, so the ladder isolates
the view; each step adds exactly one cost):

| Step (adds)                        | Jit   | NoTiering |
|------------------------------------|------:|----------:|
| `Calls` — engine + dispatch floor  | 11.60 |     18.32 |
| `Walk` — + bare foreach traversal  | 20.85 |     39.46 |
| `IndexRead` — + one TrackWork read | 26.13 |     28.47 |
| `StateRead` — + ClipState          | 25.12 |     26.91 |
| `ClipRead` — + clip payload        | 26.92 |     29.53 |
| `ClipReadBlend` — Clip over pairs  | 28.06 |     32.85 |

Readings, honestly:

- **The view is most of the tick.** Full consumption is 26.92 vs an
  11.60 engine+dispatch floor: 15.3 ns (57%) is view machinery. The bare
  `foreach` with nothing read — no field touched, the work discarded —
  costs +9.25 ns (35% of the whole tick) to walk FOUR items: the
  enumerator state machine, per-iteration TrackRow loads, and a
  TrackWork construction the JIT cannot dead-code (ref struct with ref
  returns).
- **The bundle copy tax is real.** Making one field read live
  (`IndexRead`) adds +5.3 ns over bare traversal — that is the
  ~10-field table-span bundle copied into a fresh TrackWork per visited
  work. After the first read, further reads are nearly free (StateRead
  ≈ IndexRead; the bundle is already materialized), and the actual
  payload read (`ClipRead`) adds only +0.8 — the data is cheap; the
  plumbing is not.
- **Blend machinery is NOT the problem**: +1.1 ns/tick for two full
  crossfades (factor math + Blend + scratch slot) — fine.
- **NoTiering confirms the fat-struct cliff**: bare traversal balloons
  to 39.46 under FullOpts (the v0.2 guard root-cause: the 5-arg callback
  inlines, spends the budget, and `Enumerator.Current` becomes a real
  call shuttling the fat ref struct through a hidden buffer).
- For scale: the real mixed consumer (`PlaybackSingle`, Vitals) runs
  ~24-25 ns on this machine today; the frozen floor is 1.06. The 11.6 ns
  floor here is engine bookkeeping (bridge call, checks, locate,
  movement, Playback) — the view tax rides on top of it.

Verdict: the enumerator + per-iteration bundle materialization is the
dominant cost of generic playback. The fix direction (receipted next):
region-stable materialization — the active track set is constant inside
a region, so materialize a dense resolved array once per region ENTRY
(clip refs constant; only blend factors vary per tick) and hand the
callback a thin indexed span. Traversal becomes a flat for-loop,
TrackWork becomes a 2-3 field view of one entry, and clip access reads
pre-resolved refs — projected to reclaim most of the 15.3 ns while
keeping uniform per-work semantics and adding first-class indexing.

### v0.3: region-stable materialization — the view tax paid once per region

The TrackViewDecomp receipts above priced the enumerator + per-iteration
bundle materialization at 57% of a consuming tick. v0.3 restructures the
view around the one fact the tables encode: the active work set is
CONSTANT inside a region. On region entry the engine materializes a dense
`WorkSlot` span once (payload-map hops resolved, blend pairs + factor
windows captured, enter-reference edges precomputed — the bake-v3
encoding); every tick in the region reuses it. `Tracks` becomes a thin
6-field carrier with `Count`, `this[int]`, `Slice` (new, zero-copy), and a
flat enumerator; `TrackWork` becomes a small view of ONE slot instead of
a copy of the whole table bundle. Blends still resolve lazily into
scratch on first visit — works never visited are never blended. The
callback signature is unchanged; a new `Indexed:` edge battery cross-
checks Count/indexer/Slice against enumeration.

Ladder (same class, core 4, medians, ns/tick; v0.2 = the table above):

| step          | v0.2 Jit | v0.3 Jit | v0.2 NoTiering | v0.3 NoTiering |
|---------------|---------:|---------:|---------------:|---------------:|
| Calls (floor) |    11.60 |    10.44 |          18.32 |          17.18 |
| Walk          |    20.85 |    15.09 |          39.46 |          16.75 |
| IndexRead     |    26.13 |    17.03 |          28.47 |          23.76 |
| StateRead     |    25.12 |    18.26 |          26.91 |          23.72 |
| ClipRead      |    26.92 |    18.09 |          29.53 |          30.18 |
| ClipReadBlend |    28.06 |    19.16 |          32.85 |          32.05 |

Sweep vs the v0.2 same-session references (Jit tiered): PlaybackSingle
23.91 → 16.05 (−33%), PlaybackBackwardSingle 23.28 → 15.40,
PlaybackParamsFour 19.83 → 15.34, HubDispatch 26.37 → 22.07; NoTiering
PlaybackSingle 35.19 → 28.96. Blend arms: stack single 47.13 → 37.31,
buffer single 35.30 → 30.24, zero-blend single 49.72 → 24.48 Jit and
49.72 → 29.87 NoTiering (−40%). The v0.2 NoTiering fat-struct cliff
shrank but did not fully die on the fat-payload blend fixture
(StackSingle 219.47 → 194.05) — the thin TrackWork removed the view half;
the 260 B payload copies remain. No arm regressed.

Readings, honestly:

- Full consumption hit its target (≤18): 18.09, −33%; blends −32%. The
  floor itself dropped 11.60 → 10.44 — materialization replaced per-tick
  row walking even before the callback runs.
- The v0.2 structural cliff is GONE on the ladder: bare traversal
  NoTiering 39.46 → 16.75, now BELOW tiered. Thin structs stopped the
  hidden-buffer shuttle.
- Two stretch targets missed narrowly (Walk ≤13 landed 15.09, IndexRead
  ≤16 landed 17.03): residual ≈ 1.2 ns/work of thin-TrackWork
  construction + bounds checks, halved from v0.2's ≈ 2.3. NoTiering
  ClipRead is flat (29.53 → 30.18) — the FullOpts blend of materialize +
  consume did not improve there; noted, not diagnosed further.
- The cursor filter arm was not re-run (its region-hint mechanism is
  untouched by v0.3); zero-blend and blend arms cover the view cost.
