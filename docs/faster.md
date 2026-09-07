# Next performance pass — agent handoff

Work from the current `tl` prototype. Keep the interface-based API, typed struct
hooks, `uint` ticks, `[start, end)` windows and 8-byte `Playback`. The real
library extraction is still a separate roadmap task. Do not implement every
idea at once: complete one item, compare it with the preceding version, and
keep it only when its receipts and measurements justify it.

Read [review.md](review.md) and `benchmarks/Hooks.cs` first. The measured fixes
already include terminal/gap handling, one-tick blends, authored blend data,
capacity checks, binary lookup, early termination of movement scans, and a
cursor local to each multi-tick call. Do not redo those changes.

## 1. Keep the cursor between single-tick calls

**Best first experiment for ordinary frame-by-frame playback.** Currently the
region cursor survives only inside one call. A single-tick call must locate
the previous and destination regions again. The measured 512-clip sequential
case still has a substantial gap between single calls and eight-tick batches.
This is evidence for testing a persistent cursor, not a promised speedup.

Add an optional caller-owned cache, separate from `Playback`:

```cs
public struct Cursor
{
    internal object? Owner;
    internal int Revision;
    internal uint Tick;
    internal int Region;
    internal bool Loops;
}
```

The instance overload should look like this:

```cs
public Playback Forward(
    in Playback from,
    ref Cursor cursor,
    ref TData data,
    params ReadOnlySpan<uint> ticks)
```

The entry check is small:

```cs
var valid = ReferenceEquals(cursor.Owner, this)
    && cursor.Revision == _revision
    && cursor.Loops == IsLooping
    && cursor.Tick == from.Tick;

var previousRegion = valid ? cursor.Region : -1;
```

These snippets are additions to `Timeline<,,>`; `_revision` does not exist yet.
Add it and advance it only after a successful `Build`. Teach
`PlaybackCore.Advance` to accept the initial region hint and return the final
region. Reuse the existing `Locate` helper. Store the cache after successful
playback. An empty tick span must leave the cache valid or invalidate it;
never label an uninitialized region as valid.

The cache is advisory. On an owner, revision, loop-mode or starting-tick
mismatch, fall back to searching. An invalid cache must never change results.
For generated timelines, use a cache whose owner identifies the closed
compiled schedule; sharing a clip type does not imply sharing a schedule.
Keep the current overloads for callers that do not want to maintain a cache.

**Tests:** same tick, adjacent forward/backward ticks, random seeks, wraps,
restored `Playback` snapshots, alternating two timeline instances, rebuilds,
loop-mode changes and empty spans. Separate callers must have separate cursors.
**Benchmarks:** single-tick calls at 16, 64 and 512 clips, sequential and random,
with valid and deliberately invalid caches. Include the cache checks in timing.

## 2. Bound scratch memory, and size it by blends

Today every call reserves `MaxActiveTracks * sizeof(TClip)` bytes on the stack,
even when most active tracks have one clip. Large payloads can make that an
unsafe amount of stack memory. This is the next memory/API task.

Add a caller-provided scratch overload. The caller allocates a buffer once:

```cs
// Inside GeneratedTimeline<TTrack, TClip, TData>.
public static void Forward(
    ref TData data,
    Span<TClip> scratch,
    ReadOnlySpan<uint> ticks)
{
    if (scratch.Length < TTrack.MaxActiveTracks)
        throw new ArgumentException("Scratch buffer is too small.", nameof(scratch));

    // Move the current sampling loop here and pass scratch into Tracks.
}
```

Keep the convenient stack overload, with a byte budget checked before
`stackalloc`. Example guard:

```cs
const int StackBytes = 4096;
var count = TTrack.MaxActiveTracks;
var capacity = StackBytes / Unsafe.SizeOf<TClip>();
if ((uint)count > (uint)capacity)
    throw new InvalidOperationException("Use the scratch-buffer overload.");

Span<TClip> scratch = stackalloc TClip[count];
Forward(ref data, scratch, ticks);
```

Apply the same arrangement to forward/backward stateful calls and clip hooks.
Do not silently rent from a pool and describe that as guaranteed zero allocation.
Each simultaneous/reentrant call needs its own scratch; do not use one global
mutable buffer.

Then add `MaxActiveBlends` to the compiled tables and runtime builder. Count
tracks with exactly two active clips in each region. In the enumerator, assign
scratch slots only to blended tracks:

