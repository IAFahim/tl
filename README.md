# tl

**tl** compiles designer-authored timeline data into deterministic execution — JSON in, baked `.tlb` bytes out, advanced by an unmanaged runtime on .NET and Unity. No reflection, no runtime compilation, 0 B per frame.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Live playground: [iafahim.github.io/tl](https://iafahim.github.io/tl/)

## Install

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.9
dotnet tool install --global Tl.Bake --prerelease     # the tlb bake command
```

.NET 10 (JIT + NativeAOT) via NuGet. Unity 6000+ via UPM: [`tl.unity`](https://github.com/IAFahim/tl.unity). Offline: append `--source ./packages`.

## Showcase: a character that jumps

### 1. Define the domain

```cs
using Tl;

namespace Game
{
    public readonly struct JumpClip
    {
        public readonly float Velocity;                 // height delta per tick
        public JumpClip(float velocity) => Velocity = velocity;
    }

    public readonly struct JumpTrack : IBlend<JumpClip>
    {
        public readonly float Scale;
        public JumpTrack(float scale) => Scale = scale;

        public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
            => result = new JumpClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
    }

    public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
    {
        public static void Execute(in Frame<JumpTrack, JumpClip> frame, ref float y)
            => y += frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
    }
}
```

- `IBlend<TClip>` — how two overlapping clips blend (authored factor)
- `ITrack<TTrack, TClip>` — one consumer per pair, discovered compilation-wide: no registration, no catalog
- one `ref float` column per consumer; `frame.Direction` makes rewind the exact inverse
- track/clip structs are unmanaged and immutable

### 2. Author and bake one timeline

`jump.json` — half-open windows `[start, end)`, execution order is authored order, `data` maps onto struct fields:

```json
{
  "name": "jump",
  "duration": 30,
  "loop": true,
  "tracks": [
    {
      "namespace": "Game",
      "type": "JumpTrack",
      "data": { "Scale": 1.0 },
      "clips": [
        { "namespace": "Game", "type": "JumpClip", "start": 0,  "end": 15, "data": { "Velocity": 2.0 } },
        { "namespace": "Game", "type": "JumpClip", "start": 15, "end": 30, "data": { "Velocity": -2.0 } }
      ]
    }
  ]
}
```

```sh
tlb jump.json jump.tlb --assembly bin/Release/net10.0/MyGame.dll
```

The assembly must be the one shipping those types at runtime (pair keys hash assembly-qualified names). Same input, same bytes, every machine and culture. `tlb --watch` re-bakes on save; `tlb --json --assembly ...` lists every authorable pair; `tlb --report` audits sizes; `tlb --strip` drops metadata.

### 3. Load and play

```cs
ushort jump = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));

var tick = new ushort[] { 0 };
var y    = new float[] { 0f };

for (var frame = 0; frame < 30; frame++)
    Timeline<JumpTrack, JumpClip>.Advance(jump, tick, true, y);

// y[0] climbed to +30 and came back to 0 — the character jumped and landed; loop: true wraps
```

- `Load` interns the bytes and returns the timeline index — one dense `ushort`, the whole acquisition step
- the first typed use folds the pair's measured tables once; every later call is a table read
- one advance = one frame, always: finite timelines clamp, looping ones wrap, equal positions collapse into vector runs

### 4. Rewind, catch-up, jump-to

```cs
Timeline<JumpTrack, JumpClip>.Advance(jump, tick, false, y);  // rewind — walks the arc back, bit-exact

while (lag-- > 0)                                             // catch-up: repeated single-frame calls
    Timeline<JumpTrack, JumpClip>.Advance(jump, tick, true, y);

tick[0] = 12;                                                 // jump-to: the clock is your column —
                                                              // write it and continue; no frames replayed
```

No multi-frame skip parameter exists, ever: every system observes every tick, and sequential folds stay bit-exact (owner decision). Loop counts come from `FrameFlags.TimelineEnd` / the position column.

### 5. Crowds — mixed timelines, one call

```cs
ushort jump  = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));
ushort hop   = TimelineAsset.Load(File.ReadAllBytes("hop.tlb"));
ushort slam  = TimelineAsset.Load(File.ReadAllBytes("slam.tlb"));   // any timeline with the same pair

var ids  = new ushort[] { jump, jump, hop, slam };
var tick = new ushort[] { 0, 5, 0, 12 };
var y    = new float[4];

Timeline<JumpTrack, JumpClip>.Advance(ids, tick, true, y);          // whole crowd, one frame
Timeline<JumpTrack, JumpClip>.Seek(ids, tick, true).Apply(y);       // same call, two steps

var jumpTick = new ushort[] { 0, 5 };                               // jump's rows only — fastest shape,
var jumpY    = new float[2];                                        // no index column at all
Timeline<JumpTrack, JumpClip>.Advance(jump, jumpTick, true, jumpY);
```

## IBake — attach host markers

The host-wiring API: declare what to attach when a timeline carrying your pair is baked onto an entity, then call one type-agnostic `Timeline.Bake`.

```cs
// 1. declare the wiring — on its own struct or directly on the consumer
public readonly struct MoveYBake : IBake<MoveY, World, Entity>
{
    public static void Bake(MoveY consumer, World world, Entity entity)
        => world.Add<JumpingTag>(entity);                     // your marker component
}

