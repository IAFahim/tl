# Playback review

Closed at `ccd344a`, retained as history: this reviews the pre-extraction prototype playback path, not a shipped package.

This changes the prototype in `benchmarks/Hooks.cs`. The library extraction
under `src/` remains a separate roadmap step. The interface-based consumer API
and 8-byte `Playback` layout are preserved.

## Correctness

The new [edge checks](../benchmarks/Dispatch/EdgeVerification.cs) failed on the
previous implementation and now run as part of Dispatch `--verify`:

- Leading gaps exposed an active clip too early; the runtime builder now
  includes tick zero. A terminal empty row makes sampling at or beyond the
  duration safe. Empty timelines also build and sample safely.
- The generated fixture had data through tick 599 but a duration of 515.
  Its duration, terminal row and clip edges now match the runtime fixture.
- A one-tick overlap produced `0 / 0`. It now uses a factor of `0.5`.
- Blending invoked a default track and ignored its authored fields. It now
  uses the matching track payload, and the enumerator no longer contains a
  spare `TTrack`. Blend implementations should be pure; live mutable state
  belongs in `ref TData`.
- Runtime crossfade pairs are ordered by start tick, with insertion order
  breaking equal-start ties.
- Packed cycles could overwrite flag bits. The constructor rejects counts
  beyond `Playback.MaxCycles` (67,108,863) and invalid flag bits. Advancing
  past the cycle limit rejects that step before invoking its callback.
  Earlier successful ticks in a multi-tick call are not rolled back.
- Forward local wrap (`last tick -> 0`) now mirrors the existing backward
  wrap convention. Stateless looping calls normalize ticks too. Raw ticks
  moving in the requested direction count quotient crossings; ticks moving
  against that direction are interpreted as local positions, with at most
  one wrap. `Playback.Start` still positions silently.
- Missing track references and capacity overflows are rejected. Checked CSR
  offsets prevent silently truncated indices; timeline IDs stop at 65,535
  rather than reusing zero.

The tests include 28,900 movement-flag comparisons against a clip-by-clip
reference, single/batch parity, generated/runtime parity across the terminal
boundary and loops, `uint.MaxValue`, packing, and ID exhaustion. Flags are
aggregate facts; they do not preserve the count or ordering of every crossed
clip event. Returning `Playback` reports the last tick of the call, while
`Tracks.Status` exposes each tick's facts inside its callback.

## Performance

The updated lookup scans tables of at most 16 region starts, uses binary
search for larger random jumps, and retains a local region cursor within
multi-tick calls. Movement scans stop when both Enter and Exit are known.
No extra persistent index, dense tick table, or per-call heap object is added.

Measured medians in ns/tick on CPU 4 (.NET 10.0.11, tiered JIT). All 16
comparison cases reported **0 B allocated per tick** after setup.

| Clips | Queries | Ticks/call | Before | After | Speedup |
|---:|---|---:|---:|---:|---:|
| 16 | Random | 1 | 46.02 | 41.45 | 1.11x |
| 16 | Random | 8 | 41.16 | 29.07 | 1.42x |
| 16 | Sequential | 1 | 20.31 | 19.83 | 1.02x |
| 16 | Sequential | 8 | 14.10 | 8.71 | 1.62x |
| 512 | Random | 1 | 265.90 | 72.13 | 3.69x |
| 512 | Random | 8 | 254.05 | 52.66 | 4.82x |
| 512 | Sequential | 1 | 176.03 | 62.07 | 2.84x |
| 512 | Sequential | 8 | 176.91 | 14.54 | 12.16x |

[Full comparison](../benchmarks/Review/results/release/bdn/results/Tl.Review.Movement-report-github.md)
· [raw measurements](../benchmarks/Review/results/release/bdn/results/Tl.Review.Movement-report-full.json)
· [environment and source hashes](../benchmarks/Review/results/release/environment.json).


