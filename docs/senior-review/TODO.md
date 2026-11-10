# TODO: executing the senior review plan

Source of truth for the plan: `docs/senior-review/senior-plan.md` (written from code reading only, nothing run). This file turns it into work an agent can claim. The plan's numbers marked `[guess]` are guesses until `benchmarks/source_budget.py` or a benchmark prints them.

Facts already checked against the real code (2026-11-11): the 5.0 emitter feeds `default` to any kind whose value is not float/int (`SystemEmitter.LaneValue` returns null); `PlaybackTable.Grow` doubles the slot stride for every entity; a `Playback` is 8 bytes. Finding 1 of the plan (25 sites that "do not compile") was caused by a lossy paste, not by the repo: `PlaybackTable.Grow` and `NativeAlloc.AllocZeroed` are complete.

Decision already made: keep 4.0 (`main`), PR #592 (5.0) does not merge, no budget waiver. Lift from 5.0 only: the stable radix sort (`Requests5.StableRadixSort`), the `LibraryError`/error-offset decoder shape, count-based tests, the row-range threading idea.

## Standing rules for every phase

- **R0 receipt (every phase, every PR):** `dotnet build tl.slnx -c Release` 0 errors and 0 new warnings; all suites at least Bake 865, Core 31, Packs 38, Gen.CSharp 47, Tlb 17, Fuzz 11, Analyzers 11, Systems 113, Python 99 (a lower count only where the PR lists the deleted tests by name); all 13 pinned hashes unchanged (3 core, 4 packs, 6 scenes, see `ask-senior-dev.md` section 4; note hash 6607956939547305625 is asserted only in tl.unity tests (`EventsPinned`), so Core changes need the Unity gate to prove it); `python benchmarks/source_budget.py` passes with the printed src number recorded in the PR; no assertion loosened (a changed assertion is listed with old and new text).
- Gate script: `bash D:\GitHub\_work\fin-gate-dotnet.sh <worktree> <tag>`; Unity: `bash D:\GitHub\_work\p6s-gate.sh <tag> full` and `TL_UNITY_PACKS=1|0 bash eng/test-com-tl-unity.sh`. One Unity process at a time.
- Performance receipts: same machine, same worker count on both sides, median of 5, first run discarded, state machine and worker count. A **second agent reruns every headline number** before the phase is called done. Label every number measured / projected / not tested.
- One phase = one issue = one claimed workstream (AGENTS.md claim process). Atoms are short: validate, commit, push, record on the issue, then the next.
- No code comments. Unmanaged data only on the runtime path. 0 B warm. No force push, no tags/releases, no closing issues (the owner closes). No budget waiver.
- **Kill rules are binding.** If a kill rule fires: stop, revert only that step, report the numbers on the issue. A hash that moves means the pinned behaviour wins and the divergence table goes to the owner.
- **Public API changes** need the owner's approval (one recorded line on the parent issue). Until it is recorded, work on a branch and open a PR; do not merge to `main`. API-free sub-steps may merge once R0 is green and a second agent has reviewed.
- Anything an agent cannot see (Unity side) goes on the issue as a question; nothing is assumed.

## Order and parallelism

```
0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 8
          \-> 7b, 7c, 7d (generator-only, after 2)  -> 7a (needs 3) -> 8
U (Unity questions U1..U11) runs in parallel with everything and must be answered before 6 and 7a merge.
```

Phases 3, 4, 5 touch overlapping files (`KindStore`, `KindTable`, `PlaybackModel`): do them one after another. Phase 7b/7c/7d only touch `src/Tl.Gen.CSharp` and may run in parallel with 3 to 6.

## Phase 0. Truth gate (no source change)  -- DONE 2026-11-11, `docs/senior-review/baseline-main.md` (8b872ad8)

