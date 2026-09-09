# tl compiler plan and v0.6 report

Date: 2026-09-09
Status: implemented, measured, and release-gated in `codex/tl-consumer-fusion-20260909`

## Result

The experiment produced a real production gain. The compiled path is no longer an interpreter expressed as generated C#. It emits direct region blocks and static per-work consumer operations. The minimal production consumer now measures 2.473 ns/tick, down from the 11.157 ns runtime interpreter. The full state-aware consumer measures 4.489 ns/tick, down from 15.340 ns. Both allocate zero managed bytes.

The 1–2 ns target is possible for a narrower workload. The verified research backend reached 1.195 ns/tick for a predictable batch of eight with an ordered sum operation and 1.843 ns/tick for a state-aware operation with runtime input. It is not the universal scalar latency of an arbitrary consumer. The production API now has the correct shape to add that stricter tier without another consumer rewrite.

## Smallest domain model

The system is defined by these values and laws:

1. A timeline is an ordered set of unmanaged tracks.
2. A track owns half-open clip windows `[start, end)` and has at most two active clips at one tick.
3. One active clip resolves to itself. Two active clips resolve through the track's pure `IBlend<TClip>` operation.
4. A destination tick resolves to an ordered sequence of `(ordinal, count, track index, track, clip, state)` values.
5. `ITrack.Forward` and `ITrack.Backward` interpret one resolved value into caller-owned result state.
6. `Playback` is an 8-byte value carrying raw tick, cycle count, and lifecycle flags.
7. Runtime authoring and build-time compilation must produce the same observable sequence.

Everything else is representation or acceleration. Removing a representation may reduce speed or supported authoring, but it must not alter these laws.

## Production API

The authored timeline and generated kernel are one named partial type:

```cs
public readonly partial struct PulseTimeline : ITimeline<PulseTrack, PulseClip>
{
    public static void Define(scoped TimelineBuilder<PulseTrack, PulseClip> timeline)
    {
        var pulse = timeline.Track(new PulseTrack());
        timeline.Clip(in pulse, new PulseClip(4f), 0u, 60u);
    }
}
```

The consumer implements one interface for both engines:

```cs
public struct PulseResult : ITrack<PulseTrack, PulseClip, PulseInput, PulseResult>
{
    public float Sum;

    public static void Forward(
        int ordinal,
        int count,
        ushort index,
        in PulseTrack track,
        in PulseClip clip,
        ClipState state,
        in uint tick,
        in PulseInput input,
        ref PulseResult result)
        => result.Sum += clip.Amount;

    public static void Backward(
        int ordinal,
        int count,
        ushort index,
        in PulseTrack track,
        in PulseClip clip,
        ClipState state,
        in uint tick,
        in PulseInput input,
        ref PulseResult result)
        => result.Sum -= clip.Amount;
}
```

Normal compiled use contains no build handle and no `Authoring.Author` indirection:

```cs
var playback = PulseTimeline.Start();
playback = PulseTimeline.Forward(in playback, in input, ref result, tick);
```

Runtime data uses the same definition and consumer:

```cs
var id = Timeline<PulseTrack, PulseClip>.Build(PulseTimeline.Define).InMemory();
Timeline<PulseTrack, PulseClip>.Bind<PulseInput, PulseResult>(id);
var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, in input, ref result, tick);
Timeline.Destroy(id);
```

`Build(...).InMemory()` means runtime construction. It is absent from normal compiled playback. The build-time generator reads `Define` before C# compilation and emits the remaining partial members. Rebuilding unchanged input is a content-addressed cache hit; it does not regenerate or rewrite output.

## What changed

### Runtime

- Removed `Tracks<TTrack,TClip>` and `TrackWork<TTrack,TClip>`.
- Removed indexed and sliced ephemeral work views.
- Removed caller scratch overloads and the 4 KB automatic blend scratch policy.
- Removed scratch binding pointers and max-blend metadata.
- Streams one work at a time from native tables.
- Resolves a blend exactly once into one local unmanaged value.
- Calls a static abstract `ITrack` operation, allowing constrained specialization without boxing or delegates.
- Keeps track and clip storage unmanaged and immutable after publication.
- Keeps input and result caller-owned; neither is retained by a timeline.

### Generator

