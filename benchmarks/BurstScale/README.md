# Native scale fixture

This measures the existing `TimelineWorld.Tick` and generated `Scale.Run` path. It does not introduce another engine.

```powershell
dotnet build benchmarks/BurstScale -c Release -p:NuGetAudit=false
dotnet benchmarks/BurstScale/bin/Release/net10.0/BurstScale.dll --fixture scale.tlib
dotnet benchmarks/BurstScale/bin/Release/net10.0/BurstScale.dll scale.tlib 4000000 4 11
```

Arguments are the library file, entity count, active timelines per entity, and total .NET participants including the caller. Use 1M and 4M entities with 1, 2 and 4 active timelines, and 10,000 players with 40, 100 and 150 active timelines. The catalog contains 1,000 available definitions. It does not store 1,000 paused playbacks per player; Core's slot limit is 256.

Definitions share one looping 1,024-tick span and a sum kind. Each playback starts at a different phase; its feeder reads that phase, materializes its frame and writes a real output. Membership, speed and direction stay fixed. This is a controlled scale fixture, not a gameplay trace.

State and output buffers are native. Setup includes construction, enqueuing requests and the first tick that applies them. `readySetupMs` includes that first tick. `privateDelta` is process private memory before verification, including runtime and allocator effects, rather than an exact live-buffer count.

There are 32 warmup ticks followed by six rounds of 32 ticks. Retain raw rounds, discard round zero, and use the median of the remaining five. Repeat in at least five independent processes before making a headline claim. Allocation uses the precise process-wide GC counter around all 192 measured ticks, including workers. Profile intervals are separate from those timings.

The final pass checks every entity against an independent arithmetic oracle and hashes the count, all eight bytes of each playback, and the output. The matching Unity domain and benchmark live under `tl.unity/com.tl.unity/Tests/BurstScale`. Both domain files must stay byte-identical and use the same `scale.tlib`.

Take the machine's shared `bench.lock` and `unity.lock` before timing. Preserve the raw output, machine/editor/runtime versions, worker interpretation and other running processes with the report.