- [ ] Build `main` from a fresh clone; record suite counts, the budget lines, where the 13 pinned hashes are asserted.
- [ ] Spot check 5 rows of plan Finding 1 against the real files; record paste-loss or real. Any real row outranks everything and becomes its own fix with a count-based regression test (append N+1 items, read back all N+1).
- [ ] Baselines on `main` (measured, median of 5): `--timelinebench` 4M x 4 and 1M x 1 tick-only; setup at 16M playbacks.
- [ ] New baseline (a): native bytes of `PlaybackTable` (and `KindTable`) for 40,000 entities with 4 playbacks plus ONE entity with 150, against the same without it. Reviewer expects about 81.9 MB vs 1.3 MB.
- [ ] New baseline (b): 1,000-timeline library, 2,000 entities, 40 to 150 active each: tick time and ns per active visit. If it cannot be built honestly, report NOT TESTED with the blocker.
- [ ] Fact-check: (i) the three playback state machines disagree on rewinding a non-looping `Next`/`Hold` playback off tick 0 (`PlaybackRules.Advance`, `KindRules.TickSlot`, 5.0 `World.StepSoloOnce`); (ii) 5.0 feeds a two-float kind zero; (iii) `RequestQueue.Sort` and `Events.Drain` are insertion sorts, 15,000 descending requests.
- [ ] Commit `docs/senior-review/baseline-main.md` with every number and the commit hash.
- **Kill rule:** none. This phase only measures.

## Phase 1. Close 5.0, keep three ideas

- [ ] Do not merge PR #592. Leave the branches `feat/591-integration` and `feat/29-burst-world-parity` in place as reference (do not delete).
- [ ] Record the lift list on #591: stable radix sort (into `RequestQueue`, Phase 5f), `LibraryError`/offset shape (Phase 3b), count-based test style.
- [ ] Post the consequence on #591: TLIB v3 and `Run(World, Direction)` never ship publicly.
- Bytes: 0. Hashes: none. Receipt: R0 on `main` unchanged.

## Phase 2. Delete what 4.0 no longer reaches (one commit per type)

Core:
- [ ] `PairTable`, `TimelineRef.ExecuteDispatch`, `ExecuteWindow`, `Resolve`, `Uses`, `PairOf` (`Data.cs`).
- [ ] `InternTable.cs` / `TimelineTable`; `BankView.cs`; `Lane.cs` (`SlotView`, `LaneSegment`, `LaneEncoding`); `Checked.cs` except `Fail` members still referenced; `Inspection.cs` except what `tlb inspect` reads.
- [ ] Keep: `NativeHeader`, `NativePair`, `NativeStage`, `NativeStep`, `SlotRow`, `TimelineRef.Validate`, `Keying`, `TypeKey<T>`.

Generator:
- [ ] `BakeReader.cs`; `TypeAnalyzer.Bake`, `TryReadMethod`, `Signature`, `Format`; `JobReader.Bakes`; `BakeDeclaration`, `BakeParameter`, `BakeModifier`; `TypeAnalyzer.Definition` and the slot model; `UnityJobEmitter` (make `GeneratorCli` call `JobEmitter.Emit` for both backends; output is already identical).
- [ ] Keep pairing discovery and the `TlConsumerLayout` attribute emit (the baker reads it).

- [ ] First, `grep -rn "PairTable\|TimelineTable\|IBake\|LaneEncoding\|SlotView" --include=*.cs .` including `Tl.Bake`, `Tl.Unity.Bake`, `Tl.Gen.Tlb`, `Tl.Bake.Oracle`, `tl.unity` vendored Core, and record every non-test caller on the issue before deleting.
- **Public API change (owner approval needed):** remove `TimelineWorld.Reference(ushort)`, `TimelineWorld.IsLive(ushort)`, static `TimelineWorld.Default`; `Inspection` shrinks; diagnostics TLGEN66 to TLGEN81, TLGEN70 to TLGEN74, TLGEN87 to TLGEN89 stop. Regenerate `PublicApi.approved.txt` for review.
- Bytes: -45,000 to -70,000 [guess]. Hashes: none. Receipt: R0 and the grep returns only test files deleted in the same commit.
- **Kill rule:** a deletion that breaks a non-test project is reverted; report the type and its caller. Do not move it into tools (shipped-tools has 362 B headroom).

## Phase 3. One motion rule, and a decoder that cannot half-fail

