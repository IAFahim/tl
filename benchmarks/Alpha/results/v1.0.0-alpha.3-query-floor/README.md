# Alpha.3 catalog query floor

This evidence measures the complete generated `Catalog.Query.Tick` operation against an honest hand-written oracle with the same three ordered consumer effects, state transition, game tick, direction, and receipt. It does not report an empty kernel or a throughput number as scalar latency.

The performance base is `a5344cd68029b7ebb1900652936fa226bd83860a`. The candidate production commit is `cd9f7553621e529ea1f7ae43f3e54f883d46715d`, merged over integration commit `51abaa0f15a3588fd9282f846c3085c5585c0ec7`. There are no changes under `src`, `benchmarks/Alpha`, or `tests/Tl.Gen.CSharp.Tests` between the performance base and the integration commit.

## Result

| Complete operation | Base median | Candidate median of three process medians | Direct candidate oracle | Allocation |
|---|---:|---:|---:|---:|
| Scalar forward | 12.510 ns | 3.535 ns | 2.062 ns | 0 B |
| Scalar alternating direction | 20.330 ns | 3.653 ns | 2.072 ns | 0 B |
| Batch, 1 row | 9.039 ns/entity-tick | 4.090 ns/entity-tick | 2.181 ns/entity-tick | 0 B |
| Batch, 32 rows | 7.988 ns/entity-tick | 9.174 ns/entity-tick | 1.880 ns/entity-tick | 0 B |
| Batch, 10,000 rows | 7.892 ns/entity-tick | 8.341 ns/entity-tick | 1.770 ns/entity-tick | 0 B |

The generated scalar path improved by 71.7% forward and 82.0% under alternating direction. It did not reach the requested strict `<3 ns` full-operation target. The repeatable forward result remains about 0.535 ns above it. The 2.062 ns direct fixture is the lower bound observed for the three actual consumer effects and state transition on this machine; it is not a universal timeline floor.

The first retained inline shape accidentally enlarged the shared signed `Tick` method enough to make the 10,000-row path fall to 38.225 ns/entity-tick. Commit `cd9f755` separates the staged scheduler into a non-inlined method. This preserves scalar fusion and restores the 10,000-row result to 8.341 ns/entity-tick. Its PMU cost is within 3.1% of the exact base in cycles and within 0.5% in retired instructions.

[summary.csv](summary.csv) retains every process mean, median, per-entity-tick projection, and allocation result. The `run1`, `run2`, and `run3` directories retain the raw BenchmarkDotNet JSON and process logs. BenchmarkDotNet could not raise process priority on this host; each log records that failure.

## PMU

| Scenario | Cycles/tick | Instructions/tick | Branches/tick | Branch misses/tick |
|---|---:|---:|---:|---:|
| Candidate direct scalar | 21.598 | 114.853 | 11.412 | 0.01725 |
| Candidate generated scalar | 52.000 | 217.236 | 35.519 | 0.01756 |
| Base direct batch | 10.363 | 63.629 | 4.073 | 0.00027 |
| Base generated batch | 52.499 | 332.494 | 77.224 | 0.00195 |
| Candidate direct batch | 10.440 | 63.629 | 4.073 | 0.00028 |
| Candidate generated batch | 54.118 | 333.951 | 77.253 | 0.00201 |

The collector pinned its child to CPU 0 and used one `perf_event_open` group for cycles, instructions, branches, and branch misses. Every counter ran without multiplexing. [pmu-candidate.jsonl](pmu-candidate.jsonl) and [pmu-baseline-batch.jsonl](pmu-baseline-batch.jsonl) retain the raw records.

The remaining generated scalar overhead is visible in [QueryPaths.asm](disassembly/QueryPaths.asm): query shape and asset routing, total state movement, lifecycle flags, component bounds, and three typed effects all remain in the measured operation. The hand oracle establishes how much of the result is consumer work. Deleting type, state, lifecycle, or effect-order semantics would make the comparison dishonest.

## Generated and native size

The fusion policy budgets the exact combined UTF-8 source for the forward and reverse schedules of a schema at 32 KiB. Small schemas fuse. A schema above the threshold keeps the staged execution already required by the general path.

| Shape | Count | Base generated | Candidate generated | Base FullOpts JIT | Candidate FullOpts JIT | Base NativeAOT code | Candidate NativeAOT code |
|---|---:|---:|---:|---:|---:|---:|---:|
| Assets | 1 | 6,763 B | 10,213 B | 1,292 B | 2,848 B | 1,160 B | 2,078 B |
| Assets | 16 | 63,972 B | 107,064 B | 11,146 B | 22,291 B | 10,412 B | 22,048 B |
| Assets | 256 | 983,996 B | 984,305 B | 229,442 B | 229,413 B | 202,982 B | 202,954 B |
| Tracks | 1 | 6,745 B | 10,187 B | 1,292 B | 2,848 B | 1,160 B | 2,078 B |
| Tracks | 16 | 23,668 B | 38,686 B | 6,201 B | 18,328 B | 5,107 B | 9,900 B |
| Tracks | 256 | 297,766 B | 298,075 B | 87,978 B | 87,959 B | 75,566 B | 75,547 B |

The 256 cases exceed the fusion budget and differ from base generated source by only 309 B. Their JIT and NativeAOT code return to the base shape within link/JIT layout noise. Without the budget, the measured 256-asset candidate generated 1,727,883 B and 398,252 B of NativeAOT generated methods; the 256-track candidate generated 499,708 B and 148,796 B of NativeAOT methods. The full 256-asset generated source remains large because it contains 256 independently generated timeline definitions and catalog routing even when fusion is disabled. The 250,000-byte product source budget applies to repository production source and paths, which measure 188,676 B at `cd9f755`; generated consumer output is reported separately.

[code-size.csv](code-size.csv) retains the source, FullOpts JIT, NativeAOT method, and executable byte counts. `benchmarks/Alpha/prepare_code_size.py` deterministically creates the six fixtures.

## Rejected shapes

- Forcing the full signed scalar body into one large method produced 3,691 B of JIT code and regressed forward to 7.244 ns and alternating direction to 18.248 ns.
- Specializing duration and looping after the asset switch and deleting the one-region bound reduced the scalar method to 704 B, about 207.8 instructions and 33.08 branches per tick, but measured 3.775 ns forward and about 3.87 ns alternating direction. It was slower than the retained code despite retiring less work.
- Sharing generic movement while specializing only frame flags measured about 209.4 instructions, 30.89 branches, and 52.20 cycles per tick without a latency improvement.

These results show a front-end and dependency-layout limit for this fixture: fewer instructions did not produce lower end-to-end latency. They do not justify weakening schema-before-effects validation, lifecycle semantics, or deterministic ordering.

## Reproduction

```sh
dotnet build benchmarks/Alpha/Alpha.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false
dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --verify
dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*CatalogQueryBenchmarks*'
python3 benchmarks/Alpha/collect_pmu.py --scenario scalar-direct --scenario scalar-query --scenario batch-direct --scenario batch-query --output pmu.jsonl
python3 benchmarks/Alpha/prepare_code_size.py --repository . --output /tmp/tl-code-size
```

Full-optimization assembly was collected with ReadyToRun and tiering disabled. NativeAOT code bytes are the sum of generated catalog/timeline text symbols, excluding the user job and compiler-created state helper. Executable bytes are reported separately in [code-size.csv](code-size.csv).
