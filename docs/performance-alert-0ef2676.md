# CI performance-alert audit: `703661f` → `0ef2676`

## Finding

The alert contains a proven benchmark-identity bug. It is not evidence that all listed methods regressed. Counts reconstructed below pair the two occurrences using the Jit-then-NoTiering ordering present in the unchanged configuration and committed full reports; the historical action output discarded the job labels themselves.

The workflow combines every BenchmarkDotNet full JSON report into one file at `/home/i/GitHub/tl/.github/workflows/benchmark.yml:60-73`, then sends it to `benchmark-action/github-action-benchmark` as `tool: benchmarkdotnet` at lines 87-97. The action's exact `v1` branch currently resolves to `52576c92bccf6ac60c8223ec7eb2565637cae9ba` (release 1.22.1). Its BenchmarkDotNet extractor names each result with `benchmark.FullName` and ignores `DisplayInfo`, the job, and host metadata ([extract.js lines 414-428](https://github.com/benchmark-action/github-action-benchmark/blob/52576c92bccf6ac60c8223ec7eb2565637cae9ba/dist/src/extract.js#L414-L428)). It finds the historical comparison row with the first exact name match ([normalizeBenchmark.js lines 5-17](https://github.com/benchmark-action/github-action-benchmark/blob/52576c92bccf6ac60c8223ec7eb2565637cae9ba/dist/src/normalizeBenchmark.js#L5-L17), [write.js lines 116-130](https://github.com/benchmark-action/github-action-benchmark/blob/52576c92bccf6ac60c8223ec7eb2565637cae9ba/dist/src/write.js#L116-L130)).

`Tl.Hooks.Config` defines both `Jit` and `NoTiering` jobs at `/home/i/GitHub/tl/benchmarks/Dispatch/Benchmarks.cs:12-23`. BDN puts the job only in `DisplayInfo`; `FullName` is identical. The committed full report demonstrates this directly: Jit is at `/home/i/GitHub/tl/benchmarks/Dispatch/results/v04-compile-20260909/Tl.CompiledBench.CompiledVsInterpreter-report-full.json:24-30`, and the same `FullName` under NoTiering is at lines 3819-3825.

The exact gh-pages entries contain:

- `703661f`: 236 rows, 142 unique names, 94 duplicate rows.
- `0ef2676`: 248 rows, 148 unique names, 100 duplicate rows.

Every duplicate has multiplicity two. The six new compiled methods account for the 12 new rows and six new names. Because they have no historical match, the action skips them; they did not cause this alert.

Replaying the action's comparison against those two exact entries produces 74 alerts. Pairing occurrence 0 with Jit and occurrence 1 with NoTiering reduces that to eight. Four of the eight are effectively zero-cost `PassPlayback` estimates between 0 and 0.0041 ns. With values below 0.1 ns excluded, only four correctly paired rows exceed 1.20x. Therefore 66 of the 74 emitted alerts are proven cross-job false comparisons.

Concrete examples:

- `ApiShape.PlaybackSingle`: the action compares current NoTiering 66.731 ns to previous Jit 32.632 ns and reports 2.045x. The correct previous NoTiering value is 71.065 ns, so the same-job ratio is 0.939x.
- `ApiShape.ShellSingle`: the action reports 48.499 / 26.813 = 1.809x; correct NoTiering pairing is 48.499 / 48.479 = 1.0004x.
- `CountsShape.ScanSingle(Clips: 16, Sequential: True)` appears twice because its current Jit row is 1.212x previous Jit, while current NoTiering is also incorrectly compared to previous Jit and reported as 1.382x. Correct NoTiering pairing is 1.034x.
- `PassPlayback.PassIn` has a previous Jit mean of exactly zero, so both tiny current values can produce an infinite ratio. These sub-resolution estimates are unsuitable regression gates.

## Baseline and source changes

