# Playback review

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

The measured target is .NET 10.0.11, x64 JIT on this machine. NativeAOT, Unity
IL2CPP and WASM performance remain unmeasured. Those targets need their own
build and benchmark gates before the roadmap's choices are treated as portable
performance guarantees.

The [small API regression run](../benchmarks/Dispatch/results/release/bdn/results/Tl.Hooks.ApiShape-report-github.md)
also completed all 18 cases with 0 B allocated. Some small stateless cases
remain slower than the historical run, especially with tiering disabled;
the navigation gains above should not be read as a speedup for every API shape.
See [faster.md](faster.md) for the next experiments and their acceptance checks.
