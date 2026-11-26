# Kind-free lazy tick benchmark

This measures `TimelineWorld.Advance` on a `PlaybackTable` with lazy slot ticks on (horizon limit 1 to 255) or off (limit 0), with 1,000 looping definitions, varied timeline IDs and phases, native playback buffers and one caller. With the last argument `system` each tick also runs a generated `[TimelineSystem]` with one `OnClipStart` consumer over every slot before `Advance`, and the hash includes the clip starts it counted. It does not measure kind evaluation or Unity's integrated tick. The verdict and the numbers are in `docs/PERF-GLIDE.md`.

Build against one runtime asset and bake a shared fixture:

```sh
dotnet build benchmarks/Glide -c Release -p:NuGetAudit=false -p:CoreTfm=net10.0
dotnet benchmarks/Glide/bin/Release/net10.0/Tl.Bake.Bench.dll --fixture 3000 scale-3000.tlib
```

`CoreTfm` defaults to `netstandard2.1`, the asset Unity consumes; its due compare is scalar. The executable reuses the existing `Tl.Bake.Bench` friend identity to link the unchanged cold fixture baker. The project name is `Glide`.

Run arguments: fixture, entity count, active timelines, inline capacity, horizon limit, warmup ticks, ticks per round, number of rounds, ISA expectation, and `advance` (default) or `system`:

```sh
dotnet benchmarks/Glide/bin/Release/net10.0/Tl.Bake.Bench.dll scale-3000.tlib 20000 200 4 255 8192 256 8 auto
```

Warm up by call count: at least 8,192 ticks. A lazy tick is short enough that a 512-tick warmup still measures tier-0 or on-stack-replaced code and reports several times the settled cost.

For the scalar leg set `DOTNET_EnableHWIntrinsic=0` in the child environment and pass `scalar`. The executable fails if the observed ISA flags disagree.

Build the identical harness against an unchanged baseline with `GlideBuild=false` and `CoreProject=<absolute baseline Tl.Core.csproj>`. Freeze each complete output directory before another build. The baseline accepts limit 0; the candidate accepts 0 or 1 to 255. The candidate's limit 0 uses the two-argument constructor and checks default-off overhead.

Owner shapes are 10,000 x 40, 20,000 x 100 and 20,000 x 200 with inline capacity 4; 4M x 4, 4M x 1, 1M x 4 and 10k x 150 are the earlier Unity-scale shapes. Durations 120, 600 and 3000 cover frequent and rare boundaries; durations under 16 show where an eager tick is cheaper. Initialize with the same bounded request batches on both arms, run nothing else on the machine, discard timing round 0 and keep every raw allocation round.

The harness captures memory, the remaining eligibility (`actualLimit`) and the count of entities with a future due (`positiveHorizons`) before obtaining a public mutable view. It then checks every count, playing bit, timeline, tick, flag, rate, phase and unused inline slot against an analytic oracle, and consumes all live slot bytes in a hash. Reading a mutable view earlier would turn lazy ticks off and invalidate an enabled comparison.

Report setup, first tick, process memory, all allocation windows and the spread alongside speed. `firstMs` is the first `Advance` after the mass start: it runs tier-0 code and, on a lazy table, encodes every slot. Process-private memory includes runtime and allocator effects. Opt-in storage is 2 bytes per slot and 2 bytes per entity; lazy ticks cost memory, they do not save it.
