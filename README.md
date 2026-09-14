# tl

**tl** compiles designer-authored timeline data into deterministic execution — JSON in, baked `.tlb` bytes out, advanced by a small unmanaged runtime on .NET and Unity.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Timeline data and timeline behavior are separate. Designers author tracks, clips, windows, and loop points as JSON; a deterministic `tlbake` compile produces canonical TLB1 assets; typed C# consumers (`ITimelineJob<TTrack,TClip>`) describe what one active `(track, clip)` pair does to borrowed component storage. The runtime has no reflection, no delegates on warm paths, no runtime compilation, and no warm-path allocation — the same baked bytes drive .NET and Unity.

- **Deterministic bake** — same inputs, same bytes, on every machine and culture; content-keyed cache hits preserve timestamps
- **Heterogeneous assets** — tracks and clips of different types in one asset; execution order is authored order (A-B-A preserved)
- **Total signed `Tick`** — forward, backward, and clamped movement over borrowed component columns, committed once per crossed frame
- **Typed frame queries** — a read-only stage view over the row's currently selected step; never advances time
- **Kernel-compiled assets** — baked assets bind to generated kernels by content hash; the interpreter is the verified fallback
- **NativeAOT-safe** — no generator assemblies in application output; one shared domain file compiles for both .NET and Unity
- **Flawless install** — `dotnet add package` and run; package targets configure consuming projects automatically

## Supported environments

