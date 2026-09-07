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
  duration 63, the 4th tick of each group is the gap. The ~1 ns headline arm.

What the frozen form specializes away from `PlaybackCore`: positional flags
and payloads are immediates and constant compares, and movement facts
(Enter/Exit) are rank comparisons over the known clip-start/clip-end cut
boundaries — the boundary walk collapses because a cut boundary lies in the
crossed span exactly when its rank at the two ends differs. Region lookup and
the ranks come out of the emitter's **dense LUT mode**, picked when duration
is ≤ 1024: three per-tick tables (`s_region`, `s_startRank`, `s_endRank`)
loaded behind an in-range guard, with a jump-table switch over region indices
carrying leaf bodies identical to the tree mode's (ticks past the duration
take the sentinel region / total-cut-count fallbacks, so out-of-range jumps
keep their parity). Longer timelines fall back to binary branch trees over
the starts and cut boundaries. The frozen form is bound to one exact
non-looping timeline: no wraps, no cycle arithmetic (`Cycles` passes
through), no duration-0/empty handling.

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

Quick measurement (`--filter '*Frozen*'`, pinned to one core, tiered-JIT
medians, ns per tick; the ApiShape reference `PlaybackSingle` on the same
Vitals fixture is 21.5 ns with random ticks):

| Method                 | Random ticks | Sequential ticks |
|------------------------|-------------:|-----------------:|
| `FrozenVitalsSingle`   |        9.53  |            3.48  |
| `FrozenVitalsBatch8`   |        8.52  |               —  |
| `Fused16Single`        |       12.38  |            3.11  |
| `Fused16Batch8`        |       11.35  |               —  |

Readings, honestly:

- Frozen beats the generic engine everywhere it should: 2.3x on random
  jumps, 6.2x on sequential streams for the full Vitals semantics
  (movement flags + sink + `Playback` return), 0 B allocated. The dense
  LUT mode (tables + jump-table switch, ≤ 1024 ticks, now in the emitter)
  moved the sequential arms most — Fused16 sequential went 7.19 → 3.11,
  Vitals 4.22 → 3.48 — because three predictable loads replaced the
  data-dependent tree walks. Tiered PGO still matters, just less: with
  tiering off, sequential Vitals is 4.67 ns (was 8.6).
- `Fused16` still trails the harder `Vitals` fixture on random ticks
  (12.4 vs 9.5 ns), but the reason has moved. The tree mispredicts are
  gone (19.89 → 12.38); what remains is fixture shape. Random ticks
  spread almost uniformly over Fused16's 32 thin regions (three-tick
  clips, one-tick gaps), so the jump table's indirect branch is a coin
  toss no predictor learns,
  while Vitals's regions are skewed enough (three of them cover about
  two thirds of the ticks) that its switch target predicts well. Vitals
  random barely moved (9.50 → 9.53) for the same reason: its old trees
  were already mostly predicted, so there the LUT is a wash and the
  remaining cost is per-tick `Playback` bookkeeping plus four-track
  sampling. The next milestone is branchless computed sampling, not more
  lookup tuning.
- The sub-1 ns frozen receipts from the sibling LUT repo (0.165 ns
  sampling, 1.33 ns one-tick traverse) measured a repeated single tick
  and pure sampling, without per-tick `Playback` flags on varied ticks;
  this path is not there yet. 3.1–3.5 ns/tick sequential (Fused16,
  Vitals) with full movement semantics is the current proven floor for
  tl timelines.

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

Measured hub-vs-direct deltas (same `--filter '*Frozen*'` run, medians,
ns per tick; negative = hub faster, and anything under ~0.2 ns is
run-to-run noise on these arms):

| Arm pair (Jit / NoTiering medians)                  | Direct              | Via hub             | Delta               |
|-----------------------------------------------------|--------------------:|--------------------:|--------------------:|
| `Fused16Single` ↔ `HubFused16Single` (random ticks) | 12.513 / 14.374 ns  | 12.286 / 14.482 ns  | −0.23 / +0.11 ns    |
| `Fused16Sequential` ↔ `HubFused16Sequential`        |  3.025 /  5.170 ns  |  3.271 /  5.952 ns  | +0.25 / +0.78 ns    |
| `Fused16Batch8` ↔ `HubFused16Batch8`                | 11.495 / 13.153 ns  | 11.378 / 13.171 ns  | −0.12 / +0.02 ns    |
| `FrozenVitalsSingle` ↔ `HubVitalsSingle` (random)   |  9.518 / 11.411 ns  |  9.429 / 11.284 ns  | −0.09 / −0.13 ns    |

Readings:

- The prediction held, and was conservative on the random side. The
  dispatch verdicts expected the hop to be ~free on sequential streams
  and to cost ~an indirect-branch mispredict on random. Measured: on the
  random-tick arms the hub delta is noise-level (−0.23…+0.11 ns, both
  signs) — the hub's switch sees a per-call-site *constant* index (only
  the ticks randomize), so there is no second random indirect branch to
  mispredict; the one mispredict those arms already pay lives in the
  timeline's own region jump table.
- On sequential streams the hop is measurable but tiny: +0.25 ns/tick
  with tiered PGO (about one cycle on the tightest 3.0 ns arm; the guard
  and call do not fully fold), +0.78 ns without tiering (no PGO, the hub
  stays a plain call). Batched calls amortize it out of sight
  (−0.12/+0.02 ns).
- Net: one call site dispatching a dense ≤ 256 index into the frozen
  timelines costs effectively nothing — the frozen path keeps its
  2.3×/6.2× wins over the generic engine with a registry in front, and
  the hub gives every consumer a single, receipt-verified entry point.
