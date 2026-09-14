# Per-asset playback tables for the data-authored coordinator

Design and prototype record for [issue #88](https://github.com/IAFahim/tl/issues/88). It extends the approved data-authored contract in [data-authored-api.md](data-authored-api.md) with a coordinator shape for its dominant real workload: many entities, each carrying exactly one timeline, most entities sharing a small set of assets, clocks staggered per entity. It proposes no public API change, ships no production source change, and does not alter any #56 gate. `Timeline.Rows` remains the general heterogeneous lane; nothing here removes it.

Status: prototype + measurement. The API below is a sketch the prototype approximates; implementation issues would be filed as children of #56 only after the owner approves this document.

## The workload

Issue #85's cost model shows the interpreter paying ~9-13 ns/row when many rows sit at distinct timeline points, because every row pays stage search, occurrence dispatch, chain walking, and the two-pass commit. When rows share one asset, all of that is derivable from the asset once: the per-row work collapses to movement plus the job. The gap (order 20x on the measured machine) is the design space for playback tables.

The coordinator must also carry the owner's gameplay patterns:

1. **Pulse loops** — duration-1 looping timelines (event/listener ticks), potentially millions of rows.
2. **Cross-entity watches** — one entity's timeline reads another entity's state (a car reads its player's input; a gun waits on a trigger flag). Reads must observe the tick's input snapshot and must be expressible per row ("which entity do I watch").
3. **Ephemeral one-question timelines** — spawn a short non-looping window ("did the player parry within 20 ticks?"), auto-retire at completion, zero per-row allocation, dense arrays preserved.
4. **One clock per frame** — the coordinator owns movement; jobs never advance clocks.

The reference discipline is Frent's archetype model: one compiled loop per table, SoA columns, zero per-entity dispatch, structural changes buffered. The tl equivalent is a per-asset instance table.

## What belongs where (unchanged from #56)

| Owner | Responsibility |
| --- | --- |
| Asset (TLB1) | Type identity, track data, clip windows, track order, looping — no name, no job binding |
| `TimelinePlayback` (proposed) | Row membership, movement, per-asset tables, commit |
| Jobs | Reusable behavior per type pair, discovered or attached, never stored in the asset |
| Application | Columns, entity storage, game clock, spawn/retire decisions |

`Attach` binds a job pair to an asset at the application level. The asset carries no binding: any job whose pair the asset uses may attach to it, and the same job may attach to many assets. This is not per-asset job binding in the authoring sense rejected by #56 — nothing about the asset changes.

## API sketch

```cs
var playback = new TimelinePlayback();

var moveTable = playback.Attach<MoveJob>(moveAsset);
moveTable.Columns(read: inputs, write: outputs);

int handle = moveTable.Spawn(position: 0, watched: playerRow);
moveTable.Retire(handle);

playback.Tick(clock: gameTick, dt: delta);
```

- `Attach<TJob>(asset)` — once per (job pair, asset). `TJob : ITimelineJob<TTrack, TClip>` names the pair; the table is keyed by (pair key, asset address). Attaching the same pair and asset twice returns the same table.
- `Columns(read: ..., write: ...)` — bound once, validated at attach: lengths, complete required component sets for the job, no writable aliasing. Binding failures prevent attach; they are never discovered per row.
- `Spawn(position, watched...)` — appends a row at local `position` with per-row watch indices. New rows begin at local zero semantics identical to `TimelineComponent`: forward movement from the given position, cycle zero.
- `Retire(handle)` — removes the row; storage stays dense through swap-remove; handles are stable slots with generation words in the production design (the prototype uses plain slots).
- `Tick(clock, dt)` — the only per-frame surface, non-generic. The coordinator selects every live row once per crossed frame, executes the job, commits once. `dt` is the signed delta of the #56 semantics, including multi-frame catch-up and reverse.

The typed frame query stays a read-only stage view: a table job never mutates timeline state, never advances a clock, and sees exactly the frame (track data, clip data, ticks, cycle, flags) the facade would produce for the same position. Frame values come from per-position tables precomputed at bind, never from a second interpreter walk.

## Table mechanics

- **SoA row storage.** One native block set per table: `positions[]` (u32), `cycles[]` (i64, looping assets only), per-row `watched[]`/`owners[]` (i32, watch tables only), handle slots for `Retire`, and the job's output columns. Non-looping tables carry no `cycles[]`: position == duration encodes completion, and cycle is identically zero by the movement law.
- **Frame precomputation at bind.** The asset is immutable, so each position's resolved frame (blended clip value, track data, presence) is computed once per attached pair through the public `Timeline.Query<TTrack, TClip>` stage view and stored as `duration` entries. The tick sweep indexes by the position the movement selected; there is no per-row stage search and no per-row slot decode.
- **Duration-1 loop classification.** A looping asset with duration 1 has a constant frame; the sweep degenerates to per-row cycle increment plus the job. The classification is exact (the movement law yields tick 0, position 0, cycle+1, `TimelineStart|TimelineEnd|Looping` every frame) and is checksum-verified against the facade in the prototype.
- **Swap-remove retirement fused into the advance sweep.** A non-looping row whose next position would be `duration` retires during the sweep: the last live row's columns move into the vacated slot and are processed in the same pass. No per-retire allocation, no second compaction pass, membership stays dense.
- **Command-buffered structural changes.** `Spawn`/`Retire` requested during a tick are recorded and applied after the sweep, so the sweep iterates one immutable membership per frame. The prototype applies spawn batches between passes and completion-driven retirement inside the sweep, which is the same discipline.
- **Consumer order.** One job per table. A pair consumed by several jobs gets one table per job, executed in attach order; reverse frames execute tables in reverse attach order, which is the same mirrored-chain law the facade enforces inside one query. Multi-consumer ordering across tables is an open question (below).

## Cross-entity reads

A watch job declares a read-only span plus a per-row target column; the coordinator gathers `inputs[watched[row]]` inside the sweep and scatters the result through `owners[row]`:

- Reads observe the tick's input snapshot: snapshot columns are updated between ticks, never during a sweep.
- Iteration order cannot change results: writes land in coordinator-mapped output cells indexed by `owners[row]`, and jobs that can write the same cell must be proven reductions (associative and commutative accumulation), the same law #56 applies to cross-row mutation.
- The facade cannot express per-row indirection today; the prototype's watch lane compares the table against the honest facade emulation (gather pass + `Timeline.Rows` tick + scatter pass) with identical checksums.

## Compatibility with Timeline.Rows

`Timeline.Rows` remains the general lane and the oracle: heterogeneous assets across rows, multiple pairs per row, managed-consumer exception propagation, one-off queries, and any shape tables do not cover. Rows driven by a table and rows driven by a facade query are disjoint memberships by construction in the prototype; making that exclusivity a validated rule is an open question (below). The two paths share movement math (`TimelineMovement`), the shared asset format, and the job contracts, so results agree by construction rather than by test alone; the prototype nevertheless verifies every lane against the facade by checksum.

## Memory statement

Table storage is runtime data path state and follows the repository memory rules: native blocks with explicit widths, a single managed owner handle per table, caller-owned columns borrowed for the call, zero managed allocation on the warm path, no registries reachable from playback. Baking and attach may use managed types freely; their output is the native table.

Per-row, per-table widths in the production design:

| Column | Width | Present |
| --- | ---: | --- |
| `positions` | 4 B | always |
| `cycles` | 8 B | looping assets only |
| handle slots (`row->handle`, `handle->row`) | 8 B | retire-capable tables |
| `watched` | 4 B | watch tables |
| `owners` | 4 B | watch tables |
| job output words | job-defined | per job declaration |

Per-asset, shared by all rows of the table: precomputed frame/value tables, `duration` x (resolved job inputs + presence; 12 B for the prototype's float amount + int code + byte) per attached pair, plus O(duration) classification bytes.

Honest worst case: one million entities each holding ten live looping tables with handles and watch columns carry `1M x 10 x 24 B` = 240 MB of coordinator row state plus precompute on the order of a megabyte. The facade keeps 24 B/row (`TimelineComponent`) no matter how many pairs play, so tables trade memory for the ~6x per-row tick cost measured below. Tables are opt-in per workload; a game that does not need the table shape keeps the facade. The prototype allocates the full column set for every lane regardless of use, so its resident numbers slightly overstate the production widths above.

## Unity host mapping

- A per-asset table is the Unity analog of a per-asset query: rows sharing one asset reference form one contiguous run, so the coordinator's chunk iteration materializes table sweeps directly.
- Read columns map to `[ReadOnly]` native arrays of previous-frame state; the input-snapshot law is Unity's existing dependency discipline (read-only containers scheduled against the writer's dependency).
- `Spawn`/`Retire` map to `EntityCommandBuffer`: structural changes are recorded during the frame and played back before the next coordinator update, which is the same command-buffering the table applies to its membership.
- The sweep is a flat loop over native arrays with unmanaged-convention jobs, so it is Burst-eligible where the interpreter's managed consumer seam is not. Duration-1 pulse classification needs no special Burst support: it is already a straight per-row loop.

## Prototype

`benchmarks/PlaybackPrototype/` (not part of `tl.slnx`; built and run directly) demonstrates the sweep pattern against the shipped facade. It registers real consumers through `PairRuntime<TTrack, TClip>.Consume`, bakes its assets through the public `TimelineBaker.BakeJson` conversion API (the `tlbake` CLI is a thin front-end over the same library), and deliberately does not depend on kernel binding — the table sweep is hand-written harness code by design, since tlbake-emitted kernels do not bind at runtime until issue #87's atom lands.

Reproduce from a clean checkout of the PR branch:

```sh
dotnet build benchmarks/PlaybackPrototype/PlaybackPrototype.csproj -c Release
dotnet run --project benchmarks/PlaybackPrototype -c Release
dotnet run --project benchmarks/PlaybackPrototype -c Release -- sweep movement floor pulse watch churn
```

Asset bytes are reproducible with the CLI; the hashes printed by the harness equal the CLI bake of the same committed JSON against the harness assembly:

```sh
dotnet run --project tools/Tl.Bake -c Release -- \
  benchmarks/PlaybackPrototype/Assets/move-loop.tlb.json /tmp/move-loop.tlb \
  --assembly benchmarks/PlaybackPrototype/bin/Release/net10.0/PlaybackPrototype.dll
```

Lanes:

- `sweep` — 1M rows, one shared looping asset (duration 64), staggered clocks (`position = row % 64`), one read column plus one write column: table SoA sweep vs `Timeline.Rows` facade.
- `pulse` — 1M rows on a duration-1 looping asset: constant-frame classification vs facade.
- `watch` — 1M rows with per-row target index into a read span and per-row owner index into the output column: fused table sweep vs gather + facade tick + scatter emulation.
- `churn` — 100k ephemeral 20-tick non-looping rows spawned per pass (2.1M steady state): sweep with fused retirement vs facade tick plus explicit swap-remove retirement.
- `movement` (context) — the per-row timeline state alone: select, position and cycle commit, no job, no frame columns.
- `floor` (context) — the same read/write fold with no timeline state at all.

Every timed pass consumes observable results: each pass mutates deterministic input cells, folds per-row checksum words, and mixes a strided sample into a running seed; each lane also compares full checksums and membership state against the facade after equal pass counts from equal starting state. A lane reporting sub-physical nanoseconds per row, a checksum mismatch, or nonzero warm-path allocation is a defect, not a result. Allocation is read from `GC.GetAllocatedBytesForCurrentThread()` around the measured window.

## Measured results

All four lanes verified checksum-identical to the `Timeline.Rows` facade (and membership-identical for churn) with 0 B warm-path allocation on both sides. Medians of three full harness runs on the machine below; run-to-run spread was within about ±5% on every number.

| Lane | rows | facade ns/row | table ns/row | ratio | facade B/pass | table B/pass |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `sweep` (staggered clocks, shared asset) | 1,000,000 | 11.1 | 1.83 | 6.0x | 0 | 0 |
| `pulse` (duration-1 loop, constant-frame sweep) | 1,000,000 | 10.9 | 1.01 | 10.7x | 0 | 0 |
| `watch` (per-row gather/scatter vs gather + facade + scatter) | 1,000,000 | 15.1 | 3.90 | 3.9x | 0 | 0 |
| `churn` (100k spawns/pass, 20-tick windows, 2.1M steady) | 2,101,000 | 18.4 | 1.73 | 10.6x | 0 | 0 |
| `movement` (context: movement state alone, no job) | 1,000,000 | 1.01 | — | — | 0 | — |
| `floor` (context: same fold, no timeline state) | 1,000,000 | 0.82 | — | — | 0 | — |

Reading the numbers:

- The sweep lane's table cost sits directly on top of the movement-state context lane (~1.0 ns/row): per-row selection, position/cycle commit, and the frame/value column load account for almost the whole table, with the dispatcher gone. The facade pays stage search, occurrence dispatch, chain walking, and a two-pass commit per row, which is worth ~6x here and matches the shape of issue #85's cost model.
- The duration-1 classification halves the table sweep again (1.83 -> 1.01 ns/row) by collapsing frame loads and movement to a constant fold; the facade cannot see any of this because it re-derives the frame every row.
- The watch lane is where tables win least on paper and most in expressiveness: the facade number is an emulation (gather pass, facade tick, scatter pass) because `Timeline.Rows` cannot index a read column per row; the table does gather, frame lookup, and scatter in one sweep. The scatter to non-local `owners` cells also costs cache locality, which is why 3.9 ns/row is honest rather than the ~1.8 of a row-local write.
- The churn lane's facade number includes rebuilding the query every pass (membership changed, so compatibility must be re-established) and the explicit retire sweep; the table fuses retirement into its advance sweep and allocates nothing. The same-per-pass retire counts and handle-keyed checksums verify identical membership evolution, including the `Retire(handle)` API exercised mid-run.
- Comparison with the same-machine reference points recorded when issue #88 was filed (facade mixed ~9 ns/row, table ~0.54, raw floor ~0.33): this machine's floor for the same read/fold work is 0.82 ns/row and its facade lane is ~11 ns/row, so absolute numbers are not comparable across sessions; ratios against the same-session floor are. The residual difference between the referenced 0.54 ns/row sweep and this harness's 1.83 ns/row is state width, not dispatch: the prototype carries the full designed per-row state (position 4 B, cycle 8 B, inputs 4 B, precomputed frame 12 B, output 8 B, all actually touched every row), while the reference point measured a leaner sweep. Any implementation claim should re-run this harness rather than extrapolate either number.

## Machine and methodology

- Machine: Intel Core i9-14900K (32 threads), 31 GiB RAM, Omarchy (Arch Linux), kernel 7.2.3-arch1-3, x64.
- Runtime: .NET SDK 10.0.401, runtime 10.0.12, workstation (non-server) GC, default tiering with tier-1 steady state reached during warmup.
- Harness: a Stopwatch harness, median of 15 samples after 5 warmups (25 warmups for the churn lane, whose first 21 passes are membership transient), one pass per sample. Every timed pass includes the deterministic input mutation and a strided checksum sample, so results are consumed and cannot be legally collapsed by the JIT; a lane reporting sub-physical ns/row or nonzero allocation is invalid and must be fixed, not reported.
- Warm-path allocation is read from `GC.GetAllocatedBytesForCurrentThread()` around the measured window; all lanes report 0 B on both sides.
- Parity: each lane first runs an untimed phase comparing per-pass checksum sequences and end-of-run full checksums and membership state (sums of positions/cycles; for churn, per-pass retire counts and handle-keyed samples, which are independent of physical row layout) between the facade and the table over identical pass counts from identical starting state. The timed phases then re-verify the final full checksums.
- Nothing else ran concurrently; three back-to-back full-suite runs agreed within ~5%. Baked-asset bytes are printed as SHA-256 by the harness and reproduce byte-identically through the `tlbake` CLI (all four fixtures verified).
- The facade lanes build one query and keep ticking it for the static lanes, and rebuild per pass for the churn lane, which is the documented cost of re-establishing compatibility when membership changes.

## Open questions

The repository contract requires these to be answered before they are treated as solved. Each carries a proposed answer, not an assumption.

1. **Missing-component policy.** Proposed: attach-time validation only — a table whose job's complete required column set cannot be bound does not attach, mirroring the complete-row compatibility law; there is no per-row partial execution. Per-row eligibility (Unity enableable columns, runtime component removal) would need per-row presence bits, a preallocated exclusion sink, and the adopted no-concealment diagnostics. Open: whether per-row enablement earns its per-row bit and coordinator re-evaluation cost, or ineligible memberships must be retired by the host instead.
2. **Consumer identity.** Inside one table the LIFO-chain problem disappears: one job, one execution per occurrence. The unresolved question is identity across paths: the generated module initializer registers the same job in the global pair table for facade queries, so a membership driven by both a table and a facade query would execute the consumer twice. Proposed: table rows are exclusively owned — attach marks ownership, facade queries and duplicate attaches of the same (pair, asset) to a second table are diagnostics — and identity dedupes by job type across both paths. Open: who owns the exclusivity rule (runtime validation vs documented contract) and how a hand-written wrapper and its generated counterpart prove shared identity across the two paths.
3. **Loaded-asset lifetime.** Tables borrow the asset's native TLB1 block and derive precomputed frame tables from it at attach. Proposed: the table holds a strong managed reference to the `TimelineAsset` owner (a managed object holding only the pointer), `Tick` validates the asset stamp per frame, and disposing the asset before detaching its tables is diagnosed misuse rather than undefined behavior. Open: the explicit publication, ownership, and safe-reclamation proof AGENTS.md requires for runtime-loaded definition storage — including precompute cost for assets with very large durations, where per-position precompute is impractical and a lazy or sparse fallback is needed.
4. **Multi-consumer ordering across tables.** Multiple jobs on one pair execute as separate tables in attach order with reverse-order symmetry on backward frames. Open: whether that reproduces every facade A-B-A trace across tables or whether heterogeneous multi-consumer memberships must stay on the facade lane for v1.
5. **Reverse and multi-frame sweeps.** The design keeps movement in `TimelineMovement`, so reverse and crossed frames are semantically covered, but the prototype measures forward delta-1 only. Open: measure reverse and multi-frame catch-up before implementation is proposed for approval.
