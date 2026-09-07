# tl

Timelines compiled into C#, with typed struct hooks and caller-owned playback state.

Designers author tracks and clips. A build-time adapter uses Waffle to emit C#;
applications consume the resulting timeline through `IBlend`, `IForward` and
`IBackward`. Runtime authoring is also part of the design. Blob export is not
required.

**Status: v0.1 Core and Generator Extracted.** The core library (`src/Tl.Core`)
and build-time code generator (`src/Tl.Gen`) are extracted, fully tested, and verified
against NativeAOT. Baseline benchmark verification receipts remain intact.

## Packages

| Package | Version | Description |
| --- | --- | --- |
| `Tl.Runtime` | `0.1.0` | Core timeline runtime, builder, copy-on-write registry, and zero-allocation playback engine. |
| `Tl.Gen` | `0.1.0` | Build-time MSBuild generator emitting compiled C# timeline tables and specialized kernels. |

## Consumer API

A track blends its clip type. A consumer handles forward and backward work.
`Tracks` provides each active track, its resolved clip and its `Enter`, `Stay`
or `Exit` state. The consumer decides what those states do.

This complete example runs against `Tl.Core` (`Tl` namespace).

```cs
using Tl;

ushort id = Timeline<HealthTrack, HealthClip>.Build(static builder =>
{
    var track = builder.Track(new HealthTrack());
    builder.Clip(in track, new HealthClip(2f), start: 0, end: 4);
});

Timeline<HealthTrack, HealthClip>.Bind<Health>(id);

var health = new Health();
var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, ref health, 0u, 1u, 2u, 3u);

Console.WriteLine(health.Value); // 6: ticks 0, 1 and 2 are Stay; tick 3 is Exit.

playback = Timeline.Stop(id, in playback);
Timeline.Destroy(id);

public readonly record struct HealthClip(float Amount);

public readonly struct HealthTrack : IBlend<HealthClip>
{
    public void Blend(in HealthClip first, in HealthClip second,
        float t, out HealthClip result)
    {
        result = new HealthClip(first.Amount + (second.Amount - first.Amount) * t);
    }
}

public struct Health :
    IForward<HealthTrack, HealthClip, Health>,
    IBackward<HealthTrack, HealthClip, Health>
{
    public float Value;

    public void Forward(ref Health data,
        in Tracks<HealthTrack, HealthClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                data.Value += work.Clip.Amount;
    }

    public void Backward(ref Health data,
        in Tracks<HealthTrack, HealthClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                data.Value -= work.Clip.Amount;
    }
}
```

`Bind<Health>` explicitly registers the closed consumer type before playback;
this is the intended AOT path. The example chooses to apply only `Stay` work.
Backward behavior is application code, not automatic undo. See the
[semantics](docs/semantics.md) before writing consumers.

## What v0.1 preserves

- Three consumer hooks, typed struct calls and `ref` mutation of caller data.
- `uint` ticks, `[start, end)` clip windows and an 8-byte `Playback`.
- One hook call per non-empty destination tick; one resolved work per active track.
- Runtime region tables, a caller-owned cursor and bounded blend scratch.
- Generated C# tables and specialization where its semantics are proven.
- Zero library allocations during valid playback after build/bind/initialization,
  subject to release verification. User hooks may allocate independently.

Build, binding, generation and registry growth may allocate. Large blends use
caller-provided scratch. Storage deduplication remains opt-in. Generated-table
infrastructure is separate from the three consumer hooks.

The first library target is .NET 10. NativeAOT is fully supported and verified;
the test suite and smoke harness publish and execute native binaries under NativeAOT.
Unity and WebAssembly integration are future adapter/target work, with no compatibility claim for v0.1.

## NativeAOT and Code Generation

`tl` is designed for strict NativeAOT compilation and zero-allocation execution:

1. **AOT Consumer Binding**: Dynamic code generation is avoided under NativeAOT by explicitly registering consumer types:
   ```cs
   Timeline<HealthTrack, HealthClip>.Bind<Health>(id);
   ```
   If a consumer is not bound under NativeAOT, a clear exception is thrown explaining the need for `Bind<TConsumer>()`. Under JIT runtimes, dynamic dispatch bridges bind automatically on first use.
2. **Build-Time Generation (`Tl.Gen`)**: The `Tl.Gen` MSBuild package compiles declarative `.def` files directly into C# source at build time:
   ```xml
   <ItemGroup>
     <TimelineDefinition Include="Cutscene.def">
       <Namespace>Game.Animations</Namespace>
     </TimelineDefinition>
   </ItemGroup>
   ```
   It supports two emission strategies:
   - `Table`: Emits static read-only struct tables implementing `ITrackTables<TTrack, TClip>` for standard runtime dispatch.
   - `Bake`: Emits unrolled specialized playback kernels with inline blending, branch pruning, and strict preservation of floating-point arithmetic order.

