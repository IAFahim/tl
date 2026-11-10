# TLIB v3 bake path (M2 slice — BakeV3, 2026-11-10)

`tl library` can emit TLIB v3 with `--format v3`:

```sh
tl library <dir> --assembly Game.dll --out game.tlib --format v3
tl --inspect game.tlib
tl decode game.tlib -o decoded.json --assembly Game.dll
```

The per-timeline TLB bake is unchanged. `tools/Tl.Gen.Tlb/LibraryV3Bake`
converts each TLB part into a `TimelineSpec`/`StepSpec` and calls
`Tl.LibraryWriter.Write`. `tl --inspect` and `tl decode` read the v3 blob
through `Tl.Library.Verify` (`Tlib3Inspection`); v1/v2 and TLB inputs keep
their existing paths.

## Mapping

| TLB | v3 |
|---|---|
| header `Duration`, `Loops` | `TimelineSpec.Duration`, `Loop` |
| stage program rows in stage order | one `StepSpec` each |
| slot `[WindowStart, WindowEnd)` | `StepSpec` window |
| pair table row `Key` | `StepSpec.PairKey` |
| slot track/clip data (pools, blend windows) | not mapped (see below) |
| header `Next` (at 52) | not mapped; reader default: no successor |
| header `Channel` (at 56) | not mapped; reader default: channel 0 |
| header `Hold` (at 60) | not mapped; reader default: not held |
| marker section (at 64) | not mapped; reader default: synthetic start markers from `LibraryWriter` |
| timeline name (`LibraryPart.Name`) | not mapped; reader default: synthetic `Timeline0`, `Kind0` |
| multi-stage structure | flattened to one stage; reader still correct because every window is guarded by `[WindowStart, WindowEnd)` |
| kind payload (`LibraryPart.Kinds`) | not mapped; reader default: single `Kind0` row |

Every step gets `KindId 0` and zero forward/backward lanes.

## Why lanes are structural-only

The 4.0 fold lane is consumer-derived at load, not baked into the v1/v2
library, so there is no bake-time lane computation to reuse:

- `LibraryAssembler.Emit` (`src/Tl.Core/LibraryAssembler.cs:312-385`) writes
  header + manifest rows + name strings + verbatim TLB blobs + the kind
  section + SHA. It computes no floats (`Lane|Fold|float` has zero hits
  in that file).
- `TimelineBakerFast` (`tools/Tl.Gen.Tlb/TimelineBakerFast.cs:2186-2227`)
  writes only slot rows and raw track/clip pools; overlapping clips record
  a blend window (`FactorStart`/`FactorSpan`) that is never evaluated.
- `KindLayout.Section` (`tools/Tl.Gen.Tlb/KindLayout.cs:189-270`) writes
  kind windows and events only.
- `LaneEncoding.Encode` (`src/Tl.Core/Lane.cs:80`) has no callers, and
  `SlotView` is never populated with real data (`InternTable.cs:19-22`
  allocates the zeroed `RestView`).
- The lane is materialized at load: `TimelineWorld.MaterializeSteps`
  (`src/Tl.Core/TimelineWorld.Library.cs:313-324`) calls
  `entry.Module.FoldStep(...)` per tick per direction. The generated
  `FoldStep`
  (`tests/Tl.PackageConsumer/Generated/TlSystemsPackage.g.cs:31-54`) builds
  a `Frame` via `Frame_0` (track/clip resolution + `Blend` with
  `blend = (tick - FactorStart) / (FactorSpan - 1)`) and calls
  `Consumer.Fold(in frame, out float)`. Runtime reads the lane at
  `((tick - WindowStart) * 2 + backward) * 4` (same file, lines 157-158).

A bake-time fold would have to invoke arbitrary consumer code through
reflection over whatever assemblies happen to be loaded, with ambiguous
consumer discovery for shared pairs (e.g. two `ITrack<MoveTrack, MoveClip>`
implementations). That would make bake output depend on host-process
state instead of being content-stable, so the converter stays
structural-only: it emits the window as `float[window]` zeros
(`LaneClass.Constant`). `LibraryV3Tests.PairsWithoutBakedFoldEmitZeroLanes`
pins all-zero lanes and `KindId 0`.

## Verified

- `tl library samples/Runner/timelines --assembly Runner.dll --out game.tlib --format v3`:
  5 timelines, 21 steps, 2976 bytes, magic `0x42494C54`, version 3,
  `Library.Verify` accepts it (proved by `--inspect` and `decode` exiting 0
  on the same bytes; `decode` round-trips the inspect JSON).