3a. One transition function
- [ ] `PlaybackRules.Move(ref Playback, LibraryTimeline*, int, bool forward)` returning `Motion { Stayed, Stepped, Wrapped, Handed, Parked, Held, Removed, Unknown }`.
- [ ] `PlaybackRules.Advance` becomes `Move` plus a map to the existing public `AdvanceResult`.
- [ ] `KindRules.TickSlot` keeps request/stop handling, then calls `Move` and emits kind events from `(before, after, motion)`.
- [ ] Keep the `TickEntity` fast path.
- [ ] A paused slot stops keeping its entity in `_playing` (resume re-marks it through `ApplyRecord`).
- [ ] Canonical answer for backward exit: `PlaybackRules.Advance` (never follows `Next`; `Hold` stays at tick 0).
- [ ] New exhaustive count-based law test: 256 flags x ticks {0, mid, last} x loop x hold x next x rate {0, 8, 32} = 18,432 cases; `Move` returns a defined `Motion`, tick stays in `[0, duration)`, and the three former implementations agree except where the divergence table lists them.
- Bytes: about -3,000 [guess].
- **Kill rule:** if a pinned hash moves, the pinned behaviour wins: `Move` reproduces the `TickSlot` answer, the differential test stays red-listed for that case, and the divergence table goes to the owner.

3b. Decode before allocate
- [ ] Internal `LibraryError` and pure `Check(ReadOnlySpan<byte>, out int offset)` validating manifest, every blob, every step's pool indices and the kind section before the first allocation; `LoadLibrary` builds with no throw paths (removes the eight `Free(); throw` pairs and the duplicated pool-bound checks in `MaterializeSteps`).
- [ ] `Tl.Fuzz` target: random mutations of a valid library either return an error or load; native allocation count before and after a failed load is equal.
- Bytes: about -2,000 [guess]. **Kill rule:** any exception message pinned by a snapshot is reverted to the old text.

## Phase 4. Per-entity capacity classes (#568)  (needs owner API approval to merge)

- [ ] Keep `Playback` at 8 B, fields unchanged (`TlStateHash.Slots` hashes the raw bytes).
- [ ] `PlaybackView` gains `Spilled` bitset, `Blocks` arena, `RunOf(entity)`; `SlotOf` stays correct. An entity needing more than `SlotsPerEntity` moves to a power-of-two block (up to 256) in a shared arena; first 4 B of its inline run hold the block offset; per-size free lists; it returns inline only when its count fits again.
- [ ] Delete `PlaybackTable.Grow()`; `Slots.TryAdd`, `Reserve`, `SlotsOf` move one entity's run; rated `Reserve` becomes per entity.
- [ ] `KindTable`: `_capacity` and `_heap` become a `Spilled` bit per row with the heap pointer in the first 8 B of the inline run; rows with `Inline == 0` keep a dense pointer array.
- [ ] Align the slot block to 64 bytes (Phase 6 needs it).
- [ ] Count-based memory test: 40,000 entities with 4 playbacks plus one with 150. Assert native bytes at most 1.6 MB (main: 81.9 MB expected, confirm with Phase 0 baseline).
- [ ] Rated test: one entity at 2x with 4 playbacks leaves every other entity untouched.
- Bytes: about +5,000 [guess]. Hashes: none expected.
- **Kill rule:** tick-only median at 4M x 4 worse than `main` by more than 3%; first hoist the two base pointers and keep the branch on the bit only.

## Phase 5. Every per-tick loop follows visits (each sub-step independent, revertable)

- [ ] 5a `TimelineWorld.Advance` walks `_playing` words; clear a bit only when no slot still moves, is parked or is stopping.
- [ ] 5b Emitted legacy loops (`EmitLegacyLoop`, `EmitFusedKernel`) use the same bit scan instead of `for __e < __n`.
- [ ] 5c Dense dispatch: `SystemSteps` with `Bind(TimelineWorld, ulong system, ReadOnlySpan<ulong> pairKeys)`, native, freed in `Free()`; delete `SystemRegistry.Filters` and the body of `SystemDispatch.FilteredSteps`.
- [ ] 5d `WindowSteps` record (16 B) replacing `_firstStage`, `_stageCount`, `_stepArrays`; `StepOf` returns `Steps[0]` when `StageCount == 1`.
- [ ] 5e Marker tables move onto the world (native array by timeline id); the two public static methods stay as forwards.
- [ ] 5f (measured on main: 15,000 requests in descending entity order sort in 79 ms, 0.037 ms ascending) Stable O(n) sorts: `RequestQueue.Sort` = LSD radix (lift `Requests5.StableRadixSort`) over `(entity, slot)` with owned scratch and an already-sorted exit; `Events<T>.Drain` and `SortEvents` = bottom-up stable merge.
- [ ] 5g `KindStore.Build` one pass per owner (no `List<int>`, `ToArray`, `int[] rowIn`); `ClearPulses` walks `Active` words.
- [ ] Probe counter under `TL_CHECKED`: 1,000,000 entities, 10 playing: one `Advance` and one legacy `Run` each report 10 (main: 1,000,000). Sort test: 15,000 descending requests apply in the same order as `main` with at most 4 x 15,000 moves.
- Public API change (owner approval): `SystemDispatch.FilteredSteps` replaced by `Bind`; `SystemSteps` new (generated-code only).
- Bytes: about +2,000 [guess]. Hashes: entities still ascending, slots in slot order; any hash movement reverts that sub-step.
- **Kill rule:** 5c reverted if the #540 A-shape improves by less than 1.5x (byte-neutral, revert costs nothing).

