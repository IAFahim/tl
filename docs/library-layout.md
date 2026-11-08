# game.tlib library layout (versions 1 and 2)

`tlb library` compiles a library folder (one `tl.project.json` + many timeline JSON
files) into a deterministic `game.tlib` container plus a `Timelines.g.cs` id table.
This page specifies the byte layout so an out-of-process parser — a browser
viewer, C catalog tooling, Unity-side import — can read the structure without
running this repository's code. The committed fixtures under
`tools/Tl.Bake.Tests/golden/library/` are the test corpus for that parser.

The spec describes what the packer emits today (library version 1 over TLB v6
timelines). It promises no stability across future versions: the header version
field is the handshake, parsers reject what they do not know, and any format
change bumps the version and regenerates the fixtures in the same change.

## Conventions

- All integers are unsigned, little-endian. Widths are written `u8`/`u16`/`u32`.
- Offsets are absolute byte positions from the start of the file.
- Reserved fields and alignment padding are written zero and ignored on read.
- Section starts are 8-byte aligned. Timeline blobs are each 8-byte aligned.
- Packing is deterministic: the same library folder against the same assembly
  bytes produces the identical file on every machine and culture. No timestamps,
  no absolute paths, no hash-ordered containers appear anywhere.

## tl.project.json (authoring input, not shipped)

One `tl.project.json` per library folder:

```json
{
  "schema": 1,
  "tickRate": 60,
  "channels": ["locomotion", "combat"],
  "timelines": ["jump.json", "combat/*.json"]
}
```

- `tickRate` (u32, default 60): designer seconds-to-ticks scale. The Unity
  exporter uses it; the runtime counts ticks only. Stored in the header.
- `channels` (string[]): exclusivity lanes in order. Channel order sets channel
  numbers (1-up, 0 = none). Unknown channel in a timeline fails the bake.
- `timelines` (string[]): timeline JSON files or `*`/`?` globs relative to the
  library folder. Expansion is ordinal-sorted; every match must exist.

Each timeline JSON is JSON v2 (`name`/`duration`/`loop`/`next`/`hold`/`channel`/
`tracks`/`markers`; see `docs/tlb1-layout.md` v6 section). The timeline name is
the root `name` when present, else the file name without extension. Manifest
order is names sorted ordinally; the manifest id is index+1 (0 = none).
Duplicate names fail the bake.

## Sections in file order

```
offset 0          64-byte header
ManifestOffset    manifest: TimelineCount x 32 bytes
(end of entries)  timeline names (UTF-8, concatenated)
BlobOffset        timeline blobs (TLB v6 bytes, 8-aligned, deduped)
(total - 32)      32-byte SHA-256 digest
total             end of file
```

The writer places them with: `ManifestOffset = 64`,
`BlobOffset = align8(ManifestOffset + 32*TimelineCount + totalNameBytes)`,
each blob start 8-aligned, `total = align8-end + 32`.

## Header (64 bytes)

| Offset | Width | Field | Value |
|---|---|---|---|
| 0 | u32 | Magic | `0x42494C54`; bytes `54 4C 49 42`, ASCII `TLIB` |
| 4 | u32 | Version | `1` |
| 8 | u32 | TimelineCount | Number of manifest entries |
| 12 | u32 | ManifestOffset | `64`; 8-byte aligned |
| 16 | u32 | BlobOffset | 8-byte aligned |
| 20 | u32 | TotalBytes | Equals the file size |
| 24 | u32 | TickRate | From `tl.project.json` |
| 28 | u32 | Reserved0 | Zero |
| 32 | u32 | Reserved1 | Zero |
| 36 | u32 | Reserved2 | Zero |
| 40 | u32 | Reserved3 | Zero |
| 44 | u32 | Reserved4 | Zero |
| 48 | u32 | Reserved5 | Zero |
| 52 | u32 | Reserved6 | Zero |
| 56 | u32 | Reserved7 | Zero |
| 60 | u32 | Reserved8 | Zero |

## Manifest (TimelineCount x 32 bytes at ManifestOffset)

One entry per timeline in manifest (ordinal-name) order:

| Offset | Width | Field | Meaning |
|---|---|---|---|
| 0 | u32 | Id | `index + 1`; dense, 1-based |
| 4 | u32 | Channel | From the baked TLB header (0 = none, else 1-up) |
| 8 | u32 | NameOffset | Absolute offset of the UTF-8 name |
| 12 | u32 | NameLength | Name byte length (no terminator) |
| 16 | u32 | BlobOffset | Absolute offset of this timeline's TLB bytes |
| 20 | u32 | BlobLength | TLB byte length |
| 24 | u32 | Reserved0 | Zero |
| 28 | u32 | Reserved1 | Zero |

