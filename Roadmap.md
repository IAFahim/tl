# Roadmap — tl, correct by construction

> Proposal from a static read of `src/` at `985ddf1` (2026-10-21), with `README.md`, `AGENTS.md`, `docs/tlb1-layout.md` and `benchmarks/source_budget.py` as context. Nothing was built, run or measured: every finding cites the lines that prove it by reading and the receipt that would confirm it by execution. AGENTS.md forbids untracked task lists as an authority, so this file is input, not a tracker — file the atoms in §10 through `eng/agent-work`, then delete it. It sits at the repository root because `source_budget.py` counts untracked files under `src/` against the 334,000-byte budget.

## 0 · The answer in one screen

The magic is not a new feature. It is making the system small enough to prove: one movement law, one kernel family, one immutable bank snapshot, one ordered consumer program — and a hard wall between **Prepare** (cold: may allocate, validate, lock, throw, run consumer code) and **Play** (warm: total, pure, 0 B, lock-free, Burst- and NativeAOT-compilable). Every current API survives as `Prepare ∘ Play`; hosts that need determinism audits, zero-hitch frames or Burst call the halves directly. Once that wall exists, cross-engine bit identity, kernel proofs by exhaustion, load-order-independent results and safe hot reload follow from laws instead of vigilance.

What reading found, ranked:

1. **S0-1** Long multi-lane banks decode another lane's runs when both directions segment. Backward playback of lane 0 — the table `Apply(…, forward: false, …)` reads — is wrong for any timeline of 2,048+ ticks with two or more lanes.
2. **S0-4** A concurrent first use can pair a stale bank directory with a fresh bound and read past the old allocation, on any CPU.
3. **S0-2** Typed bind never compares the asset's pool value widths with `sizeof(TTrack)`/`sizeof(TClip)`; stale or crafted assets read outside their validated pools.
4. **S0-3** The last `Dispose` overwrites the block header in place and the next `Load` on any thread frees it, with no reader grace period.
5. **S1-1** Consumer order is reverse install order, and across assemblies it is module-load order — contradicting README's "MoveY before ScreenShake".
6. **S2-6** After the last lease is disposed, `Timeline.Advance` freezes the clock while `Apply` keeps adding the frozen tick's value every frame.
7. **S3-10** Burst hosts can only consume the `SlotView` ABI, which is 64-bit-only, pointer-based and has no conformance vectors.

Start with Phase 0.3 (round-trip receipt, expected red), then 1.1, 1.8 and 1.2.

## 1 · Axioms

```
Motion    m = (D : u16, looping : bool)
Position  p : u16            playable domain: finite [0, D], looping [0, D)
Step      σ(m, dir, p) = (p′, edge?)          total over every u16 p
Edge      (tick, flags)      tick = p forward; p − 1, or D − 1 at p = 0, backward
Program   Π(tick) = ordered [(pair, slot)]    immutable, from TLB stages
Frame     φ(slot, tick, flags)                pure
Lane      L_k(dir, tick) = ordered fold over Π(tick) in dir order of Fold_k(φ)
Apply     cell′ = cell ⊕ L_k(dir, tick) when σ crosses an edge, else cell
Dispatch  ExecuteActive over Π(tick) in dir order — the only effect
```

Each law is a test; the ledger names the law each defect breaks.

| # | Law | Statement |
|---|---|---|
| L1 | Totality | σ, Apply and Advance are defined for every u16 input; Play never throws or faults |
| L2 | Reversibility | σ(m, fwd, p) = (p′, (t, f)) ⇒ σ(m, bwd, p′) = (p, (t, ι f)); ι sets Reverse and swaps CompletedAfter with CompletedBefore |
| L3 | Rest | finished and out-of-domain positions are fixed points with no edge and no effect |
| L4 | Row independence | Play(xs ++ ys) = Play(xs) ++ Play(ys): chunking, threading and SIMD width never change a bit |
| L5 | Memo coherence | a bank read equals direct evaluation of the fold at that edge; the bank is a cache, not a semantics |
| L6 | Round trip | decode(encode(L)) = L bitwise |
| L7 | Sound sharing | two indices share a block ⇒ their lanes are bitwise equal |
| L8 | Order | consumer order is a function of consumer identity, never of load order |
| L9 | Identity | index ↔ content bytes for the entry's lifetime; Load is idempotent; leases add |

The position domain is finite — 2¹⁶ durations × 2¹⁶ positions × 2 × 2 ≈ 1.7·10¹⁰ cases — so L1–L3 can be checked exhaustively for every kernel instead of sampled.

## 2 · What becomes possible

| Capability | Mechanism | Gate |
|---|---|---|
| Same bits in .NET, Burst, C and wasm | one law, a specified bank ABI, conformance vectors, strict float mode | Phase 5 |
| Burst-compiled, lock-free, allocation-free hot path | Play over an explicit unmanaged snapshot | Phase 3 |
| Kernels proven, not sampled | exhaustive differential tests against a scalar oracle | Phase 0 |
| Results independent of assembly and plugin load order | sealed registry with a canonical order | Phase 2 |
| Live consumers at hand-written speed | per-pair generated programs: frame built once, direct calls the JIT or Burst can inline | Phase 4 |
| Hot reload and modding without use-after-free | owning leases plus reclamation at host-proven quiescence | Phase 1/3, owner |
| No runtime folding; load is a map | folded bank shipped beside content (a Unity BlobAsset) | owner |
| Half the table footprint for direction-blind and direction-signed lanes | classify backward as equal to, or the exact negation of, forward at fold time and store one table | Phase 7, hypothesis |
| One gather per mixed row | 8-byte index entry: arena offset, duration, flags | Phase 7, hypothesis |
| O(1) scrub and late join for Fold lanes | integral lanes: the sequential prefix from zero equals sequential playback from rest | conflicts with the no-skip rule — owner |
| Rollback and replay netcode | L2 plus integer or fixed-point lanes; replicated state is 4 B per entity | exists; formalize |
| Any SoA ECS, any engine | spans or pointers in, no engine types; optional NativeAOT C exports | Phase 5/7 |

## 3 · Defect ledger

Severity: **S0** memory safety or silent wrong values · **S1** determinism and concurrency · **S2** contract and API traps · **S3** performance and hygiene. Paths are relative to the repository root.

### S0 — memory safety, silent wrong values

#### S0-1 · Multi-lane segmented banks decode another lane's runs
- **Where** `src/Tl.Core/TimelineSet.cs:910-946` (plan), `:473-490` (directory bases)
- **Breaks** L6, L5
- **Mechanism** With `TableTicks ≥ 2049` and both encodings smaller than flat, `EncodeAll` appends every forward lane, then every backward lane: `[F0 … Fn, B0 … Bn]`. The re-encode branch (`:919`) runs only when a direction is flat, so that order survives. `BakeBlock` walks lanes assuming `[F0, B0, F1, B1 …]`. With two or more lanes every directory except lane 0 forward is built over another lane-direction's runs — including lane 0 backward. A bucket whose search leaves its window stores `EscapeBit | last`, and `SegValue`/`LaneEncoding.Value` then walk past the run pool. Nothing compares the published block with the measured values; `SameTables` (`:386-408`) runs only on dedupe candidates.
- **Trigger** Any pair with two or more lanes (two `out` results, or a sugar consumer beside a Fold) on a timeline of 2,048+ ticks whose values compress. The Numbers harness uses 1,024-tick lanes, which stay flat.
- **Fix** One canonical encode loop in `(lane, direction)` order; decide "segmentation is smaller" with a pure size function instead of by appending; run `SameTables(view, measured, …)` after `BakeBlock` as a publish gate.

```cs
internal static LaneBlockPlan Plan(MeasuredLanes measured, nuint ticks)
{
    var tickCount = checked((int)ticks);
    var flatBytes = (long)tickCount * sizeof(float) * measured.LaneCount;
    var segmentable = tickCount >= LaneEncoding.SegmentedMinTicks;
    var plan = new LaneBlockPlan(measured.LaneCount)
    {
        FlatForward = !segmentable || EncodedBytes(measured.LaneForward, measured.LaneCount, tickCount) >= flatBytes,
        FlatBackward = !segmentable || EncodedBytes(measured.LaneBackward, measured.LaneCount, tickCount) >= flatBytes,
    };
    for (var lane = 0; lane < measured.LaneCount; lane++)
    {
        if (!plan.FlatForward) plan.ForwardSegments[lane] = LaneEncoding.Encode(measured.LaneForward[lane], tickCount, plan.Segments, plan.Dense);
        if (!plan.FlatBackward) plan.BackwardSegments[lane] = LaneEncoding.Encode(measured.LaneBackward[lane], tickCount, plan.Segments, plan.Dense);
    }
    return plan;
}

static long EncodedBytes(float** lanes, int laneCount, int ticks)
{
    var segments = new List<LaneSegment>();
    var dense = new List<float>();
    for (var lane = 0; lane < laneCount; lane++)
        LaneEncoding.Encode(lanes[lane], ticks, segments, dense);
    return segments.Count * (long)sizeof(LaneSegment) + dense.Count * (long)sizeof(float);
}
```

