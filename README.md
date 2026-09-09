# tl

`tl` compiles authored tracks and clips into direct C# playback kernels. It also provides an unmanaged runtime interpreter for timelines created from data at run time. Both paths use the same typed consumer contract and preserve the same playback semantics.

**Status: v0.6.** The runtime and generator target .NET 10 and are verified under JIT and NativeAOT. The library source footprint is 178,299 of 200,000 bytes including relative paths.

## Packages

| Package | Version | Purpose |
| --- | --- | --- |
| `Tl.Runtime` | `0.6.0` | Authoring, unmanaged runtime storage, playback, and typed operations. |
| `Tl.Gen` | `0.6.0` | Build-time C# timeline compiler and deterministic output cache. |

## Compiled timeline

Declare one named partial value type. `Define` is the complete authoring program.

```cs
using Tl;

namespace Game;

public readonly record struct HealthClip(float Amount);

public readonly struct HealthTrack : IBlend<HealthClip>
{
    public void Blend(in HealthClip first, in HealthClip second, float t, out HealthClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * t);
}

public readonly partial struct HealthTimeline : ITimeline<HealthTrack, HealthClip>
{
    public static void Define(scoped TimelineBuilder<HealthTrack, HealthClip> timeline)
    {
        var health = timeline.Track(new HealthTrack());
        timeline.Clip(in health, new HealthClip(2f), 0u, 4u);
    }
}
```

Include the declaration in the generator input:

```xml
<ItemGroup>
  <PackageReference Include="Tl.Runtime" Version="0.6.0" />
  <PackageReference Include="Tl.Gen" Version="0.6.0" PrivateAssets="all" />
  <TlCompileTimeline Include="HealthTimeline.cs" />
</ItemGroup>
```

The build emits the other half of `HealthTimeline`. Application code calls the named type directly:

```cs
var input = new HealthInput(100f);
var result = new HealthResult { Value = input.Seed };
var playback = HealthTimeline.Start();
playback = HealthTimeline.Forward(in playback, in input, ref result, 0u, 1u, 2u, 3u);
playback = HealthTimeline.Stop(in playback);
```

An unchanged source set is a generation-cache hit. Identical output keeps its timestamp. Deleted declarations remove only generator-owned unchanged files. A project containing only named partial timelines receives no legacy compatibility source.

## Consumer contract

`ITrack` receives one resolved active track at a time. It replaces the former `Tracks<TTrack,TClip>` view and the separate compiled callback API.

```cs
public readonly record struct HealthInput(float Seed);

public struct HealthResult : ITrack<HealthTrack, HealthClip, HealthInput, HealthResult>
{
    public float Value;

    public static void Forward(
        int ordinal,
        int count,
        ushort index,
        in HealthTrack track,
        in HealthClip clip,
        ClipState state,
        in uint tick,
        in HealthInput input,
        ref HealthResult result)
        => result.Value += state == ClipState.Stay ? clip.Amount : 0f;

    public static void Backward(
        int ordinal,
        int count,
        ushort index,
        in HealthTrack track,
        in HealthClip clip,
        ClipState state,
        in uint tick,
        in HealthInput input,
        ref HealthResult result)
        => result.Value -= state == ClipState.Stay ? clip.Amount : 0f;
}
```

`ordinal` is the work position in the destination tick, `count` is the number of active works, and `index` is the authored track index. `track` and `clip` are typed values. `state` is `Enter`, `Stay`, or `Exit`. Input is read-only; result is caller-owned mutable state. `ordinal == 0` and `ordinal == count - 1` replace begin/end logic from the former per-tick callback. `IForward` and `IBackward` remain available for direction-specific generic constraints; `ITrack` combines them.

The compiler specializes direction, loop behavior, region selection, work count, track and clip identity, blend shape, movement state, and payload access. The generated scalar entry point stays scalar. Batch methods retain playback locals across the span. Static abstract calls allow the JIT and NativeAOT compiler to inline the closed consumer operation into the generated kernel.

## Runtime-authored timeline

Use the interpreter when the timeline data is unavailable at build time:

```cs
ushort id = Timeline<HealthTrack, HealthClip>.Build(HealthTimeline.Define).InMemory();
Timeline<HealthTrack, HealthClip>.Bind<HealthInput, HealthResult>(id);

var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, in input, ref result, 0u);
playback = Timeline.Stop(id, in playback);
Timeline.Destroy(id);
```

`.InMemory()` lowers the authored graph into one explicitly owned native block. It contains region data, work slots, unmanaged track and clip payloads, and native binding slots. Playback is read-only over that immutable snapshot and allocates zero managed bytes after binding. `Destroy` must run exactly once and must not race playback. Handles are process-local and never reused.

`TTrack` and `TClip` are constrained to `unmanaged`. `Playback`, `Cursor`, and generated timeline state contain no managed references. `TInput` and `TResult` are caller-owned and may also be unmanaged ECS components; the runtime never retains either value. Unity Burst remains a separate toolchain qualification because it does not implement the complete .NET 10 runtime surface.

## Measured result

BenchmarkDotNet v0.15.8, .NET 10.0.11, x64 RyuJIT x86-64-v3, i9-14900K, 16 warmups, 12 measured iterations, 250 ms per iteration:

| Operation | Runtime interpreter | Compiled kernel | Speedup |
| --- | ---: | ---: | ---: |
| Full consumer, scalar | 15.340 ns | 4.489 ns | 3.42x |
| Full consumer, batch 8 | 11.231 ns | 4.104 ns | 2.74x |
| Sum consumer, scalar | 11.157 ns | 2.473 ns | 4.51x |

Every arm reported zero managed allocation. Each invocation starts from fresh state, consumes a fixed tick sequence, returns the complete playback and consumer receipt, and checks interpreter/compiled parity during setup. The earlier exact-consumer research kernel remains useful evidence: predictable batches reached 1.195–1.344 ns/tick. The production kernel preserves arbitrary typed `ITrack` behavior and currently lands at about 2.5 ns for a minimal consumer and 4.5 ns for the full receipt consumer. See the [design report](https://github.com/IAFahim/tl/blob/v0.6.0/plan.md) for the physical floor, rejected approaches, and the next specialization tiers.

## Semantics

- Ticks are `uint`; clip windows are half-open `[start, end)`.
- A track may have at most two simultaneous clips. A pair is blended once per work.
- Work is delivered in authored track order.
- Empty destinations invoke no consumer operation.
- A large jump samples the destination and movement facts; it does not replay every crossed clip.
- `Playback` is 8 bytes and caller-owned.
- Compiled and interpreted paths are bit-exact for the same authored timeline and consumer.
- Callback effects before an exception remain observable.

The detailed contract is in the [semantics reference](https://github.com/IAFahim/tl/blob/v0.6.0/docs/semantics.md). The release changes and evidence are in the [v0.6 notes](https://github.com/IAFahim/tl/blob/v0.6.0/docs/v0.6.md).

## Verification

```sh
dotnet build tl.slnx -c Release --no-restore -m:1
dotnet test tl.slnx -c Release --no-build --no-restore -m:1
dotnet run --project samples/Compiled -c Release --no-build
dotnet run --project benchmarks/Dispatch -c Release --no-build -- --verify
dotnet run --project benchmarks/Algorithms -c Release --no-build -- --verify
dotnet publish samples/Compiled/Compiled.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

Historical release notes and benchmark artifacts remain under `docs/` and `benchmarks/`. They describe the API and hypotheses at the commit where each result was measured.
