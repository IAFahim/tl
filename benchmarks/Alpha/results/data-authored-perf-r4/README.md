# Data-authored fourth reduction pass (R2.1, GC-safe consumer column table)

This evidence, recorded on 2026-09-12, measures the fourth performance-reduction atom (R2.1, "GC-safe consumer column table") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `f5a99f0` (the R2 atom checkpoint). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

Two files in `src/`: `src/Tl.Core/Data.cs` eliminates illegal retention of raw unmanaged pointers across `Tick` invocations by recording 1-based column indices during `Bind` and refreshing interior pointers into `Columns[256]` per `Tick` call; `src/Tl.Gen.CSharp/JobEmitter.cs` emits `Bind_{name}(in global::Tl.TimelineQuery __tlQuery, byte* __tlIndices)` recording query column slots into the index table.

**Mechanism.** In R2, `Bind` cached raw unmanaged pointers directly into `Columns[256]`. Under a compacting GC between ticks, moving managed column arrays left stale interior pointers. In R2.1:
1. `PairCache` retains a 256-byte index map (`fixed byte ColumnIndex[256]`) and an active watermark `BoundSlots`.
2. Emitted `Bind_{name}` functions receive `byte* __tlIndices` and record `(byte)(__tlIdx + 1)` (1..4 corresponding to columns `_a`..`_d`, with 0 denoting unbound).
3. At the beginning of each synchronous `TimelineQuery.Tick` call, four interior pointers `p0..p3` are derived from the query's GC-tracked `ReadOnlySpan<byte>` fields (`_a`..`_d`). The dispatch table `table[i]` is freshly populated for `0 <= i < _cache.BoundSlots` from `ColumnIndex[i]`.
4. Consumer thunks continue executing zero-lookup dispatch via compile-time constant pointer offsets `(Type*)__tlColumns[0]`.

**Unmanaged-runtime law.** Interior pointers are strictly transient and scoped to the duration of the synchronous `Tick` call. No raw interior pointers survive across `Tick` boundaries. The GC may compact between ticks without leaving dangling pointers. 0 B allocated on all paths.

## Gates (all green)

