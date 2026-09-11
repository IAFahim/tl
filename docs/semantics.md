# Timeline execution semantics

This contract applies to generated C# v1.0.0-alpha.3 catalog queries.

## Definitions

A timeline is an immutable ordered collection of tracks, clips, and hooks. Each track binds immutable settings to one typed job. Each clip binds an immutable payload to a half-open `[start, end)` window. A track has at most two active clips at any tick.

A catalog contains closed schemas. A schema names the timeline assets permitted in one component-column layout. A row selects an asset through generated catalog-local state. Route zero is empty.

## State and clock

Public timeline state is the triple `(asset, position, cycle)`:

- `asset` selects one generated catalog asset or empty.
- `position` is the next forward boundary.
- `cycle` is the signed loop cycle.

The generated query also carries pending selection inside each state value between selection and commit. The game tick remains caller-owned and is never persisted by the timeline.

For `Tick(G, d)`:

| Movement | Emitted game ticks |
| --- | --- |
| `d > 0` | `G, G + 1, ..., G + d - 1` |
| `d < 0` | `G - 1, G - 2, ..., G + d` |
| `d = 0` | none |

Game-tick arithmetic is unchecked `uint` arithmetic. A delta represents work to replay, not a destination sample. `Tick(G, 5)` is observationally equal to five forward unit calls with consecutive game ticks when no operation throws. The reverse law is analogous.

## Finite movement

For duration `D`, valid positions are `0..D`. Forward selection at `p < D` emits local tick `p` and commits `p + 1`. Reverse selection at `p > 0` emits `p - 1` and commits `p - 1`.

Forward selection at `D` and reverse selection at zero emit nothing. Empty timelines emit nothing. Finite rows clamp independently, so completion of one row does not stop another row that can still move. A query stops a multi-step call once no row can move further in that direction.

The last forward frame carries `TimelineEnd | CompletedAfter`. Reversing from completed state emits that same last local frame with `TimelineEnd | CompletedBefore | Reverse`. The first local frame carries `TimelineStart`; a reverse frame arriving at zero also carries `Reverse`.

## Loop movement

For nonzero duration `D`, looping positions stay in `0..D-1`.

| Movement | Emitted `(tick, cycle)` | Committed `(position, cycle)` |
| --- | --- | --- |
| forward, `p + 1 < D` | `(p, c)` | `(p + 1, c)` |
| forward, `p + 1 = D` | `(p, c)` | `(0, unchecked(c + 1))` |
| reverse, `p > 0` | `(p - 1, c)` | `(p - 1, c)` |
| reverse, `p = 0` | `(D - 1, unchecked(c - 1))` | `(D - 1, unchecked(c - 1))` |

Every looping frame carries `Looping`; reverse frames also carry `Reverse`. `TimelineStart` and `TimelineEnd` identify normalized local boundaries. Cycle arithmetic wraps explicitly in two's complement. Duration one follows the same law. A zero-duration looping timeline emits nothing.

An arbitrary effectful looping delta performs every requested occurrence. Its time is proportional to observable work. The implementation does not collapse effects algebraically.

## Clip resolution

At one local tick:

- No active clip on a track produces no occurrence.
- One active clip is borrowed directly.
- Two active clips resolve once through the track's `Blend` method and produce one occurrence.

The blend factor covers the clips' intersection. For intersection start `S`, length `L > 1`, factor is `(tick - S) / (L - 1)`. A one-frame intersection uses `0.5f`. Equal-start clips retain authored order.

`ClipStart` and `ClipEnd` describe the outer resolved window. A one-frame window carries both. These flags are independent of timeline boundary and completion flags.

## Ordered stages

Each region owns one immutable occurrence slice. Array position is stage identity. Forward order is before hooks, authored tracks, then after hooks. Reverse order is the exact reverse slice: after hooks, tracks, then before hooks, each reversed internally.

For every requested simulation step, a query performs:

1. Validate every mutable route against the schema.
2. Select at most one frame for each row without invoking user code.
3. For each stage in order, execute every compatible row through its typed operation kind.
4. Commit every selected row once after all stages finish.

Rows can carry different assets and reach different completion points while sharing the same call. An A→B→A asset and a B→A asset both retain their own order. Grouping the entire call by operation type is invalid because a type set cannot encode repeated or opposing occurrence order.

Delayed commit keeps every operation in one selected frame on the same pre-commit timeline state. A user exception propagates immediately. Effects already executed remain visible, no selected state is committed for that step, and a later call replaces pending selection before executing.

## Borrowed component columns

An operation's required unmanaged parameters define logical component slots. Name and type together identify a slot. `in` is a read-only borrowed alias; `ref` is a writable borrowed alias. When any operation writes a shared slot, the generated schema exposes one writable column.

All schema columns have equal row count. Query construction rejects prohibited overlap whenever either column is writable, including overlap with state storage. Read-only columns may alias each other. An `in` alias does not freeze underlying storage against a write through another independently obtained alias.

The query is a ref struct and cannot escape to the heap, cross asynchronous suspension, or survive storage relocation. Frames are ref structs scoped to operation execution. The timeline retains no component, span, track, clip, or frame reference.

The parallel model permits row-local effects and immutable shared data. A method that mutates global state, follows a shared mutable pointer, or writes another row introduces a dependency outside this contract and requires an explicitly ordered phase or proven reduction.

## Errors and total behavior

Ordinary movement has no failure result. Default query, default state, empty asset, gap, empty timeline, zero delta, and finite completion are no-ops.

Schema construction rejects unequal lengths, invalid current routes, and prohibited aliases before execution. Every nonzero tick rechecks mutable routes before any effect. The generator rejects malformed definitions, incompatible slots, invalid windows, excess tracks, excess overlap, unsupported operation signatures, and invalid include graphs during compilation.

An invalid manually constructed position outside the asset's domain emits nothing. Consumers should create state with generated asset identity and a valid position. User exceptions are never converted into timeline status.

## Determinism and inversion

Given equal immutable definitions, equal initial component bytes, equal state, equal game ticks, and deterministic operations, execution order and outputs are deterministic.

Structural reverse guarantees reversed occurrence order and mirrored movement coordinates. It does not prove that arbitrary user operations are mathematical inverses. Exact restoration additionally requires reversible operations, matching external inputs, suitable event history, and arithmetic whose reverse is exact. Floating-point subtraction is not a universal inverse of addition.

## Allocation and ownership

Generated query execution allocates zero managed bytes after warmup. Timeline and catalog definitions are static immutable data. Callers own state and component columns. There is no runtime registry, binding cache, playback object, destruction protocol, or hidden frame queue.

Generated source bytes, neutral payload and schedule bytes, static data, per-row state, managed assembly size, NativeAOT image size, native text, scratch, and allocations are reported separately.
