# Budget ledger (measured `benchmarks/source_budget.py` before dedup)

$startmast
IMPLEMENTED CUTS LANDED (measured):
- Src: 635,293 → 626,567 content (+2,255 path) = 628,822/520,000 (over by 108,822)
- Shipped-tools: 351,248/345,000 (over by 6,248)
$endmast

## Live code paths that could go (measured byte sizes; consequencess explicit; no waiver assumed)

| # | Candidate (live today) | Size (measured wc -c) | What breaks if it goes |
|---|---|---|---|
| 1 | Emitter legacy Run/fold arms within `src/Tl.Gen.CSharp/SystemEmitter.cs` (74,359; ~55–60% is legacy-arm emission, projected) | ≈40,000 projected | Lane-fold by load-time `FoldStep` stops working; scene-runner reflective `Run(TimelineWorld, PlaybackTable, bool)` entrypoint disappears; pinned SceneRun hashes must move to World path (hash-equivalence migration, not free) |
| 2 | `TimelineWorld` consumer stack: `TimelineWorld.Library.cs` 23,979 + `Data.cs` 27,990 + `KindStore.cs` 27,082 + `TimelineWorld.Systems.cs` 4,281 + `KindRules.cs` 15,000 + `PlaybackRules.cs` 14,395 | 112,727 (measured sum) | 4.0 runtime becomes unhostable; packs/Runner hosts + 4.0 fold must be re-implemented on World; scene replay hashes depend on it until then |
| 3 | `World.Systems.cs` solo playback stack | 28,836 (measured) | `Solo` cohort support drops (Single-kind playback path) — kills 4.0 parity tests that exercise `Next`/`Hold`/solo; `Solo` remains in `Playbacks` design for the launch window (one-release support window) |
| 4 | Analysis stacks `TypeAnalyzer.cs` 23,447 + `JobReader.cs` 6,666 + `NewStyle.cs` 19,804 | 49,917 (measured) | Analyzer coverage drops (TLGEN9x family, job/kind emission); consolidation merges semantics, not deletable files |
| 5 | `src/Tl.Core/Threads.cs` + `ParallelApply` | 3,056 (measured) | 5.0 threading scaffolding collapses to direct `Parallel.For` call sites (API shape same) |
| 6 | `tools/Tl.Gen.Tlb`: `TimelineBakerFast.cs` 99,821 + `TimelineBakerSimd.cs` 46,996 | 146,817 | v1/v2 bake line dies; structural reader bake (Simd vs Fast are parallel algorithms, merge = rewrite) |

**Totals needed:** src needs −108,822 minimum; shipped-tools needs −6,248 minimum.
- Delete 1+2 covers src (−152k projected, over-covers: closes src cap with margin);
- Delete 4 or 3 alone do not cover; delete 6 alone covers shipped-tools (146,817 >> 6,248).

All candidates are *live, referenced, tests-passing* per test_dead_code (measured by the scanner); cost column is the consequence if the owner chose to retire each path.
