# Benchmark status

`Alpha` is the executable v1 benchmark project and the only benchmark project included in the current solution. Its verifier, BenchmarkDotNet configuration, full-query receipts, JIT assembly, PMU collector, and code-size fixtures are current alpha.3 release evidence. The retained [alpha.3 shape matrix](Alpha/results/v1.0.0-alpha.3-shape-matrix/README.md) covers 1/3/16/256 tracks, A-B-A order, gaps, blends, three input columns, mixed assets, both directions, tiering, allocation, and generated/JIT/NativeAOT sizes.

Older Alpha result directories and every other benchmark directory are commit-scoped history. They explain algorithms and rejected designs but may target removed APIs. Their numbers apply only to the source, runtime, controls, and contract named in each local report.