## Defects Resolved in v0.1

During extraction from the prototype into `Tl.Core` and `Tl.Gen`, six critical defects were identified and resolved:

- **Defect A: Timeline duration overflow**:
  Duration is now represented as `uint` throughout all entry records, builder validation, compiler passes, and public query APIs (`Timeline.Duration(id)`), eliminating 31-bit integer truncation.
- **Defect B: Unsafe pointer tracking during dispatch**:
  Removed all `Unsafe.AsPointer(ref data)` and `Unsafe.AsPointer(ref cursor)` casts that bypassed GC tracking for references into managed heap objects. The dispatch pipeline now passes true managed references (`ref TData`, `ref Cursor`) throughout typed function pointers (`delegate*<Entry, in Playback, ref TData, ...>`), with zero unsafe indexing or GC pinning.
- **Defect C: Scratch forward/backward payload type mismatch**:
  `Timeline<TTrack, TClip>.Forward` and `Backward` methods accepting caller scratch buffers now explicitly validate that the timeline payload is a valid table representation (`if (entry.Payload is not Tables) throw new ArgumentException(...)`), preventing invalid memory access when dispatched on non-table timeline entries.
- **Defect D: Hub snapshot publication race and entry destruction race**:
  Replaced mutable shared array allocations with thread-safe copy-on-write snapshot publication (`Volatile.Write(ref s_runs, next)` and atomic snapshot reads). Slot indexes are monotonic and non-reusable; `Destroy` uses copy-on-write tombstoning to prevent race conditions or use-after-free bugs.
- **Defect E: Builder cross-instance track leakage**:
  `TrackRef` structs carry an internal `Owner` reference. `TimelineBuilder.Clip` verifies `ReferenceEquals(track.Owner, _state)`, rejecting attempts to add clips using track references created by a different builder instance.
- **Defect F: Code generation safety and budget enforcement**:
  The `Tl.Gen` pipeline validates clip budgets, structural correctness, and timeline bounds prior to code generation. Generated `Bake` kernels preserve exact float order-of-operations (no illegal reassociations) and include safety guards.

## Measurements, with scope

These are committed research results, not a new benchmark run or a guarantee
for arbitrary timelines and hooks. Times are normalized per tick.

| Recorded experiment | Result | Meaning |
| --- | --- | --- |
| Bake v3 `Fused16Batch8`, BenchmarkDotNet | 1.088 ns JIT / 1.061 ns NoTiering | Specialized frozen fixture |
| Same named fixture, separate manual harness | 1.064 ns JIT / 1.094 ns NativeAOT | Cross-runtime comparison within that harness |
| Persistent runtime cursor | 1.7–2.7× on measured sequential cases | Keep opt-in; random access did not universally improve |
| Storage deduplication | About 6× less retained storage on the duplicate-heavy fixture | Keep default off; build cost and small-case playback can rise |

The **sub-1 ns target remains unmet** for the full-state Fused16 result above.
The receipts identify a serial float dependency and useful optimization limits;
they do not prove a universal lower bound or that sub-1 ns is impossible.
Starting `src` uses correctness and measured regression gates. Sub-1 ns remains
a research target rather than a claim needed to begin v0.1.

See [benchmark history](docs/benchmarks.md), [optimization verdicts](docs/review.md)
and the [completed experiment queue](docs/faster.md). Sampling-only measurements
from other repositories are not substitutes for full playback measurements here.

## Check the prototype

From the repository root, using the .NET 10 SDK:

```sh
dotnet run --project benchmarks/Dispatch -c Release -- --verify
dotnet run --project benchmarks/Dispatch -c Release -- --verify-edges
dotnet run --project benchmarks/Algorithms -c Release -- --verify
dotnet run --project benchmarks/Review -c Release -- --verify
dotnet run --project benchmarks/AotBench -c Release -- --check
```

These run correctness receipts. The last command runs the AOT harness under the
JIT; native validation requires publishing and executing a NativeAOT binary.
Initial builds may restore packages and generate fixtures. Waffle is currently
a build-time dependency of the generator projects.

CI runs full solution build, test suites (`dotnet test`), published NativeAOT binary smoke testing,
all five baseline verification commands, and package packing.

## Start here

- [v0.1 handoff](docs/v0.1.md): ordered tasks, source map, fixes and acceptance gates.
- [Semantics](docs/semantics.md): the behavior to preserve during extraction.
- [Adapters](docs/adapters.md): build-time boundaries and C# output.
- [Roadmap](docs/roadmap.md): current scope and deferred work.

`docs/api/` is an earlier API discussion mock. Historical benchmark implementations
are evidence, not competing library specifications. Use the handoff when they
conflict with the current direction.
