# Lane kernel tail dedup (issue #409)

One-variable A/B on the same machine: `baseline/` is the untouched `origin/main` tree, `candidate/` collapses the four verbatim 16-lane effect tails (`EffPermuteForward`, `EffPermuteBackward`, `EffectForward`, `EffectBackward`) into `ApplyGather` (`AggressiveInlining | AggressiveOptimization`). Both runs: `dotnet run --project benchmarks/FusedAdvance -c Release -- --artifacts <dir>` (24 benchmarks: 6 shapes x 2 jobs x 2 arms), then 3x `perf stat -e cycles,instructions,branches,branch-misses taskset -c 2 dotnet FusedAdvance.dll --parity`.

## Verdict

Ship. The dedup removes 581 src bytes with no measured cost:

- Parity: 27/27 cases PASS on both arms; FNV-1a position and effect checksums are **byte-identical** between baseline and candidate (`diff` of the `PASS pos=` lines is empty), so ordered effects and floating-point evaluation are unchanged.
- BenchmarkDotNet medians (InProcess job, stable code layout): 12/12 within ±2.4% of baseline, most within ±0.6%, moving in both directions. The single isolated-job outlier (`Job-TZECNT SetStaggeredMixed Fused` +3.8%) splits against its paired arm on the same shape (-1.7%); both arms execute the identical deduped kernels, so opposite-direction movement is the code-layout variance the #148 verdict already documented, not a kernel regression.
- PMU (3 runs each, pinned core 2, parity workload): instructions 5,062.5 M vs 5,062.8 M (+0.006%, two runs effectively identical), branches +0.02%, cycles 1,358.6 M vs 1,365.5 M (+0.5%, inside the 1.348-1.372 G baseline spread), branch-misses +1% absolute-noise on 3.5 M. Equal instruction count is the inline-back proof.

Host: Arch Linux 7.2.5, .NET SDK 10.0.401, perf 7.2.3, core pinned via taskset.