## Phase 6. Row-range workers  (needs Unity answers U3, U4, U5 before merge)

- [ ] `KindRules.TickEntity` callers and emitted `Kind_i` / legacy loops take `[firstWord, endWord)`; sequential entry points call the full range.
- [ ] New public `sealed unsafe class TlWorkers : IDisposable` (instance-owned, no static state, .NET only): `Run(delegate*<void*,int,int,int,void> body, void* state, int* chunkEnds, int chunks)`, threads created once, semaphore wait after a short bounded spin, no delegates/closures/arrays per run.
- [ ] Split: one popcount pass cuts `workers x 8` chunks of equal set-bit count; chunks claimed with one `Interlocked.Increment`.
- [ ] Requests and events raised in a parallel region go to per-chunk segments, concatenated in chunk order, `Sequence` renumbered from the queue base. `ApplyRequests` and `MergePulses` stay sequential.
- [ ] `KindTable.Append` stops writing `MaxLength`/`Overflows` (stats become a cold scan in `GetStats`).
- [ ] A parallel `Run` overload only when no consumer takes a context by `ref`; otherwise a diagnostic naming consumer and parameter, system runs sequentially.
- [ ] Spin gate around the spill block allocator.
- [ ] Receipts: all 13 hashes at worker counts 1, 2, 3, 8, 11; `GC.GetTotalAllocatedBytes(true)` delta across 1,000 warm ticks at 8 workers equals 0; 4M x 4 tick-only at 8 workers no slower than single-thread / 4 (do not expect better than about 11.1 ms on the reference machine [derived]).
- Bytes: about +7,000 [guess].
- **Kill rule:** under 2x over one worker at 8 workers on 4M x 4, or any hash that depends on worker count: the pool does not merge. The range kernels stay (Unity needs them).

## Phase 7. Generator: fewer bytes, real incrementality

- [ ] 7b Delete `NewStyle.Rewriter`; `SystemReader` learns `TlKindAttribute` == kind attribute and a first parameter `TlFrame<C>` == frame of `(TlNoSettings<C>, C)`.
- [ ] 7c Per-type incremental models: `ForAttributeWithMetadataName` for `TlKind`, `TimelineKind`, `TimelineSystem`, `TlState`, `Feeds`, `Pulse` plus a syntax predicate for `ITlTrack<>`; assignment runs on the collected equatable models. Test: editing an unrelated method body leaves each per-type step `Cached`/`Unchanged` (tracking name `Tl.Type`).
- [ ] 7d `Identity.cs` (FNV-1a, stable-name grammar, layout grammar over plain strings) compiled into Core and the generator by source link; reflection and Roslyn supply adapters. Twelve golden types (nested, generic, enum field, fixed buffer, explicit layout) give the same `ulong` from both adapters and equal the published conformance-vector keys.
- [ ] 7a (after Phase 3, needs U6) Move the kind loop into Core: `IKindBody<T>` + `KindLoop.Run<T,TBody>(in KindPass, ref TBody, int firstWord, int endWord)`; the generator emits only the body struct. The four pack tests call the generated `TlAuto_Tl_Pack_*.Run` again. Explain every emitted-snapshot diff in the PR.
- Bytes: about -12,000 [guess]. Hashes: none expected.
- **Kill rule:** 7a dropped if Burst/Mono fails to specialise the struct body (Unity team checks one kind in the Burst inspector: no interface call, no boxing). 7d dropped if any golden key differs from a published vector key: report the type and both strings.