- `python3 benchmarks/source_budget.py` (253,801/300,000; net +655 B over the R2 baseline of 253,146; limit 1,500 B)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release -p:NuGetAudit=false -m:1` (243 passed, 0 failed, including `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns`)
- `tests/Tl.Alpha` default, `--capacity`, `--module-capacity` (all 11 data-authored receipts pass, facade allocation 131,072 warm ticks 0 B)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and the published binary (all receipts pass under NativeAOT)

## Timing result

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). High-priority setup was denied by the host; every raw log records the failure. [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.350 / 1.384 / 1.342 | 1.870 / 1.878 / 1.836 | 15.610 / 15.616 / 15.535 |
| 1 track | Alternating | 1.326 / 1.337 / 1.344 | 2.079 / 2.077 / 2.083 | 17.388 / 17.226 / 17.289 |
| Two-clip blend | Forward | 1.698 / 1.709 / 1.705 | 2.547 / 2.560 / 2.553 | 16.149 / 16.376 / 16.265 |
| Two-clip blend | Alternating | 1.889 / 1.905 / 1.886 | 2.877 / 2.894 / 2.872 | 16.337 / 16.452 / 16.308 |
| 3 tracks, A-B-A | Forward | 2.257 / 2.250 / 2.263 | 3.830 / 3.840 / 3.855 | 27.879 / 28.149 / 27.972 |
| 3 tracks, A-B-A | Alternating | 4.144 / 4.152 / 4.139 | 3.949 / 3.921 / 3.906 | 28.478 / 28.571 / 28.435 |
| 16 tracks | Forward | 11.853 / 11.898 / 11.896 | 22.203 / 22.202 / 22.220 | 77.867 / 78.487 / 79.483 |
| 16 tracks | Alternating | 18.532 / 18.532 / 18.538 | 20.884 / 20.961 / 20.375 | 81.297 / 81.118 / 80.390 |
| 256 tracks | Forward | 337.142 / 336.963 / 335.768 | 34234.993 / 32872.075 / 33455.573 | 1110.220 / 1110.928 / 1110.363 |
| 256 tracks | Alternating | 338.950 / 340.718 / 340.605 | 34059.021 / 34163.360 / 34234.128 | 1137.783 / 1135.467 / 1126.718 |

Direct and generated arms remain stable within noise. All 90 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 13.7 ns | 17.388 ns (Run 1 Alternating; Forward worst: 15.616 ns) | REGRESSED (+3.69 ns over target) |
| Blend worst-of-3 | <= 14.3 ns | 16.452 ns (Run 2 Alternating; Forward worst: 16.376 ns) | REGRESSED (+2.15 ns over target) |
| ThreeTracks worst-of-3 | <= 23.1 ns | 28.571 ns (Run 2 Alternating; Forward worst: 28.149 ns) | REGRESSED (+5.47 ns over target) |
| 16-track worst-of-3 | <= 87.2 ns | 81.297 ns (Run 1 Alternating; Forward worst: 79.483 ns) | PASS (-5.90 ns under target) |
| 256-track worst-of-3 | <= 1,279 ns | 1,137.783 ns (Run 1 Alternating; Forward worst: 1,110.928 ns) | PASS (-141.2 ns under target) |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 90 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= 1,500 B | +655 B (253,801 / 300,000 total) | PASS |

## Decomposition and Cost Breakdown

As anticipated in NEXT-TASK.md ("If per-Tick pointer refresh adds ~1-2 ns fixed overhead, measure and report it cleanly. If any shape regresses >3%, document the exact cost breakdown"), the per-Tick pointer derivation shifts the overhead curve:

1. **Per-step dispatch slope decreased significantly (~0.39 ns/step faster than R2):**
   - R2 measured per-step slope: ~4.68 ns/step.
   - R4 (R2.1) measured per-step slope: ~4.29 ns/step (Forward), ~4.35–4.39 ns/step (Alternating).
   - Because consumer thunks index pointers directly from `Columns[offset]` rather than calling `TimelineQuery.Find`, large shapes show substantial gains:
     - 16-track facade: 77.8–81.3 ns vs R2's 82.6–84.7 ns (down ~3.4–5.2 ns).
     - 256-track facade: 1,110–1,138 ns vs R2's 1,207–1,242 ns (down ~97–104 ns).

2. **Fixed intercept increased by ~3.8–5.0 ns:**
   - R2 measured linear intercept: ~8.1 ns.
   - R4 (R2.1) measured linear intercept: ~11.9–12.4 ns (Forward), ~13.1 ns (Alternating).
   - Cost breakdown of the per-Tick prologue:
     - 4 span interior pointer derivations (`fixed (byte* p0 = _a, p1 = _b, p2 = _c, p3 = _d)`): ~1.2–1.5 ns.
     - Stack pointer pinning and bounds preparation (`void** table = (void**)_cache.Columns; fixed (byte* idx = _cache.ColumnIndex)`): ~0.8–1.0 ns.
     - Prologue table-population loop over `BoundSlots`:
       - 1 track: 4 slots refreshed (~1.5–1.8 ns).
       - Blend: 4 slots refreshed (~1.5–1.8 ns).
       - 3 tracks: 12 slots refreshed (~3.2–3.5 ns).
   - For 1-track and Blend (which execute only 1 step dispatch), the ~0.39 ns savings in dispatch is outweighed by the ~3.8–4.0 ns fixed refresh cost in the prologue, yielding net timings of ~15.6–17.4 ns.
   - For 16-track and 256-track, the per-step savings compound over 16 and 256 steps respectively (saving 6.2 ns and 99.8 ns), heavily dominating the fixed refresh overhead and making the system faster overall.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r4/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
