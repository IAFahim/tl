# Fused advance experiment (issue #148)

Does one fused call (`Timeline<T>.Advance(positions, forward, effects)`, `TimelineSet<TTrack, TClip>.Advance(ids, positions, forward, effects)`) measurably increase warm throughput over the two-call chain (`Timeline<T>.Seek(positions, forward).Apply(effects)`, `TimelineSet<TTrack, TClip>.Gather(ids).Seek(lastTick, forward).Apply(effects)`)? The fused entry points funnel into the identical internals (`new TimelineLane<T>(positions, forward).Apply(effects)` and `Gather(ids).Seek(positions, forward).Apply(effects)`), so arithmetic, run grouping, and ordered effects are equal by construction.

## Verdict

No measurable win. In the isolated-process job every shape measures within noise (ratios 0.98-1.04, roughly one standard deviation). The same-process job swings up to 30 percent in both directions for the same binaries, which identifies code layout, not the API boundary, as the residual variance source. The two-call chain already costs one real call: `Seek` is a trivial ref-struct constructor the JIT inlines, so `Seek(...).Apply(...)` was already fused after tier-1. Fusing is a pure ergonomics change with no throughput or allocation effect; whether to keep the extra public surface is an API-decision, not a performance one. A 0 B warm allocation is preserved on both arms.

## Host

- Windows 11 (10.0.22631.6936), AMD Ryzen 5 8500G 3.55 GHz, 6 physical / 12 logical cores
- .NET SDK 10.0.401, runtime .NET 10.0.12, X64 RyuJIT x86-64-v4, Release build
- BenchmarkDotNet 0.15.8, two jobs: `Job-TZECNT` (default toolchain, isolated child process per benchmark, 8 warmup + 16 x 200 ms iterations) and `InProcess` (InProcessEmitToolchain, default pilot schedule)
- Workload: 100,000 rows per operation, looping 1024-tick two-stage asset (`LaneTrack(2)` with `LaneClip` amounts 1.25 then -0.5), positions advanced in place each invocation, one effect element consumed into a sink per invocation

## Parity receipts

27 cases: BakedLane and TimelineSet-mixed (8 timelines, staggered ids) at 100k and 1M rows x uniform / waves-of-100 / staggered clocks x forward and backward, plus TimelineSet-single, plus a finite clamping asset (separate pair) at a clamped clock, forward and backward, three passes each. Fused and two-call arms run on independent arrays from fixed xorshift64-seeded effect columns; the tool asserts elementwise `ushort` position equality and bit-identical IEEE `float` effect columns, then prints FNV-1a 64 checksums. All cases PASS; checksums are byte-identical across repeat runs (see `run2-fixed-setup/console-full.log`):

```
lane      100,000 rows staggered forward  PASS pos=0x27684817689cf945 eff=0x65165d02d05ee269
lane      1,000,000 rows staggered backward PASS pos=0x8d1a051580741a65 eff=0xd3eff0708b77d3bd
set-mixed 1,000,000 rows staggered forward  PASS pos=0x1a22473529af7b65 eff=0x9f2ab346dd060715
edge      100,000 rows clamped   forward  PASS pos=0xac74ab800226a975 eff=0x9ccb0ca8802a0839
edge      100,000 rows clamped   backward PASS pos=0x71b141ff5a7dafd9 eff=0xe89f4c514ffbb7bf
parity: all cases PASS
```

## Headline (isolated child process per benchmark, medians of 16 x 200 ms, 100k rows)

| Shape | Two-call | Fused | Ratio (fused/two-call) | Allocated |
|---|---:|---:|---:|---|
| lane uniform forward | 8.398 us | 8.155 us | 0.98 | 0 B / 0 B |
| lane waves-of-100 forward | 13.398 us | 13.110 us | 0.98 | 0 B / 0 B |
| lane staggered forward | 31.440 us | 32.637 us | 1.04 | 0 B / 0 B |
| lane uniform backward | 8.168 us | 8.094 us | 1.00 | 0 B / 0 B |
| set one-timeline waves forward | 10.456 us | 10.789 us | 1.03 | 0 B / 0 B |
| set 8-timeline staggered mixed forward | 129.480 us | 130.952 us | 1.00 | 0 B / 0 B |

## Same-process job (InProcessEmitToolchain, same binaries)

| Shape | Two-call | Fused | Ratio |
|---|---:|---:|---:|
| lane uniform forward | 9.999 us | 10.700 us | 1.07 |
| lane waves-of-100 forward | 18.865 us | 19.207 us | 1.02 |
| lane staggered forward | 41.058 us | 31.166 us | 0.76 |
| lane uniform backward | 6.269 us | 7.940 us | 1.27 |
| set one-timeline waves forward | 8.909 us | 10.548 us | 1.18 |
| set 8-timeline staggered mixed forward | 132.461 us | 130.322 us | 0.99 |

The same two arms measure 0.98-1.04 in fresh processes and 0.76-1.27 in one process; the same variant alone moves between jobs by up to 30 percent (two-call staggered 31.4 vs 41.1 us). InProcessEmit emission fixes code layout per benchmark, and these tight vector loops are layout-sensitive. The isolated-process job is the only one with trustworthy absolute numbers here. The in-process job is retained as the layout-variance evidence. Both arms allocate 0 B; the single `1 B` on set staggered mixed appears on both arms and is a harness artifact of the emitted toolchain.

## Recorded dead end: run 1

`run1-invalid-lane-setup/` is retained deliberately. Its default-toolchain lane rows report ~3 ns because the BDN child processes never executed `BakedLane.Bind`; `LaneTable.Duration` stayed 0 and `Apply` early-returned, so the benchmark measured an empty loop. Setup that mutates process-global native tables must run inside `[GlobalSetup]` (or static initializers) or child-process benchmarks silently no-op. Run 2 fixes this with `Host.BindLane()` in `[GlobalSetup]`.

## Boilerplate delta

Line count per call site is unchanged (one statement either way). The fused form removes the chained member invocations (two for the lane, three for the set) and the intermediate ref structs from the expression, and passes each span exactly once:

```csharp
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(health);
Timeline<BakedLane<DamageTrack, DamageClip>>.Advance(positions, true, health);

jumps.Gather(timelineIds).Seek(lastTick, true).Apply(health);
jumps.Advance(timelineIds, lastPositions, true, health);
```

Consumers that chunk large arrays still write the chunking loop themselves; fusion does not change that.

## Reproduce

From the worktree root (NuGet restore needs nuget.org or a warm BenchmarkDotNet 0.15.8 cache):

```sh
export PATH="$HOME/.dotnet:$PATH"
dotnet build benchmarks/FusedAdvance/FusedAdvance.csproj -c Release -m:1 -p:NuGetAudit=false
dotnet run --project benchmarks/FusedAdvance -c Release --no-build -- --parity
dotnet run --project benchmarks/FusedAdvance -c Release --no-build -- --artifacts benchmarks/FusedAdvance/results/<run-name>
```

Parity always runs first and exits nonzero on any mismatch; `--parity` stops there. Raw BDN JSON, CSV, HTML, and markdown reports land under the artifacts directory. One full run takes about six minutes on the host above.
