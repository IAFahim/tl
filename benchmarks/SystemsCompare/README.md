# SystemsCompare

Gate comparison harness: old baseline vs pre/post fast-path systems codegen on the
NewA/B/C/D shapes (plus netstandard2.1 twins), with LiveBatch baselines alongside.

Committed release-machine snapshot (`results/new-fastpath`, i9-14900K, .NET 10.0.12):
NewA 945 μs, NewB 908 μs, NewC 836 μs, NewD 1450 μs medians, ~0 B allocated.
Pre-fast-path and old-baseline receipts sit beside them for the gate comparison.

Receipts: `results/old-baseline`, `results/new-prefast`, `results/new-prefast-21`,
`results/new-fastpath`, `results/new-fastpath-21` (BenchmarkDotNet reports + logs).

Status: active work on the stack — the harness does not build yet (24 errors,
identical on the stack tip; generator-analyzer emission pending). The committed
release-machine reports below are the current evidence. Intended runs once wired:
`dotnet run --project benchmarks/SystemsCompare/SystemsCompare.csproj -c Release -- --smoke`
(determinism double-run) or `--verify` (closed-form check, exit 1 on MISMATCH).
