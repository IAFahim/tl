# tl

Timelines compiled into C#, with typed struct hooks and caller-owned playback state.

Designers author tracks and clips. A build-time adapter uses Waffle to emit C#;
applications consume the resulting timeline through `IBlend`, `IForward` and
`IBackward`. Runtime authoring is also part of the design. Blob export is not
required.

**Status: v0.5.** The runtime and build-time generator target .NET 10 and are
verified under JIT and NativeAOT. Runtime-authored timelines lower into
explicitly owned native memory. Generated `.Compile()` outputs are
deterministic, repairable and stricter about which declarations they accept.
See the [v0.5 release notes](docs/v0.5.md).

## Packages

| Package | Version | Description |
| --- | --- | --- |
| `Tl.Runtime` | `0.5.0` | Core timeline runtime, authoring API, native timeline storage, and zero-allocation playback engine. |
| `Tl.Gen` | `0.5.0` | Build-time MSBuild generator emitting compiled C# timeline tables and specialized kernels. |

## Consumer API

A track blends its clip type. A consumer handles forward and backward work.
`Tracks` provides each active track, its resolved clip and its `Enter`, `Stay`
or `Exit` state. The consumer decides what those states do. Consumer data is
split in two: an immutable `in TInput` (read-only context, e.g. a seed) and a
mutable `ref TResult` (the live state, which also implements the hooks).

This complete example runs against `Tl.Core` (`Tl` namespace).

```cs
using Tl;

ushort id = Timeline<HealthTrack, HealthClip>.Build(static builder =>
{
    var track = builder.Track(new HealthTrack());
    builder.Clip(in track, new HealthClip(2f), start: 0, end: 4);
}).InMemory();

Timeline<HealthTrack, HealthClip>.Bind<HealthInput, HealthResult>(id);

var input = new HealthInput(Seed: 100f);
var health = new HealthResult { Value = input.Seed };
var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, in input, ref health, 0u, 1u, 2u, 3u);

Console.WriteLine(health.Value);

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

public readonly record struct HealthInput(float Seed);

public struct HealthResult :
    IForward<HealthTrack, HealthClip, HealthInput, HealthResult>,
    IBackward<HealthTrack, HealthClip, HealthInput, HealthResult>
{
    public float Value;

    public void Forward(in Tracks<HealthTrack, HealthClip> tracks,
        in HealthInput input, in uint tick, ref HealthResult result)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                result.Value += work.Clip.Amount;
    }

    public void Backward(in Tracks<HealthTrack, HealthClip> tracks,
        in HealthInput input, in uint tick, ref HealthResult result)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                result.Value -= work.Clip.Amount;
    }
}
```

`Bind<HealthInput, HealthResult>` explicitly registers the closed input/result
pair before playback; this is the intended AOT path. The engine treats the
input as read-only and never writes through it; all mutation flows through the
`ref` result. The example chooses to apply only `Stay` work. Backward behavior
is application code, not automatic undo. `Build(...)` is authoring syntax;
`.InMemory()` creates and registers the runtime timeline. `Timeline.Destroy(id)`
releases its native allocation. See the [semantics](docs/semantics.md) and
[v0.5 release notes](docs/v0.5.md) before writing consumers.

## What v0.5 provides

- Three consumer hooks, typed struct calls and `ref` mutation of the caller's
  result, now alongside an immutable `in TInput`.
- `uint` ticks, `[start, end)` clip windows and an 8-byte `Playback`.
- One hook call per non-empty destination tick; one resolved work per active track.
- Runtime region tables in one native allocation, a caller-owned cursor and
  bounded blend scratch.
- Generated C# tables and direction-specialized playback while preserving the
  existing public generated method signatures.
- Deterministic generation caching that repairs missing or changed owned
  outputs, removes only unchanged obsolete owned outputs, and leaves unowned
  files alone.
- Stricter `.Compile()` declaration discovery, name resolution, collision
  checks and diagnostics for unsupported forms.
- A 24-byte runtime work slot, down from 28 bytes by removing field padding.
- Zero library allocations during valid warmed playback. User hooks may
  allocate independently.

