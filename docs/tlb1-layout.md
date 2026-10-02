# TLB1 container byte layout (version 4)

`tlb` compiles authored timeline JSON into TLB1, a deterministic binary container. This page specifies the byte layout so an out-of-process parser — a browser viewer over `.tlb` files, C catalog tooling, Unity-side import — can read the structure without running this repository's code. The committed fixtures under `tools/Tl.Bake.Tests/golden/fixtures/` are the test corpus for that parser.

The spec describes what the baker emits today (TLB1 version 4). It promises no stability across future versions: the header version field is the handshake, parsers reject what they do not know, and any format change bumps the version and regenerates the fixtures in the same change.

## Conventions

- All integers are unsigned, little-endian. Widths are written `u8`/`u16`/`u32`/`u64`.
- Offsets are absolute byte positions from the start of the file unless a field is explicitly marked relative.
- The file splits into a hot section `[0, HotLength)` — the bytes playback loads — and an optional cold metadata tail `[HotLength, Bytes)`. `tlb --strip` removes the tail and rewrites `HotLength` and `Bytes` so the hot section stands alone.
- Reserved fields and alignment padding are written zero and ignored on read.
- Section starts (`PairOffset`, `StageOffset`, program offsets, slot addresses, `SlotStride`) are 8-byte aligned. Value pools are 16-byte aligned.
- Baking is deterministic: the same authored JSON against the same assembly bytes produces the identical file on every machine and culture.

## Sections in file order

```
offset 0          64-byte header
PairOffset        pair table: PairCount x 48 bytes
StageOffset       stage table: StageCount x 16 bytes
(end of stages)   program steps: total steps x 8 bytes, stages in order
PoolOffset        value pools, per pair in pair order (see below)
FrameOffset       slot rows: one 24-byte row per program step
HotLength         cold metadata tail (optional)
Bytes             end of file
```

The writer places them with: `PairOffset = 64`, `StageOffset = PairOffset + 48*PairCount`, program steps immediately after the stage table, `PoolOffset` = that end rounded up to 16, each pool region rounded up to 16, and `FrameOffset` = the pool end rounded up to 16. A parser must not assume the rounding, only the order and the alignment rules the header invariants below state.

## Header (64 bytes)

| Offset | Width | Field | Value in v4 |
|---|---|---|---|
| 0 | u32 | Magic | `0x31424C54`; bytes `54 4C 42 31`, ASCII `TLB1` |
| 4 | u32 | Version | `4` |
| 8 | u32 | Loops | `1` when the authored timeline loops, else `0` |
| 12 | u32 | Duration | Timeline length in ticks; at most 65,535 |
| 16 | u32 | TrackCount | Number of authored tracks |
| 20 | u32 | StageCount | Number of stage-table entries |
| 24 | u32 | PairCount | Number of pair-table entries; at most 256 |
| 28 | u32 | PairOffset | At least 64; 8-byte aligned |
| 32 | u32 | StageOffset | 8-byte aligned |
| 36 | u32 | PoolOffset | 8-byte aligned |
| 40 | u32 | FrameOffset | 8-byte aligned |
| 44 | u32 | HotLength | Greater than 0, at most `Bytes` |
| 48 | u32 | Bytes | Equals the file size |
| 52 | u32 | Reserved0 | Zero |
| 56 | u32 | Reserved1 | Zero |
| 60 | u32 | Reserved2 | Zero |

## Pair table (`PairCount` x 48 bytes at `PairOffset`)

One entry per distinct `(track type, clip type)` pair used by the timeline, sorted by strictly ascending `Key`. The key is the 64-bit runtime identity of the type pair; a parser treats it as an opaque ordering token.

