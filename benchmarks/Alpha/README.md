# Alpha

`Alpha` is the executable release benchmark: `--verify` runs the typed lane against the `TimelineMovement`-based oracle and asserts 0 B warm allocation. It is a core-gate and CI step on every push; that console receipt is the live evidence.

## Retained evidence (`results/`)

- `v1.0.0-alpha.3-shape-matrix/` — the alpha.3 shape matrix: 1/3/16/256 tracks, A-B-A order, gaps, blends, three input columns, mixed assets, both directions, tiering, allocation, and generated/JIT/NativeAOT sizes. SHA-256 manifested.
- `v1.0.0-alpha.3-query-floor/` — the alpha.3 catalog query floor A/B against the hand-written oracle.

Both document the removed alpha.3 surface; issue #27 is its design and evidence record. Their numbers apply only to the commits named inside each README.

All earlier commit-scoped result directories (initial/final, v1.0.0-alpha.2, signed-seek, the data-authored r1-r10 reduction ladder) were removed under issue #195; `git log` retains them.
