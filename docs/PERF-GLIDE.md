# Horizon glide

A playback row can cache a proven number of ordinary tick steps (its horizon) and move only the tick until the next edge. S1 adds it to the kind-free `TimelineWorld.Advance` path (opt-in); S4 adds kind calm, so tl.unity's kind tick job glides between kind edges (on by default there). It does not change the Unity scheduler, add SIMD dispatch, or defer observable ticks.

## API and ownership

`new PlaybackTable(entityCapacity, slotsPerEntity, horizonLimit)` opts in with a limit from 0 to 255. The existing two-argument constructor uses 0. `HorizonLimit` reports the table's remaining eligibility; obtaining its mutable public `View` permanently sets it to 0. Retained playback pointers can then be edited under the existing rules without invalidating a hidden cache.

Public `TimelineWorld.View` also exposes mutable library pointers. Obtaining it permanently disables glide for that world. Borrowed `AdoptView` libraries are conservatively ineligible; owned byte-loaded libraries are eligible until a mutable view escapes. Use value-returning `Find`, `IsPlaying` and `CountOf` for inspection when retaining eligibility. The optimization is intentionally conservative about pointer aliases.

The public additions are the three-argument constructor, `PlaybackTable.HorizonLimit`, and the pure `SlotWord`, `Horizon` and `GlideKernel` helpers. Existing signatures remain available. The public API approval records additions only. This local candidate retains the current 4.0 preview package version; it does not authorize a release or publish.

`SlotWord.Glide` and `GlideKernel.RunScalar` are unsafe primitives. Call them only on live, caller-owned little-endian 8-byte playback storage, under a previously established positive `Horizon.OfRun`, with the same direction, unchanged library and no intervening mutation. They deliberately do not check timeline boundaries. A zero horizon requires the existing motion rules. Counts exclude sample copies and dead inline tails. Table/world mutations and disposal require exclusive ownership; they must not overlap advancement or borrowed pointer use. No slot pointer survives a glide call internally.

## Invariant

An ordinary slot has a nonzero timeline, Rate 0 and no flags other than Reverse. Phase may contain any byte because `Step` ignores it at Rate 0. An inert slot has Paused or Ended without StartPending, Stopping, StopAged, Began or ReenterPending. It is a fixed point of `Step`, including at nonzero rates because `Moves` prevents `Cross`.

For a valid timeline with positive duration at most 65535 and a tick below duration, the forward calm is `Duration-1-Tick`; backward calm is `Tick`. The run horizon is the positive minimum over ordinary slots, capped by the configured limit. Empty and inert slots do not reduce it. Any other live slot, invalid bound, or absence of an ordinary slot yields0.

For every `k <= horizon`, integer glide equals `Step` repeated `k` times, byte for byte. The horizon excludes the edge step. Its positive run contains a slot that stays moving, so the playing bit cannot become false during glide. At zero, the original `AdvanceEntity` executes unchanged, and the new horizon is computed afterward.

The cache keys a unique host world identity, library generation and direction. Identities are allocated lazily with an atomic counter only when an eligible world first glides; adopting/reloading invalidates the identity. Switching through an ineligible world forgets the cache key. Requests invalidate the row before mutation, including exceptional exits; reset, rebind and kind-store attachment invalidate before changing playback. Direction changes clear the byte column once before processing.

The dense 64 shortcut requires all 64 playing bits, no spilled rows, full live counts in every row and 64 positive horizons. It checks every byte before subtracting packed ones, so no subtraction can borrow between bytes. Full counts are necessary: a legal spill/unspill sequence can leave nonzero dead inline slots. Sparse/partial rows only touch `CountOf(entity)` slots. Checked visit accounting includes both paths.

## Memory and execution

Opt-in storage is one 64-byte aligned native header and one byte per entity, allocated once and released with the table. The managed table gains one native-owner pointer; no new managed reference or per-tick object is retained. Default tables allocate no native cache. A mutable-view escape disables the cache but retains its allocation until disposal; `NativeBytes` includes it.

At 4M x4, logical playback storage including both bitsets grows from 145,000,000 bytes to 149,000,064 bytes. This is +1 byte/entity plus the fixed header, not a RAM saving. Allocator overhead/RSS is measured separately. The overall setup/memory replacement gate is not implied by S1.

The scalar kernel uses integer arithmetic only, with unchecked word wrapping after horizon proof. It has no intrinsics, allocation, recursion, exceptions or mutable static data. It targets both netstandard2.1 and net10.0; host cache allocation and lazy identity operations are outside that kernel. No claim about Unity's integrated tick speed or wide-vector code follows from this step.

## Verification and reproduction

New tests cover the complete flag byte across representative rates/phases, both directions and boundary shapes; mixed ordinary/inert/empty runs; 255-step windows; packed-byte subtraction; dense/partial words; dead unspill tails; retained playback/library aliases; borrowed libraries; equal-generation world switches; reload; exceptional request exits;10,000 deterministic request streams at limits 0/1/2/7/255; and process-wide warm allocation across boundaries. They inspect trusted internal storage while comparing, so public view deoptimization cannot silently bypass the tested fast path. Existing test bodies and pinned hash expectations are unchanged.

Run the Core tests in Release, with `TlCoreTfm=netstandard2.1`, and with `TlChecked=true`. Full suite, approval, source budget, package-only consumer, native AOT, ISA-disabled measurements and independent verification receipts accompany the local handoff. Treat any missing receipt as unverified.

The benchmark under `benchmarks/Glide` measures the existing kind-free `Advance` path with the same library and source on baseline/candidate builds. It validates state after timing and reports all rounds, allocation, setup and memory. S1's scalar kill rule is below 1.5x at 4M x4 and duration at least60; it does not replace the full Unity 2x gate.

## Kind calm (S4)

`KindCalm.Build` turns a library's kind event bits into two byte tables per kind timeline: the number of ordinary forward or backward steps before the next tick whose event bit fires in that direction, capped at 255. The host allocates the `Length` bytes per direction plus one start index per timeline, once per library bind; the tables are derived, never hashed or serialized. Kind-free timelines keep the motion calm. `Horizon.OfKindRun` is `OfRun` with that calm, so a positive result proves that `KindRules.TickEntity` would only move ticks by one and make no row call for that many steps. `KindGlideTests` checks the table against a brute-force scan, every flag byte at every tick of the kind fixture, and random request/direction streams at limits 1/2/7/255 against `TickEntity` with recorded row calls and entry bytes. The .NET `KindStore` does not use it yet; tl.unity's `TimelineKindTickSystem` does (`TimelineHorizon`, `TimelineGlide`, see tl.unity `docs/KINDS.md`).

## Unity result (S4 + S5)

Measured in Unity 7000.0.0a16 with Burst, 11 workers, safety checks off, median of 5 fresh processes per arm, against tl.unity main `3b44b20`: 4M x4 (duration 3000) 38.15 -> 31.40 ms per tick (tick job 14.02 -> 9.86 ms, kind jobs 24.89 -> 21.96 ms with tl.unity's direct window step), duration 300 39.80 -> 31.74 ms, 10k x150 2.87 -> 1.94 ms; with editor-default safety checks 48.4 -> 39.1 ms. Hashes identical, 0 B on the main thread over 1000 warm ticks. This is 1.22x at 4M x4, not the 2x replacement gate: the tick job still reads and writes every row's in-chunk playback bytes, so it sits near the memory floor; only deferring ticks (S8) would go lower.