| Offset | Width | Field | Meaning |
|---|---|---|---|
| 0 | u64 | Key | Pair identity; strictly ascending across entries |
| 8 | u32 | SlotStride | `24` in v4; at least 24 and 8-byte aligned |
| 12 | u32 | TrackPoolOffset | Track value pool start, **relative to this pair entry's own file offset** |
| 16 | u32 | TrackPoolCount | Unique track values in the pool; at most 65,535 |
| 20 | u32 | TrackValueBytes | `sizeof` of the track struct |
| 24 | u32 | ClipPoolOffset | Clip value pool start, **relative to this pair entry's own file offset** |
| 28 | u32 | ClipPoolCount | Unique clip values in the pool; at most 65,535 |
| 32 | u32 | ClipValueBytes | `sizeof` of the clip struct |
| 36 | 4 | — | Zero padding |
| 40 | u64 | Layout | Consumer layout ABI fingerprint recorded by the consumer's published attributes; `0` when none was recorded |

Pool addressing: the absolute pool address of pair `i` is `PairOffset + 48*i + TrackPoolOffset` (or `ClipPoolOffset`). Pools therefore move with their pair entry, and the two pools of one pair need not be adjacent (each is rounded up to a 16-byte boundary after the previous region).

## Stage table (`StageCount` x 16 bytes at `StageOffset`)

Stages partition `[0, Duration)` into half-open tick ranges with constant program content. Boundaries are the sorted, deduplicated set `{0, Duration}` plus every clip's `Start` and `End`. A timeline with `Duration = 0` has zero stages.

| Offset | Width | Field | Meaning |
|---|---|---|---|
| 0 | u32 | Start | Stage start tick; first stage starts at 0 |
| 4 | u32 | End | Stage end tick; equals the next stage's `Start`; the last ends at `Duration` |
| 8 | u32 | ProgramOffset | Absolute offset of this stage's program steps; 8-byte aligned |
| 12 | u32 | ProgramCount | Number of steps in this stage |

## Program steps (8 bytes each)

Each stage's steps are contiguous at its `ProgramOffset`; stages are laid out in order immediately after the stage table. Steps within a stage are ordered by track ordinal, then by the earliest authored index of the clips covering that track in the stage.

| Offset | Width | Field | Meaning |
|---|---|---|---|
| 0 | u32 | Slot | Absolute offset of this step's slot row; 8-byte aligned, inside the hot section |
| 4 | u32 | Pair | Index into the pair table |

## Value pools

For each pair in pair order, first the track pool then the clip pool: `Count` consecutive values of `ValueBytes` each, with byte-identical values deduplicated to one slot within the pair. Two pools of different pairs never share bytes. Slot-row value indices (below) index into these pools.

Pool content is raw struct bytes — little-endian fields at their C# layout offsets, including any layout padding the struct type defines. A parser that does not know the struct type can carry the bytes opaquely; the metadata tail names the types.

## Slot rows (24 bytes each at `FrameOffset`, in program-step order)

One row per program step, at the `Slot` address the step names.

| Offset | Width | Field | Meaning |
|---|---|---|---|
| 0 | u16 | TrackValueIndex | Index into the pair's track pool |
| 2 | u16 | FirstValueIndex | Index into the pair's clip pool |
| 4 | u16 | SecondValueIndex | Second clip value for a blend, or `0xFFFF` (`NoClipIndex`) when the step has no second clip |
| 6 | u8 | TrackIndex | Authored track ordinal |
| 7 | 1 | — | Zero padding |
| 8 | u32 | WindowStart | First clip's `Start` |
| 12 | u32 | WindowEnd | `max(First.End, Second.End)`; the exclusive end of the step's active window |
| 16 | u32 | FactorStart | `max(First.Start, Second.Start)`; `0` without a second clip |
| 20 | u32 | FactorSpan | `min(First.End, Second.End) - FactorStart`; `0` without a second clip |

At playback tick `t` inside the window, the blend factor is `0.5` when `FactorSpan <= 1`, else `(t - FactorStart) / (FactorSpan - 1)`; a row without a second clip uses `FirstValueIndex` directly. A blend row (non-zero `FactorSpan`) always carries a `SecondValueIndex` different from `0xFFFF`.

