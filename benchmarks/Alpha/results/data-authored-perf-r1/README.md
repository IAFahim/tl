# Data-authored first reduction pass (R1+R2+R3, native pair table)

This evidence, recorded on 2026-09-12, measures the first performance-reduction atom for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `c3ac02d`; the recorded baseline for comparison is commit `84f45fd` (the first-pass facade), re-measured on this host today from the same worktree via `git stash` and retained in [control-old-code/](control-old-code/). Both sides ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

Three authorized reductions, implemented after the owner's mid-atom constraint revision (no managed types in the runtime data path; stackalloc per-Tick resolution; source budget 300,000):

- **R1 — bind once per query construction.** `TimelineQuery.Tick` no longer calls a per-tick global bind sweep. A `bool _bound` field gates a first-Tick pre-pass that validates every referenced asset's relevant consumers before any effects; a missing column still throws before effects, and a failed validation leaves `_bound` false so the next Tick revalidates.
- **R2 — bind relevance (semantics refinement).** The first-Tick validation binds, in consumer installation order, only consumers whose pair key appears in a referenced asset's pair table. Foreign process-global consumers no longer gate playback or force foreign columns. `tests/Tl.Core.Tests/DataTests.cs` `TickBindsRelevantConsumerColumnsBeforeEffects` re-encodes the refinement: a missing column for a *present* pair still throws before effects, and a pair not in the asset no longer blocks playback. The `tests/Tl.Alpha` `ColdFailures` receipt is unchanged and still surfaces the `DamageJob` message first (installation order is required for this; the asset's sorted key order would surface `MarkJob` first).
- **R3 (revised) — no per-step binary search.** `PairTable` is converted from managed `Pair[]`/`Consumer[]` arrays to one process-lifetime 64-aligned `NativeMemory.AlignedAlloc` block (40,960 B: 1,024 open-addressed 16-byte pair slots for at most 512 pairs, 1,024 24-byte consumer entries with `Next`, pair-slot index, and the two function pointers). Installs serialize on an `Interlocked` spin gate and publish entries with volatile writes; dispatch reads are lock-free. Per Tick call, `Tick` stackallocs one `int` buffer sized to the maximum pair count among the rows, resolves each distinct asset's pair indices to chain heads with one hash probe per pair, and hands the borrowed buffer to `TimelineRef.Execute`, whose step walk is `steps[i] -> resolved[pairIndex] -> consumer chain -> thunk`. Zero managed allocation, zero GC visibility, no cross-call state. `Keying.Of` now returns `hash | 1` so a native slot can use key zero as its empty sentinel (keys are opaque; both table sides and baked assets derive from the same function).

`TimelineQuery.Tick`, `TimelineRef.Execute`, and both `ToFrame` members carry `[MethodImpl(AggressiveOptimization)]`: without them the now-fatter `Tick` tiered to a stable state ~9 ns slower on the first shape and stranded one shape per process ~57 ns slower. The wide-shape results below are stable across all three processes with the attributes.

### Unmanaged-memory lifetime and concurrency proof

The pair block is allocated once in `PairTable`'s class initializer (CLR-guaranteed single execution), never freed, and reclaimed by process teardown; capacity is fixed (512 pairs, 1,024 consumers, checked with the same capacity exceptions as before), and entries are append-only and immutable after publication, so no safe-reclamation scheme is required. Ownership: the block is process-global registry state addressed by 64-byte-aligned native memory; identity is the normalized FNV pair key; consumers are identified by append index. Publication order under the spin gate is: write the consumer entry fully, then volatile-write the pair slot head (release), so a reader that volatile-reads a matching key and head (acquire) observes a fully written entry. A reader racing an in-flight install observes either the old chain or the new one — never a torn thunk pointer — and because resolution is re-done every Tick, a late install becomes visible on the next Tick call (eventual, bounded by one Tick). The static `_pairs`/`_consumers`/`_gate` fields are scalar machine words (pointers and counters, not GC objects), matching the v0.4 native-entry precedent. The warm path takes no locks, allocates nothing, and reads only native memory plus borrowed caller columns.

Per the owner's revision, an asset swapped into a row after the query's first Tick is dispatched **correctly** (resolution is per-Tick) but its consumers' columns are not re-validated; a missing column then fails loudly inside the consumer thunk (empty-span index) rather than with the bind diagnostic. The contract line "replacing an asset ... must pass compatibility validation" is enforced at construction-time membership; per-swap revalidation without cross-call managed state is deferred with this atom's report.

