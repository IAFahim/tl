# Value pool format A/B (issue #155)

Measures the shipped TLB v2 format — per-pair value pools of unique whole structs plus fixed-width `ushort` slot index rows — on real assets baked from authoring JSON, against the inline-layout numbers from #149 and the lane baseline from #148/#153. Not in `tl.slnx`; standalone BenchmarkDotNet probe mirroring `benchmarks/FusedAdvance`.

## Format summary (TLB1 magic, Version 3)

- Header 64 B: `Magic, Version=3, Loops, Duration, TrackCount, StageCount, PairCount, PairOffset=64, StageOffset, PoolOffset, FrameOffset, HotLength, Bytes, Reserved*3`.
- Pair entry 48 B: `Key u64, SlotStride u32, TrackPoolRel u32, TrackPoolCount u32, TrackValueBytes u32, ClipPoolRel u32, ClipPoolCount u32, ClipValueBytes u32, Reserved u64`; pool rels are relative to the pair entry.
- Pool region before frame slots: per pair, the track-value pool then the clip-value pool (unique whole structs, byte-compared, ordinal byte-sorted, 16-aligned).
- Slot row 24 B uniform for every pair (was 32 B through Version 2; repacked in #168): `u16 trackValueIndex, u16 firstClipValueIndex, u16 secondClipValueIndex (0xFFFF = none), u8 trackIndex, u8 zero, u32 windowStart/End, factorStart/Span`. Slot strides and step slot offsets are 8-aligned (reads are scalar u16/u32/u8 through `SlotRow.ToFrame`; no vectorized row loads exist to require 16).
- More than 65,535 unique values in one pool is a bake diagnostic naming the type and count.
- Strip semantics unchanged: only the authoring metadata tail is strippable; pools are hot data.
- `BakeCacheKey.ToolVersion` is `tlbake-bake-v4` (bumped in #168 with the row repack; `tlbake-bake-v3` covered #155's pooling). The loader accepts Version 2 (the 1.0.0-alpha.8 32 B row, 16-aligned stride) and Version 3; a Version 2 image keeps its rows but the loader relocates `trackIndex` from byte 24 to byte 6 inside its private block copy so the read path stays single-layout. Other versions are rejected at load (`TLB magic or version invalid`).

## Workloads

- `QueryScan` — per tick over a real 64-tick looping asset with two pair types (`Tlb.DualTrack` with `DualAlphaClip`/`DualBetaClip` plus `Tlb.BlendTrack`/`BlendClip` with a real blend window), baked in `GlobalSetup` by `TimelineBaker.BakeJson`: one-pair query scans and a full three-query program walk. Every row resolves `u16 -> pool -> struct` through `SlotRow.ToFrame`.
- `LaneApply` — `Timeline<BakedLane<...>>.Advance` on a real 1024-tick looping two-stage asset baked from JSON at the #148/#153 shapes: 100,000 rows, uniform / waves-of-100 / staggered clocks, forward and backward. The lane reads pre-folded float tables; the format affects only the one-time `MeasuredLanes` fold.

Both classes carry MemoryDiagnoser; positions and effect columns advance in place, nothing allocates.

## Results (AMD Ryzen 5 8500G, Windows 11, .NET 10.0.401 SDK / 10.0.12 runtime, isolated child process, 8 warmup + 16 x 200 ms, medians)

Warm managed allocation is **0 B per operation in all 7 benchmarks** (`alloc/op=0` in `results/*-report-full.json`; N >= 13 iterations each). Self-check receipts are in `verify.txt`; the raw engine log is `BenchmarkRun-20260917-181144.log`; console output is `console-full.log`.

### Lane path vs the #148/#153 baseline (100k rows, medians)

| Shape | Baseline (inline slots, #148 run2) | Pooled u16 (this run) | Delta |
|---|---:|---:|---:|
| lane uniform forward | 8.398 us | 6.494 us | -22.7% |
| lane waves-of-100 forward | 13.398 us | 13.726 us | +2.4% |
| lane staggered forward | 31.440 us | 30.903 us | -1.7% |
| lane uniform backward | 8.168 us | 6.399 us | -21.7% |

Waves and staggered — the shapes that exercise the gathered records path — are within noise (|delta| <= 2.4%). Uniform shows a 22% improvement, outside noise in the favorable direction; no shape regressed. This is expected: warm lane playback reads pre-folded float tables that are byte-identical to the inline layout's tables (proven by the parity receipts), so the format can only affect the one-time bind.

### Query-shaped reads on the new format (100k ticks, medians)

| Shape | ns/tick | Notes |
|---|---:|---|
| `DualAlpha` (one pair, two stage steps per 64-tick loop) | 6.582 | includes stage select, program walk, pair-key compare, u16->pool resolution, consumer dispatch |
| `BlendWindow` (blend pair, factor math) | 6.548 | reads two pool entries plus blend |
| `MixedProgram` (all three pair queries per tick) | 19.047 | full stage program every tick |

The inline layout no longer exists to A/B end-to-end, so the accepted comparison stays with #149's controlled receipts: its same-shape scan paid **+6% to +12%** for pooled `ushort` reads over inline (4.0-4.2 ns/row inline vs 4.35-4.65 ns/row pooled), and its direct-indirection microbench showed the `u16 -> pool` dependent load itself is within +/-4% of free because pools stay L1-resident. The real-asset numbers above include the whole query pipeline (stage select, program walk, pair-key compare), so they are not directly comparable to #149's tight loop; they fix the shipped end-to-end cost at ~6.5 ns per single-pair tick and ~19 ns per full-program tick on this corpus, with no SIMD kernels written yet.

## Repro

```sh
export PATH="$HOME/.dotnet:$PATH"
dotnet build benchmarks/ValuePoolFormat/ValuePoolFormat.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/ValuePoolFormat -c Release --no-build -- --verify
dotnet run --project benchmarks/ValuePoolFormat -c Release --no-build -- --run validated
```

Parity receipts for the format change (bit-identical playback, pre- vs post-change builds) live in `results/parity/`; baked-size deltas per corpus asset are in `results/parity/sizes.csv`.