Build, binding, generation and registry growth may allocate. Large blends use
caller-provided scratch. Storage deduplication remains opt-in. Generated-table
infrastructure is separate from the three consumer hooks.

Each `.InMemory()` call owns one native block. Handles are never reused, and
`Timeline.Destroy(id)` must run exactly once after playback has stopped. Build,
registration, explicit binding and destruction must be single-threaded or
externally synchronized with playback. Playback reads an immutable snapshot
and can run concurrently from any number of threads. Destroying a timeline
while playback is in flight for that handle is invalid and can cause a
use-after-free.

The first library target is .NET 10. The test suite and smoke harness publish
and execute native binaries under NativeAOT. Unity, Burst and WebAssembly are
separate future qualifications; v0.5 makes no compatibility claim for them.

## NativeAOT and Code Generation

`tl` is designed for strict NativeAOT compilation and zero-allocation execution:

1. **AOT Consumer Binding**: Dynamic code generation is avoided under NativeAOT by explicitly registering consumer types:
   ```cs
   Timeline<HealthTrack, HealthClip>.Bind<HealthInput, HealthResult>(id);
   ```
   If a consumer pair is not bound under NativeAOT, a clear exception is thrown explaining the need for `Bind<TInput,TResult>()`. Under JIT runtimes, dynamic dispatch bridges bind automatically on first use.
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

The package also supports C# authoring discovered from
`Timeline<TTrack,TClip>.Build(...).Compile()`. That path emits a generated
static kernel during the consumer build. v0.5 keeps its public surface intact
while making output reuse deterministic and specializing forward and backward
direction internally. Unsupported or ambiguous declaration forms fail closed
with diagnostics.

## Production evidence

The v0.5 release gates passed 46 core tests, 52 generator tests, the compiled
interpreter/generated parity sample, a published NativeAOT execution, local
package consumption from a clean package cache, and repeat installed-package
generation with a cache hit. The library source footprint is
186,154/200,000 bytes including paths.

On the recorded Pulse kernel comparison, direction specialization improved
five of six measured shapes and regressed repeated single-tick throughput by
about 1.8%. The accepted storage result is exact: field reordering reduces each
runtime work slot from 28 to 24 bytes, or 14.3%. Timing results are machine and
workload measurements, not universal performance promises. See the
[v0.5 release notes](docs/v0.5.md) and [benchmark history](docs/benchmarks.md).

## Experimental research

A separate consumer-fusion prototype recognizes one exact Pulse payload,
blend law and ordered sum/subtract operation. Its tree/carry batch measured
about 1.3 ns/tick for sequential and repeated inputs and about 7.5 ns/tick for
random inputs; the scalar entry point measured about 2.6 ns/tick. All reported
zero managed allocation after setup.

That prototype is research, not the production `.Compile()` backend. It does
not analyze arbitrary consumer C#, change the public API, establish a universal
2 ns result, or establish Unity or Burst compatibility. Production generated
playback still uses the general view/callback path and retained arrays.

## Verify the release

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
Initial builds may restore packages and generate fixtures. Waffle is a
build-time dependency of the generator projects.

CI runs full solution build, test suites (`dotnet test`), published NativeAOT binary smoke testing,
all five baseline verification commands, and package packing.

## Start here

- [v0.5 release notes](docs/v0.5.md): current changes, evidence and limits.
- [v0.4 unmanaged runtime](docs/v0.4-unmanaged.md): native ownership and threading contract.
- [v0.4 compiled path](docs/v0.4-compile.md): `.Compile()` grammar, behavior and receipts.
- [v0.1 handoff](docs/v0.1.md): ordered tasks, source map, fixes and acceptance gates.
- [Semantics](docs/semantics.md): the behavior to preserve during extraction.
- [Adapters](docs/adapters.md): build-time boundaries and C# output.
- [Roadmap](docs/roadmap.md): current scope and deferred work.

`docs/api/` is an earlier API discussion mock. Historical benchmark implementations
are evidence, not competing library specifications. Use the handoff when they
conflict with the current direction.