| Environment | Install | Status |
| --- | --- | --- |
| .NET 10 (JIT and NativeAOT) | NuGet packages (below) | receipted |
| Unity 6000+ (Mono, IL2CPP, Burst jobs) | UPM package `com.iafahim.tl` from [tl.unity](https://github.com/IAFahim/tl.unity) | EditMode-receipted |

The packages are development prereleases on [nuget.org](https://www.nuget.org/).

## Quick Start

#### 1. Install

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.5
dotnet tool install --global Tl.Bake --prerelease --version 1.0.0-alpha.5
```

For an offline install, copy the `.nupkg` files into a local `packages/` folder and append `--source ./packages` to both commands.

`Tl.CSharp` brings the runtime and the build-time generator, which discovers your consumers on every compilation — including IDE design-time builds — and sets consuming projects up automatically.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# install: runtime plus build-time consumer binding |
| `Tl.Runtime` | Small declaration, frame, state, and movement ABI |
| `Tl.Gen.CSharp` | Build-time generator that binds typed consumers |
| `Tl.Bake` | `dotnet tool` (command: `tlbake`): JSON to baked TLB1 assets |

#### 2. Define the domain

You author the domain values and one typed consumer per `(track, clip)` pair. `Tl` supplies `IBlend<TClip>`, `ITimelineJob<TTrack,TClip>`, and `Frame<TTrack,TClip>`; the generator discovers consumers compilation-wide, so there is no registration, catalog, or schema marker.

```cs
using Tl;

namespace Combat
{
    public readonly partial struct Resistance
    {
        public readonly float Scale;
        public Resistance(float scale) => Scale = scale;
    }

    public partial struct Health
    {
        public float Value;
    }

    public readonly struct DamageClip
    {
        public readonly float Amount;
        public DamageClip(float amount) => Amount = amount;
    }

    public readonly struct DamageTrack : IBlend<DamageClip>
    {
        public readonly float Multiplier;
        public DamageTrack(float multiplier) => Multiplier = multiplier;

        public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
            => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
    }

    public readonly struct ApplyDamage : ITimelineJob<DamageTrack, DamageClip>
    {
        public static void Execute(
            in Frame<DamageTrack, DamageClip> frame,
            in Resistance resistance,
            ref Health health)
        {
            var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
            health.Value += frame.IsBackward ? amount : -amount;
        }
    }
}
```

Track values hold immutable settings. Clip values hold immutable authored payload. `in` declares a borrowed read-only component column; `ref` declares a borrowed writable column. The generator derives each consumer's column set from the `Execute` signature. The `partial` modifiers are optional in .NET; keeping them lets the same file compile inside Unity, where the [tl.unity guide](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md) adds host hooks in a second partial file.

#### 3. Author and bake one timeline

`boss.json` — flat schema v1. Track/clip `namespace`+`type` name the C# types above; `data` field names map onto struct fields; windows are half-open `[start, end)`; execution order is authored clip order:

```json
{
  "name": "boss_phase_one",
  "duration": 8,
  "loop": true,
  "tracks": [
    {
      "name": "main_damage",
      "namespace": "Combat",
      "type": "DamageTrack",
      "data": { "Multiplier": 2.0 },
      "clips": [
        { "namespace": "Combat", "type": "DamageClip", "start": 0, "end": 3, "data": { "Amount": 5 } },
        { "namespace": "Combat", "type": "DamageClip", "start": 4, "end": 8, "data": { "Amount": 9 } }
      ]
    }
  ]
}
```

Bake with the `tlbake` tool, pointing `--assembly` at the compiled domain DLL so the baker resolves the authored type names. **The assembly name must match the assembly that ships those types at runtime** — pair keys hash assembly-qualified names. Baking is deterministic; cache hits preserve timestamps:

```sh
dotnet build -c Release
tlbake boss.json boss.tlb --assembly bin/Release/net10.0/MyApp.dll --cache ~/.tlbcache
```

`tlbake --report boss.tlb` audits sizes and `tlbake --strip boss.tlb boss.dist.tlb` trims metadata for distribution (ship kernel-bound assets stripped).

#### 4. Load, advance, and query

The application owns the rows, component arrays, and the game clock. `TimelineAsset.Load` is a cold validated import; dispose the asset after all rows and readers are done.

```cs
using var asset = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
var rows    = new[] { new TimelineComponent(asset.Reference) };
var resist  = new[] { new Resistance(1f) };
var health  = new[] { new Health { Value = 100f } };
var query   = Timeline.Rows(rows).Read(resist).Write(health);
query.Tick(gameTick: 200_000u, delta: 1);   // consumers dispatch, movement commits
```

`Tick(G, +N)` emits game ticks `G` through `G + N - 1`; `Tick(G, -N)` emits `G - 1` through `G - N`. Non-looping assets clamp at duration; loops carry independent per-instance cycles. Every available crossed frame executes.

The same bytes drive the typed query lane, which reads the row's currently selected stage without advancing it:

```cs
foreach (var frame in Timeline.Query<DamageTrack, DamageClip>(in rows[0]))
    ApplyDamage.Execute(in frame, in resist[0], ref health[0]);
```

The query is a read-only stage view: it never advances `Position` or `Cycle`, so repeated queries return identical frames. A gap or clamped-completed position yields no frames; reverse movement re-observes the same stages in reverse; multiple occurrences of one pair in a step appear in authored order. In this view `Track`, `Clip`, `TimelineTick`, `Cycle`, and `TrackIndex` are populated; `GameTick` and `Flags` belong to the execution path. Frames exist only during `Execute`; consumers must not retain their borrowed references.

## Generated reports

Normal compilation owns generated sources inside the generator. Run the explicit export target when a standalone, content-stable snapshot is useful for review, another build pipeline, or size inspection:

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release
```

The export writes generated `.g.cs`, a manifest, and `TlGenCompile.report.txt` under `obj/Release/<tfm>/TlGenCompile`. A repeated identical invocation is a cache hit and preserves generated content.

## Performance contract

The hot path is allocation-free after warmup. `benchmarks/Alpha --verify` checks kernel-lane, data-authored, and allocation receipts; scalar latency and multi-entity throughput are reported separately and never inferred from a partial inner loop.

The repository enforces a 300,000-byte budget over production source contents plus relative UTF-8 paths. Generated source, static data, per-entity state, managed/native output, scratch, and allocations are measured separately.

## Unity ECS

Install the UPM package in Unity (Package Manager → *Add package from git URL*):

```
https://github.com/IAFahim/tl.unity.git?path=com.iafahim.tl
```

Unity is not a NuGet consumer here — the runtime is a net10.0 library with `ref` fields, so the UPM package compiles the shared `src/Tl.Core` sources directly (the same model MagicOnion uses: NuGet for .NET, UPM for Unity). The full walkthrough from baking to typed queries lives in the tl.unity repository: [END-TO-END.md](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md). A coordinator advances `TimelineComponent` rows, and your own system consumes typed frames:

```cs
foreach (var (timeline, resistance, health) in
         SystemAPI.Query<RefRO<TimelineComponent>, RefRO<Resistance>, RefRW<Health>>())
    foreach (var frame in TimelineEcs.Query<DamageTrack, DamageClip>(in timeline.ValueRO))
        ApplyDamage.Execute(in frame, in resistance.ValueRO, ref health.ValueRW);
```

## Scope

- Track and clip types are closed at build time; introducing executable types requires recompilation
- Asset track/clip `namespace` names are a single bare segment; dotted namespaces diagnose
- Half-open clip windows `[start, end)`; execution order is authored clip order
- Unmanaged track settings, clip payloads, and component slots
- Arbitrary looping deltas perform every observable effect and are proportional to the requested work
- Designer GUI authoring, C asset consumption, and runtime-loaded arbitrary schemas are deferred

## Repository map

| Path | Role |
| --- | --- |
| `src/Tl.Core` | Runtime declarations, asset import, frames, state, and total movement |
| `src/Tl.Gen.CSharp` | C# consumer discovery, typed binding, and export tool |
| `src/Tl.CSharp` | One-package C# installation |
| `samples/Mixed` | Data-authored timeline sample |
| `samples/NuGetQuickStart` | Runnable quick start that consumes the published nuget.org packages; CI runs it on every build |
| `tools/Tl.Bake` | `tlbake` JSON-to-`TLB1` baker with cache, report, and strip |
| `tests/Tl.Alpha` | Kernel-lane, data-authored, and allocation receipts |
| `tests/Tl.PackageConsumer` | Isolated package-only JIT and NativeAOT consumer |
| `benchmarks/Alpha` | Oracle, latency, throughput, assembly, and PMU evidence |

The data-authored Unity host package (`com.iafahim.tl`) lives in the [tl.unity](https://github.com/IAFahim/tl.unity) repository, published under the MIT license decided in issue #64.

## Documentation

- [Data-authored API contract](docs/data-authored-api.md) — the frozen design contract
- [Execution semantics](docs/semantics.md) — select, execute, commit, and movement laws of the removed alpha.3 catalog surface
- [Architecture](docs/architecture.md) — package and boundary map
- [Unity end-to-end](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md) — JSON bake to Unity ECS typed queries

## License

[MIT](LICENSE) — decided by the repository owner in [issue #64](https://github.com/IAFahim/tl/issues/64).
