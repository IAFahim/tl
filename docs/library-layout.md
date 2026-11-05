# game.tlib library layout (version 1)

`tlb library` compiles a library folder (one `library.json` + many timeline JSON
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

## library.json (authoring input, not shipped)

One `library.json` per library folder:

```json
{
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
| 24 | u32 | TickRate | From `library.json` |
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
tlb library <dir> [--assembly Game.Logic.dll]... [--out game.tlib] [--ids Timelines.g.cs] [--namespace Game] [--auto] [--diagnostics json]
```

`--out`/`--ids` default beside the library folder (`game.tlib`,
`Timelines.g.cs`). `--auto` infers missing `namespace` fields as in single
bakes. With `--diagnostics json`, success prints one JSON summary line and
failures print `{"errors":[{"message":...}]}` to stderr.

## Self-contained tlb artifacts

Any `dotnet publish` of `tools/Tl.Bake` can emit a self-contained single file;
CI publishes per OS from the same bytes this layout pins:

```sh
dotnet publish tools/Tl.Bake/Tl.Bake.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish tools/Tl.Bake/Tl.Bake.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
dotnet publish tools/Tl.Bake/Tl.Bake.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Fixtures

`tools/Tl.Bake.Tests/golden/library/` holds `library.json` plus `jump.json`
(loop, channel, markers) and `slide.json` (`next: "jump"`, enum + `TimelineId`
fields). The committed `game.tlib` + `Timelines.g.cs` beside them are the
golden pack; `TimelineWorld` loads them and the library tests pin manifest
order, ids, channels, digest, events, and frame-table equality.
