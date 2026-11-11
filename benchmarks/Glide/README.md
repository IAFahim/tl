# Kind-free Horizon/Glide benchmark

This measures `TimelineWorld.Advance`, with 1,000 looping definitions, varied timeline IDs and phases, native playback buffers and one caller. It does not measure kind evaluation or Unity's integrated tick.

Build the netstandard2.1 runtime asset and bake a shared fixture:

```powershell
dotnet build benchmarks/Glide -c Release -p:NuGetAudit=false
dotnet benchmarks/Glide/bin/Release/net10.0/Tl.Bake.Bench.dll --fixture 3000 scale-3000.tlib
```

The executable reuses the existing `Tl.Bake.Bench` friend identity to link the unchanged cold fixture baker. The project name is `Glide`.

Run arguments: fixture, entity count, active timelines, inline capacity, horizon limit, warmup ticks, ticks per round, number of rounds, ISA expectation:

```powershell
dotnet benchmarks/Glide/bin/Release/net10.0/Tl.Bake.Bench.dll scale-3000.tlib 4000000 4 4 255 256 32 6 auto
```

For the scalar leg set `DOTNET_EnableHWIntrinsic=0` in the child environment and pass `scalar`. The executable fails if the observed ISA flags disagree. Set `CoreTfm=net10.0` to measure that runtime asset separately.

Build the identical harness against an unchanged baseline with `GlideBuild=false` and `CoreProject=<absolute baseline Tl.Core.csproj>`. Freeze each complete output directory before another build. Baseline accepts limit0; candidate accepts0 or1–255. The candidate's limit0 uses the existing constructor and checks default-off overhead.

Use 4M x4, 4M x1, 1M x4 and 10k x150. Fixtures30,300,3000 and1024 expose frequent edges and long glides. Initialize with the same bounded request batches on both arms. Hold the shared benchmark and Unity locks, alternate arm order, discard timing round0 and compare medians from five fresh processes. Keep every raw allocation round.

The harness captures memory, configured cache eligibility and positive-horizon occupancy before obtaining a public mutable view. It then checks every count, playing bit, timeline, tick, flag, rate, phase and unused inline slot against an independent oracle, and consumes all live slot bytes in a hash. Reading a mutable view earlier would disable the optimization and invalidate an enabled comparison.

Report setup/first tick, process memory, all allocation windows and the spread alongside speed. Process-private memory includes runtime and allocator effects. Opt-in storage grows by one byte/entity plus a fixed64-byte header; this step does not claim a RAM saving or the full Unity replacement gate.