## Cold metadata tail (optional, at `HotLength`)

Authoring metadata the runtime never reads: string pool, type identities, pair types, and user labels. Header (48 bytes):

| Offset | Width | Field | Meaning |
|---|---|---|---|
| 0 | u32 | Magic | `0x4D42544C`; bytes `4C 54 42 4D`, ASCII `LTBM` |
| 4 | u32 | Version | `1` |
| 8 | u32 | StringCount | Number of strings |
| 12 | u32 | StringIndexOffset | `48` |
| 16 | u32 | StringBlobOffset | `48 + 4*StringCount` |
| 20 | u32 | StringBlobLen | UTF-8 blob length in bytes |
| 24 | u32 | TypeCount | Number of type rows |
| 28 | u32 | TypeTableOffset | Absolute offset within the tail |
| 32 | u32 | PairTypeCount | Equals the container's `PairCount` |
| 36 | u32 | PairTypeTableOffset | Absolute offset within the tail |
| 40 | u32 | LabelCount | Number of label rows |
| 44 | u32 | LabelTableOffset | Absolute offset within the tail |

- String index: `StringCount` u32 offsets into the blob. Strings are UTF-8, deduplicated, ordinal-sorted; the last string runs to the end of the blob.
- Type table rows (12 bytes): u32 namespace, u32 name, u32 assembly — string indices. Rows are ordinal-sorted by (namespace, name, assembly).
- Pair-type table rows (8 bytes): u32 track-type index, u32 clip-type index — type-table indices, in pair-table order.
- Label table rows (12 bytes): u32 track entry, u32 clip index, u32 name string index — the authored names for tracks and clips.

## Rejection contract

The runtime's loader validates the structure and fails with a located reason; a conforming parser rejects at least the following, and rejects unknown magic or version rather than guessing:

- File shorter than 64 bytes — "TLB truncated."
- Magic or version unknown — "TLB magic or version invalid; rebake the asset with the current toolchain."
- `Duration` above 65,535; `PairCount` above 256; `Bytes` not the file size; `HotLength` outside `[1, Bytes]`.
- Section offsets not 8-byte aligned; pair table, stage table, pools, or slots out of bounds or out of order.
- Pair keys not strictly ascending; slot stride under 24 or not 8-byte aligned; pool counts above 65,535.
- Stages not monotonic and contiguous from 0 to `Duration`; program offsets out of bounds; a step naming a pair or slot out of bounds.
- Slot rows with value indices beyond the pool counts, a blend window without a second value index, or a second index set without one.

## Worked example: `minimal.tlb` decoded in full

The minimal fixture (327 bytes, sha256 `d223843e1a457b388f11c7965af8c05f6bedbd1de72219b202ef929fbc9a6e8f`) is one track, one clip, four ticks, no loop. Every byte:

```
0000  54 4c 42 31 04 00 00 00  TLB1, version 4
0008  00 00 00 00 04 00 00 00  Loops=0, Duration=4
0010  01 00 00 00 01 00 00 00  TrackCount=1, StageCount=1
0018  01 00 00 00 40 00 00 00  PairCount=1, PairOffset=64
0020  70 00 00 00 90 00 00 00  StageOffset=112, PoolOffset=144
0028  b0 00 00 00 c8 00 00 00  FrameOffset=176, HotLength=200
0030  47 01 00 00              Bytes=327
0034  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00   Reserved0..2 = 0
0040  c3 a8 4c c9 18 ea ca 83  pair 0 Key=0xC3A84CC918EACA83
0048  18 00 00 00              SlotStride=24
004c  50 00 00 00 01 00 00 00  TrackPoolOffset=80 (abs 64+80=144), TrackPoolCount=1
0054  04 00 00 00              TrackValueBytes=4
0058  60 00 00 00 01 00 00 00  ClipPoolOffset=96 (abs 160), ClipPoolCount=1
0060  08 00 00 00              ClipValueBytes=8
0064  00 00 00 00              padding
0068  00 00 00 00 00 00 00 00  Layout=0
0070  00 00 00 00 04 00 00 00  stage 0: Start=0, End=4
0078  80 00 00 00 01 00 00 00  ProgramOffset=128, ProgramCount=1
0080  b0 00 00 00 00 00 00 00  step: Slot=176, Pair=0
0090  00 00 80 3f              track pool: float 1.0 (Multiplier)
00a0  00 00 00 40 01 00 00 00  clip pool: float 2.0 (Amount), int 1 (Steps)
00b0  00 00 00 00 ff ff 00 00  slot row: TrackValueIndex=0, FirstValueIndex=0,
                                SecondValueIndex=0xFFFF, TrackIndex=0, padding
00b8  00 00 00 00 04 00 00 00  WindowStart=0, WindowEnd=4
00c0  00 00 00 00 00 00 00 00  FactorStart=0, FactorSpan=0  -> hot section ends at 200
00c8  4c 54 42 4d 01 00 00 00  LTBM, tail version 1
00d0  04 00 00 00 30 00 00 00  StringCount=4, StringIndexOffset=48
00d8  40 00 00 00 1f 00 00 00  StringBlobOffset=64, StringBlobLen=31
00e0  02 00 00 00 5f 00 00 00  TypeCount=2, TypeTableOffset=95
00e8  01 00 00 00 77 00 00 00  PairTypeCount=1, PairTypeTableOffset=119
00f0  00 00 00 00 7f 00 00 00  LabelCount=0, LabelTableOffset=127
00f8  00 00 00 00 07 00 00 00  string offsets: 0, 7, 15, 28
0100  0f 00 00 00 1c 00 00 00    -> "JobClip", "JobTrack", "Tl.Bake.Tests", "Tlb"
0110  4a 6f 62 43 6c 69 70 ...  blob: "JobClipJobTrackTl.Bake.TestsTlb"
0150  03 00 00 00 00 00 00 00  type 0: (3,0,2) = Tlb/JobClip/Tl.Bake.Tests
015c  02 00 00 00              (offsets shown per row; row 1 follows)
0160  03 00 00 00 01 00 00 00  type 1: (3,1,2) = Tlb/JobTrack/Tl.Bake.Tests
016c  02 00 00 00
0170  01 00 00 00 00 00 00 00  pair type 0: (track=1, clip=0)
0178  (end of file at 327)
```

## Fixtures

| File | Bytes | sha256 | Exercises |
|---|---|---|---|
| `minimal.tlb` | 327 | `d223843e1a457b388f11c7965af8c05f6bedbd1de72219b202ef929fbc9a6e8f` | Single pair, single stage, no blend, no labels, empty label table |
| `blended.tlb` | 1021 | `a3b735b1a682e0acb6780cf22f687abd5347bad16e1ad27c8d4f0bdbed038855` | Two pairs with ascending keys, six stages, a blend window (`SecondValueIndex != 0xFFFF`, `FactorSpan = 2`), loop flag, named tracks and clips, duplicate clip payloads deduplicated in the pool |

Each fixture's `.json` beside it is its authored source and its `.inspect.json` is the canonical rendering `tlb --inspect` produces (`schemaVersion: 1` structural JSON). Regenerate all three files per fixture with:

```sh
tlb minimal.json minimal.tlb --assembly <assembly-defining-the-types>
tlb --inspect minimal.tlb > minimal.inspect.json
```

`TlbLayoutFixtureTests` (in `tools/Tl.Bake.Tests`) loads each committed `.tlb` through `TimelineAsset.Load` and pins `TlbInspection.Inspect` to the committed rendering byte-for-byte, so any format change that does not regenerate the fixtures fails the build.