- Added named `public readonly partial struct ... : ITimeline<TTrack,TClip>` declarations.
- Emits methods into that partial type, so the authored and executable timeline have the same name.
- Emits a true scalar method for one tick and a separate span loop for batches.
- Specializes forward and backward independently.
- Folds duration, looping, region cuts, work count, track index, payload identity, blend pairs, factor ranges, and state possibilities.
- Emits a balanced region decision tree rather than a generic search followed by a work-table interpreter.
- Fully inlines timelines with at most 64 generated work sites and switches above that limit to one direction-specific compact track program shared by scalar and batch entry points.
- Merges adjacent per-track intervals with identical work in the compact fallback, so emitted work grows with authored segments instead of the region-by-active-work expansion.
- Emits track and clip payloads as typed static readonly fields with constructor-once semantics.
- Calls `TResult.Forward` or `TResult.Backward` directly at each generated work site.
- Emits the legacy `.Compile()` shim only for projects that still use a legacy declaration.
- Retains deterministic content hashing, stable timestamps, stale-owned-output cleanup, and diagnostics for unsupported authoring.

### Surface

- Added `ITimeline<TTrack,TClip>` as the declaration law.
- Added `ITrack<TTrack,TClip,TInput,TResult>` as the combined directional operation law.
- Changed `IForward` and `IBackward` to static per-work operations.
- Removed the separate `Tl.Compiled` consumer interfaces.
- Preserved `ordinal`, work `count`, authored track `index`, typed track, typed resolved clip, movement state, effective tick, input, and mutable result.

This is a pre-1.0 breaking change. Existing consumers replace their per-tick `foreach` body with one static per-work body. `ordinal == 0` expresses once-before-work logic, `ordinal == count - 1` expresses once-after-work logic, and ordinal/index conditions express slices and filters without constructing a view. Consumers that require random access or multiple passes over the complete resolved set need an explicit future tick-phase contract or caller-owned storage.

## Measured comparison

BenchmarkDotNet v0.15.8 ran on .NET 10.0.11, x64 RyuJIT x86-64-v3, an i9-14900K, 16 warmups, 12 target iterations, and a requested 250 ms iteration time. Each operation processes an observable dependent stream and returns playback/result receipts. MemoryDiagnoser reported no managed allocation.

| Method | Tiered JIT median | No tiering median |
| --- | ---: | ---: |
| Interpreter full scalar | 15.340 ns | 28.283 ns |
| Compiled full scalar | 4.489 ns | 6.322 ns |
| Interpreter full batch 8 | 11.231 ns | 16.830 ns |
| Compiled full batch 8 | 4.104 ns | 5.891 ns |
| Interpreter sum scalar | 11.157 ns | 25.332 ns |
| Compiled sum scalar | 2.473 ns | 2.480 ns |

Tiered-JIT speedups are 3.42x for the full scalar consumer, 2.74x for full batch throughput, and 4.51x for the minimal sum consumer. The full consumer performs tick accumulation, count updates, state counters, and state-dependent floating-point work, so its higher floor is real application work.

The previous v0.5 exact-operation research remains a separate controlled result:

| Specialized research workload | Result |
| --- | ---: |
| Ordered sum, sequential batch 8 | 1.195 ns/tick |
| State and runtime input, sequential batch 8 | 1.843 ns/tick |
| Ordered sum, random batch 8 | 7.071 ns/tick |
| State and runtime input, random batch 8 | 9.335 ns/tick |

Random seeks are dominated by region choice and branch misses. Earlier hardware counters measured about 7.60 cycles/tick for predictable batches and 40.09 cycles/tick for random batches, with roughly 0.0029 versus 1.339 branch misses per tick. No API rename can erase unpredictable information.

## The 1–2 ns limit

At a nominal 4–6 GHz, 1–2 ns is roughly 4–12 core cycles. A tick that must normalize a loop, determine a region, derive movement state, load a payload, mutate a result, and construct the next playback has very little budget. Independent instructions can overlap, but dependent loads, division, mispredictions, and ordered floating-point effects cannot.

The lower bound depends on observable behavior:

```cs
result.Sum += clip.Amount;
```

can approach the throughput target when the region is predictable and several ticks share one call. This operation:

```cs
result.Audit = Notify(result.Audit, index, state, tick, input.Scale);
```

must execute the call in order. The compiler cannot delete it, reorder it across earlier effects, or precompute a runtime input. A thrown exception must expose exactly the mutations that happened before it.

There is also no computable universal smallest representation for arbitrary authored programs. Finding the smallest equivalent program is a form of Kolmogorov minimization and is undecidable in general. For restricted timeline data, the information floor is the entropy of the selected track identities, boundaries, payload bit patterns, ordering, and semantics. Equal payloads and equal row runs can be canonicalized; distinct observable bits cannot be removed without restricting the language.