Names are not deduplicated (timeline names are unique by construction).
Content-digest dedup applies to blobs only: byte-identical TLB outputs share
one `(BlobOffset, BlobLength)` pair (first occurrence in manifest order wins).

## Timeline blobs

Each blob is a complete TLB v6 asset (`docs/tlb1-layout.md` v6 section) with
its `NextId`/`Channel`/`Hold`/marker section already resolved against the
manifest. `BlobLength` is the exact TLB file length; each blob start is
8-aligned with zero padding between blobs (padding is not part of any blob).

## Content identity digest

SHA-256 over `[0, TotalBytes - 32)`; the 32 digest bytes are the file tail.
`TimelineWorld` loads only when the stored digest equals the computed digest.

## Rejection contract

The loader validates the structure and fails with a located reason; a
conforming parser rejects at least the following, and rejects unknown magic
or version rather than guessing:

- File shorter than 96 bytes (`64` header + `32` digest) — "game.tlib truncated."
- Magic or version unknown — rebake with the current toolchain.
- `TotalBytes` not the file size; `TimelineCount` above 65,535.
- Offsets not 8-byte aligned; `ManifestOffset` not 64; manifest, names, or blobs
  out of bounds or out of order; blob starts unaligned.
- Manifest ids not dense (`entry id != index + 1`); names not valid UTF-8 or
  not strictly ordinal-sorted; any blob failing `TimelineRef.Validate`
  (reported as `game.tlib timeline '<name>': <cause>`).
- Digest mismatch — file is corrupt or truncated.

## Timelines.g.cs (companion output)

`--ids` writes a C# 9 file (namespace from `--namespace`, default `Game`):

```csharp
namespace Game
{
    public static class Timelines
    {
        public const ushort Jump = 1;
        public const ushort Slide = 2;
        public const string LibraryDigest = "<lowercase hex sha256>";
        public const uint TickRate = 60;
    }
}
```

Timeline member names are the PascalCase of manifest names (split on
`_`/`-`/space/`.`, capitalize each part, `_`-prefix a leading digit).
Collisions (two names mapping to one member, or mapping to the reserved
`LibraryDigest`) fail the bake. Values are `const` so Burst reads them.

## CLI

```sh
tlb library <dir> [--assembly Game.Logic.dll]... [--out game.tlib] [--ids Timelines.g.cs] [--kinds Kinds.g.cs] [--namespace Game] [--auto] [--diagnostics json]
```

`--out`/`--ids` default beside the library folder (`game.tlib`,
`Timelines.g.cs`). `--auto` infers missing `namespace` fields as in single
bakes. With `--diagnostics json`, success prints one JSON summary line and
failures print `{"errors":[{"message":...}]}` to stderr.

## Kinds (library version 2)