The [comparison project](../benchmarks/Review/README.md) runs the original and
updated implementations in the same executable. Setup verifies identical
payload sums, callback counts and movement-flag sums before timing. Each tick
visits at most one active track in this fixture; it isolates navigation and
movement costs and does not establish a universal speedup for arbitrary clips.

The runner now builds and verifies before timing, writes fresh result folders,
checks that every discovered benchmark has measurements, propagates failure,
and checks that sources did not change during measurement. Earlier
`results/validated` folders remain historical records.

## Scope still open

Warm managed allocation is measured separately from setup and retained memory.
The runtime builder still allocates its tables and retains authoring lists;
this review does not turn it into a zero-allocation compiler. Scratch storage
is still proportional to `MaxActiveTracks * sizeof(TClip)` on the stack. A
bounded default plus a caller-provided scratch overload is needed before
supporting arbitrary large payload/track counts in the shipping library.

The measured target is .NET 10.0.11, x64 JIT on this machine. The frozen path
is now also measured under NativeAOT (manual harness; see the NativeAOT
section in [benchmarks.md](benchmarks.md)): +7–27% ns/tick depending on arm,
bit-exact parity, zero allocation — the design holds without tiering. Unity
IL2CPP and WASM performance remain unmeasured. Those targets need their own
build and benchmark gates before the roadmap's choices are treated as portable
performance guarantees.

The [small API regression run](../benchmarks/Dispatch/results/release/bdn/results/Tl.Hooks.ApiShape-report-github.md)
also completed all 18 cases with 0 B allocated. Some small stateless cases
remain slower than the historical run, especially with tiering disabled;
the navigation gains above should not be read as a speedup for every API shape.
See [faster.md](faster.md) for the next experiments and their acceptance checks.

## Faster queue results

The four-item queue from [faster.md](faster.md), re-run against the
redesigned API (global index registry, function-pointer bridges, per-work
`ClipState` instead of aggregate movement flags). All timings are Core 6
medians (`taskset -c 6`), .NET 10 x64, tiered and `NoTiering` jobs, 0 B
allocated on every warm arm; each run started from a fresh baseline of the
current code and `--verify`/`--verify-edges` stayed green throughout
(`CursorParity`, `Scratch`, `PrefixCounts` and `Dedup` batteries were added
for these items). Summaries live under
[benchmarks/Dispatch/results/faster/](../benchmarks/Dispatch/results/faster/).

### 1. Persistent caller-owned cursor — KEEP

`Cursor` is caller-owned side state; it validates against the registry
Entry itself (`ReferenceEquals` replaces the handoff's owner+revision
checks: a rebuild mints a new Entry and loop mode is per-entry immutable)
plus the source tick. `PlaybackCore.Advance` takes a caller-validated
region hint and returns the final region; `Timeline.Forward/Backward` gain
`ref Cursor` overloads routed through two new bridge pointers. Stale
cursors fall back to searching and never change results; empty spans echo
a valid cursor through. Generated timelines stay out of scope.

| Clips | Stream | Jit scan | Jit cursor | NoTiering scan | NoTiering cursor |
|---:|---|---:|---:|---:|---:|
| 16 | sequential | 18.60 | 10.95 | 26.52 | 26.04 |
| 64 | sequential | 19.28 | 11.45 | 27.58 | 26.40 |
| 512 | sequential | 30.93 | 11.52 | 37.65 | 26.11 |
| 16 | random | 31.60 | 25.43 | 41.27 | 41.34 |
| 64 | random | 37.80 | 34.56 | 47.08 | 49.40 |
| 512 | random | 47.78 | 45.80 | 57.38 | 60.24 |

Sequential single-tick stepping (ordinary frame-by-frame playback) improves
1.7–2.7x tiered and up to 1.44x untiered; the deliberately invalid arm runs
at parity (0.84–1.06x) and random seeks are neutral to slightly negative
untiered — the overload is opt-in, so random-seeking callers simply do not
use it. Kept.

### 2. Blend-sized scratch — KEEP