public readonly struct Heal : ITrack<HealTrack, HealClip>, IBake<Heal, World> { ... }

// 2. attach — one call, any loaded timeline; context arguments can be anything
ushort jump = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));
Timeline.Bake(jump, world, entity);

// 3. advance only marked entities — absence is structural, no filtering branch
foreach (var chunk in world.Chunks<JumpingTag, Tick, Y>())
    Timeline<JumpTrack, JumpClip>.Advance(jump, chunk.Ticks, forward, chunk.Y);
```

Rules:

- `IBake<TConsumer, T0, ..., T3>` — consumer first, then zero to four context types
- a bake runs when every declared context type appears among the `Timeline.Bake(id, args...)` argument types: subset match, exact type identity, first argument of that type wins, declaration order is the parameter order
- zero-context bakes run on every call; a missing context keeps the bake silent; extra arguments are ignored
- discovered and validated at build time (TLGEN70-73 locate invalid shapes), installed into an unmanaged table at module init — no reflection, no runtime compilation, warm path untouched, repeated calls deterministic
- manual install/inspection for hosts without the generator: `BakeRuntime<TTrack, TClip>.Bake(&thunk, TypeKey<World>.Value)`, `BakeCount`, `BakeContextCount(i)`, `BakeContextKey(i, c)`
- a timeline that lacks the pair is a loud located diagnostic at first typed use — host wiring error, never designer data

## Query without advancing

```cs
using var asset = TimelineAsset.Of(jump);

foreach (var frame in Timeline.Query<JumpTrack, JumpClip>(
             new TimelineComponent(asset.Reference) { Position = 7 }))
    MoveY.Execute(in frame, ref y[0]);      // reads the arc at tick 7; never moves the clock
```

## Generated reports

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release   # content-stable .g.cs snapshot + manifest
```

## Benchmarks

One million rows per call, one frame per call (i9-14900K, .NET 10, Release; best of 5 × 3 interleaved rounds; every shape bit-exact forward and backward, 0 B warm) — fast to slow:

| workload | ms/frame | ns/row |
| --- | ---: | ---: |
| static hand lane, waves of 100 | 0.14 | 0.13 |
| static hand lane, uniform clocks | 0.16 | 0.15 |
| **one timeline index, grouped rows** | **0.19** | **0.18** |
| 100 timelines, id blocks of 100, waves of 100 | 0.21 | 0.20 |
| one looping timeline, staggered clocks | 0.24 | 0.22 |
| finite timeline, staggered clocks | 0.34 | 0.32 |
| plain floor (`effects += 1; positions += 1`) | 0.41 | 0.39 |
| 16 timelines, id blocks of 16, waves of 100 | 0.50 | 0.48 |
| ids alternating per row, staggered clocks | 1.80 | 1.72 |

Crowd envelopes (100k rows): 0.18-0.19 one index · 0.32 eight grouped · 0.86 eight alternating · waves 0.17-0.20 grouped, 0.67-0.69 alternating. **Group rows by timeline index — ECS archetypes do it for free.**

Bake/load (20 MB authoring corpus, `tools/Tl.Bake.Bench`): bake 179.7 ms end to end (legacy DOM path 3,393 ms); `Load` of the 15.8 MB result 2.9 ms. Memory: host columns 8 B/row (index 2 + position 2 + effect 4); per-timeline tables `28 * (duration + 1) + 48` bytes; `TimelineState` 8 B; 0 B warm.

## Unity ECS

```sh
# Package Manager → Add package from git URL
https://github.com/IAFahim/tl.unity.git?path=com.iafahim.tl
```

```cs
foreach (var (timeline, resistance, health) in
         SystemAPI.Query<RefRO<TimelineComponent>, RefRO<Resistance>, RefRW<Health>>())
    foreach (var frame in TimelineEcs.Query<DamageTrack, DamageClip>(in timeline.ValueRO))
        ApplyDamage.Execute(in frame, in resistance.ValueRO, ref health.ValueRW);
```

Walkthrough: [tl.unity END-TO-END.md](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md).

## Scope

- Types are closed at build time; new executable types require recompilation
- Half-open windows `[start, end)`; execution order is authored clip order
- Unmanaged track/clip/component data; assets cap at 65,535 ticks and 256 pairs
- Designer GUI authoring and C asset consumption are deferred

## Map

| Path | What |
| --- | --- |
| `src/Tl.Core` | runtime, import, frames, movement, typed lane |
| `src/Tl.Gen.CSharp` / `src/Tl.CSharp` | build-time generator / one-package install |
| `tools/Tl.Bake` (`tlb`) + `tools/Tl.Gen.Tlb` | bake CLI + baking library |
| `tools/Tl.Bake.Bench` · `tools/Tl.Blender` · `tools/Tl.Playground` | bake receipts · Blender bridge · live playground |
| `samples/` Mixed · NuGetQuickStart · ManyEntities | samples; CI runs them all |
| `tests/` Tl.Alpha · Tl.PackageConsumer · … | receipts |
| `benchmarks/` Alpha · PairHandles · TypedPlaybackProto | parity + throughput receipts |

Contributing: [AGENTS.md](AGENTS.md) · Security: [SECURITY.md](SECURITY.md) · License: [MIT](LICENSE) (issue #64)
