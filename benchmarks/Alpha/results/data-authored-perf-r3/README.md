# Data-authored third reduction pass (R2, generated per-pair fused dispatch)

This evidence, recorded on 2026-09-12, measures the third performance-reduction atom for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `c5bd8eb` (the R1.1 atom "stack-resident resolution cache"). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

Two files in `src/`: `src/Tl.Core/Data.cs` updates the consumer dispatch ABI and adds pre-resolved column pointer slices; `src/Tl.Gen.CSharp/JobEmitter.cs` emits direct compile-time constant pointer indexing in `TlConsumerBinding.g.cs` along with consumer pointer binding.

**Mechanism.** The generic consumer dispatch signature evolves from:
`delegate*<in TickFrame, in TimelineQuery, int, void>`
to a fused form:
`delegate*<byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row, void>`
During query initialization and asset binding, parameter column data pointers (`Unsafe.AsPointer(ref MemoryMarshal.GetReference(col.Data))`) are resolved once into the stack-resident `PairCache` pointer table. At dispatch time, `TimelineRef.Execute` offsets `columns` by `consumer.Offset` (where each consumer has a fixed slice of 4 pointers), passing `void**` directly to the consumer thunk. The emitted consumer thunks index parameter column pointers using compile-time constants (`(Type*)__tlColumns[0]`, etc.), completely eliminating the per-dispatch 4-way `Find` comparison chain and `Span<T>` reconstruction on the warm path.

**Unmanaged-runtime law.** No managed allocations, boxing, delegates, or reflection. Pointers are stack-resident unmanaged addresses indexing row buffers directly.

## Gates (all green)

- `python3 benchmarks/source_budget.py` (253,146/300,000; net +1,484 B over the R2 tree)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false` (242 passed, 0 failed)
- `tests/Tl.Alpha` default, `--capacity`, `--module-capacity` (all 11 data-authored receipts pass, facade allocation 131,072 warm ticks 0 B)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and the published binary (all receipts pass under NativeAOT)

## Timing result

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). High-priority setup was denied by the host; every raw log records the failure. [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.361 / 1.421 / 1.415 | 1.851 / 1.899 / 1.859 | 12.431 / 12.548 / 12.342 |
| 1 track | Alternating | 1.347 / 1.348 / 1.325 | 2.085 / 2.084 / 2.084 | 13.607 / 14.011 / 13.989 |
| Two-clip blend | Forward | 1.710 / 1.694 / 1.710 | 2.580 / 2.536 / 2.516 | 13.247 / 13.258 / 12.975 |
| Two-clip blend | Alternating | 1.897 / 1.929 / 1.907 | 2.848 / 2.839 / 2.828 | 13.254 / 13.150 / 13.186 |
| 3 tracks, A-B-A | Forward | 2.224 / 2.256 / 2.260 | 3.840 / 3.895 / 3.779 | 20.641 / 20.741 / 20.811 |
| 3 tracks, A-B-A | Alternating | 4.150 / 4.182 / 4.150 | 4.030 / 4.047 / 3.975 | 21.494 / 21.336 / 21.246 |
| 16 tracks | Forward | 11.964 / 11.927 / 12.096 | 22.542 / 22.511 / 22.499 | 75.726 / 76.276 / 75.886 |
| 16 tracks | Alternating | 18.676 / 18.633 / 18.792 | 20.853 / 20.246 / 20.795 | 78.012 / 76.908 / 78.906 |
| 256 tracks | Forward | 341.334 / 340.933 / 340.971 | 34847.441 / 35136.346 / 34086.985 | 1122.204 / 1136.856 / 1103.913 |
| 256 tracks | Alternating | 343.348 / 342.764 / 341.804 | 34474.255 / 34982.583 / 34327.107 | 1140.430 / 1146.662 / 1140.304 |

Direct and generated arms remain stable within noise. All 90 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 8.0 ns | 14.011 ns (Run 2 Alternating; Forward worst: 12.548 ns) | MISSED |
| Blend worst-of-3 | <= 8.0 ns | 13.258 ns (Run 2 Forward; Alternating worst: 13.254 ns) | MISSED |
| No shape regresses > 3% | <= ~1,280 ns on 256-track | 1,146.662 ns (all shapes improved: 256-track down from ~1,242 ns to ~1,140 ns; 16-track down from ~84 ns to ~78 ns) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 90 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |

## Decomposition and Mechanism Analysis

- **Per-step dispatch cost dropped by ~0.35–0.40 ns:**
  - R2 measured per-step slope: ~4.68 ns/step.
  - R3 measured per-step slope: ~4.28–4.35 ns/step (Run 1: 4.355 ns, Run 2: 4.413 ns, Run 3: 4.282 ns).
  - Across 16 tracks, this saved ~6.5–7.4 ns (75.7 ns vs 83.1 ns).
  - Across 256 tracks, this saved ~85–100 ns (1,103–1,146 ns vs 1,207–1,242 ns).
- **Fixed facade overhead remains the floor (~7.2–7.8 ns):**
  - The linear regression intercept across track counts is ~7.256 ns (Run 1), ~7.132 ns (Run 2), and ~7.811 ns (Run 3).
  - This fixed overhead comprises: `TimelineQuery.Tick` call, row iteration loop, `Select` movement resolution, `StageOf` binary search over stages, step array slicing, and chain head indexing.
  - Because OneTrack and Blend execute exactly 1 step, their total time is:
    `Total = Fixed Overhead (~7.5 ns) + Step Dispatch (~4.3 ns) = ~11.8–12.5 ns (Forward), ~13.2–14.0 ns (Alternating)`.
  - Even if step dispatch were 0.0 ns, the fixed overhead alone (~7.5 ns) leaves virtually no headroom to reach <= 8.0 ns without addressing the fixed stage/select pipeline in `TimelineQuery.Tick` and `TimelineRef.Execute`.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r3/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