`MaxActiveBlends` (compiled tables hand-set, runtime builder counts tracks
with exactly two active clips per region) sizes the resolution buffer:
blend results take ordinal slots, standalone clips resolve in place, and a
zero-blend timeline reserves nothing. Every entry point guards a 4,096-byte
stack budget (`BlendScratch.StackCount` throws past it, pointing at the
new `Span<TClip>` scratch overloads on `Timeline<TTrack,TClip>` and
`GeneratedTimeline`); the scratch overloads check length before any
callback. No pooling, no shared buffer — the reentrancy receipt plays a
second blended timeline from inside a callback with its own scratch.

| Arm (2 blends + 2 standalone × 260 B clips) | Jit | NoTiering |
|---|---:|---:|
| stack single | 38.03 ns | 208.32 ns |
| caller buffer single | 30.86 ns | 200.57 ns |
| stack batch4 | 21.67 ns | 189.71 ns |
| caller buffer batch4 | 20.37 ns | 188.72 ns |
| zero-blend single | 25.93 ns | 37.27 ns |

Stack bytes per call drop from `MaxActiveTracks × sizeof` to
`MaxActiveBlends × sizeof` (the runtime Vitals fixture 12 → 4 B; the
260 B fixture 1,040 → 520 B; a 4,204 B blend refuses the stack and plays
from a retained caller buffer). Kept.

### 3. Prefix counts for the movement scan — REJECT

Adaptation first: the aggregate Enter/Exit scan this idea replaced no
longer exists — the redesign answers movement per work with one O(1)
`Crossed` call, so the `CutCounts` prefix table can only gate that single
well-predicted branch. The table was implemented anyway (checked build
from the authored clip edges, gate in `MovementSpan.EnterPossible`, wraps
and multi-cycle jumps keep the full check, receipts pin on/off parity over
the whole step space and the table against a raw-edge prefix oracle).

| Clips | Stream | scan | counts | scan batch4 | counts batch4 |
|---:|---|---:|---:|---:|---:|
| 16 | sequential (Jit) | 25.39 | 25.21 | 15.26 | 15.95 |
| 512 | sequential (Jit) | 30.74 | 33.81 | 17.01 | 16.45 |
| 512 | sequential (NoTiering) | 40.76 | 42.09 | 17.53 | 18.26 |
| 512 | random (Jit) | 51.03 | 51.95 | 47.50 | 46.34 |

No shape wins consistently; the 512-clip sequential tiered single regresses
10 percent and several batch arms 3–5 percent, for 4 B per region of table.
Rejected per the handoff's own warning: `Timeline.EmitCutCounts` defaults
to false; the flag, receipts and A/B arms stay for the record.

### 4. Dedup compiled storage — KEEP, opt-in (default off)

Payload storage merges on bitwise-equal `TClip` bytes through a
`PayloadMap` indirection (float bit patterns preserved; +0/−0 and distinct
NaNs never merge; the `ClipIndex == ClipEdges` 1:1 invariant holds —
payload slots, not row renumbering), and long clips spanning regions stop
duplicating CSR rows (byte-identical runs and region slices alias, then
the pools compact). Dictionaries are build-time only.

| Metric (320 authored clips, 8 distinct 260 B payloads, 256 regions) | plain | deduped |
|---|---:|---:|
| retained bytes | 684,744 | 114,184 (16.7%) |
| warm single ns/tick (Jit / NoTiering) | 160.90 / 158.80 | 158.60 / 155.60 |
| warm batch4 ns/tick (Jit / NoTiering) | 144.40 / 153.40 | 143.70 / 150.30 |
| cold build | 5.89 ms / 3.37 MB | 8.00 ms / 5.35 MB |

Playback parity on the duplication-heavy fixture and 6x less retained
memory — but defaulted on, the small Vitals fixture regressed 5–15 percent
on its hot arms (instance/playback/hub, both JIT modes) for a near-zero
retained delta, because the map hop taxes every clip read. So
`Timeline.DedupStorage` defaults to false: duplication-heavy content opts
in; small timelines keep the zero-indirection read. Kept as an opt-in.
