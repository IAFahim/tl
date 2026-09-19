# tl

**tl** compiles designer-authored timeline data into deterministic execution — data first, an authoring step that bakes it, a game system that plays it. No reflection, no runtime compilation, 0 B per frame.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Live playground: [iafahim.github.io/tl](https://iafahim.github.io/tl/)

## Install

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.10
dotnet tool install --global Tl.Bake --prerelease     # the tlb bake command
```

## Run the full thing

`samples/Showcase` is the whole pipeline with the code inline — data, bake, system, output.

**1 · the data** — `jump.json`, authored by a designer, naming the game's own C# types:

```json
{
  "name": "jump", "duration": 30, "loop": true,
  "tracks": [
    {
      "name": "arc", "namespace": "Showcase", "type": "JumpTrack", "data": { "Scale": 1.0 },
      "clips": [
        { "name": "rise", "namespace": "Showcase", "type": "JumpClip", "start": 0, "end": 15, "data": { "Velocity": 2.0 } },
        { "name": "fall", "namespace": "Showcase", "type": "JumpClip", "start": 15, "end": 30, "data": { "Velocity": -2.0 } }
      ]
    }
  ]
}
```

**2 · the bake** — JSON to canonical TLB1 bytes, deterministic on every machine:

```sh
tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
```

**3 · the system** — `Program.cs`, three structs and one call per frame:

```cs
public readonly record struct JumpClip(float Velocity);

public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
{
    public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
    {
        result = new JumpClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
    }
}

public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
{
    public static void Execute(in Frame<JumpTrack, JumpClip> frame, ref float y)
    {
        y += frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
    }
}

ushort jumpTimeline = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));
var ids = new ushort[] { jumpTimeline, jumpTimeline, jumpTimeline, jumpTimeline };
var tick = new ushort[4];
var y = new float[4];

for (var frame = 1; frame <= 30; frame++)
{
    Timeline<JumpTrack, JumpClip>.Advance(ids, tick, true, y);
    if (frame % 3 == 0)
        Console.WriteLine($"  tick {frame,2}   y = {y[0],4:0.0} m   {new string('#', (int)Math.Round(y[0] / 3))}");
}
```

Run it:

```sh
cd samples/Showcase
dotnet build -c Release
tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
dotnet run -c Release --no-build
```

You see:

```
four characters jump, one call per frame:
  tick  3   y =  6.0 m   ##
  tick  6   y = 12.0 m   ####
  tick  9   y = 18.0 m   ######
  tick 12   y = 24.0 m   ########
  tick 15   y = 30.0 m   ##########
  tick 18   y = 24.0 m   ########
  tick 21   y = 18.0 m   ######
  tick 24   y = 12.0 m   ####
  tick 27   y =  6.0 m   ##
  tick 30   y =  0.0 m

rewind walks the arc back exactly:
  after 30 back ticks: y = 0.0 m, tick = 0

Timeline.Bake marked entities 42, 43 as jumping; unmarked entities never reach the advance
```

## Data

The JSON above is the whole input format — one file per timeline, authored by a designer, naming the game's own C# types:

- windows are half-open `[start, end)`; execution order is authored clip order
- `data` fields map onto struct fields by name (`Scale` → `JumpTrack.Scale`)
- an optional `name` on the timeline, each track, and each clip labels the asset for tools and reports; `--strip` drops the labels
- two clips overlapping on one track blend through the type's `IBlend` with the authored factor
- `loop: true` wraps at `duration`; finite timelines clamp
- a timeline may mix track and clip types freely; caps are 65,535 ticks and 256 pairs per asset

## Authoring

`tlb` compiles the data to canonical TLB1 bytes — deterministic (same input, same bytes, every machine and culture), pooled (each distinct string, type, and payload stored once; repeated designer copy shrinks on the way in), and cached (content-keyed hits preserve timestamps):

```sh
tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
```

`--assembly` names the DLL that ships the JSON's types (pair keys hash assembly-qualified names). The explicit form always wins; the lazy forms fill in what is unambiguous:

```sh
tlb jump.json          # output defaults to jump.tlb beside the input; assembly discovered
tlb                    # exactly one *.json in the directory; more than one names the candidates
```

Discovery sweeps the DLLs under the launch directory (skipping `.git`, `obj`, and friends) and keeps the ones defining every `(namespace, type)` pair the JSON references — exactly one is used, several fail naming them, zero fails naming the missing types and swept roots. `tlb.db` in the launch directory remembers the last resolution per JSON and is reused while the recorded DLL still exists and still defines the types; it is machine-local, gitignored, and rewritten on every re-sweep.

The designer loop:

```sh
tlb --watch jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
```

`--watch` bakes at startup, re-bakes on save (debounced, hash-skipped), and prints one JSON event per action (`ready`, `rebuild`, `skip`, `diagnostic`) — a broken file stays in the loop as a `diagnostic` until fixed. More: `tlb --json --assembly ...` lists every authorable pair for tooling, `tlb --report` audits asset sizes, `tlb --strip` drops authoring metadata for distribution. The Blender NLA bridge (`tools/Tl.Blender`) exports this same JSON and bakes through the same CLI.

## System

The three structs in the run above are the whole game side — the clip payload, the track settings with its blend, and the consumer that writes one effect column. `ITrack<TTrack, TClip>` consumers are discovered compilation-wide — no registration, no catalog. `frame.Direction` is +1 forward and −1 backward, which is why rewind is exact. Loading is one call that returns the timeline's index — a dense `ushort`, the whole acquisition step; the first typed use folds the pair's measured tables once, every later call is a table read.

Per frame, three caller-owned columns — timeline index, clock, effect — and one call advances every character one frame. Finite timelines clamp, looping ones wrap, rows sharing a clock collapse into vector runs. Rewind is `forward: false` and returns columns bit-exactly. There is no multi-frame skip parameter, ever: every system observes every tick, and sequential folds stay bit-exact (owner decision). Loop counts come from `FrameFlags.TimelineEnd` or the position column.

```cs
for (var frame = 0; frame < 30; frame++)
    Timeline<JumpTrack, JumpClip>.Advance(jumpTimeline, tick, false, y);