The practical target is therefore three explicit tiers:

| Tier | Contract | Expected domain |
| --- | --- | --- |
| General runtime | Dynamic authored data, native CSR-like tables, indirect bound operation | 10–30 ns/tick |
| General compiled | Named timeline, arbitrary typed `ITrack`, direct region blocks or compact per-track intervals | 3–8 ns predictable scalar; higher for random or heavy work |
| Restricted fused | Known operation algebra, profile-selected batch/cursor backend | 1–2 ns/tick for simple predictable throughput |

The current implementation completes the first two tiers and provides measured proof for the third tier's feasibility.

## Algorithm and data-structure policy

Runtime storage is cut-based sparse partitioning. Sorted unique boundaries define regions. Each region references a contiguous ordered work run. Each work stores track and clip identities plus movement and blend facts. This avoids `duration × tracks` dense storage and makes retained size proportional to authored structure.

Build-time compilation removes tables that become code constants. Small kernels use a balanced region tree because its storage cost is zero and random lookup depth is logarithmic. Kernels above 64 expanded work sites use a compact per-track interval program: adjacent intervals with identical work collapse into one segment, and scalar and batch methods share one helper per direction. Its source size is linear in the authored per-track segments rather than the sum of active works across all cut regions. A dense dispatch table remains useful only when duration is small and random traffic dominates; it costs proportional to duration and lost on predictable Pulse streams. A compiled sequential cursor should make the common case one comparison against the next cut.

Payload and work-run deduplication must use equality after hashing. Hash equality alone is incorrect. Dedup is profitable when repeated payload bytes or long repeated region rows outweigh one level of indirection. Generated code must deduplicate constants and basic blocks subject to a measured instruction-cache budget.

Backend selection should minimize a declared cost rather than always choosing the fastest isolated microbenchmark:

```text
cost = hot_cycles
     + random_seek_weight * branch_miss_cycles
     + code_byte_weight * emitted_native_bytes
     + data_byte_weight * retained_bytes
```

Weights belong to the target build profile. A mobile AOT build, a desktop JIT build, and a server simulation do not have the same optimum.

## Experiment verdicts

| Technique | Verdict | Evidence |
| --- | --- | --- |
| Per-work static operation | Production | Removes views and enables consumer inlining; 4.51x sum speedup. |
| True scalar generated entry | Production | Avoids scalar-to-span wrapper in compiled hot path. |
| Separate direction kernels | Production | Prior controlled run improved five of six shapes; accepted code-size tradeoff. |
| Balanced generated region branches | Production | No region table and logarithmic random selection. |
| Input-linear compact fallback | Production | Small timelines stay fully inline; kernels above 64 expanded work sites merge identical adjacent track intervals and share one compact helper per direction across scalar and batch. |
| Batch inlining | Conditional | Reached 1.195 ns sum and 1.843 ns state throughput; effect/random regressed 3.3%. |
| Dense dispatch data | Conditional | Helped random seeks but cost 7,800 bytes and lost predictable cases. |
| Maximal same-region unrolling | Rejected | Slower in every measured stream and expanded generated C# from 18,651 to 44,175 bytes. |
| Generic direct-span reshaping | Rejected | Regressed sequential and repeated scalar calls. |
| Prefix cut counts | Rejected | Added storage without a reliable playback gain. |
| Persistent resolved-work views | Removed | View construction and repeated clip access dominated simple consumers. |
| Blend scratch arrays | Removed | Streaming needs one local blend value and no scratch lifetime API. |
| Storage deduplication | Opt-in | Real retained-byte gain; warm indirection and cold build cost depend on content. |
| `switch` for regions | Profile-dependent | The JIT may emit compares, a jump table, or a tree. Density and predictability decide. Generated balanced branches are the stable default. |

C# has no portable public equivalent of Burst's `Hint.Likely()` that guarantees branch layout. Dynamic PGO and tiered compilation collect real branch behavior under the JIT. `MethodImplOptions.AggressiveOptimization` changes optimization tiering, not branch probability. The generator can express hot fall-through layout when a build profile provides probabilities; it must measure the native result on both x64 and ARM64.

## Memory and code-size budget

The exact current `src` receipt is:

```text
files=29
content=177443
paths=856
total=178299
budget=200000
remaining=21701
```

