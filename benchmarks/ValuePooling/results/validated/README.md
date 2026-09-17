# Value pooling prototype run — 2026-09-17, win-x64

Branch `perf/149-valuepool-proto`, base commit `e8729be`. All fixtures use fixed xorshift seeds; every benchmark's `GlobalSetup` re-asserted bit-identical scan checksums between inline and pooled reads on the exact measured fixture before timing. Raw BenchmarkDotNet JSON is in `bdn/results/`; the engine log is `bdn/BenchmarkRun-20260917-161747.log`; the standalone parity receipt is `parity.txt`; toolchain details are in `environment-dotnet.txt`.

- Host: AMD Ryzen 5 8500G, 6C/12T, Windows 11 Pro, no CPU pinning (Windows), no `DOTNET_*` perf overrides set.
- Runtime: .NET 10.0.12 (X64), BenchmarkDotNet 0.15.8, in-process emit toolchain, 8 warmup + 12 measurement iterations of 250 ms, MemoryDiagnoser, `JsonExporter.Full`.
- Repro:

```sh
export PATH="$HOME/.dotnet:$PATH"
dotnet build benchmarks/ValuePooling/ValuePooling.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/ValuePooling -c Release --no-build -- --parity
dotnet run --project benchmarks/ValuePooling -c Release --no-build -- --run validated
```

## Parity receipt

`PASS pooled reads are bit-identical to inline` — scan and indirect checksum bits agree across inline/pooled-byte/pooled-ushort at pools 10, 256, and 1000 (byte is out of domain at 1000 by construction). All 32 measured cases: `BytesAllocatedPerOperation = 0` (warm managed allocation 0 B), N >= 9 iterations each.

## Cache density (computed)

| Variant | Bytes/slot | Full slots per 64 B line | Line density |
|---|---:|---:|---:|
| Inline (today) | 36 | 1 | 1.778 |
| Pooled byte | 24 | 2 | 2.667 |
| Pooled ushort | 28 | 2 | 2.286 |

## Query-shaped scan (ns/row, median of N iterations)

Values are ns/row via `OperationsPerInvoke` = row count; ratio is pooled median / inline median for the same rows and pool. Warm managed allocation is 0 B in every case.

### 100,000 rows

| Pool | Inline | Pooled byte | Pooled ushort |
|---:|---:|---:|---:|
| 10 | 3.987 | 4.104 (+3.0%) | 4.398 (+10.3%) |
| 256 | 4.092 | 4.120 (+0.7%) | 4.347 (+6.2%) |
| 1000 | 4.119 | out of domain | 4.416 (+7.2%) |

### 1,000,000 rows

| Pool | Inline | Pooled byte | Pooled ushort |
|---:|---:|---:|---:|
| 10 | 4.133 | 4.379 (+6.0%) | 4.619 (+11.7%) |
| 256 | 4.145 | 4.392 (+6.0%) | 4.600 (+11.0%) |
| 1000 | 4.233 | out of domain | 4.646 (+9.8%) |

Pooled loses everywhere, including pool 10 where the hypothesis should be strongest. Byte width is consistently ~5 points cheaper than ushort at equal pool size, but never beats inline.

## Direct indirection microbench (ns/row, median; one Multiplier chain per row)

| Rows | Pool | Inline | Pooled byte | Pooled ushort |
|---:|---:|---:|---:|---:|
| 100k | 10 | 0.608 | 0.608 (-0.03%) | 0.628 (+3.3%) |
| 100k | 256 | 0.614 | 0.611 (-0.5%) | 0.609 (-0.9%) |
| 100k | 1000 | 0.604 | out of domain | 0.616 (+2.0%) |
| 1M | 10 | 0.652 | 0.628 (-3.7%) | 0.644 (-1.3%) |
| 1M | 256 | 0.622 | 0.616 (-1.1%) | 0.617 (-0.8%) |
| 1M | 1000 | 0.619 | out of domain | 0.622 (+0.4%) |

The index-to-pool dependent load is effectively free (deltas within noise, at most ~±4% of a 0.6 ns chain) because pools of up to 1000 floats are 4 KB and stay L1-resident. The hypothesis's core assumption about cache-resident pools holds.

## Verdict

Integration (baker + runtime change) is not justified by read throughput. The 33% slot shrink (36 to 24 B) and doubled line density do not convert into scan throughput: the full query-shaped scan pays ~3-12% for pooled reads at every pool size and both row counts, with the penalty growing at 1M rows and with ushort width. Since the isolated indirection chain costs ~nothing, the loss comes from quadrupling per-row loads (4 indices plus 3-4 pool loads versus 4 contiguous floats in one stream), not from pool cache misses. Recorded as a dead end for the read-throughput motivation of #149; byte pooling remains a candidate only if asset-size reduction (not scan speed) becomes the goal, and the paired consumer shape here is the common one for this contract.

Measurement caveats: single machine, in-process toolchain, Windows (no CPU affinity pinning, unlike the repo's Linux receipts); the scan models one pair type (2-float track, 1-float clip) and pseudo-random uniform pool assignment; real assets with heavily clustered values would favor pooled slightly, but would need to recover the entire 3-12% gap.