```

More systems on the same pair just declare the marker again — no registration, no chaining. Every consumer of `(JumpTrack, JumpClip)` runs inside the same one `Advance` call, folding its contribution into the effect column after the consumers before it:

```cs
public readonly struct ScreenShake : ITrack<JumpTrack, JumpClip>
{
    public static void Execute(in Frame<JumpTrack, JumpClip> frame, ref float shake)
    {
        if (frame.Has(FrameFlags.TimelineEnd))
            shake += 1f;
    }
}
```

Consumers fold in consumer-name order (`MoveY` before `ScreenShake`) — ordinal, culture-independent, deterministic on every machine; rename a consumer to move it. Receipts: `TandemFirstJob` and `TandemSecondJob` in `tests/Tl.Alpha` both run from generated installs, and the fold order is pinned by `tests/Tl.Core.Tests`. Order across different pairs is the host's call order.

Host wiring is declared, not registered — implement `IBake<TConsumer, ...TContext>` (zero to four context types) and one type-agnostic call attaches your markers at load time:

```cs
public readonly struct AttachJumping : IBake<MoveY, World, int>
{
    public static void Bake(MoveY consumer, World world, int entity)
    {
        world.MarkJumping(entity);
    }
}

var world = new World();
Timeline.Bake(jumpTimeline, world, 42); Timeline.Bake(jumpTimeline, world, 43);
```

`Timeline.Bake(id, args...)` walks the timeline's pairs and runs every bake whose declared context types appear among the argument types — exact type match, first argument of that type wins, declaration order is the parameter order, zero-context bakes run on every call, a missing context keeps the bake silent. Discovered and validated at build time (TLGEN70-73), installed into an unmanaged table, warm path untouched. A timeline that lacks the pair is a loud located diagnostic at first typed use — host wiring error, never designer data. Reading without advancing: `Timeline.Query<TTrack, TClip>(in TimelineComponent)` is a read-only stage view that never moves the clock.

## Generated reports

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release   # content-stable .g.cs snapshot + manifest
```

## Numbers

<!-- tl-numbers: generated by eng/refresh-numbers from benchmarks/Numbers/results/numbers.json; edit the renderer, not this block -->
One million characters, one frame per call (i9-14900K, .NET 10, Release; best of 5 × 3 interleaved rounds; every shape bit-exact forward and backward, 0 B warm):

| scenario | ms/frame | ns/character |
| --- | ---: | ---: |
| whole crowd on one timeline (a raid jumping in sync) | 0.52 | 0.52 |
| 100 timelines, crowds of 10,000 each (per-ability groups) | 0.73 | 0.73 |
| one looping timeline, every character on its own clock | 0.52 | 0.52 |
| one-shot finite timeline, staggered clocks | 0.55 | 0.55 |
| hand-written loop for comparison (`effects += 1`) | 0.36 | 0.36 |
| small squads: 16 timelines × 16 characters | 0.90 | 0.90 |
| worst case: unsorted rows, a different timeline each | 1.10 | 1.10 |

A single-timeline crowd floors at 0.52 ns per character; grouping rows by timeline keeps every crowd on the fast rows (ECS archetypes cluster identical rows for free). Authoring a full game's data — 19.3 MB of JSON — bakes in 57 ms and loads in 1.6 ms. Memory: 8 B per character of host columns, `28 * (duration + 1) + 48` bytes of tables per timeline, 0 B allocated per frame at any crowd size.
<!-- /tl-numbers -->

Receipts: `benchmarks/Numbers` (generates this section; `eng/refresh-numbers` re-measures and re-renders it from a fingerprinted receipt, and CI fails if the two disagree), `benchmarks/PairHandles`, `benchmarks/Alpha`, `tests/Tl.Alpha` — parity, allocation, and throughput evidence, run in CI on every push.

Contributing: [AGENTS.md](AGENTS.md) · Security: [SECURITY.md](SECURITY.md) · License: [MIT](LICENSE)