## Gates (all green)

`python3 benchmarks/source_budget.py` (250,675/300,000; net +1,417 B); `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK); `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings); `dotnet test tl.slnx -c Release --no-build` (52+16+3+58+113 = 242 passed, 0 failed); `tests/Tl.Alpha` default, `--capacity`, `--module-capacity` (all 11 data-authored receipts, including `ColdFailures` with the unchanged `DamageJob` message and `FacadeAllocation` at 131,072 warm ticks, 0 B); `samples/Mixed` (exit 0); `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape); `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and the published binary (all receipts pass; function pointers stored in native memory execute under NativeAOT).

## Timing result

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8. Three processes of the changed build ([data-authored-jit](data-authored-jit/), [run2](data-authored-jit-run2/), [run3](data-authored-jit-run3/)) and one control process of the baseline build ([control-old-code](control-old-code/)). Direct and generated control arms are stable within 1-2% across all four processes. [summary.csv](summary.csv) retains every arm.

| Shape | Direct ns/tick | Generated ns/tick | Facade baseline (control today) | Facade run 1 | run 2 | run 3 | Median of 3 | vs baseline |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 track | 1.36-1.41 | 1.84-1.86 | 15.052 | 17.628 | 76.890 | 17.128 | 17.628 | +17.1% |
| Two-clip blend | 1.685-1.693 | 2.520-2.540 | 16.304 | 25.138 | 15.981 | 25.323 | 25.138 | +54.0% |
| 3 tracks, A-B-A | 2.262-2.294 | 3.767-3.882 | 27.248 | 26.729 | 25.863 | 26.841 | 26.729 | -1.9% |
| 16 tracks | 12.086-12.159 | 22.091-22.426 | 111.016 | 83.461 | 84.270 | 84.513 | 84.270 | -24.1% |
| 256 tracks | 338.5-339.2 | 34,394-34,800 | 1,641.604 | 1,183.313 | 1,246.747 | 1,247.373 | 1,246.747 | -24.0% |

All arms allocate 0 B.

## Readings, honestly

- **The step cost reduction is real and stable.** Three, sixteen, and 256-track facades are now stable across processes (26.7-26.9 / 83.4-84.5 / 1,183-1,247) and 24% below baseline on the wide shapes. The 256-track facade is now 27.7x faster than the alpha.3 generated kernel (was 21.1x); the issue #10 cliff advantage widened.
- **The fixed cost did not drop; it rose about 4 ns.** Clean-state ladder decomposition (runs 1 and 3): per-step cost `4.4-4.8 ns` (was 6.43; the per-step binary search, its call, and the per-step `Dispatch` frame are gone), fixed cost `about 12.7 ns` (was 8.7). The new fixed cost is the owner-mandated per-Tick work: the row scan for buffer sizing, the dynamic stackalloc, the memo compare, and one native hash probe per distinct asset. The R1 bind-sweep saving (several ns of indirect bind calls every tick) was consumed by it. The expectation that one-track shapes approach 8-11 ns is **not met**; 17.1-17.6 ns is the honest clean-state one-track result, and the two one-step shapes see-saw a residual ~+9 ns compile-state between them (One fast / Blend +9, or Blend 16.0 / One +9, per process).
- **A quantized ~+61 ns per-tick lottery appears on random arms in random processes** (run 2's one-track 76.9; earlier diagnostic runs hit Blend, ThreeTracks, or Sixteen). It is uniform within a process (tight per-iteration data), exactly one DRAM-miss-sized constant per tick, and absent from the baseline control run today, which reproduces the recorded first-pass medians within 1%. Working diagnosis: the single per-Tick native pair-slot line conflicts with a per-Tick-written line (stack resolution buffer, row commit, or accumulator column) under that process's address randomization, evicting each other every tick. Roughly one in four arm-processes is affected; it is a hazard of the mandated per-Tick native-table read, not of any one arm, and per verdict law B19 it is reported rather than iterated on: candidate next-atom mitigations are a conflict-avoiding slot layout, or moving resolution off the per-Tick path under a future revised constraint.
- Blend's clean state (15.98) slightly beats baseline (16.30); its median above is carried by the +9 compile-state present in two of three processes.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r1/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
