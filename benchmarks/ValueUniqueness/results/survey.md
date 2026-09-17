# Payload value-uniqueness survey (issue #149)

Base commit: `e8729be0ce64e2881b6495d1096a698b54c8b29c`. Generator: `benchmarks/ValueUniqueness/survey.py` (deterministic; sorted paths, culture-invariant output).

Question: does real authoring data in this repository have few unique values per pair-type data field, so that per-asset value pools behind byte/ushort indices would shrink baked assets?

## Headline

- All 51 data fields across the 22 valid real documents have at most 3 unique values; 51/51 (100%) fit a byte pool, 0 need ushort. The owner's 'few unique values' claim is true for this corpus.
- Yet projected on the real bakes, per-field pooling makes every real asset *larger* (+44 to +136 bytes, +8% to +19%): slot strides are 16-byte aligned, the inline payload occupies only 12-20 of every 32-48 stride bytes, and the pool tables add cold metadata. Where the stride does shrink, break-even is 4-5 frame-slot occurrences per pair; repo assets carry at most 6.
- Identity strings for contrast: 29 track and 44 clip identity occurrences collapse to 11 track and 12 clip unique (namespace, type) names - that side is already pooled.

## Corpus

Standalone documents were collected from `samples/`, `tests/tlbake_cli/fixtures/`, `tools/Tl.Blender/fixtures/`, `tools/Tl.Playground/presets/` (their embedded `timeline` documents).
Embedded documents were extracted from C# raw-string literals and Python sources in `tools/Tl.Bake.Tests/`, `tools/Tl.Playground/Play.Core/`, and `tests/test_tl_blender.py`.
`tools/Tl.Bake.Tests/DiagnosticTests.cs` fixtures are intentionally invalid negative diagnostics and are excluded.
`tools/Tl.Bake.Tests/golden/introspection.json` is a metadata golden, not an authored asset. `samples/Mixed/` is code-authored (no JSON document).

| class | documents |
| --- | --- |
| diagnostic | 25 |
| embedded | 14 |
| fixture | 5 |
| preset | 4 |
| sample | 4 |

## Per-asset results (baked + projected)

Baseline `tlb/total-bytes` comes from `tlbake --report` on the real bake. The Python TLB1 layout model reproduced `pair/count`, `stage/count`, `program/step-count`, and `frame-slot/region-bytes` exactly for every bake (zero mismatches).

| document | class | tracks | clips | total B | pooled B | delta B | delta % |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `samples/ManyEntities/move64.json` | sample | 1 | 1 | 271 | 315 | +44 | +16.2% |
| `samples/ManyEntities/pulse.json` | sample | 1 | 1 | 272 | 316 | +44 | +16.2% |
| `samples/ManyEntities/window.json` | sample | 1 | 1 | 275 | 319 | +44 | +16.0% |
| `samples/NuGetQuickStart/boss.json` | sample | 1 | 1 | 301 | 345 | +44 | +14.6% |
| `tests/test_tl_blender.py#L136` | embedded | 2 | 3 | 609 | 661 | +52 | +8.5% |
| `tests/test_tl_blender.py#L191` | embedded | 1 | 1 | 337 | 381 | +44 | +13.1% |
| `tests/tlbake_cli/fixtures/alpha.json` | fixture | 1 | 2 | 424 | 468 | +44 | +10.4% |
| `tests/tlbake_cli/fixtures/alpha_edited.json` | fixture | 1 | 2 | 424 | 468 | +44 | +10.4% |
| `tools/Tl.Bake.Tests/CacheCliTests.cs#L62` | embedded | 1 | 1 | 259 | 303 | +44 | +17.0% |
| `tools/Tl.Bake.Tests/CliTests.cs#L19` | embedded | 1 | 1 | 259 | 303 | +44 | +17.0% |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | embedded | 2 | 4 | 955 | 1091 | +136 | +14.2% |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | embedded | 2 | 3 | 558 | 650 | +92 | +16.5% |
| `tools/Tl.Bake.Tests/MetadataTests.cs#L14` | embedded | 1 | 2 | 307 | 355 | +48 | +15.6% |
| `tools/Tl.Bake.Tests/MultiPairBakeTests.cs#L102` | embedded | 2 | 2 | 348 | 400 | +52 | +14.9% |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | embedded | 2 | 4 | 1094 | 1230 | +136 | +12.4% |
| `tools/Tl.Bake.Tests/WatchModeTests.cs#L15` | embedded | 1 | 1 | 271 | 323 | +52 | +19.2% |
| `tools/Tl.Blender/fixtures/receipt_scene.json` | fixture | 2 | 3 | 612 | 664 | +52 | +8.5% |
| `tools/Tl.Blender/fixtures/receipt_scene_hand.json` | fixture | 2 | 3 | 612 | 664 | +52 | +8.5% |
| `tools/Tl.Playground/presets/finite-clamp.json` | preset | 1 | 2 | 321 | 369 | +48 | +15.0% |
| `tools/Tl.Playground/presets/staggered.json` | preset | 1 | 2 | 321 | 369 | +48 | +15.0% |
| `tools/Tl.Playground/presets/uniform-crowd.json` | preset | 1 | 2 | 321 | 369 | +48 | +15.0% |
| `tools/Tl.Playground/presets/waves.json` | preset | 1 | 2 | 369 | 417 | +48 | +13.0% |