The release `Tl.Core.dll` is 40,448 bytes and `Tl.Gen.dll` is 108,544 bytes. The `Tl.Runtime` archive is about 24 KB compressed and contains 50,930 uncompressed bytes. The build-only `Tl.Gen` archive is about 3.77 MB compressed and contains 10,442,506 uncompressed bytes because it carries Roslyn and Waffle beside the executable tool. None of the generator assemblies enter application output, where the clean package consumer contains only the 40,448-byte runtime library. The generated Pulse timeline is 38,829 C# bytes. Generated application code is outside the 200 KB library-source rule, but it is not free: every backend decision must separately report emitted C# bytes, native method bytes, and the working set across many timelines.

The runtime timeline is one 16-byte-aligned native block containing its header, region starts and rows, unmanaged track values, deduplicated unmanaged clip values, materialized work slots, and inline native binding slots. Intermediate track rows, clip rows, edges, and payload maps die after lowering. Overflow binding storage is also native and explicitly freed. Runtime authoring uses temporary managed collections, but no managed authoring graph survives `.InMemory()`.

`Playback`, `Cursor`, track payloads, clip payloads, generated constants, and retained runtime data contain no GC references. `TInput` and `TResult` remain caller-owned generic structs, so a normal .NET application may use managed fields without storing them in the timeline. ECS users can and should provide unmanaged input and result components. Enforcing Burst's full subset requires a Unity-specific build and verification package rather than pretending NativeAOT proves Burst compatibility.

## API fault analysis

The former view API was a performance fault for the target. It required view construction, iterator/indexer machinery, deferred clip resolution, scratch ownership, and a once-per-tick callback boundary even when the consumer wanted one addition. That abstraction has been removed.

The current `ITrack` API carries every fact needed to recreate ordinary single-pass enumeration logic: work order, total count, authored index, typed track, resolved clip, movement state, tick, input, and result. The first and last ordinals provide deterministic begin/end points. Random access and multiple passes are intentionally absent because they require retaining or recomputing the resolved set.

Public lifecycle validation remains because default and stopped `Playback` values are part of the current semantics. Those highly predictable branches are not the dominant remaining cost. A future zero-check API should use distinct `RunningPlayback` and `StoppedPlayback` value types so invalid states are unrepresentable; an `Unchecked` method on the same weak type would move bugs into memory-adjacent game code.

## Definition of done for v0.6

- [x] One consumer contract for interpreted and compiled execution.
- [x] Named partial timeline declaration with no `Authoring.Author` or placeholder field.
- [x] Direct per-work production kernel.
- [x] Scalar and batch generated entry points.
- [x] No managed arrays in a compiled timeline.
- [x] Unmanaged retained runtime storage.
- [x] Zero managed allocation during warmed valid playback.
- [x] Bit-exact interpreter/generated parity across direction, jumps, loops, gaps, batches, and lifecycle errors.
- [x] Full solution build with zero warnings and errors.
- [x] 43 core and 60 generator tests passing.
- [x] Runtime NativeAOT smoke application passing.
- [x] Named compiled timeline parity battery passing under NativeAOT.
- [x] BenchmarkDotNet comparison with full receipts and MemoryDiagnoser.
- [x] Source plus paths below 200,000 bytes.
- [x] Package versions and release notes advanced to 0.6.0.
- [x] Both NuGet packages created and consumed from a clean package-only project.

## Next compiler work

1. Add a typed, closed operation algebra for common ECS mutations and generate the operation body into each region block. Keep arbitrary `ITrack` as fallback.
2. Add a compiled sequential cursor whose common transition is `tick < nextCut`; fall back to the branch tree for jumps and wraps.
3. Generate batch kernels in bounded widths selected by profile. Inline only when native code-size and workload measurements justify it.
4. Add a many-timeline native code-size benchmark and select tree, dense, compact, or fused blocks under an explicit profile budget. The current threshold replaces region expansion with input-linear per-track segments, but it does not solve working-set selection across a full game.
5. Add disassembly receipts for x64 JIT, x64 NativeAOT, ARM64 NativeAOT, and eventually Burst.
6. Build a Unity package with unmanaged component examples and Burst compilation gates. Do not infer Burst support from .NET NativeAOT.
7. Replace legacy `.Compile()` input after one migration release, recovering its generator and shim bytes.

Every future speed claim must include the same semantic receipt, allocation result, generated bytes, native bytes, machine identity, stream distribution, and interpreter comparison. A result that removes required state, effects, validation, or output is a different product mode and receives a different name.