- `tools/Tl.Bake.Tests`: `LibraryV3Tests` covers conversion, determinism,
  verify round-trip, window preservation, zero lanes, `--format v3` end to
  end and inspect/decode reads. Whole suite 869 passed.
- `tools/Tl.Bake.Tests/golden/library` through the CLI with `--format v3`:
  2 timelines, 928 bytes, SHA-256 `345a453e…` (full digest on request),
  `Library.Verify` accepts it (`--inspect` exits 0).

## Not done here (blocked)

- Default `tl library` still emits v1/v2. Switching the default requires the
  v1/v2 runtime to go away in the same change: `TimelineWorld` rejects v3
  bytes (`TimelineWorld.Library.cs:84`, only versions 1 and 2 pass), and 861
  bake fixture tests plus the `Tl.Systems.Tests` and `Tl.Packs.Tests` suites
  play v1/v2 libraries through them. A container-only switch would leave those
  suites red, and `tests/Tl.Systems.Tests` plus `tests/Tl.Packs.Tests` were
  outside this slice to edit.
- No goldens were re-pinned: `tools/Tl.Bake.Tests/golden/**`,
  `tests/Tl.Systems.Tests/{KindLibrary,GroupLibrary,TwoAssembly}`,
  `tests/Tl.Packs.Tests` `*.tlib` plus `*LibraryBytes.cs`, and
  `tests/tlb_cli/config.json` all still pin v1/v2/TLB bytes. Re-pinning them
  to v3 without migrating the runtime would break the suites that consume
  them. Regenerate them only together with the runtime cutover.
- Nothing in `src/Tl.Core` was deleted: `LibraryAssembler`, `KindPayload`,
  `KindSection`, `Data` (TLB v5/v6 readers), `InternTable` and `Inspection`
  still have live callers (`tools/Tl.Bake` `RunCommand`/`KindsPack`,
  `tests/Tl.Packs.Tests` ×4, `tests/Tl.Systems.Tests` ×3). Remaining TLB-v6
  readers: `Tl.TimelineRef.Validate` and `TimelineWorld` table readers in
  `src/Tl.Core`, plus `TimelineJson`, `TlbMetadata`, `TlbInspection` in
  `tools/Tl.Gen.Tlb`.

## Fixture hash table (2026-11-10, pre-cutover v1/v2 bytes)

`tests/Tl.PackageConsumer/game.tlib` 552 B `9418ed5a…`;
`tests/Tl.Systems.Tests/GroupLibrary/group.tlib` 2544 B `fd4cb8b0…`;
`tests/Tl.Systems.Tests/KindLibrary/game.tlib` 5656 B `dae99d5a…`;
`tests/Tl.Systems.Tests/KindLibrary/stale.tlib` 5656 B `5858af8e…`;
`tests/Tl.Systems.Tests/LoaderLibrary/game.tlib` 656 B `0f8b3225…`;
`tests/Tl.Systems.Tests/TwoAssembly/push.tlib` 4568 B `e4588cd1…`;
`tests/Tl.Systems.Tests/TwoTrackLibrary/game.tlib` 720 B `eea44e99…`;
`tests/Tl.Packs.Tests/Force/force.tlib` 5232 B `89174c56…`;
`tests/Tl.Packs.Tests/Inventory/inventory.tlib` 7448 B `210797b8…`;
`tests/Tl.Packs.Tests/Motion/motion.tlib` 6952 B `00019eb5…`;
`tests/Tl.Packs.Tests/Stats/stats.tlib` 10744 B `d6f7d067…`;
`tests/Tl.Core.Tests/kinds.tlib` 3840 B `3b2ab919…`;
`tests/Tl.Core.Tests/v1.tlib` 656 B `c9e6a62c…`;
`tools/Tl.Bake.Tests/golden/kinds/game.tlib` 4488 B `b9bc5b86…`;
`tools/Tl.Bake.Tests/golden/fixtures/blended.tlb` 1149 B `b463b5e6…`;
`tools/Tl.Bake.Tests/golden/fixtures/minimal.tlb` 375 B `679a442c…`.
`kinds/game.tlib` does not exist on disk. Before/after state-hash comparison
requires the old 4.0 runtime and the new v3 runtime running the same
consumer graph — Main to pair; the new v3 probe hash is `345a453e…`
(928 B) for the golden library above.