Whole-struct variant (one byte index per slot side, pool entries are full structs) also loses on every asset:

| document | struct-pooled B | delta B |
| --- | ---: | ---: |
| `samples/ManyEntities/move64.json` | 331 | +60 |
| `samples/ManyEntities/pulse.json` | 332 | +60 |
| `samples/ManyEntities/window.json` | 335 | +60 |
| `samples/NuGetQuickStart/boss.json` | 361 | +60 |
| `tests/test_tl_blender.py#L136` | 677 | +68 |
| `tests/test_tl_blender.py#L191` | 397 | +60 |
| `tests/tlbake_cli/fixtures/alpha.json` | 468 | +44 |
| `tests/tlbake_cli/fixtures/alpha_edited.json` | 468 | +44 |
| `tools/Tl.Bake.Tests/CacheCliTests.cs#L62` | 319 | +60 |
| `tools/Tl.Bake.Tests/CliTests.cs#L19` | 319 | +60 |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | 1139 | +184 |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | 682 | +124 |
| `tools/Tl.Bake.Tests/MetadataTests.cs#L14` | 371 | +64 |
| `tools/Tl.Bake.Tests/MultiPairBakeTests.cs#L102` | 416 | +68 |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | 1278 | +184 |
| `tools/Tl.Bake.Tests/WatchModeTests.cs#L15` | 323 | +52 |
| `tools/Tl.Blender/fixtures/receipt_scene.json` | 680 | +68 |
| `tools/Tl.Blender/fixtures/receipt_scene_hand.json` | 680 | +68 |
| `tools/Tl.Playground/presets/finite-clamp.json` | 385 | +64 |
| `tools/Tl.Playground/presets/staggered.json` | 385 | +64 |
| `tools/Tl.Playground/presets/uniform-crowd.json` | 385 | +64 |
| `tools/Tl.Playground/presets/waves.json` | 433 | +64 |

## Per-field uniqueness (included documents)

Occurrences count authored `data` entries per (document, type, side, field). Authored values are mostly distinct within a field (71 distinct of 71 authored) simply because each document is small; the absolute distinct count per field is what bounds the pool, and it stays at or below 3.