The action compared with `703661f`, not the immediate parent `cfb32c4`, because gh-pages records `703661f` immediately before `0ef2676`. `addBenchmarkEntry` deliberately searches backward for the latest different commit ([addBenchmarkEntry.js lines 39-57](https://github.com/benchmark-action/github-action-benchmark/blob/52576c92bccf6ac60c8223ec7eb2565637cae9ba/dist/src/addBenchmarkEntry.js#L39-L57)). Rapid intermediate pushes were absent from successful stored history, consistent with workflow cancellation via `/home/i/GitHub/tl/.github/workflows/benchmark.yml:22-24`.

The identity defect predates this alert. The earliest inspected stored entry already has 96 rows but only 72 unique names. Stored history grew to 94 duplicate rows by `703661f`.

Across `703661f..0ef2676`, all existing benchmark definitions and fixtures are byte-identical: `benchmarks/Hooks.cs`, `benchmarks/Shared.cs`, `benchmarks/Dispatch/Benchmarks.cs`, `benchmarks/Dispatch/Generated/**`, `benchmarks/Dispatch/ReceiverLink.g.cs`, and the entire `benchmarks/Algorithms` project, including generated fixtures. The Dispatch project added `CompiledCompare.cs` and a reference to `samples/Compiled` at `/home/i/GitHub/tl/benchmarks/Dispatch/Dispatch.csproj:10-13`. `src/Tl.Core` changed substantially (568 additions, 350 deletions), but the older dynamic Dispatch arms use the separate `Tl.Hooks` sandbox implementation. The added production project reference does not make those arms exercise the native implementation. Assembly composition/JIT layout and the run environment can still affect measured timings; measure the production runtime with its dedicated Native comparison. The independent Algorithms suite did not change but still has two same-job rows above 1.20x, which points to a run or environment effect rather than those methods' source changes. The exact cause remains unverified.

After correct job pairing and excluding values below 0.1 ns, the median current/previous ratio across 230 comparable rows is 1.051. Unchanged controls also moved together: `Dispatch.Direct` is about 1.124x in both jobs, `DataFlow.DirectData` about 1.123x in both, and `ApiShape.DirectTicks` about 1.06x. This broad shift is evidence of run-environment variation, though the exact CPU/runtime difference cannot be proven from stored history.

## Evidence limits

The `jq` merge retains only `. [0].HostEnvironmentInfo` at workflow lines 62-63, then the action discards `HostEnvironmentInfo` entirely. The gh-pages rows therefore contain no CPU, OS-image, SDK, runtime, or job metadata. The committed v0.4 full report is a local Omarchy/i9-14900K receipt (`...report-full.json:3-20`), not the GitHub-hosted Actions report. Exact Actions artifacts/log metadata could not be fetched: the local `gh` credential is invalid and unauthenticated API access returns 404 for this repository. No conclusion about the exact Actions CPU or runtime version is supportable from retained history.

The workflow also uses mutable `ubuntu-latest`, `.NET 10.0.x`, and action `@v1` at `/home/i/GitHub/tl/.github/workflows/benchmark.yml:34-40,88`. The action itself warns GitHub-hosted benchmark variation can be roughly 10-20% and recommends a stable self-hosted environment when that is unacceptable ([stability note](https://github.com/benchmark-action/github-action-benchmark#stability-of-virtual-environment)).

## Concrete benchmark-system fixes

1. Convert BDN reports to `customSmallerIsBetter` rows with a unique identity containing `FullName`, parsed job identity from `DisplayInfo`, parameters, runtime/configuration, and a stable environment fingerprint. Fail collection on duplicate identities or missing statistics. Use `Statistics.Median` consistently; the stock extractor currently takes `Statistics.Mean` despite the suite adding a Median column.
2. Preserve raw estimates, but exclude values below a documented resolution floor such as 0.1 ns from alert comparison. Near-zero benchmarks can remain informational.
3. Gate comparison on compatible environment fingerprints. Retain full host metadata per input report rather than only the first report. If the fingerprint changes, create a new series or skip alerts with an explicit message.
4. Pin the action to `52576c92bccf6ac60c8223ec7eb2565637cae9ba`, pin the SDK/runtime rather than `10.0.x`, and use an explicit Ubuntu image. Hardware variance remains on GitHub-hosted runners; use a stable self-hosted pinned core or normalize against stable controls for tight nanosecond thresholds.
5. Set `cancel-in-progress: false` if every main commit must become a baseline. Otherwise report the exact previous successful benchmark SHA prominently so a non-parent comparison is clear.
6. Keep the raw BDN JSON artifact and the normalized custom JSON together, including conversion version and source-file hashes, so an alert can be reconstructed without Actions log access.

Exact unmodified gh-pages benchmark entries for only `703661f` and `0ef2676` are saved in `/tmp/tl-ci-regression-data.json` (92,873 bytes). The local parsing probes are `/tmp/parse_tl_bench_history.py`, `/tmp/pair_tl_bench.py`, and `/tmp/show_tl_bench_slice.py`.


## Correctly paired cases still above the original 20% threshold

| Case | Job inferred from report order | Previous mean ns | Current mean ns | Ratio |
|---|---|---:|---:|---:|
| Algorithms Large Binary, Step 7 | steady | 11.803 | 15.961 | 1.352 |
| Algorithms Medium GeneratedState, Step 1 | steady | 5.487 | 6.671 | 1.216 |
| CountsShape ScanSingle, 16 clips, sequential | Jit | 51.321 | 62.190 | 1.212 |
| CursorShape SingleCursorInvalid, 16 clips, random | NoTiering | 85.745 | 103.556 | 1.208 |

These remain investigation candidates, not established source-code regressions. The four near-zero PassPlayback cases are retained in the raw evidence but unsuitable for ratio-based decisions.

## Implemented collection correction

`benchmarks/collect.py` preserves each job's DisplayInfo and FullName, including parameters, in a unique identity. It adds CPU, runtime, SDK, OS, ISA, configuration and BenchmarkDotNet metadata to the environment fingerprint. Missing statistics and duplicate identities reject collection. The new custom action format consistently compares medians; historical action values above were means.

The comparison floor is explicitly configured at 0.1 ns. Measurements below it remain in `benchmark-excluded.json` and the raw artifacts. This is an alert policy, not a claim that every measurement below 0.1 ns is physically impossible.

The corrected identities intentionally start new history series; corrupted old series are not silently relabeled. A different environment also starts a separate series. GitHub-hosted runners with the same reported metadata can still differ in frequency, contention or virtualization, so the correction does not turn hosted CI into a precision laboratory.

The action is pinned to the audited full commit. This change does not pin a self-hosted runner or assert that the four remaining cases were fixed. No GitHub comment, workflow dispatch or history rewrite was performed in this session.
