# ManyEntities — many entities, one asset, per-entity clocks

The dominant real workload: every entity carries the same timeline asset at its own clock position. Each lane runs the identical workload twice — the shipped facade and the per-asset SoA "playback table" pattern from [docs/playback-tables-design.md](../../docs/playback-tables-design.md) — and requires identical checksums:

| lane | workload | facade | table | ratio |
| --- | --- | ---: | ---: | ---: |
| sweep | 200k staggered clocks, movement + write | 9.00 ns/row | 0.71 ns/row | 12.7x |
| pulse | duration-1 looping (event ticks) | 8.83 ns/row | 0.50 ns/row | 17.6x |
| watch | every row reads another entity's input (car/player pattern) | 9.55 ns/row | 0.95 ns/row | 10.1x |
| churn | 20-tick windows spawning/retiring, ~400k live | 11.52 ns/row | 0.70 ns/row | 16.4x |

The table side is the shape the playback-tables coordinator (issue #56) will turn into a public API; today it is written out here so the numbers are reproducible from source.

Run:

```sh
dotnet build -c Release
dotnet run -c Release --no-build
```

Measured on i9-14900K, .NET 10.0.12, best of 5 passes; every pass checksum-parity checked (the program exits nonzero on any mismatch).
