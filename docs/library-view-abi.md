# LibraryView ABI (v2)

`LibraryView` is the unmanaged, versioned pointer struct a loaded `game.tlib`
publishes to every system kernel. It is the only boundary P2 hands to P3: P3
kernels switch on `PairKey`, iterate `Steps` in authored order (reversed when
running backward), filter per system via `PairKey`, and tolerate
`FoldLane == null` (frame-consumer path). This page pins the C# 9 shape, the
widths, the ownership, and the reader contract so .NET and Burst hosts (and
the Unity `tl.unity` port) read identical bytes.

## Versioning

`LibraryView.AbiVersion == 2` (v2 appends `Kinds`; v1 readers stop before it). Additive fields arrive only after the documented
fields with a version bump; readers reject unknown `AbiVersion`. Field order,
widths, and alignment below are the ABI. The structs use sequential layout
with the natural C# pointer width (64-bit hosts); the file format
(`docs/library-layout.md`, `docs/tlb1-layout.md`) stays 32-bit explicit-width
and the loader widens it into these pointers.

## Structs (C# 9, unmanaged, Burst-readable)

```csharp
public readonly struct TimelineId { public readonly ushort Value; } // 0 = none
public unsafe struct LibraryView   // AbiVersion == 2
{
    public uint AbiVersion;        // == 2
    public int TimelineCount;
    public LibraryTimeline* Timelines; // manifest order, id = index + 1
    public uint ByteCount;         // game.tlib total bytes
}
public unsafe struct LibraryTimeline
{
    public ushort Id;              // 1-based, manifest order
    public ushort Channel;         // 0 = none, else 1-up per library.json order
    public ushort Next;            // TimelineId value, 0 = none
    public byte Loop;              // 0/1
    public byte Hold;              // 0/1
    public uint Duration;
    public int StageCount;
    public LibraryStage* Stages;
    public int EventCount;
    public LibraryEvent* Events;   // sorted (tick, kind, step, marker)
    public ulong* EventTickBits;   // 1 bit per tick, (Duration+63)/64 words
}
public unsafe struct LibraryStage
{
    public uint Start; public uint End;
    public int StepCount;
    public LibraryStep* Steps;     // authored track order
}
public unsafe struct LibraryStep
{
    public uint Pair;              // pair index within this timeline TLB
    public ulong PairKey;          // runtime pair identity
    public int TrackIndex;
    public uint WindowStart; public uint WindowEnd;
    public uint FactorStart; public uint FactorSpan;
    public ushort TrackValueIndex; public ushort FirstValueIndex; public ushort SecondValueIndex;
    public void* FoldLane;         // per-step fold lane (D10); lane blocks per (tick, direction) filled at load via ITlModule; null when no Fold is registered for the pair (frame-consumer path)
    public void* FrameRow;         // materialized [track][first][second] payload base for this step, copied from the pool values at load (never the baked index row)
}
public struct LibraryEvent
{
    public uint Tick;
    public byte Kind;              // 0 = ClipStart, 1 = ClipEnd, 2 = Marker
    public byte _pad0; public ushort _pad1;
    public int StepIndex;          // flat step ordinal, -1 for marker-only
    public uint Marker;            // marker section index, uint.MaxValue when not a marker
}
```

`Kind` values are stable: clip-start (window open), clip-end (last active tick
`WindowEnd - 1`), marker (marker tick). `EventTickBits[tick >> 6] & (1UL <<
(tick & 63))` is set for every tick carrying at least one event.

## Ownership and lifetime

- `new TimelineWorld(bytes)` copies the library into native memory, validates
  (magic, version, offsets, size, manifest order, every TLB, digest), and builds
  one `LibraryTimeline` per manifest entry with its own `Stages` (one flat
  `Steps` block per timeline), `Events`, and `EventTickBits`. It then
  materializes derived tables in one arena per timeline: per-step `FrameRow`
  payload rows resolved from the pool indices, per-step `FoldLane` blocks
  evaluated per (tick, direction) through the registered `ITlModule`
  (`FoldLane` stays null where no Fold is registered), and one marker table
  per timeline published to `MarkerTableRegistry`.
- `View` returns the struct by value; the pointers stay valid until `Dispose`
  or `Reload`. `Reload` frees the old tables before loading the new bytes.
- Content dedup stays inside a world: identical TLB blobs may share file bytes,
  but each timeline owns its derived tables.

## Reader surface

```csharp
LibraryViewReader.GetTimeline(in view, id) -> LibraryTimeline*
LibraryViewReader.StageAt(timeline, tick)  -> LibraryStage*   // binary search, >4 stages
LibraryViewReader.StepsFor(timeline, stage) -> Span<LibraryStep> // authored order
LibraryViewReader.FoldLane(step, lane)      -> void*          // lane 0 today, null-safe
LibraryViewReader.Events(timeline)          -> Span<LibraryEvent>
LibraryViewReader.HasEvent(timeline, tick)  -> bool            // tick-bit test
LibraryViewReader.FrameAt(timeline, tick, stepIndex) -> LibraryStep* // window check
```

Forward iteration walks `Steps` in order; backward iteration walks them in
reverse. Per-(system, timeline, stage) dispatch is the caller's filter over
`PairKey`: the view exposes all steps, systems select their pairs. Event
methods run after `ExecuteActive` of the same playback and tick (P3).

## Proofs

- Frame tables equal `TrySample` per tick and step in every fixture, both
  directions (library tests).
- The event index equals clip boundaries (`WindowStart`, `WindowEnd - 1`) plus
  marker ticks, sorted with the tick-bit set.
- Fold lanes are per step (D10, track): two tracks of one pair keep one lane
  each, where the legacy single-pair view kept one.
- Corrupt libraries (bad magic/version/offsets/size/order/digest, bad TLB,
  mis-sorted manifest) are rejected with located reasons.

## Kind section

A version 2 `game.tlib` carries a kind section (`docs/library-layout.md`,
"Kinds"). The loader validates it and publishes `LibraryView.Kinds`, a
`KindSectionView` of pointers into the loaded library bytes (no copy):

```csharp
public unsafe struct KindSectionView
{
    public byte* Base; public uint Bytes;
    public int KindCount, FeederCount, TimelineCount;
    public LibraryKind* Kinds;                 // 40-byte rows
    public LibraryFeeder* Feeders;             // 32-byte rows
    public LibraryKindTimeline* Timelines;     // 48-byte rows, manifest order
    public byte* Strings; public uint StringsLength;
    // NameOf, GroupOf, IndexOf, TimelineOf(id), Windows(t), Events(t),
    // EventBitsAt(t, tick), EventStart/EventEnd(t, tick, direction)
}
```

`Kinds.IsEmpty` is true for a version 1 library. The row structs mirror the
file layout byte for byte; `LibraryWindow` and `LibraryKindEvent` are the
16- and 8-byte window and event rows. The loader rejects, with a located
message, a section whose magic, version, counts, table offsets, strings,
entry sizes, per-timeline offsets (8-aligned, in bounds), event index
(ascending, covering the events), wrap lists, event fields or kind masks are
invalid. Kind groups load but the runtime store rejects them until K10 lands.
