# Playback comparison

`Before.cs` is `benchmarks/Hooks.cs` from commit
`d3fe314`, with only its namespace changed to `Tl.Before` so both versions can
run in one BenchmarkDotNet executable. Keep this baseline unchanged.
`../Hooks.cs` is the current implementation.

The fixture has one track, 16 or 512 clips, three active ticks followed by a
gap, and identical payloads in both implementations. Queries stay below the
terminal tick because the original implementation throws there. Each run uses
65,536 queries. `Single` calls once per tick; `Batch` passes eight ticks per call.
Both paths accumulate payloads, callback counts, and movement flags. Setup
rejects any mismatch before timing.

```sh
python3 benchmarks/run.py review
python3 benchmarks/run.py dispatch --filter '*ApiShape*'
```

The runner builds and verifies first, writes a fresh dated results directory,
and rejects nonzero exits, missing measurements, or sources changed during a
run. It never replaces the earlier `results/validated` reports. Use `--cpu` to
select an available core. `NuGetAudit=false` was used for local runs with cached
packages when the NuGet audit endpoint was unavailable; it is recorded in the
result environment and is not a project setting.

These are warm .NET JIT throughput measurements, normalized per tick. They do
not measure initialization, arbitrary blend implementations, Unity IL2CPP,
NativeAOT, WASM, or all timeline sizes.