- **Receipt** Property test: 1–20 lanes, ticks ∈ {2049, 4096, 65536}, values drawn from constant, ramp and dense mixtures, both directions forced segmented; every decoded word equals the measured word in both directions. Expected red on `985ddf1`.

#### S0-2 · Typed bind never checks pool value widths
- **Where** `src/Tl.Core/TimelinePair.cs:618-631` (bind), `src/Tl.Core/Data.cs:71-78` (reads with `sizeof`), `:260-263` (pools validated with the asset's own widths)
- **Breaks** L1
- **Mechanism** `Validate` bounds each pool by the asset's `TrackValueBytes`/`ClipValueBytes`; `ToFrame` indexes with `sizeof(TTrack)`/`sizeof(TClip)`. Nothing compares the two. The layout fingerprint is skipped whenever either side is 0 (README §Data documents this for hand-written consumers and layout-0 assets) and misses fixed-buffer lengths (S2-11).
- **Fix** At typed bind, require `TrackValueBytes == sizeof(TTrack)` and `ClipValueBytes == sizeof(TClip)` unconditionally, with a located rebake diagnostic; the fingerprint stays an additional check.
- **Receipt** Crafted fixture with `ClipValueBytes = 4` for an 8-byte clip: bind throws; no playback API touches the pool.

#### S0-3 · Released blocks are corrupted at once and freed without reader grace
- **Where** `src/Tl.Core/InternTable.cs:347-354` (the graveyard link and size are written into bytes 0–15: Magic, Version, Loops, Duration), `:86` (every `Load` drains), `:356-371`; raw-pointer holders `src/Tl.Core/Data.cs:311-316` (`TimelineComponent`), `:614-657` (`FrameQuery`), `src/Tl.Core/TimelinePair.cs:91,160,459`
- **Breaks** L9, L1
- **Mechanism** A `TimelineRef` captured before the last `Dispose` — in an ECS component, a running dispatch loop, a `FrameQuery` — immediately reads a garbage `Duration`, then freed memory after any thread's next `Load`. The graveyard defers the free, but nothing ties the deferral to readers.
- **Fix** (a) Keep graveyard links in a side list, never inside the block (no API change). (b) Free only in an explicit `Reclaim()` the host calls at a quiescent point — Unity: after the frame's timeline jobs complete (owner decision; changes the documented lifetime model). (c) Components carry indices, never block pointers (§5.6).
- **Receipt** N readers dispatch and query while one thread loads and releases distinct contents in a loop, under the guard-page allocator seam (§8): zero faults, header bytes unchanged until reclaim.

#### S0-4 · Bank growth race pairs a stale array with a fresh bound
- **Where** readers `src/Tl.Core/TimelineSet.cs:986-987`, `:753-754`, `:791-792`, `:561-563`; writer `:261` (`_views`) and `:288` (`_count`)
- **Breaks** L1, L4
- **Mechanism** `Bind` publishes `_views` and `_count` as separate plain stores, with the whole block bake between them. Readers load `_views`, then `_count`. A preemption between those two loads — or ARM64 load reordering — pairs the old array with the new bound. The next id at or above the old capacity reads past the old allocation (only a 16-byte retire pad follows it) and the result is dereferenced as a `SlotView*`. README §"Bank blocks" assumes array and bound come from one snapshot.
- **Trigger** Two jobs touching the same newly loaded index whose bind grows the directory (indices crossing 1,024, 2,048, …).
- **Fix** Publish one immutable `BankState` (views, motion, tables, count, holes, minimum duration) per bind with a single release store; each reader takes one acquire load per call and uses only that state (§5.4).
- **Receipt** Two-thread stress over first uses straddling 1023/1024 and 2047/2048, checked and release, under guard pages.

#### S0-5 · Permute kernels read past short bank tables
- **Where** `src/Tl.Core/TimelineSet.cs:1096-1104` → `src/Tl.Core/Lane.cs:379,465` (`Vector256.Load`, 32 bytes) and `:406-407,505-506` (two `Vector128.Load`s); block layout `TimelineSet.cs:456-466`
- **Breaks** L1
- **Mechanism** Bank flat tables hold `duration + 1` floats with no padding; `LaneTable` pads to 8 (`Lane.cs:1122`), the bank does not. One lane, duration 2: the block is 104 bytes, the backward table starts at byte 84, and the permute load reads bytes 84–115 — 12 bytes past the allocation (8 at duration 3, 4 at duration 4). The extra lanes are masked arithmetically; the read itself is out of bounds.
- **Fix** Append a 32-byte zero guard to every bank block (ABI-neutral), or pad each flat table to `max(8, TableTicks)`.
- **Receipt** Guard-page allocator over durations 1–9, lanes 1–3, both directions.

#### S0-6 · Release builds turn shape errors into memory corruption
- **Where** `src/Tl.Core/Checked.cs` (every guard is `[Conditional("TL_CHECKED")]`); unchecked consumers such as `src/Tl.Core/TimelineSet.cs:766-767` (`rows[i]` is an unchecked index) and every `LoadUnsafe`/`StoreUnsafe` kernel
- **Breaks** L1; AGENTS.md "programmer misuse fails fast"
- **Status** Deliberate and documented: README "Playback misuse contract" makes release misuse undefined behaviour, and an IL scan proves zero guard call sites. The two documents disagree.
- **Proposal** Keep the O(n) domain checks conditional. Make the O(1) checks unconditional — length equality and `Overlaps` cost one predictable branch per call and nothing per row — and fold `(uint)row < (uint)count` into the row loops that already load `rows[i]`. Keep the change only if every Numbers row stays within noise. Owner decision: it changes the IL-scan gate.
- **Receipt** Raw BDN JSON for all nine Numbers rows before and after, with PMU counters; the IL scan allows exactly the new O(1) guards.

#### S0-7 · The fold reads uninitialized scratch
- **Where** `src/Tl.Core/MeasuredLanes.cs:167` (zeroes only the column-pointer region), `:177` (`indices` never initialized), `src/Tl.Core/Data.cs:576-580` (scans all 40 bytes per consumer)
- **Breaks** determinism of the fold; harmless today by luck
- **Mechanism** Bytes past a consumer's own slots are garbage; non-zero garbage becomes refresh entries that aim unused column slots at garbage-derived lane cells. No thunk reads those slots — today.
- **Fix** Zero `indices` before `BindPair`, or have `Bind` return its slot count. Same atom: free `scratch` when the first lane pass throws (`MeasuredLanes.cs:60-61` precede the `try`), and zero the whole table for duration 0 (`:205` clears only `[duration]`, leaving `[1]` uninitialized for `HashPlan` and `Inspection`).

#### S0-8 · Window-constant folding captures lane 0, not the consumer's lane
- **Where** `src/Tl.Core/MeasuredLanes.cs:236,316-326`; `src/Tl.Core/Data.cs:200,208` (replay), `:452` (`AnyWindowConstant`), `:601` (registration)
- **Breaks** L5
- **Mechanism** Capture reads lane cell 0 and zeroes it before every consumer (`*scratch = 0f`), then replays into each consumer's column slot 0. A window-constant consumer whose lane is not 0 folds zeros, and other steps' lane-0 values are clobbered during capture. Reachable only through the public hand-written `PairRuntime.Consume(execute, bind, TickPurity)` — the generator never emits it — yet one such registration moves every fold in the process onto `FillWindows`.
- **Fix** Delete `TickPurity`, `FillWindows`, `Capture`, `AdvanceOrFail`, the cached branch of `ExecuteWindow`, `ChainWindowConstant`, `ChainLength` and `BlendThunk` (§6). Cold-path speed does not justify a second fold semantics.

#### S0-9 · `IBlend.Blend` can mutate shared asset memory
- **Where** `src/Tl.Core/Data.cs:71` (`ref var track`), `:79` (`track.Blend(...)`)
- **Breaks** L9, L4
- **Mechanism** `track` is a mutable reference into the interned block shared by every loader and thread; a non-`readonly` `Blend` writes through it. The bytes then stop matching their entry, so a later `Load` of the original bytes misses and publishes a second index for the same content.
- **Fix** `ref readonly var track` (a non-readonly method then runs on a defensive copy), plus a generator diagnostic requiring `readonly struct` tracks and clips — the README samples already are.
- **Receipt** A deliberately mutating `Blend`: asset bytes are identical before and after playback.

### S1 — determinism, concurrency

#### S1-1 · Consumer order is load order, and contradicts the README
- **Where** `src/Tl.Core/Data.cs:440` (each install prepends), `:498,524` and `src/Tl.Core/MeasuredLanes.cs:123` (head-first walks); install order `src/Tl.Gen.CSharp/JobEmitter.cs:55-67`, ascending by name from `src/Tl.Gen.CSharp/Analysis/JobReader.cs:28`
- **Breaks** L8; README.md:343 ("Consumers fold in consumer-name order (MoveY before ScreenShake)")
- **Mechanism** Within one binding, forward folds and dispatch run in descending name order and reverse runs ascending. Across assemblies the chain is reverse module-initializer order, which depends on which module the process touches first. Two-term float sums commute, so most receipts cannot see it; three terms, `ExecuteActive` side effects and `out`-lane numbering can.
- **Fix** Seal the registry into per-pair contiguous arrays sorted by (assembly name, consumer type name) — a total order independent of load order (§5.5). Changing the observable order is an owner decision with a release note.
- **Receipt** Consumers A, B, C (by name) contribute 1e8, −1e8 and 1 to one shared float lane: ascending order yields 1, descending yields 0. Add a dispatch log. Two processes with opposite assembly first-touch orders must produce identical bits and logs; reconcile with the order pinned in `tests/Tl.Core.Tests`.

#### S1-2 · Lane assignment is global mutable state
- **Where** `src/Tl.Core/MeasuredLanes.cs:135` (every measurement writes `Consumer.OutLanes`), `src/Tl.Core/TimelinePair.cs:476` (the live path reads it to feed memo cells)
- **Breaks** L5, L4
- **Mechanism** Lane numbers depend on which pairs a measurement includes. The public `MeasuredLanes.Measure(TimelineAsset)` (`MeasuredLanes.cs:40-46`) measures every pair, so afterwards a per-pair bank's live path feeds `ExecuteActive` from the all-pairs numbering. Concurrent folds race on the same words.
- **Fix** Lane maps become part of each view, immutable beside `LaneKeys`; `OutLanes` is deleted.

#### S1-3 · Fold-to-ExecuteActive links are inferred from thread-static install order
- **Where** `src/Tl.Core/Data.cs:354-355,439,441`; fallback `src/Tl.Core/TimelinePair.cs:485-487`
- **Mechanism** An `ExecuteActive` links to "the last Fold installed for this pair on this thread". That matches today's generator output; any other order — hand-written registration, a second generator, another thread — links the wrong consumer or none, and an unlinked consumer reads memo feeds from feed 0 of whichever consumer came first.
- **Fix** `Consume` returns a consumer handle; `ConsumeDispatch(…, feed: handle)`; the generator passes it.

#### S1-4 · Bake links are published with plain stores
- **Where** `src/Tl.Core/BakeTable.cs:78-79`
- **Mechanism** `Head`/`Next` are plain stores after the entry body. README.md:463 claims release publication. ARM64 readers can observe a link before `ParamKeys` and `Invoke`.
- **Fix** `Volatile.Write` both links, or move bakes into the sealed registry.

#### S1-5 · Registry readers mix a cached base with a later head
- **Where** `src/Tl.Core/TimelinePair.cs:414,425-436`; `src/Tl.Core/Data.cs:477-498`
- **Mechanism** `ConsumerAt` is cached and `HeadOf` read later; a concurrent install that grows the consumer array yields indices beyond the cached array. The guard at `TimelinePair.cs:467` covers one of three loops.
- **Fix** Sealed registry.

#### S1-6 · Late registration splits one pair into several semantics
- **Where** `src/Tl.Core/Data.cs:423-450`; banks keep views for the process lifetime
- **Mechanism** An assembly loaded after a pair has folded adds consumers to later folds only: one pair, different consumer sets per index, undetected. `tlb --live` and plugin hosts load assemblies dynamically. For long-lived hosts that unload a collectible `AssemblyLoadContext`, its function pointers outlive the context.
- **Fix** A registry digest per sealed version, stored in each view; Prepare refolds or rejects on mismatch; registrations scoped to their load context.

#### S1-7 · First use folds unrelated indices inside the warm call
- **Where** `src/Tl.Core/TimelinePair.cs:634-638,641-654`
- **Mechanism** First use of index i folds every pending live index below i. `Determine` checks `IsLive`, then calls `Reference`, under separate gate acquisitions: a concurrent release of an unrelated lower index throws `NotLoaded` out of this `Apply`. Indices that stay dead are rescanned, one gate each, on every later first use.
- **Fix** Prepare folds the ids it is given; holes are a property of the request, not of process history.

#### S1-8 · Pair identity includes the assembly version
- **Where** `src/Tl.Core/Data.cs:23,593`
- **Mechanism** `AssemblyQualifiedName` contains `Version=`, so every version bump of the assembly defining a track or clip changes every pair key and every baked asset stops binding until rebaked (README.md:185: "pair keys hash assembly-qualified names"). Keys are 64-bit FNV with `| 1`, and neither `PairTable`, `BakeTable` nor `TypeKey` users detect collisions.
- **Fix** Identity = hash of the namespace-qualified type name, with generic arguments recursively; compatibility = layout fingerprint plus value widths; registration keeps names in the cold registry and fails on a key collision. Owner decision: rebake every asset under a TLB version bump.

#### S1-9 · Dead-entry revival trusts a non-cryptographic digest
- **Where** `src/Tl.Core/InternTable.cs:311-312`, `:247-260`
- **Mechanism** Live entries compare bytes; dead entries revive on a 128-bit digest match alone. A colliding content then lives under the old index while banks keep that index's old folded views (README.md:493), so asset motion and bank duration disagree and `ApplyLive` reads lanes at ticks past `TableTicks`. Collisions are constructible for user-generated content.
- **Fix** Keep a 256-bit cryptographic digest per entry (SHA-256 or BLAKE3, specified in the TLB spec) and require equality on revive.

#### S1-10 · `LaneTable.Bind` frees tables readers still use
- **Where** `src/Tl.Core/Lane.cs:1130-1143`
- **Mechanism** The previous tables are freed while `LaneAccelerator` still points at them; pointers, duration and looping are published as independent stores. It is also one asset per pair type, process-wide.
- **Fix** Delete with the `ITimelineLane` family (§6), or give it an immutable snapshot like the bank.

#### S1-11 · Bit exactness relies on one compiler's float evaluation
- **Where** `src/Tl.Core/Lane.cs:950-951` (ramp), `src/Tl.Core/Data.cs:76` (blend factor), every `+=` kernel
- **Mechanism** Ramp runs are accepted only when RyuJIT's IEEE single evaluation reproduces every measured word. Any host that contracts to FMA or reorders — Burst `FloatMode.Fast`, C with `-ffast-math` or `-ffp-contract=fast` — decodes different bits.
- **Fix** Make the formulas normative in the spec; Burst kernels compile with `FloatMode.Strict`, C with `-ffp-contract=off`; conformance vectors enforce it (§8).

#### S1-12 · `TimelineAsset.Index` reads its field twice
- **Where** `src/Tl.Core/Data.cs:298-302`
- **Mechanism** A concurrent `Dispose` between the check and the read returns `(ushort)-1`, index 65,535. `Reference` has the same shape.
- **Fix** Read once into a local.

### S2 — contract and API traps

#### S2-1 · `TimelineAsset.Of` releases what it never acquired
- **Where** `src/Tl.Core/Data.cs:290-294`, `src/Tl.Core/InternTable.cs:143-161`
- **Mechanism** `Load` acquires and returns a bare index; `Of` reads the generation and acquires nothing, yet its `Dispose` releases. Two views of one `Load` release twice and the second kills a live asset; after a death and revival, a stale holder's `Of` adopts someone else's reference.
- **Fix** `Load` returns an owning handle that releases exactly once; `Of` acquires (+1). Owner decision (public API).

#### S2-2 · `TimelineState` cannot represent timeline 0
- **Where** `src/Tl.Core/Playback.cs:20-25,87`
- **Mechanism** `Asset == 0` means "none", but 0 is the first index `Load` returns, so it never moves through `TimelineMovement.Select`. Nothing in the runtime uses either type.
- **Fix** Delete both, or carry presence separately.

#### S2-3 · Bake dispatch ignores modifiers and silently drops wide bakes
- **Where** `src/Tl.Gen.CSharp/JobEmitter.cs:166,177-179`; `src/Tl.Core/Timeline.cs:119-139,188`; `src/Tl.Gen.CSharp/Analysis/BakeReader.cs:47-61`
- **Mechanism** (a) Keys are type-only and every generated slot is a mutable `ref`, while the host overloads take `in`: a bake declaring `ref T` writes through an `in` argument — into a compiler temporary (lost) or a readonly field (mutated). (b) A bake with more than four state parameters is generated and installed, then skipped by `Dispatch` forever.
- **Fix** TLGEN error above four state parameters; include the modifier in the key so a `ref` parameter binds only through `BakeRef`; or generate exact-signature `Bake` entry points per declared signature (TLGEN74 already makes signatures unique per pair).

#### S2-4 · Chained generation crashes, does not filter, and duplicates diagnostics
- **Where** `src/Tl.Gen.CSharp/Analysis/JobReader.cs:61`; `src/Tl.Gen.CSharp/TimelineGeneration.cs:41-43,48-62`
- **Mechanism** `consumerTrees.Add` throws on the second pairing of a multi-pair job, so `TimelineGeneration.Run` crashes. `consumers` only asserts membership and never narrows the emitted set. Diagnostics located in authored trees are reported again by the chained pass.
- **Fix** Indexer assignment; filter `selected` by `consumers` (or rename it `expected`); report only diagnostics located in `synthesized` trees.

#### S2-5 · Memo feeds bind by position and type
- **Where** `src/Tl.Gen.CSharp/Analysis/JobReader.cs:140-148` (documented at README.md:363)
- **Mechanism** Leading `in` parameters whose types match the Fold `out` types positionally become feeds. An intended live `in float wind` after `Fold(…, out float arc)` silently becomes a feed of `arc`.
- **Fix** Require the feed name to equal the `out` name, or warn on a type match with a name mismatch. No cap change (#356).

#### S2-6 · Movement is coupled to folding, with two motion sources
- **Where** `src/Tl.Core/TimelinePair.cs:63-83,86-110`; `src/Tl.Core/InternTable.cs:216`; `src/Tl.Core/Checked.cs:98-127`
- **Mechanism** `Advance(index, ref clock)` and `TrySample` fold the typed bank — and throw for a pair without consumers — just to move a clock or read a frame. After the last lease is disposed, asset motion is zeroed: `Timeline.Advance` freezes every clock on that index while `Apply` keeps playing the bank (README.md:493: dead indices keep their tables), so the frozen tick's value is added again every frame, without bound. Checked builds reject positions on a dead index that release builds accept.
- **Fix** Movement is untyped and reads one motion source; a dead index has one defined behaviour across every API family (L3: no edge, no effect).

#### S2-7 · Two column-binding rules
- **Where** `src/Tl.Core/TimelinePair.cs:217-221` against `:272-287,341`
- **Mechanism** Float `Apply` plays lane 0 by position for any 4-byte effect type; integer `Apply` and `ApplyChunk` select lanes by `TypeKey<TEffect>`. `Apply<…, Health>` succeeds where `ApplyChunk<…, Health, X>` throws.
- **Fix** One rule everywhere: bind by lane key, with lane-0-by-position available only through an explicitly named entry point.

#### S2-8 · Non-float sugar passes generation and fails at first fold
- **Where** `src/Tl.Gen.CSharp/Analysis/JobReader.cs:120-126`; `src/Tl.Core/MeasuredLanes.cs:127`; `src/Tl.Gen.CSharp/JobEmitter.cs:123-125`
- **Mechanism** Sugar `ExecuteActive(in Frame, ref T)` is accepted for any unmanaged `T`, but sugar consumers get a float-keyed lane, so the generated `Bind_` throws at first fold unless another consumer declares a shared `ref T` Fold result.
- **Fix** TLGEN for non-float sugar, or emit `Keys` for sugar so its lane is keyed by `T`.

#### S2-9 · Folding one pair binds another pair's consumers
- **Where** `src/Tl.Core/Data.cs:572`
- **Mechanism** `BindPair` binds every Bind consumer of every pair the asset uses, against the measured pair's pool. Folding pair A throws when pair B in the same asset has a sugar consumer whose type A's pool lacks — for example when A folds only `out int`.
- **Fix** Filter by the measured pair key.

#### S2-10 · All-pairs measurement feeds single-pair tables
- **Where** `src/Tl.Core/TimelineSet.cs:212-226`, `src/Tl.Core/Lane.cs:1118`, `src/Tl.Core/MeasuredLanes.cs:46`
- **Mechanism** Non-lazy `TimelineSet.Add` and `LaneTable.Bind` measure every pair, so lane 0 can belong to another pair of a multi-pair asset. Internal, but every friend assembly in `src/Tl.Core/AssemblyInfo.cs` can reach it.
- **Fix** Delete the non-lazy `Add`; always measure one pair.

#### S2-11 · The layout fingerprint misses byte-changing edits
- **Where** `src/Tl.Gen.CSharp/Analysis/PairLayout.cs:17-27`
- **Mechanism** It folds field names and type names only: enum underlying types, fixed-buffer lengths (the field type is a pointer), `StructLayout` `Pack`/`Size`/`Explicit` and `FieldOffset` can change while the fingerprint stays equal.
- **Fix** Fold sizes and offsets — emit `Unsafe.SizeOf<T>()` and field-offset assertions into the module initializer and fingerprint them, or compute sequential layout for blittable fields. Pair with S0-2.

#### S2-12 · `SlotView.Skipped` is documented but effectively never written
- **Where** README.md:483 says `Skipped` "marks a row with no next position"; only `src/Tl.Core/TimelineSet.cs:1336` writes it, on a branch `FastMixedChunk` (`:1487-1522`) makes unreachable. Every other path writes the position unchanged.
- **Fix** Identity everywhere (L3: a finished clock is a fixed point): write `p` at `:1336`, delete `Skipped` and its README sentence.

#### S2-13 · The validator enforces less than the spec promises
- **Where** `src/Tl.Core/Data.cs:246,260-263,265-274`; `docs/tlb1-layout.md:13,73,141-149`
- **Mechanism** The spec says value pools are 16-byte aligned (not checked); it lists "a second index set without [a blend window]" as rejected (not checked); stages "partition `[0, Duration)`", but only contiguity and coverage are checked, so `End < Start` passes and breaks `StageOf`'s binary search. Outside the spec but needed for total frames: `TrackIndex < TrackCount`, every slot window contains each stage that references it, and blended rows satisfy `FactorStart ≤ tick < FactorStart + FactorSpan` — `Data.cs:76,82-84` subtract these as unsigned values.
- **Fix** Validate all of them; add the last three to the spec's rejection contract; one fuzz-corpus entry each.

#### S2-14 · The generator accepts what the runtime rejects
- Non-float sugar (S2-8), more than four bake state parameters (S2-3), and user parameter names that collide with generated locals: `JobEmitter.cs:80-94` emits user names verbatim beside `__tlRow`, `__tlTyped`, `__tlClip` and `__tlColumns`.
- **Fix** TLGEN diagnostics, or generated locals that never reuse user names.

#### S2-15 · A misspelled consumer method vanishes silently
- **Where** `src/Tl.Gen.CSharp/Analysis/JobReader.cs:82-88`
- **Mechanism** A job with neither `Fold` nor `ExecuteActive` but any static frame-first method is skipped as "hosted": `Fodl(in Frame<…>, out float x)` produces no diagnostic and no consumer.
- **Fix** Hosted families opt in explicitly (a marker the host generator understands); everything else gets TLGEN66.

#### S2-16 · Comment policy
- **Where** XML docs in `src/Tl.Core/BankView.cs:5-9,24-29`, `src/Tl.Core/Inspection.cs`, `src/Tl.Gen.CSharp/TimelineGeneration.cs:8-15,22-32`; `//` comments in `src/Tl.Gen.CSharp/Analysis/JobReader.cs:82-86` and `src/Tl.Core/TimelinePair.cs:466`
- **Mechanism** AGENTS.md: "Production code contains no explanatory comments."
- **Fix** Remove them, or amend the rule to exempt public XML documentation (owner).

### S3 — performance and hygiene

- **S3-1 Locks on warm paths.** `TimelineTable.Reference` takes the process-wide spin gate (`src/Tl.Core/InternTable.cs:115-128`) once per id run in effect-less dispatch (`src/Tl.Core/TimelinePair.cs:160`), once per id run in live playback (`:459`) and per `TrySample` (`:91`); README.md:496 says the runtime "takes no lock on the warm path". `Load` validates and copies under the same gate (`InternTable.cs:97`), stalling those readers for the duration. Fix: warm paths read a prepared snapshot; validation runs before the gate.
- **S3-2 Live binding rebuilt per id run.** `TimelinePair.cs:453-522` re-derives the column map, memo plan and feed ranges — calling `Keys` thunks per consumer — at every id change, although only the slot pointer depends on the id. Fix: a `LivePlan` bound once per (pair, `ColumnSet` shape) (§5.1).
- **S3-3 Stack proportional to the whole registry.** `columns` is sized by the pair's largest global `Offset` (`TimelinePair.cs:423-437`): 40 pointers per consumer registered anywhere before it, so consumer 2,000 needs 640 KB of stack — fatal on small job-worker stacks. `feedStart`/`feedEnd` (`:416-419`) and the reverse `rev` buffers (`src/Tl.Core/Data.cs:481,514`) are sized by the global consumer count, and the latter are zero-filled on every reverse dispatch per step per row unless `SkipLocalsInit` applies (not visible in `src/`). Fix: per-pair contiguous consumer arrays; reverse is a decrementing index.
- **S3-4 Dead generated code.** `FoldRange_*`/`ExecuteActiveRange_*` are emitted for every Fold and sugar consumer (`src/Tl.Gen.CSharp/JobEmitter.cs:60,66,93,101`) and installed (`Data.cs:440`) but never called. Delete them. If live crowds on shared clocks matter, emit range thunks for live consumers instead and drive them over runs of equal (id, position): the frame is identical across the run.
- **S3-5 The generator is not incremental.** `src/Tl.Gen.CSharp/TimelineIncrementalGenerator.cs:16-19` reruns the whole semantic walk on every compilation change; every type declaration is visited twice (`JobReader.cs:28`, `BakeReader.cs:21`); the TLGEN60 probe calls `ToString()` on every descendant node (`JobReader.cs:22-23`), quadratic in file size. Fix: `CreateSyntaxProvider` over type declarations with base lists → per-type equatable models → `Collect` → emit; TLGEN60 by identifier scan.
- **S3-6 Frame rebuilt per consumer.** Every thunk rebuilds the frame and re-blends the clip (`JobEmitter.cs:83`). Fix: per-pair generated programs build the frame once per step and call consumers directly (§5.5).
- **S3-7 Integer and multi-lane playback are scalar.** `src/Tl.Core/TimelineSet.cs:657-689` decodes per row through `LaneEncoding.Value` while float lane 0 has six SIMD tiers. Fix: one kernel family parameterized by cell combine (§5.3).
- **S3-8 ARM64 paths.** `src/Tl.Core/Timeline.cs:27` calls `Vector256.EqualsAll` unconditionally, software-emulated per 16 rows on NEON (Unity mobile, Apple silicon). `src/Tl.Core/Lane.cs:377,396,463,485` call `Avx2.PermuteVar8x32` under `Vector256.IsHardwareAccelerated`; test `Avx2.IsSupported`.
- **S3-9 Hash precedence.** `TimelineSet.cs:418-419` parses as `(h ^ a ^ v0) | (v1 << 32)`, OR-ing V1 into the mix. Dedupe stays sound only because `SameTables` re-checks.
- **S3-10 ABI widths.** `SlotView` uses pointer-width fields (`src/Tl.Core/BankView.cs:37-42`) behind README offsets that hold only on 64-bit hosts (the wasm32 playground and armv7 differ). `LaneSegment` has implicit padding at bytes 6–7 and `SegGather` hard-codes `index * 4 + 2` (`Lane.cs:640`). Fix: u64 offsets, explicit layouts, compile-time offset assertions; drop `AbiVersionV1`/`V2` (`BankView.cs:33-34`).
- **S3-11 Digest.** `InternTable.cs:412-415` dereferences unaligned `ulong*` (use `Unsafe.ReadUnaligned`; armv7 can fault). The digest is unspecified, so no other implementation can compute content identity. Specify it.
- **S3-12 Limits disagree.** 65,536 ids are allowed (`InternTable.cs:98`), but the 75% load cap at capacity 65,536 stops at 49,152 distinct contents (`:100-103`).
- **S3-13 Managed work under a spin gate.** `TimelineSet.cs:265` builds `LaneBlockPlan` (lists, arrays) while holding the bank gate; a GC there leaves every other binder spinning.
- **S3-14 Internals as API.** `InternalsVisibleTo("Play.Core")` (`src/Tl.Core/AssemblyInfo.cs`) makes the playground depend on internals; the public `Inspection` surface exists for that.
- **S3-15 A layout invariant held only by prose.** `GatherRows` reads `table[-1]` for backward origin lanes before re-gathering (`TimelineSet.cs:1277-1284`); README.md:496 proves it in-bounds because a header precedes every table. Pin that with a layout test, or clamp the index.
- **S3-16 Managed state behind the warm path.** `Timeline<,>._bank` is a managed `TimelineSet` read by every warm call (`src/Tl.Core/TimelinePair.cs:18,697-703`), and first use allocates `LaneBlockPlan` and `MeasuredLanes` inside it; AGENTS.md allows only a pointer-holding owner. Covered by §5.4.

## 4 · Contract drift

| Source says | Code does | See |
|---|---|---|
| AGENTS: `Tl.Runtime` and `Tl.Compiler` around a language-neutral plan | `Tl.Core` and `Tl.Gen.CSharp`; the model carries C# type names from Roslyn straight to emitted C# (`src/Tl.Gen.CSharp/Model/Jobs.cs`) | 6.6 |
| AGENTS: "total signed `Tick`" | unsigned 16-bit positions; no `Tick` type | §9 |
| AGENTS: `IBake<TConsumer, …TContext>` | `IBake<TConsumer>` with context in the method signature (`src/Tl.Core/Compiled.cs`) | §9 |
| AGENTS: "Before 1.0, public API changes ship without shims" | README installs 3.0.0 and carries 1.0 and 2.0 migration notes | §9 |
| AGENTS: no managed state reachable from playback | managed bank read by every warm call | S3-16 |
| AGENTS: misuse fails fast | README: release misuse is undefined behaviour | S0-6 |
| AGENTS: no comments | XML and line comments | S2-16 |
| README: name order, MoveY before ScreenShake | reverse name order when folding forward | S1-1 |
| README: no lock on the warm path | global gate per id run in dispatch and live playback | S3-1 |
| README: bank publication proof | stale array with a fresh bound | S0-4 |
| README: bake publication proof | plain-store links | S1-4 |
| README: `Skipped` marks rows with no next position | identity everywhere except one unreachable branch | S2-12 |
| README: `SlotView` offset table | true on 64-bit hosts only | S3-10 |
| Spec: rejection contract | three listed checks missing | S2-13 |

## 5 · Target architecture

### 5.1 Prepare, Play, Dispatch

```
cold — may allocate, validate, lock, throw, run consumer code
  TLB bytes ──Load──▶ intern + lease ──┐
  module init ──Register──▶ Seal ──────┼──Prepare(pair, ids)──▶ BankState*   immutable, unmanaged, one pointer
  ColumnSet ───────────────────────────┴──Bind(pair, set)─────▶ LivePlan     immutable, unmanaged

warm — total, 0 B, lock-free, no throws, no managed reads, Burst- and NativeAOT-compilable
  Play(in BankState, ids, clocks, effects[, next], direction)
  Step(in MotionTable, ids, clocks, next, direction)
  PlayLive(in LivePlan, ids, clocks, columns, direction)
```

Every current public warm entry point becomes `Prepare ∘ Play` or `Bind ∘ PlayLive` — same signatures, same results, same lazy-fold convenience. Hosts that need Burst, determinism audits or hitch-free frames call Prepare at load and Play per frame. Inside Play an unfolded or absent id is L3 identity (no edge, no effect, clock untouched) and is counted into an optional miss counter; diagnostics live in Prepare. New entry points take a `Direction` enum instead of `bool forward`, so a call site names its meaning.

### 5.2 One law, many evaluators

Movement is restated in about twenty places today (`Playback.cs:30-74`, `Lane.cs:917-927`, `Timeline.cs:36-48,74-92`, `TimelineSet.cs:11-50,619-639`, the scalar tails of every kernel). One total function becomes the definition; every vector tier is an evaluation strategy proven equal to it (§8). The `IRowWalk` policies stay as compile-time specializations but call the law instead of restating it.

```cs
public readonly struct Motion
{
    public readonly ushort Duration;
    public readonly bool Looping;

    public Motion(ushort duration, bool looping)
    {
        Duration = duration;
        Looping = looping;
    }
}

public enum Direction : byte
{
    Forward,
    Backward,
}

public readonly struct Edge
{
    public readonly ushort Next;
    public readonly ushort Tick;
    public readonly FrameFlags Flags;
    public readonly bool Crossed;

    Edge(ushort next, ushort tick, FrameFlags flags, bool crossed)
    {
        Next = next;
        Tick = tick;
        Flags = flags;
        Crossed = crossed;
    }

    public static Edge Rest(ushort position) => new(position, 0, FrameFlags.None, false);

    public static Edge Cross(ushort next, ushort tick, FrameFlags flags) => new(next, tick, flags, true);
}

public static class Clock
{
    public static Edge Step(Motion motion, Direction direction, ushort position)
    {
        var duration = motion.Duration;
        var backward = direction == Direction.Backward;
        var finite = !motion.Looping;
        var playable = backward && finite ? (uint)(position - 1) < duration : position < duration;
        if (!playable) return Edge.Rest(position);
        var tick = backward ? (ushort)(position == 0 ? duration - 1 : position - 1) : position;
        var last = tick == duration - 1;
        var next = backward ? tick : (ushort)(motion.Looping && last ? 0 : tick + 1);
        var flags = (motion.Looping ? FrameFlags.Looping : FrameFlags.None)
            | (backward ? FrameFlags.Reverse : FrameFlags.None)
            | (tick == 0 ? FrameFlags.TimelineStart : FrameFlags.None)
            | (last ? FrameFlags.TimelineEnd : FrameFlags.None)
            | (finite && !backward && last ? FrameFlags.CompletedAfter : FrameFlags.None)
            | (finite && backward && position == duration ? FrameFlags.CompletedBefore : FrameFlags.None);
        return Edge.Cross(next, tick, flags);
    }
}
```

`Clock.Step` reproduces `TimelineMovement.Advance` (`Playback.cs:30-74`) case by case, including the looping rest state at `position == duration`; Phase 0.1 proves that over every input before anything switches to it.

### 5.3 Policies instead of paths

Float, integer, bool and 8-byte lanes are all 32-bit cells; only the combine differs. Zero-size structs with instance methods are the policy carrier: the JIT and Burst both devirtualize them, and unlike `static abstract` members (`IRowWalk`, `IEffectSource`, `ITimelineLane`) they compile under C# 9 and Unity's runtime.

```cs
public interface ILane
{
    uint Cell(Direction direction, ushort tick);
}

public interface ICombine<TCell> where TCell : unmanaged
{
    TCell Combine(TCell current, uint cell);
}

public readonly struct SumSingle : ICombine<float>
{
    public float Combine(float current, uint cell) => current + BitConverter.Int32BitsToSingle((int)cell);
}

public readonly struct SumInt32 : ICombine<int>
{
    public int Combine(int current, uint cell) => current + (int)cell;
}

public readonly struct AnyBoolean : ICombine<bool>
{
    public bool Combine(bool current, uint cell) => current | (cell & 0xFF) != 0;
}

public static class Oracle
{
    public static void Apply<TLane, TCell, TCombine>(in TLane lane, Motion motion, Direction direction, ReadOnlySpan<ushort> clocks, Span<TCell> cells)
        where TLane : struct, ILane
        where TCell : unmanaged
        where TCombine : struct, ICombine<TCell>
    {
        for (var row = 0; row < clocks.Length; row++)
        {
            var edge = Clock.Step(motion, direction, clocks[row]);
            if (edge.Crossed) cells[row] = default(TCombine).Combine(cells[row], lane.Cell(direction, edge.Tick));
        }
    }
}
```

Lane sources are `readonly struct`s — `FlatLane`, `SegmentedLane`, `AnalyticLane<T>` (replacing `ITimelineLane<T>`) — so `in TLane` never copies. An 8-byte result is two adjacent cells read in one step. The SIMD tiers in `LaneOps` become specializations of this one shape and must equal the oracle bit for bit.

### 5.4 Bank state and bank blob

Readers take exactly one acquire load per call; writers build the next state and publish it with one release store. Superseded states join the existing retired chain.

```cs
internal unsafe struct BankState
{
    public ulong Version;
    public uint Count;
    public uint Holes;
    public ushort MinDuration;
    public ushort Reserved;
    public IndexEntry* Index;
    public byte* Arena;
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct IndexEntry
{
    [FieldOffset(0)] public uint View;
    [FieldOffset(4)] public ushort Duration;
    [FieldOffset(6)] public ushort Flags;
}
```

The arena is append-only; views are addressed by 64-bit offsets from its base, never by pointers, so the same bytes are valid on 32- and 64-bit hosts, can be memory-mapped, cached on disk by digest and stored in a Unity `BlobArray<byte>`. Per view:

| Field | Width | Meaning |
|---|---|---|
| Magic, Version | u32, u16 | `TLBK`, 4; readers reject unknown versions |
| LaneCount | u16 | lanes, 8-byte results counted as two |
| RegistryDigest | u64 | consumer set and layout fingerprints the fold ran against (S1-6) |
| ContentDigest | 32 B | specified 256-bit digest of the TLB bytes (S1-9) |
| Motion | u32 | duration in the low 16 bits, looping in bit 31 — the same word as the motion table |
| TableTicks | u32 | `max(1, duration) + 1` |
| LaneKeys, LaneMap | u64 offsets | lane identities and the immutable consumer-to-lane map (S1-2) |
| Descriptors | u64 offset | one per (lane, direction): kind (flat, segmented, same-as-forward, negated-forward), table, directory, runs and dense offsets |

Explicit per-(lane, direction) descriptors make S0-1's ordering structural rather than positional, and the alias kinds carry the Phase 7 footprint halving. A 32-byte zero guard ends every view (S0-5). `IndexEntry` puts motion and the view offset in one 8-byte word, so the mixed tier needs one gather per row instead of a pointer gather plus a motion gather (hypothesis; Phase 7).

### 5.5 Sealed registry and per-pair programs

Module initializers keep registering. The first Prepare — or an explicit `Seal()` — freezes the installs into one published table:

```cs
internal enum ConsumerKind : byte
{
    Fold,
    Sugar,
    Dispatch,
    Live,
}

internal readonly struct PairProgram
{
    public readonly ulong PairKey;
    public readonly ulong Digest;
    public readonly int First;
    public readonly int Count;
}

internal readonly unsafe struct ConsumerRecord
{
    public readonly ulong Identity;
    public readonly int Feed;
    public readonly int ColumnBase;
    public readonly ConsumerKind Kind;
    public readonly delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> Execute;
    public readonly delegate*<ulong*, byte*, int> Keys;
}
```

- **Order** Records sort by (pair key, assembly name, consumer type name). Forward iterates `First … First + Count − 1`, reverse iterates down — no linked list, no stack buffer, no load-order dependence (S1-1, S1-5, S3-3).
- **Columns** `ColumnBase` is dense per pair, starting at 0, so a pair's scratch is 40 × its own consumers, never 40 × the global registry.
- **Feeds** `Feed` is the explicit handle the generator passed (S1-3); lane maps live in views (S1-2).
- **Versions** A later install produces a new sealed version with a new per-pair `Digest`; views record the digest they folded against (S1-6).
- **Per-pair programs** The generator emits one function per (pair, assembly): build the frame once, call every consumer of that pair directly in sealed order, with a mirrored function for reverse. The registry then holds one record per (pair, assembly) and the warm path makes one indirect call per step, or none once the Unity backend inlines it (S3-6).

### 5.6 Ownership and reclamation

- **Leases** `TimelineAsset` becomes the owning handle — a managed object holding only an index and a generation, which AGENTS.md permits. `Load` returns one; `Of` acquires another; `Dispose` releases exactly once (S2-1).
- **Borrowed indices** Columns and components hold bare `ushort` indices, valid while any lease lives. Nothing stores a block pointer (S0-3c).
- **Reclamation** A release marks the entry dead and leaves the bytes intact; memory returns only in `Reclaim()`, which the host calls when no reader can be inside a block — after `JobHandle.Complete` on the frame's timeline jobs in Unity, between frames elsewhere (S0-3b).
- **Dead indices** One rule for every API family, decided once (S2-6).

### 5.7 Unity ECS and Burst

What Burst and Unity's scripting runtime require, against what `Tl.Core` does today:

| Constraint | Today | Required |
|---|---|---|
| Burst compiles HPC#: no managed objects, no mutable managed statics (only `SharedStatic<T>`), no try/catch, throws fatal in players | managed `_bank`; static tables allocated in static constructors; throws inside warm calls | Play takes `ref BankBlob` or pointers; no statics; no throws |
| Burst calls only Burst-compiled function pointers | consumer thunks are managed `delegate*<…>` | generated direct calls, or a Burst `FunctionPointer<T>` per pair program |
| Burst does not compile `System.Runtime.Intrinsics` | every SIMD tier | portable scalar oracle that Burst auto-vectorizes, plus `Unity.Burst.Intrinsics` tiers, both held to the conformance vectors |
| Unity's scripting runtime (C# 9 and .NET Standard 2.1 as of Unity 6 — verify for the target version) lacks static abstract members, ref fields, `allows ref struct`, `params ReadOnlySpan`, `NativeMemory`, `Vector128`/`Vector256` | used throughout | portable kernel subset: instance policies, pointer plus length, nothing beyond netstandard2.1 |
| Module initializers — verify; Unity's C# 9 support list has excluded them | the `unity-entities` backend emits `[ModuleInitializer]` (`src/Tl.Gen.CSharp/UnityJobEmitter.cs` → `JobEmitter.cs:52`) | `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, plus `[InitializeOnLoadMethod]` in the Editor |
| Cross-platform bits | RyuJIT IEEE single | `[BurstCompile(FloatMode = FloatMode.Strict)]`, never `Fast` (S1-11) |

Data model:

- `TimelineId` as `ISharedComponentData` when many entities share a timeline — every chunk is then uniform and always takes the uniform kernel — or `IComponentData` when ids vary per entity.
- `TimelineClock : IComponentData` holds one `ushort`; crowds on one clock use a chunk component and the broadcast `Apply(index, clock)` path (README's 0.08 ns floor).
- Effects are the game's own single-field 4-byte components, matching today's column contract.
- The bank is a `BlobAssetReference<BankBlob>` produced during baking in the Editor, where the Fold code exists, so players fold nothing (owner decision, 7.1).
- One Apply job per pair per frame, then one Advance job; `ScheduleParallel` is safe by L4.

Once Play is pure, the generated job reduces to:

```cs
[BurstCompile(FloatMode = FloatMode.Strict)]
unsafe struct ApplyJumpJob : IJobChunk
{
    [ReadOnly] public BlobAssetReference<BankBlob> Bank;
    [ReadOnly] public ComponentTypeHandle<TimelineId> Ids;
    [ReadOnly] public ComponentTypeHandle<TimelineClock> Clocks;
    public ComponentTypeHandle<JumpY> Effects;
    public Direction Direction;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var ids = (ushort*)chunk.GetComponentDataPtrRO(ref Ids);
        var clocks = (ushort*)chunk.GetComponentDataPtrRO(ref Clocks);
        var effects = (float*)chunk.GetComponentDataPtrRW(ref Effects);
        Kernels.Apply(ref Bank.Value, ids, clocks, effects, chunk.Count, Direction);
    }
}
```

Live consumers become one generated `IJobChunk` per consumer that maps each `ExecuteActive` parameter to a component type handle — the compile-time form of `ColumnSet`: no `TypeKey` matching, and a missing column is a query mismatch instead of a runtime throw. Those systems belong in tl.unity under the AGENTS.md boundaries; `Tl.Core`'s job is the portable kernels, the blob ABI and the conformance vectors.

### 5.8 Other engines

- **.NET ECS** (Arch, Friflo, DefaultEcs, custom SoA): Prepare at load, Play per chunk span; partition entities by timeline id so chunks stay uniform; parallelize across chunks (L4).
- **Native engines**: a NativeAOT shared library exporting `tl_load`, `tl_prepare`, `tl_play` and `tl_step` through `[UnmanagedCallersOnly(EntryPoint = …)]` over the same spans, or tl.c reading the specified blob. AGENTS.md defers C work, so this is an owner decision.

### 5.9 Names that say the domain

Internal renames are free; public ones go to §9.

| Today | Proposed | Why |
|---|---|---|
| `TimelineSet<,>` | `PairBank<,>` | README and `Inspection` already call it the bank |
| `TimelineSetLane<,>` | `BankKernel<,>` | it is the batch kernel, not a lane |
| `TimelineTable` in `InternTable.cs` | `InternTable` | file and type disagree |
| `SlotRow` (24-byte TLB row) and `SlotRow = 40` in `PairTable`/`JobReader` | `SlotRecord`, `ConsumerColumns` | one name for two unrelated concepts |
| `Resolve`, `Determine`, `AddAt`, `Bind`, `Measure` | `Fold` | five verbs for one cold operation |
| `TimelineRef.Advance`/`Select`, `TimelineMovement.Advance`, `LaneMovement` | `Clock.Step` | one law |
| `MemoLane`, "memo" | `FeedLane`, "fold" | the public word is Fold |
| `Keying` | `TypeIdentity` | names what it computes |
| `Timeline`, `Timeline<T>`, `Timeline<TTrack, TClip>` | `Clock`, `AnalyticLane<T>`, `Timeline<TTrack, TClip>` | three unrelated APIs share one name |

## 6 · Deletion list

Deletion shrinks the system without removing a capability.

| Remove | Where | Replaced by | ≈ lines |
|---|---|---|---:|
| `ITimelineLane<T>`, `Timeline<T>`, `TimelineLane<T>` | `src/Tl.Core/Lane.cs:12-242` | `AnalyticLane<T>` in the unified kernel, or an extension package | 231 |
| `LaneAccelerator<T>`, `BakedLane`, `LaneTable` | `Lane.cs:1072-1145` | the bank | 74 |
| Window-constant folding: `TickPurity`, `FillWindows`, `Capture`, `AdvanceOrFail`, cached `ExecuteWindow` branch, `ChainWindowConstant`, `ChainLength`, `BlendThunk` | `MeasuredLanes.cs:183-204,208-326`; `Data.cs:191-212,452-472,585-589,601,607-608` | the plain per-tick fold | 210 |
| Range thunks: runtime field, overloads, generated code | `Data.cs:6,344,597,599`; `JobEmitter.cs:60,66,93,101` | — | 15, plus one thunk per consumer |
| Movement restatements | `Playback.cs:30-74`; `Lane.cs:832-844,917-927`; `Timeline.cs:36-48,74-92`; `TimelineSet.cs:619-639` | `Clock.Step` plus proven vector tiers | 100 net |
| `TimelineState`, `TimelineMovement.Select` | `Playback.cs:20-25,76-91` | `Clock.Step` | 22 |
| `TimelineRef.Select` | `Data.cs:105-108` | `Step` | 4 |
| `TickFrame` instance members | `Data.cs:318-334` | the static `ToFrame` | 15 |
| Non-lazy `TimelineSet.Add`, `ThrowUnboundId` paths | `TimelineSet.cs:212-226,1525-1526` and call sites | Prepare | 25 |
| `ApplyLive` dead parameters `keyFx`, `fx`, `keyIn`, `source` | `TimelinePair.cs:410,508-512` | — | 6 |
| `SlotView.AbiVersionV1`, `AbiVersionV2`, `Skipped` | `BankView.cs:33-36` | — | 3 |
| `Consumer.OutLanes`, thread-static feed inference | `Data.cs:344,354-355,439,441`; `MeasuredLanes.cs:135` | per-view lane maps, explicit feed handles | 8 |

About 700 of `Tl.Core`'s ≈5,900 lines (≈12% by line count; measure bytes with `benchmarks/source_budget.py`). Removing public surface — rows 1, 3, 4 (the public range `Consume` overloads), 6, 8 and 11 — is an owner decision and a major version.

## 7 · Phased roadmap

Each atom is one issue and one small PR: core gate from AGENTS.md "Required validation", plus the atom's own receipt, green before the next atom starts. Performance atoms follow AGENTS.md: correctness receipts first, one-variable baseline and candidate on the same machine, raw BDN JSON, `perf stat` counters.

### Phase 0 — evidence first, no behaviour change

| Atom | Outcome | Receipt |
|---|---|---|
| 0.1 | Scalar oracle (`Clock.Step`, oracle Apply and Advance) in the test project | equals `TimelineMovement.Advance` on every input |
| 0.2 | Differential tests: every movement and apply tier against the oracle | D ∈ [0, 64] × all 65,536 positions × looping × direction, plus D ∈ {255, 256, 1023, 1024, 2047, 2048, 2049, 4095, 4096, 65534, 65535}; batch lengths 0–4,096; misaligned starts; `next` aliasing `positions` |
| 0.3 | Bank round-trip property test (S0-1) | red on `985ddf1`, recorded as the known failure |
| 0.4 | Guard-page allocator seam for cold allocation sites, tests only | reproduces S0-5 |
| 0.5 | Concurrency stress: first use across capacity edges, load and release churn | reproduces or bounds S0-3 and S0-4 |
| 0.6 | Order and cross-process determinism receipts | S1-1 receipt |

### Phase 1 — memory safety and silent values

| Atom | Covers |
|---|---|
| 1.1 | S0-1: canonical encode order and the publish gate |
| 1.2 | S0-2: value-width checks at typed bind |
| 1.3 | S0-5: 32-byte block guard |
| 1.4 | S0-7: scratch zeroing, the leak on failure, duration-0 tables |
| 1.5 | S0-9: `ref readonly` track and the readonly-struct TLGEN |
| 1.6 | S0-8, S3-4: delete window-constant folding and range thunks |
| 1.7 | S0-3a: graveyard links outside blocks |
| 1.8 | S0-4: `BankState` publication |
| 1.9 | S2-12: identity at `TimelineSet.cs:1336`; delete `Skipped` |

### Phase 2 — determinism

| Atom | Covers |
|---|---|
| 2.1 | Sealed registry: per-pair arrays, canonical order, dense column bases, feed handles, per-view lane maps — S1-1, S1-2, S1-3, S1-5, S3-3 |
| 2.2 | Release-published or sealed bake links — S1-4 |
| 2.3 | Registry digest per view; load-context scoping — S1-6 |
| 2.4 | Specified 256-bit content digest; revive compares it — S1-9, S3-11 |
| 2.5 | Pair identity without assembly version — S1-8 (owner; TLB version bump) |

### Phase 3 — Prepare and Play

| Atom | Covers |
|---|---|
| 3.1 | `Prepare` and an explicit `BankState`; the current API becomes `Prepare ∘ Play`; Play is total |
| 3.2 | Untyped movement, one motion table, one dead-index rule — S2-6 |
| 3.3 | `LivePlan`: bind once, swap the slot per id, no gate, stack proportional to the pair — S3-1, S3-2, S3-3 |
| 3.4 | Warm paths never touch the intern table; validation before the gate — S3-1 |
| 3.5 | Owning leases and explicit `Reclaim` — S2-1, S0-3b (owner) |

### Phase 4 — kernels

| Atom | Covers |
|---|---|
| 4.1 | One kernel family over lane × combine × direction × next policies for float, integer, bool and 8-byte lanes — S3-7 |
| 4.2 | NEON-first `Vector128` tiers; explicit `Avx2.IsSupported` — S3-8 |
| 4.3 | Per-pair generated programs — S3-6 |
| 4.4 | Retire the `ITimelineLane` family (owner) |

### Phase 5 — ABI and portability

| Atom | Covers |
|---|---|
| 5.1 | Bank blob v4 (§5.4) and a `SlotView` v4 with u64 offsets — S3-10 |
| 5.2 | Validator parity with the spec, plus the spec additions — S2-13 |
| 5.3 | Normative float formulas; conformance vectors per fixture — S1-11 |
| 5.4 | Portable kernel subset: C# 9, netstandard2.1, instance policies |
| 5.5 | Unity registration attributes and Burst jobs, in tl.unity per AGENTS.md boundaries |

### Phase 6 — generator

| Atom | Covers |
|---|---|
| 6.1 | Incremental pipeline — S3-5 |
| 6.2 | Chained-generation fixes — S2-4 |
| 6.3 | Layout fingerprint v2 — S2-11 |
| 6.4 | TLGEN for wide bakes, non-float sugar, reserved names, readonly tracks and clips, hosted opt-in — S2-3, S2-8, S2-14, S2-15 |
| 6.5 | Explicit memo-feed binding — S2-5 (owner) |
| 6.6 | Neutral-plan extraction, or an AGENTS.md amendment that matches the code — §4 |

### Phase 7 — owner-gated capabilities, each a hypothesis with a receipt

| Atom | Capability | Receipt |
|---|---|---|
| 7.1 | Folded bank shipped with content; Unity BlobAsset | player loads with zero consumer calls; bits equal to runtime fold |
| 7.2 | Direction aliasing (same, negated) | bank bytes and Numbers cold rows with PMU |
| 7.3 | 8-byte index entries | Numbers rows 8 and 9, gather uops per row |
| 7.4 | Integral lanes for O(1) seek | conflicts with "no multi-frame skip" |
| 7.5 | Explicit lane monoids (sum, or, last) | authoring-surface change |
| 7.6 | NativeAOT C ABI | C harness reproduces the conformance vectors |

Order: 0 → 1 → {2, 3} → 4 → 5. Phase 6 atoms are independent once 2.1 lands; Phase 7 follows its base.

## 8 · Verification

| Law | Test | Scale |
|---|---|---|
| L1 Totality | every public warm entry point with random u16 ids and positions, random lengths, dead and absent ids: no throw after Phase 3, no fault under guard pages | fuzz |
| L2 Reversibility | forward then backward returns the start position with `ι`-related flags | exhaustive for D ≤ 64, sampled above |
| L3 Rest | finished and out-of-domain positions stay fixed with no effect | exhaustive |
| L4 Row independence | Play over random partitions, chunk sizes and thread counts equals Play over the whole batch, bitwise | property |
| L5 Memo coherence | every bank lane equals the direct fold at every (tick, direction) for random programs | property |
| L6 Round trip | decode(encode(L)) = L for random lanes including NaN payloads, ±0 and denormals | property |
| L7 Sound sharing | shared blocks are bitwise equal | property |
| L8 Order | the 1e8 / −1e8 / 1 receipt across processes and load orders | integration |
| L9 Identity | Load idempotence, revive equality, lease accounting under churn | stress |

- **Exhaustion is cheap.** The small-D sweep is 65 × 65,536 × 2 × 2 ≈ 17 M cases per tier, plus 11 × 262,144 ≈ 2.9 M for the edge durations — seconds. A full 2³⁴ sweep of each vector tier is ≈1.7·10¹⁰ evaluations — minutes vectorized, suitable for a nightly job.
- **Conformance vectors.** For each committed fixture in `tools/Tl.Bake.Tests/golden/fixtures/` plus generated stress fixtures, publish raw u32 words per (lane, direction, tick) and expected effects and next positions over a fixed clock grid, with SHA-256 sums. tl.unity and tl.c CI consume the same files.
- **Guard-page allocator.** Cold allocation sites go through one internal allocator indirection; tests replace it with an mmap allocator that ends every block at a `PROT_NONE` page. Any over-read faults deterministically. The indirection never sits on the warm path, so the AGENTS.md indirect-call rule holds.
- **Counters.** Kernel changes carry `perf stat` cycles, instructions, branches, branch misses and gather counts alongside raw BDN JSON.

## 9 · Owner decisions

1. Unconditional O(1) release guards (S0-6); changes the IL-scan gate.
2. Leases for `Load`/`Of` and explicit reclamation (S2-1, S0-3b); changes the lifetime model behind #150 and #160.
3. Play treats unfolded ids as identity and counts misses; lazy fold stays in the convenience API (Phase 3).
4. A canonical consumer order (S1-1); observable for lanes with three or more terms and for dispatch.
5. Pair identity without the assembly version (S1-8); rebake every asset under a TLB version bump.
6. Public removals (§6) and renames (§5.9) under a major version; AGENTS.md's "before 1.0" rule needs a post-1.0 replacement (semver major, approval files, migration notes).
7. Memo feeds bound by name (S2-5).
8. The comment rule against public XML documentation (S2-16).
9. Folded banks shipped with content (7.1); lanes are derived data keyed by digests, not authored ids.
10. Integral lanes against "no multi-frame skip, ever" (7.4).
11. Explicit lane monoids (7.5); an authoring-surface change.
12. A NativeAOT C ABI beside the deferred tl.c (7.6).
13. Extract the neutral plan, or amend AGENTS.md's architecture to match the code (6.6).
14. Reconcile "total signed `Tick`" and `IBake<TConsumer, …TContext>` in AGENTS.md with the shipped `ushort` positions and `IBake<TConsumer>`.

Respected and not reopened: the #356 cap surface (10 Fold results, 30 live columns, 40-slot row, TLGEN81), "no multi-frame skip", and license and NuGet decisions.

## 10 · Issue drafts

Labels: `ai`, one `area:*`, one `kind:*` (map the areas below to the repository's existing names); add each to Project 6; claim with `eng/agent-work start <issue> <kind> <scope> "<title>"`.

| # | Title | kind | area | Covers | First receipt |
|---:|---|---|---|---|---|
| 1 | Decode multi-lane segmented banks to their measured values | fix | runtime | S0-1 | round-trip property test |
| 2 | Reject assets whose pool widths differ from the bound pair | fix | runtime | S0-2 | crafted fixture |
| 3 | Keep released timeline blocks intact until reclaimed | fix | runtime | S0-3a | churn stress |
| 4 | Read bank views and bounds from one published state | fix | runtime | S0-4 | first-use stress |
| 5 | Keep permute loads inside bank blocks | fix | runtime | S0-5 | guard pages |
| 6 | Zero fold scratch and free it on failure | fix | runtime | S0-7 | deterministic fold bytes |
| 7 | Fold through a single per-tick path | refactor | runtime | S0-8, S3-4 | fold equality before and after |
| 8 | Keep asset bytes immutable while blending | fix | runtime | S0-9 | mutating-Blend test |
| 9 | Fold consumers in an order independent of assembly load | fix | runtime | S1-1 | order receipt |
| 10 | Store lane maps and feed links per view | refactor | runtime | S1-2, S1-3 | live feed after all-pairs measure |
| 11 | Publish bake links with release semantics | fix | runtime | S1-4 | ARM64 stress |
| 12 | Seal the consumer registry into per-pair arrays | refactor | runtime | S1-5, S3-3 | stack bound per pair |
| 13 | Detect consumer sets that change after a pair folds | feat | runtime | S1-6 | late-load test |
| 14 | Fold only requested indices on first use | perf | runtime | S1-7 | first-use latency |
| 15 | Verify content before reviving a dead index | fix | format | S1-9, S3-11 | forced-collision test |
| 16 | Key pairs by type name independent of assembly version | feat | format | S1-8 | version-bump bind |
| 17 | Make each timeline handle own exactly one acquisition | feat | runtime | S2-1 | double-view test |
| 18 | Diagnose bakes the dispatcher can never call | fix | generator | S2-3 | generator test |
| 19 | Register multi-pair jobs through chained generation | fix | generator | S2-4 | chained-generation test |
| 20 | Move clocks without folding typed banks | refactor | runtime | S2-6 | dead-index test |
| 21 | Diagnose non-float sugar consumers at generation | fix | generator | S2-8 | generator test |
| 22 | Bind only the measured pair's consumers | fix | runtime | S2-9 | two-pair asset test |
| 23 | Fingerprint enum, fixed-buffer and explicit layouts | fix | generator | S2-11 | layout-edit test |
| 24 | Validate every invariant the TLB1 spec rejects | fix | format | S2-13 | fuzz corpus entries |
| 25 | Take no lock on dispatch and live playback | perf | runtime | S3-1 | contention benchmark |
| 26 | Bind live columns once per column-set shape | perf | runtime | S3-2 | live benchmark |
| 27 | Generate bindings incrementally per declaration | perf | generator | S3-5 | generator step tracking |
| 28 | Prove every playback kernel against the scalar oracle | test | runtime | Phase 0 | differential suite |
| 29 | Publish bank conformance vectors for host ports | test | format | 5.3 | vector files and sums |
| 30 | Split playback into Prepare and Play | feat | runtime | Phase 3 | Burst-compatible Play |

## 11 · Unverified or out of view

- Tests, benchmarks, `tools/`, `src/Tl.CSharp`, tl.unity and tl.c were not read. Some items may already be covered — check `tests/Tl.Core.Tests` for segmented multi-lane round trips and for the pinned fold order first.
- Whether `[module: SkipLocalsInit]` is applied anywhere; it changes S3-3's zero-fill cost.
- Unity specifics — C# version, module-initializer support, the CoreCLR transition — must be checked against the target Unity version.
- `tools/Tl.Bake.Oracle` may already model playback; extend it rather than duplicate 0.1.
- Nothing was measured. Every performance statement here is a hypothesis with a named receipt.