When any timeline reaches a kind (issue #554, plan `Roadmap.Kinds.impl.plan.md`
section 4), `tlb library` appends a kind section, sets the header `Version` to
`2`, and writes `Kinds.g.cs`. A library whose timelines reach no kind is
byte-identical to version 1. A version 2 library needs a runtime that reads
kinds (#555); version 1 runtimes reject it as an unknown version.

### Discovery

`tlb` scans the `--assembly` types for these shapes, matched by full attribute
name, so the attribute types may live in any assembly:

- `[Tl.TimelineKind]` on a struct. Named arguments: `Accepts` (`0` Spans,
  `1` Pulses, `2` Both), `Mode` (`0` Combine, `1` Each), `Order` (int),
  `Group` (string), `FoldsPulses` (bool; Combine kinds that accept pulses only,
  lets game code fold consecutive pulses to one target, see #562). The kind needs `public static Apply`; its first parameter
  (by `in`/`ref` or value) is the kind's unmanaged value type. Combine mode also
  needs `public static Combine(ref T total, in T next)`.
- `[Tl.Feeds(typeof(K))]` on a public static method of an `ITrack<TTrack, TClip>`
  consumer: a span feeder. `K` must accept spans.
- `[Tl.Pulse(typeof(K), On = …)]` with `On` `0` Start, `1` End (on an `ITrack`
  consumer) or `2` Marker (on an `IMarker<T>` consumer): a pulse feeder. `K`
  must accept pulses.
- A feeder returns exactly the kind's value type.

Kinds that share a `Group` share one table per entity in the .NET runtime. A group entry is `12 + align4(largest pulse value)` bytes: the 8-byte entry header, a `u16` kind index and 2 bytes of padding, then the value. Members apply only their own entries.

Kinds are ordered by full type name and named by their simple name, which must
be unique; at most 64 kinds. Feeders are ordered by kind, then consumer full
name, then method name; `Ordinal` is the feeder's index within its kind.
Violations fail the bake with the type and method named and the repair stated.

### tl.project.json `kinds`

```json
"kinds": {
  "default": { "maxInlineBytes": 64, "stackDepth": 1 },
  "Damage": { "inline": 4, "gamePulses": 4, "maxInlineBytes": 128 },
  "Poison": { "inline": 0 }
}
```

`default` takes `maxInlineBytes` and `stackDepth`; a kind or group name takes
`inline` (fixed capacity), `gamePulses` and `maxInlineBytes`. Unknown names
and keys fail the bake. A kind's `[TlKind(Inline = n, GamePulses = n)]` values
apply when `tl.project.json` does not set them.

### Capacity

Per kind `K`, over the library's timelines:

- `m(T, K)`: most span entries of `K` active on one tick of one playback of `T`.
- `p(T, K)`: most pulses of `K` raised on one tick of `T`.
- `C(K)`: sum over the library's channels of the largest `m(T, K)` on that channel.
- `S(K)`: largest `m(T, K)` among timelines without a channel, times `stackDepth`.
- Worst case `C(K) + S(K)`; kinds that accept pulses add
  `max p(T, K) x (channelCount + stackDepth) + gamePulses`.
- Capacity: `inline` when set, else `min(worst, floor(maxInlineBytes / entryBytes))`.
- Entry bytes: `8` for span-only kinds, else `8 + align4(sizeof(value))`. A
  kind group's entry is `12 + align4(largest pulse value)`, and its worst case
  is the sum of its members'.

`tlb library` prints one line per kind (worst case, capacity, entry bytes,
inline bytes, and the timeline that set it); with `--diagnostics json` the
summary carries a `kinds` array with the same fields.

### Kinds.g.cs

Written beside `--ids` (or at `--kinds <path>`) when any timeline reaches a
kind or `--kinds` is given, and **only when its bytes change**, so a bake that
moves no capacity triggers no recompile:

```csharp
namespace Game
{
    public static class KindCapacity
    {
        public const int Damage = 5;
        public const int Lift = 2;
    }
}
```

One `const int` per kind and per kind group, ordinal by name.

### Header fields (version 2)

| Offset | Width | Field | Value |
|---|---|---|---|
| 4 | u32 | Version | `2` |
| 28 | u32 | KindOffset | Absolute, 8-aligned, after the last blob |
| 32 | u32 | KindLength | Section bytes (8-aligned); the digest follows |

### Kind section

All offsets inside the section are relative to `KindOffset`.

| Offset | Width | Field |
|---|---|---|
| 0 | u32 | Magic `0x444E494B` (ASCII `KIND`) |
| 4 | u32 | Version `1` |
| 8 | u32 | KindCount |
| 12 | u32 | FeederCount |
| 16 | u32 | TimelineCount (equals the manifest count, same order) |
| 20 | u32 | KindTable (`KindCount x 40`) |
| 24 | u32 | FeederTable (`FeederCount x 32`) |
| 28 | u32 | TimelineTable (`TimelineCount x 48`) |
| 32 | u32 | StringsOffset |
| 36 | u32 | StringsLength |
| 40 | 24 | Zero |

Kind entry (40 bytes): `NameOffset u32`, `NameLength u32` (into strings),
`ValueBytes u32`, `EntryBytes u32`, `Capacity u32`, `Accepts u8`, `Mode u8`,
`Flags u16` (bit 0 `FoldsPulses`, else zero), `Order i32`, `GroupNameOffset u32`, `GroupNameLength u32` (0 when
none), `GroupCapacity u32`.

Feeder entry (32 bytes): `Kind u16`, `Trigger u8` (`0` Span, `1` Start, `2` End,
`3` Marker), `Ordinal u8`, `u32` zero, `SourceKey u64` (the pair key of the
consumer's track and clip types, or the marker type key), `ConsumerOffset u32`,
`ConsumerLength u32`, `MethodOffset u32`, `MethodLength u32`.

Timeline entry (48 bytes; all zero when the timeline reaches no kind):

| Offset | Width | Field |
|---|---|---|
| 0 | u64 | KindMask (bit = kind index) |
| 8 | u32 | Windows (offset) |
| 12 | u32 | WindowCount |
| 16 | u32 | EventBits (offset; `Duration` bytes, bit 0 forward, bit 1 backward) |
| 20 | u32 | EventIndex (offset; `2 x Duration + 1` u32, `[2t + dir]` to `[2t + dir + 1]`) |
| 24 | u32 | Events (offset; 8 bytes each) |
| 28 | u32 | EventCount (tick events) |
| 32 | u32 | WrapForward (event index) |
| 36 | u32 | WrapForwardCount |
| 40 | u32 | WrapBackward (event index) |
| 44 | u32 | WrapBackwardCount |

Window (16 bytes): `Track u16`, `Pair u16` (pair index; marker index for a
marker window), `Start u16`, `End u16` (exclusive), `Slot u32` (offset of the
first TLB slot row of the window inside the timeline blob; 0 for markers),
`Flags u32` (bit 0 marker). Slot windows are the distinct
`(TrackIndex, Pair, WindowStart, WindowEnd)` of the slot rows in program-step
order; marker windows follow, one per marker that some feeder handles, with
`End = Start + 1`.

Event (8 bytes): `Window u16`, `Feeder u16` (feeder index), `Op u8` (`0` Remove,
`1` Insert, `2` PulseStart, `3` PulseEnd, `4` PulseMarker), 3 zero bytes. An
entry is the pair `(Window, Feeder)`.

Events at tick `t` in direction `dir` (0 forward, arriving from `t - 1`; 1
backward, arriving from `t + 1`), in this order, each group by entry order:

1. Remove span entries: forward `End == t`; backward `Start == t + 1`.
2. Insert span entries: forward `Start == t`; backward `End - 1 == t`.
3. Pulses: Start feeders at `Start == t`, End feeders at `End - 1 == t`,
   Marker feeders at the marker tick, the same in both directions.

`EventBits[t]` has bit `dir` set exactly when that list is non-empty. Looping
timelines also carry wrap lists: forward removes span entries with
`End == Duration`, backward removes those with `Start == 0`; the runtime then
processes tick `0` (forward) or `Duration - 1` (backward).

### One assembler, per-timeline kind payloads

`Tl.LibraryAssembler` (Tl.Core) writes every `game.tlib`: `tlb library` and the
Unity package both call it. Each timeline arrives as a `LibraryPart` (name, TLB
bytes, optional kind payload, library id of its `next`, timeline-reference
sites to patch); the assembler checks that every payload was baked against the
same kind catalog, computes the capacities above (`LibraryAssembler.Capacities`,
overrides in `LibrarySettings.Kinds` by kind or group name), merges the payloads
into the kind section and emits the file. `Tl.Gen.Tlb.LibraryBake.KindPayload`
writes a payload; `Tl.KindPayload` reads it.

Kind payload (`KindPayload`, one per timeline, little-endian):

| Offset | Width | Field |
|---|---|---|
| 0 | u32 | Magic `0x444B4C54` (ASCII `TLKD`) |
| 4 | u32 | Version `1` |
| 8 | u32 | KindCount |
| 12 | u32 | SectionLength |
| 16 | 16 x KindCount | per kind: `MaxSpans i32`, `MaxPulses i32`, `Inline i32` (-1 none), `GamePulses i32` |
| 16 + 16 x KindCount | SectionLength | a kind section of this one timeline, capacities zero |

The merged section copies the catalog (kind and feeder tables, strings) from the
first payload, writes the capacities, and moves each timeline row and its data,
shifting the row's data offsets. The result equals the section computed over
all timelines at once.

Fixtures: `tools/Tl.Bake.Tests/golden/kinds/` (`tl.project.json`, four timelines,
the committed `game.tlib` and `Kinds.g.cs.txt`). `KindLibraryTests` checks the
event lists against the clip windows in both directions, hand-computed
capacities, determinism and the no-rewrite rule. Regenerate the golden pack
after an intended format change with:

```sh
cd tools/Tl.Bake.Tests/golden/kinds && dotnet ../../../Tl.Bake/bin/Release/net10.0/Tl.Bake.dll library . --assembly ../../bin/Release/net10.0/Tl.Bake.Tests.dll --namespace KindFixture --ids /tmp/Timelines.g.cs --kinds Kinds.g.cs.txt
```

## Self-contained tlb artifacts

Any `dotnet publish` of `tools/Tl.Bake` can emit a self-contained single file;
CI publishes per OS from the same bytes this layout pins:

```sh
dotnet publish tools/Tl.Bake/Tl.Bake.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish tools/Tl.Bake/Tl.Bake.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
dotnet publish tools/Tl.Bake/Tl.Bake.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Fixtures

`tools/Tl.Bake.Tests/golden/library/` holds `tl.project.json` plus `jump.json`
(loop, channel, markers) and `slide.json` (`next: "jump"`, enum + `TimelineId`
fields). The committed `game.tlib` + `Timelines.g.cs` beside them are the
golden pack; `TimelineWorld` loads them and the library tests pin manifest
order, ids, channels, digest, events, and frame-table equality.
