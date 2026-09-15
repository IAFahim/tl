# Benchmark status

`Alpha` is the executable v1 benchmark project and the only benchmark project included in the current solution. Its `--verify` lane receipts (typed lane vs `TimelineMovement` oracle, 0 B warm allocation) are current evidence; the BenchmarkDotNet configuration, full-query receipts, and code-size fixtures referenced below are retained alpha.3-era history. The retained [alpha.3 shape matrix](Alpha/results/v1.0.0-alpha.3-shape-matrix/README.md) covers 1/3/16/256 tracks, A-B-A order, gaps, blends, three input columns, mixed assets, both directions, tiering, allocation, and generated/JIT/NativeAOT sizes.

Older Alpha result directories and every other benchmark directory are commit-scoped history. They explain algorithms and rejected designs but may target removed APIs. Their numbers apply only to the source, runtime, controls, and contract named in each local report.
