# ManyEntities — many entities, one asset, per-entity clocks

The dominant real workload: every entity carries the same timeline asset at its own clock position. Each lane runs the identical workload twice — the shipped typed lane (`Timeline<T>.Seek(...).Apply(...)`) and the per-asset SoA "playback table" pattern from [docs/playback-tables-design.md](../../docs/playback-tables-design.md) — and requires identical checksums:

| lane | workload | typed lane | hand SoA | ratio |
| --- | --- | ---: | ---: | ---: |
| sweep | 200k staggered clocks, movement + write | 4.06 ns/row | 0.76 ns/row | 5.3x |
| pulse | duration-1 looping (event ticks) | 0.42 ns/row | 0.51 ns/row | 0.8x |
| churn | 20-tick windows spawning/retiring, ~400k live | 0.70 ns/row | 0.78 ns/row | 0.9x |

The lane wins where rows group (pulse, churn: run-length vector fills beat a per-row branch) and
the hand sweep wins where positions interleave (staggered clocks fragment runs). The former
`watch` lane (every row reading another entity's input inside the frame) is gone with the facade:
cross-entity reads are not position-pure, so they cannot fold into the lane's effect tables.

Run:

```sh
dotnet build -c Release
dotnet run -c Release --no-build
```

Measured on Ryzen 5 8500G, .NET 10, best of 5 passes; every pass checksum-parity checked (the program exits nonzero on any mismatch).