## Phase 8. Developer experience  (needs owner API approval to merge)

- [ ] 8a Derived kinds `Tl.Sum<T>`, `Tl.Set<T>`, `Tl.While<T>` (rules in plan section 7); generator errors for a non-numeric field in `Sum<T>` and for a column with two writers.
- [ ] 8b `Reset`: `RequestOp.Reset`, `PlaybackTable.Reset(int entity)`, `Requests.Reset()`: zero the entity's slots, purge its kind rows, drop its queued game pulses, add nothing to the closing list. `StopAll` unchanged. This is the fix for #594.
- [ ] 8c Warning for a clip or column field never connected (#593): a normal Roslyn warning, paid for by Phase 2 generator deletions.
- [ ] 8d `TimelineWorld.Find(string name)` binary search of the sorted manifest; generated timeline id constants become optional; a clean clone builds with no `.tlib` on disk.
- [ ] 8e One host-parity fixture (library, request script, expected column bytes per tick) run by both hosts, including `Idle`, `Idle` fed from another assembly, and rewind through an assigning `Idle`.
- [ ] Receipts: `demos/hello-side` rewritten with `Sum<Teleported>` gives the same column bytes per tick as today; the silent `SideSwing` version triggers the warning; `Reset(row)` then `Start` in one step gives the new occupant zero pulses from the old one (#594).
- Public API additions (owner approval): `Sum<T>`, `Set<T>`, `While<T>`, `RequestOp.Reset`, `PlaybackTable.Reset`, `Requests.Reset`, `TimelineWorld.Find`.
- Bytes: about +9,000 [guess]. **Kill rule:** 8a dropped if the synthesised `Sum<T>` kind is not behaviourally byte-identical to the hand-written demo kind across 600 ticks.

## Unity workstream U (tl.unity; answer each yes / no / what exists, with evidence)

- [ ] U1 Language level of the vendored Core (plan Finding 13: paste uses C# 10 to 12). Name the step that compiles it (csc.rsp, langversion) and add a CI/local leg that compiles Core for netstandard2.1 at that level.
- [ ] U2 Per-entity playback capacity: `IKindRows.Reserve` never moves another entity's slots.
- [ ] U3 Rows processed ascending within a chunk, one batch per chunk, no kernel writes outside its chunk.
- [ ] U4 What key defines "row" for cross-host parity in the Unity tests, and how requests/events raised from jobs are concatenated in .NET row order.
- [ ] U5 Burst strict float mode and standard precision on every kernel and generated kind body (no FMA, no reassociation); prove with a parity test.
- [ ] U6 One generated `IKindBody<T>` checked in the Burst inspector (no interface call, no boxing).
- [ ] U7 Zero-filled native buffers handed to Core.
- [ ] U8 How marker payloads resolve in Burst today (the .NET path is a static managed registry).
- [ ] U9 Queued game pulses for a destroyed entity are dropped, matching `Reset`.
- [ ] U10 Run the 8e parity fixture in PlayMode (column bytes per tick, three `Idle` cases).
- [ ] U11 1 worker and N workers give the same hash.
- [ ] U12 Re-sync the vendored Core and bake tools after each Core phase that lands on `tl` `main` (repo sync scripts), run the Unity gates, push.
- Fallback if U4 or U5 is "no": cross-host bit-exactness is limited to shapes where requests and events are not raised from jobs, and to arithmetic without multiply-add. Record it on the issue.

## Cross-cutting

- [ ] Add the memory arm to the replacement rule in `AGENTS.md` and the skill: a change may land if it gives at least 2x less memory in a named shape with speed within 3% (plan section 9). Phase 4 is the first user.
- [ ] After each phase: update `docs/budget-ledger`-style numbers on the issue with the measured src number, replacing the plan's `[guess]` figures.
- [ ] Open question for the owner, one line: approve the public API changes listed per phase (Phases 2, 4, 5, 8)? Recommended: yes.
- [ ] Not in this plan, still open: #588 TlText, #589 frame recorder, #535 CI toolchain pin, K6 issues #567 to #571 (reassess after Phase 5), `Idle` cross-assembly and rewind tests in Unity (covered by 8e), legacy consumer path retirement (owner decision, largest remaining emitter block).
