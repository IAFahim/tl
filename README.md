# tl

`tl` compiles designer-authored timeline data into deterministic execution. A timeline is data: JSON in, baked `.tlb` bytes out, loaded and advanced by a small unmanaged runtime. Typed C# consumers describe what one active `(track, clip)` pair does to borrowed component storage — no reflection, delegates, runtime compilation, or warm-path allocation.

**Status: development prerelease.** The superseded alpha.3 authored surface (handwritten timeline declarations, catalogs, and schema markers) was removed under [issue #65](https://github.com/IAFahim/tl/issues/65). The [data-authored contract](docs/data-authored-api.md) is now the only authoring lane; [issue #56](https://github.com/IAFahim/tl/issues/56) owns its live acceptance status.

- Heterogeneous tracks and clips in one baked asset
- Deterministic bake: same inputs, same bytes; cache hits preserve timestamps
- Total signed `Tick` over borrowed component columns
- Read-only typed frame queries over the selected stage
- Function-pointer consumer bindings for coordinator-side effects
- NativeAOT-safe runtime output with no generator assemblies
- One shared domain assembly compiles for both .NET and Unity hosts

## Packages

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# install: runtime plus build-time consumer binding |
| `Tl.Runtime` | Small declaration, frame, state, and movement ABI |
| `Tl.Gen.CSharp` | Build-time generator that binds typed consumers |

## Install

Download `Tl.CSharp.1.0.0-alpha.3.nupkg` and `Tl.Runtime.1.0.0-alpha.3.nupkg` from the [GitHub prerelease](https://github.com/IAFahim/tl/releases/tag/v1.0.0-alpha.3) into `packages`, then install from that local source:

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.3 --source ./packages
```

The packages are not published to nuget.org. The generator runs whenever Roslyn compiles the project, including supporting IDE design-time builds.

## Define the domain

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

## Author and bake one timeline

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

Bake with `tlbake`, pointing `--assembly` at the compiled domain DLL so the baker resolves the authored type names. Baking is deterministic; cache hits preserve timestamps; `--report` prints sizes:

```sh
dotnet build -c Release
dotnet run --project tools/Tl.Bake -c Release -- \
  boss.json \
  boss.tlb \
  --assembly bin/Release/net10.0/MyApp.dll \
  --cache ~/.tlbcache
```

`tlbake --report boss.tlb` audits sizes and `tlbake --strip boss.tlb boss.dist.tlb` trims metadata for distribution.

## Load, advance, and query

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

The Unity surface lives in the extracted [tl.unity](https://github.com/IAFahim/tl.unity) repository pending [issue #64](https://github.com/IAFahim/tl/issues/64); this repository ships no UPM package. Its [END-TO-END guide](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md) walks the same domain code, JSON, and `.tlb` bytes into Unity ECS: a coordinator advances `TimelineComponent` rows and your own system consumes typed frames:

```cs
foreach (var frame in TimelineEcs.Query<DamageTrack, DamageClip>(in timeline.ValueRO))
    ApplyDamage.Execute(in frame, in r, ref h);
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
| `tools/Tl.Bake` | `tlbake` JSON-to-`TLB1` baker with cache, report, and strip |
| `tests/Tl.Alpha` | Kernel-lane, data-authored, and allocation receipts |
| `tests/Tl.PackageConsumer` | Isolated package-only JIT and NativeAOT consumer |
| `benchmarks/Alpha` | Oracle, latency, throughput, assembly, and PMU evidence |

The data-authored Unity host package (`unity/com.iafahim.tl` with its Unity project receipts) was extracted into the tl.unity repository; its publication and licensing remain owner decisions.

Read the [data-authored contract](docs/data-authored-api.md), the frozen alpha.3 record ([v1.0-alpha-api.md](docs/v1.0-alpha-api.md)), [execution semantics](docs/semantics.md), [architecture](docs/architecture.md), and [implementation plan](plan.md).

## Validate

```sh
python3 benchmarks/source_budget.py
python3 -m unittest discover -s benchmarks -p test_collect.py
python3 -m unittest discover -s tests -p test_release_artifacts.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```