```cs
// Enumerator field; initialize it to -1.
private int _blendSlot;

// In MoveNext, only after confirming row.ClipCount == 2:
var slot = ++_blendSlot;
_trackData[row.TrackIndex].Blend(
    in _clipData[first.ClipIndex], in _clipData[second.ClipIndex],
    factor, out _resolved[slot]);
```

Pass `_blendSlot` to `TrackItem.Current` instead of `_i`. Standalone items
already return a reference to the original clip and do not need a scratch
slot. Update all scratch length checks and allocations to `MaxActiveBlends`.
Keep each visited blend's slot stable for the duration of the view.

**Tests:** no blends, all blends, mixed single/blended tracks, one-tick blends,
large clip structs, insufficient scratch, repeated enumeration and nested
calls. **Measure:** allocated bytes, stack bytes required, retained buffer
bytes and timing. A zero-blend timeline should require no blend scratch.

## 3. Test prefix counts for movement flags

Current jump scans stop when both Enter and Exit are known. That is already
cheap when starts and ends alternate. It can still scan many cuts when a jump
crosses a long run of starts with no ends (or vice versa).

An optional four-byte record per region can answer the two presence queries
without scanning:

```cs
public readonly record struct CutCounts(ushort Starts, ushort Ends);

// Build once from the existing regionFlags; checked conversion is required.
uint startsSeen = 0, endsSeen = 0;
for (var i = 0; i < regionFlags.Length; i++)
{
    if ((regionFlags[i] & 1) != 0) startsSeen++;
    if ((regionFlags[i] & 2) != 0) endsSeen++;
    counts[i] = new(checked((ushort)startsSeen), checked((ushort)endsSeen));
}
```

For a non-wrapped forward move, after validating direction and locating both
regions:

```cs
var before = counts[previousRegion];
var after = counts[region];
if (after.Starts != before.Starts) flags |= PlaybackFlags.Enter;
if (after.Ends != before.Ends) flags |= PlaybackFlags.Exit;
```

For backward movement, compare destination and previous counts, swapping the
meaning of start/end changes. Keep positional First/Last/Active and Complete
logic unchanged. Initially retain the existing wrap path. Repeated ticks and
moves against the requested direction must keep their existing behavior.

**Tests:** use the existing independent edge oracle; add many tracks whose
starts cluster before their ends. Prefix counters must not overflow; ushort
counts rely on a validated maximum of 65,535 authored clips.
**Acceptance:** report the extra `4 * regionCount` bytes. Keep the current
scan path for shapes where prefix loads are slower or the memory cost loses.
Do not assume O(1) automatically wins on tiny tables.

## 4. Reduce duplicated compiled data

After the hot-path experiments, measure retained memory for repeated clips
and repeated region selections. Deduplicate payload storage, clip windows and
identical CSR row sequences during building/generation. Equal storage does
not make two authored track instances the same instance: preserve track order,
identity and callback counts.

Use deterministic authored/schema values as keys. Raw struct padding is not a
safe canonical key. Preserve float bit patterns where the contract distinguishes
them. Check hash collisions with equality. This is setup work, so a dictionary
here is fine; it must not migrate into playback.

The current runtime `Build` repeatedly scans clips for every track and region.
Replace that with a sorted edge sweep only if build time matters in the target
workflow. It will not by itself reduce runtime sampling latency.

## Rules for accepting a change

```sh
python3 benchmarks/run.py review
python3 benchmarks/run.py dispatch --filter '*ApiShape*'
dotnet run --project benchmarks/Algorithms -c Release -- --verify
```

Add a benchmark arm for the proposed feature rather than changing both sides of
the comparison. Keep `benchmarks/Review/Before.cs` unchanged. Start each new
experiment with a fresh baseline of the current implementation as well, so its
incremental gain is visible rather than hidden inside the gains already made.

Keep inputs variable and consume payloads, callbacks and flags in a receipt.
Report medians, variation, allocation, extra table/scratch bytes and code size
where relevant. Separate cold preparation from warm playback. Show small-case
regressions as well as large-case gains; re-run affected cases before deciding.

Do not trade away boundary correctness, checked capacities, deterministic order
or float behavior for a timing win. In particular, replacing division with a
precomputed reciprocal can change rounding; a numerically different result is
not an equivalent optimization under the current exact comparison tests.

Avoid giant generated switches and unbounded dense maps indexed by uint ticks.
Keep AOT/WASM as separate validation targets: a JIT result does not establish
an AOT result. Deliver one reviewable change at a time with its receipts,
benchmark artifacts and an explicit keep/reject decision.