| document | type | side | field | occurrences | uniques | pool width |
| --- | --- | --- | --- | ---: | ---: | --- |
| `samples/ManyEntities/move64.json` | ManyEntities:MoveClip | clip | Amount (int) | 1 | 1 | byte |
| `samples/ManyEntities/move64.json` | ManyEntities:MoveTrack | track | Mult (float) | 1 | 1 | byte |
| `samples/ManyEntities/pulse.json` | ManyEntities:PulseClip | clip | Amount (int) | 1 | 1 | byte |
| `samples/ManyEntities/pulse.json` | ManyEntities:PulseTrack | track | Power (int) | 1 | 1 | byte |
| `samples/ManyEntities/window.json` | ManyEntities:WindowClip | clip | Amount (int) | 1 | 1 | byte |
| `samples/ManyEntities/window.json` | ManyEntities:WindowTrack | track | Power (int) | 1 | 1 | byte |
| `samples/NuGetQuickStart/boss.json` | Fresh:DamageClip | clip | Amount (float) | 1 | 1 | byte |
| `samples/NuGetQuickStart/boss.json` | Fresh:DamageTrack | track | Multiplier (float) | 1 | 1 | byte |
| `tests/test_tl_blender.py#L136` | :GaGlobalClip | clip | Amount (float) | 3 | 3 | byte |
| `tests/test_tl_blender.py#L191` | :GaGlobalClip | clip | Amount (float) | 1 | 1 | byte |
| `tests/tlbake_cli/fixtures/alpha.json` | Tlb:JobClip | clip | Amount (float) | 2 | 2 | byte |
| `tests/tlbake_cli/fixtures/alpha.json` | Tlb:JobClip | clip | Steps (int) | 2 | 2 | byte |
| `tests/tlbake_cli/fixtures/alpha.json` | Tlb:JobTrack | track | Multiplier (float) | 1 | 1 | byte |
| `tests/tlbake_cli/fixtures/alpha_edited.json` | Tlb:JobClip | clip | Amount (float) | 2 | 2 | byte |
| `tests/tlbake_cli/fixtures/alpha_edited.json` | Tlb:JobClip | clip | Steps (int) | 2 | 2 | byte |
| `tests/tlbake_cli/fixtures/alpha_edited.json` | Tlb:JobTrack | track | Multiplier (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/CacheCliTests.cs#L62` | Tlb:AlphaClip | clip | Value (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/CacheCliTests.cs#L62` | Tlb:AlphaTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/CliTests.cs#L19` | Tlb:AlphaClip | clip | Value (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/CliTests.cs#L19` | Tlb:AlphaTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:DualAlphaClip | clip | Value (int) | 2 | 2 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:DualBetaClip | clip | Amount (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:DualTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:BlendClip | clip | Amount (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:BlendTrack | track | Scale (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | Tlb:AlphaClip | clip | Value (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | Tlb:AlphaTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | Tlb:BlendClip | clip | Amount (float) | 2 | 2 | byte |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | Tlb:BlendTrack | track | Scale (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/MetadataTests.cs#L14` | Tlb:AlphaClip | clip | Value (int) | 2 | 2 | byte |
| `tools/Tl.Bake.Tests/MetadataTests.cs#L14` | Tlb:AlphaTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/MultiPairBakeTests.cs#L102` | Tlb:AlphaClip | clip | Value (int) | 2 | 2 | byte |
| `tools/Tl.Bake.Tests/MultiPairBakeTests.cs#L102` | Tlb:AlphaTrack | track | Code (int) | 2 | 2 | byte |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:DualAlphaClip | clip | Value (int) | 2 | 2 | byte |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:DualBetaClip | clip | Amount (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:DualTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:EchoClip | clip | Value (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:EchoTrack | track | Code (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/WatchModeTests.cs#L15` | Tlb:JobClip | clip | Amount (float) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/WatchModeTests.cs#L15` | Tlb:JobClip | clip | Steps (int) | 1 | 1 | byte |
| `tools/Tl.Bake.Tests/WatchModeTests.cs#L15` | Tlb:JobTrack | track | Multiplier (float) | 1 | 1 | byte |
| `tools/Tl.Blender/fixtures/receipt_scene.json` | Tlb:BlendClip | clip | Amount (float) | 3 | 3 | byte |
| `tools/Tl.Blender/fixtures/receipt_scene_hand.json` | Tlb:BlendClip | clip | Amount (float) | 3 | 3 | byte |
| `tools/Tl.Playground/presets/finite-clamp.json` | Play:AmountClip | clip | Amount (float) | 2 | 2 | byte |
| `tools/Tl.Playground/presets/finite-clamp.json` | Play:ScaleTrack | track | Scale (float) | 1 | 1 | byte |
| `tools/Tl.Playground/presets/staggered.json` | Play:AmountClip | clip | Amount (float) | 2 | 2 | byte |
| `tools/Tl.Playground/presets/staggered.json` | Play:ScaleTrack | track | Scale (float) | 1 | 1 | byte |
| `tools/Tl.Playground/presets/uniform-crowd.json` | Play:AmountClip | clip | Amount (float) | 2 | 2 | byte |
| `tools/Tl.Playground/presets/uniform-crowd.json` | Play:ScaleTrack | track | Scale (float) | 1 | 1 | byte |
| `tools/Tl.Playground/presets/waves.json` | Play:AmountClip | clip | Amount (float) | 2 | 2 | byte |
| `tools/Tl.Playground/presets/waves.json` | Play:ScaleTrack | track | Scale (float) | 1 | 1 | byte |

## Per-pair projection detail (baked assets)

| document | pair | occurrences | stride B -> pooled B | saved/occurrence B | pool table B | break-even occurrences |
| --- | --- | ---: | --- | ---: | ---: | ---: |
| `samples/ManyEntities/move64.json` | ManyEntities:MoveTrack / ManyEntities:MoveClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `samples/ManyEntities/pulse.json` | ManyEntities:PulseTrack / ManyEntities:PulseClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `samples/ManyEntities/window.json` | ManyEntities:WindowTrack / ManyEntities:WindowClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `samples/NuGetQuickStart/boss.json` | Fresh:DamageTrack / Fresh:DamageClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tests/test_tl_blender.py#L136` | :GaGlobalTrack / :GaGlobalClip | 5 | 32 -> 32 | 0 | 52 | never (no stride shrink) |
| `tests/test_tl_blender.py#L191` | :GaGlobalTrack / :GaGlobalClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tests/tlbake_cli/fixtures/alpha.json` | Tlb:JobTrack / Tlb:JobClip | 2 | 48 -> 32 | 16 | 76 | 4.8 |
| `tests/tlbake_cli/fixtures/alpha_edited.json` | Tlb:JobTrack / Tlb:JobClip | 2 | 48 -> 32 | 16 | 76 | 4.8 |
| `tools/Tl.Bake.Tests/CacheCliTests.cs#L62` | Tlb:AlphaTrack / Tlb:AlphaClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/CliTests.cs#L19` | Tlb:AlphaTrack / Tlb:AlphaClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:DualTrack / Tlb:DualAlphaClip | 3 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:BlendTrack / Tlb:BlendClip | 4 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L13` | Tlb:DualTrack / Tlb:DualBetaClip | 1 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | Tlb:AlphaTrack / Tlb:AlphaClip | 3 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/DeterminismTests.cs#L67` | Tlb:BlendTrack / Tlb:BlendClip | 3 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/MetadataTests.cs#L14` | Tlb:AlphaTrack / Tlb:AlphaClip | 2 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/MultiPairBakeTests.cs#L102` | Tlb:AlphaTrack / Tlb:AlphaClip | 2 | 32 -> 32 | 0 | 52 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:DualTrack / Tlb:DualAlphaClip | 6 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:EchoTrack / Tlb:EchoClip | 3 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/Recording.cs#L20` | Tlb:DualTrack / Tlb:DualBetaClip | 4 | 32 -> 32 | 0 | 44 | never (no stride shrink) |
| `tools/Tl.Bake.Tests/WatchModeTests.cs#L15` | Tlb:JobTrack / Tlb:JobClip | 1 | 48 -> 32 | 16 | 68 | 4.2 |
| `tools/Tl.Blender/fixtures/receipt_scene.json` | Tlb:BlendTrack / Tlb:BlendClip | 5 | 32 -> 32 | 0 | 52 | never (no stride shrink) |
| `tools/Tl.Blender/fixtures/receipt_scene_hand.json` | Tlb:BlendTrack / Tlb:BlendClip | 5 | 32 -> 32 | 0 | 52 | never (no stride shrink) |
| `tools/Tl.Playground/presets/finite-clamp.json` | Play:ScaleTrack / Play:AmountClip | 2 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Playground/presets/staggered.json` | Play:ScaleTrack / Play:AmountClip | 2 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Playground/presets/uniform-crowd.json` | Play:ScaleTrack / Play:AmountClip | 2 | 32 -> 32 | 0 | 48 | never (no stride shrink) |
| `tools/Tl.Playground/presets/waves.json` | Play:ScaleTrack / Play:AmountClip | 3 | 32 -> 32 | 0 | 48 | never (no stride shrink) |

## Synthetic stress of the claim boundary

Both assets are synthetic (not repo data): one `Play.ScaleTrack`/`Play.AmountClip` lane, 1000 non-overlapping clips, baked against `Play.Core`.

| asset | clip Amount uniques | total B | pooled B | delta B |
| --- | ---: | ---: | ---: | ---: |
| synthetic_shared10 | 10 | 56193 | 56273 | +80 |
| synthetic_unique1000 | 1000 | 56193 | 60233 | +4040 |

Even with only 10 unique clip values the projection grows by +80 bytes: the 32-byte stride of this pair does not shrink because `align16(17 payload-free bytes + 3 index bytes)` is still 32, so pooling adds 80 bytes of pool table for zero frame savings. With 1000 uniques the pool table alone adds +4040 bytes, and past 256 uniques the indices become ushort, growing slots only when the wider index crosses the same alignment boundary.

## Verdict

1. The uniqueness claim holds: every real data field in the repository has at most 3 unique values, far under the 256-entry byte pool; no field needs ushort.
2. Uniqueness is not the binding constraint on this corpus; the 16-byte slot stride and small occurrence counts are. Pooling pays only when a pair has enough frame-slot occurrences to amortize the pool table and a payload wide enough for the shrunken stride to cross an alignment boundary (fat clips such as the 256-byte `FusedBake.GaClip0` introspected in test metadata would qualify; no real asset uses one).
3. For the current asset population the projected effect is a small absolute loss (+44 to +136 bytes per asset under per-field pools, +44 to +184 under whole-struct pools), so asset-size reduction alone does not justify the format change; any motivation must come from elsewhere and must be proven separately.

## Assumptions

- poolScope: per asset, per (trackType, clipType) pair, per side (track/clip), per field
- indexWidth: byte index when unique values <= 256, otherwise ushort
- poolHeaderBytes: 16
- poolLocation: metadata (cold) region; warm frame slots shrink by replaced struct bytes and grow by index bytes
- slotModel: pooled stride = align16(17 + trackIndexBytes + 2*clipIndexBytes); 17 = four uint window/factor words plus byte TrackIndex
- defaultEntries: one extra pool entry per field when some value slot carries no authored value (zero struct default)
- structModel: alternative: one byte index per slot side; pool entries are whole track/clip structs
- unchanged: header, pair table, stage table, program, labels, and identity strings are unchanged by pooling

## Skipped documents

| document | class | reason |
| --- | --- | --- |
| `tests/tlbake_cli/fixtures/broken.json` | fixture | duplicate field: 'duration' |
| `tools/Tl.Bake.Tests/CacheCliTests.cs#L180` | embedded | unresolved type (Nope:MissingTrack) |
| `tools/Tl.Bake.Tests/CliTests.cs#L61` | embedded | unresolved type (Nope:MissingTrack) |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L121` | diagnostic | unresolved type (Tlb:NoBlendTrack) |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L13` | diagnostic | duration missing or outside [0, 65535] |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L141` | diagnostic | Diagnostic error: clip type not blendable by track: track 0 resolves clip type 'Tlb.DualBetaClip' but track type 'Tlb.AlphaTrack' implements only these Tl.IBlend<TClip> pairings: Tlb.AlphaClip. |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L162` | diagnostic | unresolved type (Tlb:Inner) |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L183` | diagnostic | Diagnostic error: type resolution failed: assembly 'NotTheRightAssembly' does not contain (Tlb, AlphaTrack) for track 0; matching types exist in assembly(es): Tl.Bake.Tests. |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L205` | diagnostic | track namespace/type missing, dotted, or empty |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L225` | diagnostic | clip namespace/type missing, dotted, or empty |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L245` | diagnostic | Diagnostic error: removed property 'payload' in clip 0 on track 0: schema v1 declares 'namespace', 'type' and optional 'assembly' on every track and clip, and 'data' for payloads; type identity is never inherited between levels. See docs/data-authored-api.md. |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L265` | diagnostic | Diagnostic error: renamed property 'loops' in root: use 'loop'. |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L286` | diagnostic | Diagnostic error: unknown field name in track/payload: field 'NonExistentField' not found on type 'AlphaTrack' in track 0 (AlphaTrack). |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L307` | diagnostic | Diagnostic error: unknown field name in track/payload: field 'UnknownField' not found on type 'AlphaClip' in clip 0 on track 0 (AlphaClip). |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L328` | diagnostic | Diagnostic error: wrong-typed value: field 'Code' in track 0 (AlphaTrack) expected int, got "not_an_int". |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L33` | diagnostic | unresolved type (Tlb:NonExistentTrack) |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L349` | diagnostic | Diagnostic error: wrong-typed value: field 'Value' in clip 0 on track 0 (AlphaClip) expected int, got "not_an_int". |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L370` | diagnostic | Diagnostic error: overlapping clips on one track: track 0 (AlphaTrack/AlphaClip) has more than two overlapping clips at tick 10. |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L394` | diagnostic | Diagnostic error: overlapping clips on one track: track 0 (AlphaTrack/AlphaClip) has multiple clips starting at tick 5. |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L417` | diagnostic | structurally valid document from the negative-diagnostic test source; excluded by class |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L458` | diagnostic | clip window outside [0, duration) or reversed |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L478` | diagnostic | clip window outside [0, duration) or reversed |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L498` | diagnostic | duplicate field: 'duration' |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L519` | diagnostic | clip namespace/type missing, dotted, or empty |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L53` | diagnostic | track namespace/type missing, dotted, or empty |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L539` | diagnostic | track clips missing or empty |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L73` | diagnostic | track namespace/type missing, dotted, or empty |
| `tools/Tl.Bake.Tests/DiagnosticTests.cs#L93` | diagnostic | Diagnostic error: track or clip not unmanaged: track type 'Tlb.ManagedTrack' is not an unmanaged type. |
| `tools/Tl.Playground/Play.Core/LiveAuthoring.cs#L77` | embedded | unresolved type (Live:ScaleTrack) |
| `tools/Tl.Playground/Play.Core/Scenario.cs#L100` | embedded | source template with placeholders, not a document |

