# Tl: senior review and one-shot plan

Answer to `ask-senior-dev.md`, written from the pasted `Tl.Core` and `Tl.Gen.CSharp` sources only. Nothing was run. Nothing was looked up.

## Number labels

- **[code]** read from the pasted source.
- **[derived]** my arithmetic, shown inline.
- **[briefing]** taken from your briefing, not verifiable by me.
- **[guess]** an estimate. Never a measurement.

## 0. What I could not see, and one thing you must check first

Not in the paste: `Tl.Bake*`, `Tl.Gen.Tlb`, the CLI, every test project, `benchmarks/` (including `source_budget.py`), `AGENTS.md`, the demos, and all of `tl.unity`. Every statement about those is marked.

**The paste is the 5.0 branch, not `main`.** It contains `World*.cs`, `Library.cs`, `LibraryWriter.cs`, `Playbacks.cs`, `Requests5.cs`, `Kinds.cs`, `Kernels.cs`, `Threads.cs`, `Clock.cs`, `MonoidClassifier.cs` [code]. Those are the files your briefing attributes to `feat/591-integration`.

**The paste does not compile as shown.** At least 25 sites are missing a statement or are syntactically broken (list in Finding 1). Your briefing says a fresh clone compiles. Code and briefing disagree, so by your rule 6 the code wins until someone runs Phase 0. Either the paste is lossy (every `namespace` line is gone, so this is likely) or the tree is broken. I treat each site as a defect until a build proves otherwise, because several of them (grow without copy, free inside a loop) would be silent memory corruption, not compile errors.

---

## 1. Verdicts

### (a) The real ceiling, per shape

The data model fixes the bytes. Speed claims below are bytes divided by bandwidth.

**Bandwidth of your test machine.** 5.0 `World` touches, per tick at 4M rows x 4 visits: 6 B per visit (`_visitFlat` int + `_visitPhaseMod` ushort) and 16 B per row (`_activeRows`, `_rowStart`, `_rowFlat`, `_acc`) read, plus 4 B per row written [code]. That is 16M x 6 + 4M x 16 + 4M x 4 = 176 MB [derived]. You measured 7.18 ms [briefing]. 176 MB / 7.18 ms = 24.5 GB/s [derived]. I use 24.5 GB/s as a lower bound for that machine. The 5.0 number is a memory number, not a compute number.

**Shape 1: millions of entities, 1 to 4 active, tick only (4.0 `KindStore.TickPass`).**
Per playing entity the fast path in `KindRules.TickEntity` reads `Counts[e]` (4 B) and each 8 B `Playback`, and writes 2 B of `Tick` back into the same line [code]. At 4M x 4: read 4M x 36 B = 144 MB, write back 128 MB of dirtied lines, total 272 MB [derived]. Floor = 272 MB / 24.5 GB/s = 11.1 ms = 0.69 ns per visit [derived]. You measured 18.25 ms = 1.14 ns per visit in Unity [briefing]. **Headroom at this layout: 1.64x [derived]. Not 100000x.**

**Shape 2: every visit produces a value into a column (the `Kind_i` loop).**
Private bytes per span visit: one entry (8 B header + value rounded to 4 B, so 16 B for two floats) plus the column write (8 B for two floats) [code]. 16M x 24 B = 384 MB, floor 15.7 ms = 0.98 ns per visit [derived]. Any claim under about 1 ns per value-producing visit at 4M x 4 on that machine contradicts this arithmetic. 5.0's 0.449 ns did less work: one float per row, not one value per visit.

**Shape 3: idle entities.**
4.0 `.NET` kind path already costs 1 bit per entity: `TickPass` scans `_playing` words [code]. 4M entities = 62,500 words = 0.5 MB per tick [derived]. That is the floor short of a hierarchical bitset. The legacy generated `Run` loop and `TimelineWorld.Advance` are not there: both walk every entity and read `Counts[e]` (4 B) [code], so 16 MB per system per tick at 4M [derived]. Fixable, see Phase 5.

**Shape 4: players with 40 to 150 active among millions with 1 to 4.**
This is where the large multiplier is, and it is memory. `PlaybackTable.Slots.TryAdd` calls `Grow()`, which doubles the stride for every entity [code]. One entity with 150 playbacks forces stride 256. 4M x 256 x 8 B = 8,192 MB, against 4M x 4 x 8 B = 128 MB actually needed [derived]. **Capacity classes give about 57x less memory in this shape (8,208 MB against 145 MB with counts on both sides, Section 5) [derived].** Rated playback makes it worse: `KindRules.TickRated` reserves `count + samples` slots through the same `Grow` [code], so one entity with 4 playbacks at 2x speed needs 12 slots, stride 16, and quadruples slot memory for all 4M entities [derived].

**The multipliers, honestly.**

| Owner ask | Verdict | Achievable |
|---|---|---|
| 100000x faster | Impossible for active visits | 1.6x tick-only, bounded by 0.69 ns/visit [derived]. Legacy dispatch path: larger, unmeasured, see Finding 6 |
| 100000x less memory | Impossible in the uniform shape: a playback is 8 B and needs at least 4 B (id + tick) | 57x in the mixed shape [derived]. About 1.1x to 1.2x in the uniform shape (Section 5) |
| 10000x more scalable | Meaningful only as "cost follows active visits" | Yes, after Phase 5 every per-tick loop is O(playing entities + entries), plus a 1 bit/entity scan |
| Idle entity costs 0 | 1 bit | Already true on one path, fixable on the other two |

### (b) Is 4.0's architecture the limit, or the implementation?

The implementation. The architecture is right: one shared baked library, 8 B of playback state per timeline per entity, per-tick event bitmaps so a tick with no clip edge does no table work, per-kind entry tables that exist only while a span is live. What costs you is six implementation choices, all local: a uniform slot stride that grows for everyone; two of three tick loops driven by capacity; a managed `Dictionary` probe per visit in the legacy path; three or four dependent loads plus a stage search per entry per tick in the kind path; three separate copies of the playback state machine that already disagree; and 14 B per kind row of side arrays that only overflowed rows use.

### (c) Is 5.0's design right?

No. Start from 4.0. Two reasons, both readable in the code.

**It accelerates only the easiest shape.** Time-as-function holds for a playback that loops, is not paused, runs at 1x and never seeks. Everything else is promoted to "solo" (`World.PromoteToSolo`) [code]. The owner's game is abilities: non-looping clips, `Next` chains, pause, seek, speed. That population lives in `StepSolo`, which copies a 24 B `Tlib3Timeline` twice and a 48 B `Tlib3Step` per step per tick through bounds-checked accessors (`AccumulateSoloAt`) [code]. Removal there rescans the whole cohort (`RemoveSoloTlKey`) [code].

**It does not implement the product.** `World` owns one `float[] _acc`, one float per row, shared by every timeline and every kind [code]. The emitted bridge feeds every kind from that single float: `LaneValue` returns `__acc[row]` for float, `(int)__acc[row]` for int, and for any other value type the emitter writes `__v = default` [code]. A two-float `Move` kind on 5.0 receives zero, silently. `BinderIdOf` maps every int kind to id 0 and every float kind to id 1 [code], so two float kinds in one system share one table. There are no pulses from clips, no markers, no `Each` mode, no groups, no `Idle`, no events. The 2.5x to 5.3x you measured compared a one-float lane sum against a typed kind pipeline, which your verifier already suspected.

It also breaks your own rules in the paste: managed arrays throughout `World`, `Dictionary<int, object>` and `List<Action>` for kinds, static mutable state and a closure in `ParallelApply`, a pinned managed blob and a finalizer in `Library` [code].

**What 5.0 got right, and 4.0 should take:**

1. Row-range threading with per-row canonical order. Adopt the idea, not `Threads.cs` (Section 6).
2. Precomputed value planes. 4.0 already has them: `MaterializeSteps` fills `FoldLane` per step per direction at load [code]. Nothing to port.
3. A total decoder: `Library.Verify` returns `LibraryResult` with an error code and offset instead of throwing [code]. Adopt the shape for `LoadLibrary` (Phase 3).
4. A stable O(n) radix sort for requests (`Requests5.StableRadixSort`) [code]. Port it into `RequestQueue` before deleting the file.
5. Count-based tests instead of timing ratios.

**Start from 4.0, 5.0, or a third design?** 4.0, with the changes in Section 3. A third design would repeat #591: a second runtime inside a budget that has room for one.

---

## 2. Findings from reading the code (ranked)

### Finding 1. The paste has 25+ sites that cannot be correct as shown [code]

Verify each against the tree in Phase 0. If any is real, it outranks everything below.

| Site | What is missing | Consequence if real |
|---|---|---|
| `PlaybackTable.Grow` | the per-entity copy; the `for` now owns `AlignedFree(_slots)` | frees `_slots` once per entity, loses all playbacks |
| `RequestQueue.Grow`, `ClosingList.Grow`, `Events<T>.Grow`, `KindTable.Grow` | copy of old contents | every 17th request, 9th close, 17th event and every inline overflow reads uninitialised memory |
| `KindStore.Rebuild` | copy of the queued value into `value` | pulses queued across a reload arrive as zeros |
| `TimelineWorld.MaterializeSteps` | copies from `trackSrc`, `firstSrc`, `secondSrc`, and of marker payloads | every `FrameRow` and marker value is zero |
| `TimelineRef.Validate`, `TimelineWorld.BuildTimeline` | `next = ...Slice(52)` (`LibraryLayout.TlbNextAt` is 52) | `Next` is always 0: chaining never happens, the loop/next check is dead |
| `TimelineTable` static constructor | zeroing of `_arena` and `_motion` | `Find` and `TryLive` read garbage |
| `TimelineTable.PushGraveyard`, `DrainGraveyard` | node fields; `kept` declaration; `node = current->Next` | does not compile; if it did, an infinite loop |
| `LibraryAssembler.Emit`, `SetBy`, `Plan` | `names[i] = ...`, `name = ...`, `names.Add(...)` | null dereference on `names[i].Length`; manifest has no names |
| `LibraryWriter.Write` | two `for` bodies and the name loop body | does not compile |
| `World.Systems.PlaybackIndex.EnsureCapacity` | `nk[h] = k` and the two value writes | index is empty after growth |
| `World.TryGetPlaybackByChannel` | first constructor argument | does not compile |
| `Inspection.Tables` | nested constructors and at least one method | does not compile |
| `SystemReader.StableName` | `nested.Push(...)` | every type in a namespace gets the same key |
| `SystemReader.FrameRootedOk` | `names.Add(...)` | `names` stays empty, so `names[-1]` throws inside the generator |
| `JobEmitter.Consumers` | the body of the emitted `Install` | consumers are never installed (see Finding 9) |
| `GeneratorCli.Compilation`, `TimelineIncrementalGenerator.Produce`, `.Location` | parts of expressions | does not compile |
| every file | the `namespace` line | does not compile |
| `NativeAlloc.AllocZeroed` (non-NET) | the clear | returns dirty memory on netstandard2.1 and Unity |
| `UPoly.InitBlock` (non-NET) | uses `value` | always writes 0 |

Two of these look like real bugs rather than paste loss, because they are whole-statement omissions that still compile: `NativeAlloc.AllocZeroed` and `UPoly.InitBlock`. `TimelineTable.NewRestView` depends on the first.

### Finding 2. Three playback state machines, and they disagree [code]

`PlaybackRules.Advance`, `KindRules.TickSlot` and `World.StepSoloOnce` each implement "move one tick". A fourth lives in `Playbacks.Advance` over `Clock.Step`. A fifth is the prelude the generator emits in `EmitFusedKernel`.

They differ when a non-looping playback exits **backward** (global rewind on an un-reversed slot, tick 0, `stepDir < 0`):

| | `Next != 0` | `Hold != 0` |
|---|---|---|
| `PlaybackRules.Advance` | ignores `Next`; removes, or holds | stays at tick 0, `Ended` |
| `KindRules.TickSlot` | hands over to `Next` at `next.Duration - 1` | jumps to `duration - 1`, `Ended` |
| `World.StepSoloOnce` | hands over to `Next` at its last tick | tick 0, held |

Rewinding past the start of A lands at the end of A's successor in two of three paths. A world with a `KindStore` and a world without one give different ticks for the same requests. This is a float-order and hash hazard that no pinned hash catches unless a pinned scene rewinds through a `Next` or `Hold` boundary. I cannot see whether one does.

Also in `TickSlot`: a `Paused` slot returns `true` [code], so a paused entity stays in `_playing` and is visited every tick until resumed.

### Finding 3. Uniform slot stride [code]

`PlaybackTable.Grow` is global. Arithmetic in Verdict (a), shape 4. Per-entity bytes today: 4 slots x 8 B + 4 B count + 1 bit = 36.125 B [derived]. `Reserve` (rated sampling) goes through the same global growth.

### Finding 4. Loops that scale with capacity, not with visits [code]

| Loop | Scales with | When |
|---|---|---|
| generated `Run` legacy loop (`EmitLegacyLoop`: `for __e < __n`) | entities | every tick, per system |
| generated `RunFused` | entities | every tick, per system |
| `TimelineWorld.Advance` | entities x slots | every tick |
| `KindStore.ClearPulses` | kinds x rows | every call |
| `KindStore.Build` | owner kinds x entities, several passes, plus `List<int> members`, `members.ToArray()`, `int[] rowIn` | setup and every reload |
| `KindStore.Rebuild`, `SyncPlaybacks` | entities x slots | reload |

`KindStore.Build` at 4M entities allocates 16 MB (`rowIn`) + at least 16 MB (`members`) + 16 MB (`ToArray`) of managed memory per owner kind [derived], on top of the native blocks.

### Finding 5. Setup cost is bytes first-touched [derived]

At 4M entities, 4 slots, one two-float kind with inline 4: slots 128 MB, counts 16, mask 32, offset 16, rows 16, table `Entity` 16, inline 256, `Length` 8, `_capacity` 8, `_heap` 32, managed temporaries 48. Total 576 MB, each byte written once by `Clear` or a fill loop [derived from `PlaybackTable` constructor, `KindStore.Build`, `KindTable.Create`]. Your 492 to 537 ms [briefing] is 1.1 GB/s [derived], which is page-fault speed, not memset speed [guess]. So setup falls only by touching fewer bytes. `_capacity` and `_heap` are 10 B per row that only overflowed rows read. `Entity` is 4 B per row that is the identity map whenever every entity reaches the kind.

### Finding 6. Legacy path: a managed dictionary probe per visit [code]

`EmitLegacyLoop` emits, inside the per-entity, per-slot, per-crossing loop, a call to `SystemDispatch.FilteredSteps`. Each call does `registry.RefreshModules()` (a volatile read), a four-field view comparison, builds a `FilterKey`, and probes `Dictionary<FilterKey, SystemStep[]>`. The result is a managed array. That is the "dispatch floor" of #540. It is also managed data and a managed lookup on the runtime path, against your rule. The fix is a dense table indexed by `(timeline, stage)` built once per library generation: two loads [derived].

### Finding 7. Kind path: the per-entry lookup chain [code]

For each span entry each tick, emitted `Kind_i` does: `__routes[Feeder]`, `SlotOf`, `GetTimeline`, then `KindStore.StepOf`, which reads `_windowBase[t]`, `_stepArrays[g]`, runs `LibraryViewReader.StageAt` (linear up to 4 stages, binary above), reads `_firstStage[g]`, `_stageCount[g]`, `arr[s]`. Then `Frame_c` copies the track and clip out of `FrameRow`, and `new Frame(in track, in clip, ...)` copies both again because `Frame` holds them by value. Three arrays indexed by the same `g` are three cache lines where one 16 B record would do. A window inside one stage (the common case) needs no stage search at all. This is #567 without growing the entry.

### Finding 8. Static mutable state and locks on warm paths [code]

- `MarkerTableRegistry`: a static `ConditionalWeakTable`, a `lock`, and a `Dictionary` lookup per marker fire, called from inside emitted loops (`EmitMarkers`, `EmitKind`).
- `TlGenerated`: static module list behind a `lock`; `Modules` allocates an array per read.
- `PairTable`, `TimelineTable`: process-global tables with spin gates.
- `KindStore.Join` and `IndexOf(string)`: called per kind per tick from `Kind_i`. `Join` hashes a `(string, int)` tuple key each call.
- `KindTable.Append` writes `MaxLength` and `Overflows`, two shared counters, on every append. Under threads that is a race and false sharing.

### Finding 9. The generator carries an inert pipeline [code]

`JobEmitter.Consumers` takes `bakes` and never reads it. Its emitted `Install` has an empty body. So `TypeAnalyzer.Definition` (slots, live columns, TLGEN66 to TLGEN81), the whole `Bake` analysis (TLGEN70 to TLGEN74, TLGEN87 to TLGEN89), `BakeReader`, `BakeDeclaration` and `JobReader.Bakes` produce diagnostics and no code. `Tl.IBake<T>` is not defined in the pasted Core; if it is defined nowhere, `TypeAnalyzer.Bake` always returns null. What still matters from that path is the `TlConsumerLayout` assembly attribute, which #400 says the baker reads [briefing].

Consequently `PairTable.Install` has no generated caller, and `PairTable`, `TimelineRef.ExecuteDispatch`, `ExecuteWindow`, `Resolve`, `Checked`, `LaneEncoding`, `SlotView` and most of `Inspection` are unreachable from 4.0's public path as pasted. I cannot see whether `Tl.Bake` or tests reach them through `InternalsVisibleTo`.

### Finding 10. The incremental generator is not incremental for systems [code]

The `Tl.Systems` pipeline combines with `CompilationProvider`. On every edit it runs `NewStyle.Prepare`, which rewrites and replaces every syntax tree (to turn `[TlKind]` into `[TimelineKind]` and `TlFrame<C>` into `Frame<TlNoSettings<C>, C>`), builds new semantic models, then `SystemReader.Read` walks every type in every tree again. The fingerprint comparer stops re-emission, not recomputation. Cost per keystroke is proportional to the whole compilation.

### Finding 11. Two definitions of one identity, in two languages [code]

- Stable type name: `Keying.StableName(Type)` at run time and `SystemReader.StableName(ITypeSymbol)` in the generator. Reflection `Type.Name` carries the arity suffix (`` `1 ``); Roslyn `Name` does not. For generic track or clip types the two keys cannot match unless the missing line compensates.
- Layout digest: `PairLayoutDigest.Fold` (fields sorted by name, primitive widths, layout header) and `PairLayout.Fold` (declaration order, `name:type`). These produce different strings for the same type. If one is ever compared with the other, the layout guard of #376 either always fires or never does.
- FNV-1a is written out eight times: `Keying`, `PairLayoutDigest`, `PairLayout`, `SystemReader.StablePairKey`, `StableKey`, `TlStateHash`, `World.StateHash`, `PairTable.DigestOf`.

### Finding 12. Quadratic sorts on input-sized data [code]

`RequestQueue.Sort`, `Events<T>.Drain` and `TimelineWorld.SortEvents` are insertion sorts. 100 players spawning with 150 starts each is 15,000 requests; in descending entity order that is 15,000^2 / 4 = 56M moves of a 20 B record [derived]. A timeline with 10,000 clips sorts 20,000 events at load: 10^8 comparisons [derived].

### Finding 13. Core does not hold the language level the briefing states [code]

The briefing says Core compiles as C# 9 inside Unity. The paste uses collection expressions (`= []`, `return []`), semicolon type bodies (`interface IHostedConsumer;`), `global using`, and `using unsafe X = delegate*<...>` aliases in `Data.cs`. Those are C# 10 to 12. Either the sync script down-levels, or the vendored asmdef uses a newer compiler, or the constraint is not enforced. Tell me which; today nothing in the repo I can see pins it.

### Finding 14. Smaller hazards [code]

- `PlaybackRules.SetSpeed`: `Rate == 0` means 1x, so a request for speed 0 silently plays at 1x.
- `TlCurve` indexer reads and writes its fixed buffer unchecked.
- `TimelineRef.Validate` mixes `MemoryMarshal.Read` (host endian) with `BinaryPrimitives` (little endian), and still allocates one delegate per call (`Action<string> fail = Fail`), which #385 and #387 claim removed.
- `TimelineWorld` has a finalizer that frees memory `KindStore` holds raw pointers into (`_tls`, `_sec`).
- `Events<T>.Raise` always writes entity version 0.
- `NewStyle.FeedKey` is a 31-bit hash of the assembly name used as a routing key; two assemblies that collide merge silently in `KindStore._joined`.
- `KindTable.RemoveSpan` is a linear search plus `memmove`. It must stay ordered: swap-remove would change combine order and move float hashes. Leave it.

---

## 3. The one-shot plan

Base is `main` (4.0). Budget arithmetic: `src` is 487,962 of 520,000 on `main` [briefing], so headroom is 32,038 B [derived]. Every byte estimate below is a **[guess]** until `benchmarks/source_budget.py` prints the number; the rule is that a phase merges only if the script passes after it. The plan is net negative, so no waiver is requested.

Every phase carries the same standing receipt, called **R0**:

- `dotnet build tl.slnx -c Release`: 0 errors, 0 new warnings.
- `dotnet test tl.slnx -c Release --no-build`: suite counts at least Bake 865, Core 31, Packs 38, Gen.CSharp 47, Tlb 17, Fuzz 11, Analyzers 11, Systems 113, Python 99 [briefing]. A lower count is allowed only where the phase lists the deleted tests by name.
- All 13 pinned hashes in the briefing, byte-identical.
- `python3 benchmarks/source_budget.py`: PASS, with the printed `src` number recorded in the PR.
- No assertion loosened. A changed assertion is listed in the PR body with the old and new text.

Performance receipts use your AGENTS.md rule: same machine, same worker count, median of 5, second agent reruns.

### Phase 0. Truth gate (no source change)

- **Do:** build `main` and the pasted branch from fresh clones. For each row of Finding 1, open the file and record "paste loss" or "real". Record baselines on `main`: budget numbers, suite counts, the 13 hashes, `--timelinebench` medians for 4M x 4 and 1M x 1, setup time at 16M playbacks.
- **Add two baselines nobody has:** (1) 1,000-timeline library, 2,000 entities, 40 to 150 active each, tick time; (2) native bytes held by `PlaybackTable` for 40,000 entities with 4 playbacks plus one entity with 150. Expected on `main` for (2): 40,001 x 256 x 8 = 81.9 MB [derived].
- **Receipt:** a committed `benchmarks/baseline-main.md` with every number and the commit hash.
- **Kill rule:** if any Finding 1 row is real, stop. Each real row becomes its own fix with a count-based regression test (append N+1 items, read back all N+1) before anything else proceeds.

### Phase 1. Close 5.0, keep three ideas

- **Do:** do not merge #592. Before the branch is abandoned, lift into `main`: the stable radix sort from `Requests5.StableRadixSort` (into `RequestQueue`, Phase 5), the `LibraryError` enum and error-offset shape from `Library.cs` (Phase 3), and the count-based test style.
- **Bytes:** 0 against `main`. Against the branch: about -182,438 [briefing].
- **Hashes:** none.
- **Receipt:** R0 on `main` unchanged.
- **Consequence to say out loud:** `World`, `Library`, `LibraryWriter`, `Playbacks`, `Requests5`, `Kind<T>`, `KindTable<T>`, `KindEntries<T>`, `Kernels`, `ParallelApply`, `Clock`, `Row`, `Direction`, `Edge`, `TickReport`, `Pipeline`, `KindPulse<T>`, TLIB v3 and the generated `Run(World, Direction)` overload never ship. The Unity `BurstWorld` stays an experiment on its branch.

### Phase 2. Delete what 4.0 no longer reaches

One commit per type, so any single deletion can be reverted alone.

- **Core, delete:** `PairTable` and `TimelineRef.ExecuteDispatch`, `ExecuteWindow`, `Resolve`, `Uses`, `PairOf` (all in `Data.cs`); `InternTable.cs`; `BankView.cs`; `Lane.cs`; `Checked.cs` except any `Fail` member still referenced; `Inspection.cs` except what `tlb inspect` reads. `BurstDiscard.cs` stays: `Events<T>.Grow` uses the attribute [code].
- **Core, keep:** `NativeHeader`, `NativePair`, `NativeStage`, `NativeStep`, `SlotRow`, `TimelineRef.Validate`, `Keying`, `TypeKey<T>`.
- **Generator, delete:** `BakeReader.cs`; `TypeAnalyzer.Bake`, `TryReadMethod`, `Signature`, `Format`; `JobReader.Bakes`; `BakeDeclaration`, `BakeParameter`, `BakeModifier`; `TypeAnalyzer.Definition` and the slot model (`TimelineSlot`, `SlotMode`, `JobDefinition` reduced to a type name); `UnityJobEmitter.cs` (it calls `JobEmitter.Consumers` with id `""` and nothing else [code]).
- **Generator, keep:** pairing discovery and the `TlConsumerLayout` attribute emit, because the baker reads it [briefing, #400].
- **Public API change (owner decision):** `TimelineWorld.Reference(ushort)`, `TimelineWorld.IsLive(ushort)` and the static `TimelineWorld.Default` go. `Default` is static mutable state by your own rule. `Inspection` shrinks. Diagnostics TLGEN66 to TLGEN81 and TLGEN70 to TLGEN74, TLGEN87 to TLGEN89 stop being reported.
- **Bytes:** -45,000 to -70,000 [guess]. Basis: `Data.cs` is 28 k and `TypeAnalyzer.cs` 23 k [briefing]; roughly 60% of each goes, plus five smaller files.
- **Hashes:** none; the code is unreachable from `TimelineWorld`.
- **Receipt:** R0, plus `grep -rn "PairTable\|TimelineTable\|IBake\|LaneEncoding\|SlotView" --include=*.cs .` returns only test files that are deleted in the same commit, listed by name.
- **Risk:** `Tl.Bake`, `Tl.Unity.Bake`, `Tl.Gen.Tlb`, `Tl.Bake.Oracle` see Core internals [code: `AssemblyInfo.cs`]. I cannot see them.
- **Kill rule:** a deletion that breaks a non-test project is reverted, and that type is reported back with its caller. Do not move it into tools: shipped-tools has 362 B of headroom [briefing].

### Phase 3. One motion rule, and a decoder that cannot half-fail

**3a. One transition function.** In `PlaybackRules.cs`:

```csharp
public enum Motion : byte { Stayed, Stepped, Wrapped, Handed, Parked, Held, Removed, Unknown }

public static Motion Move(ref Playback slot, LibraryTimeline* timelines, int timelineCount, bool forward)
```

- `Advance` becomes `Move` followed by a three-way map to the existing public `AdvanceResult`.
- `KindRules.TickSlot` keeps its request and stop handling, then does `var before = *s; var motion = PlaybackRules.Move(ref *s, ...)` and emits kind events as a function of `(before, *s, motion)`: `Stepped` calls `Events`; `Wrapped` calls `WrapRemove` then `Events`; `Handed` calls `RemoveActiveSpans(before)` then `Begin`; `Parked`, `Held`, `Removed` call `RemoveActiveSpans(before)`.
- The fast path at the top of `TickEntity` stays as it is. It is the same rule specialised to `flags in {None, Reverse}` and `Rate == 0`.
- A paused slot no longer keeps its entity in `_playing`: `TickSlot` returns `false` for `Paused`. A resume request already re-marks the entity, because `ApplyRecord` calls `SetPlaying(entity, true)` for every request [code]. Live spans of a paused slot stay in their kind tables, which are scanned through `Active`, not `_playing` [code].
- **Canonical answer for the divergence in Finding 2:** `PlaybackRules.Advance`. A backward exit never follows `Next`, and `Hold` stays at tick 0. Reason: forward handover lands on tick 0 of the successor, so its inverse would need a predecessor link the format does not have.
- **Bytes:** about -3,000 [guess]; `TickSlot` loses its own wrap, next, hold and park branches.
- **Hashes:** move only if a pinned scene rewinds a `KindStore` world through the start of a non-looping timeline that has `Next` or `Hold`. I cannot see the scenes.
- **Receipt:** R0. New law test, exhaustive and count-based: for every flags byte (256) x tick in {0, mid, last} x loop x hold x next x rate in {0, 8, 32}, which is 256 x 3 x 2 x 2 x 2 x 3 = 18,432 cases [derived], assert `Move` returns a defined `Motion`, the tick stays inside `[0, duration)`, and where the result is `Stepped`, a step in the opposite direction restores the 8 slot bytes. New differential test in `Tl.Fuzz`: the same random request stream on a kind-free world (`Advance`) and on a world with a `KindStore` over a library with zero kinds (`Tick`) gives identical slot bytes each tick, 10,000 streams.
- **Kill rule:** if a pinned hash moves, the pinned behaviour wins. Make `Move` reproduce the `TickSlot` answer, keep the differential test red-listed for that one case, and hand the owner the table from Finding 2 to decide.

**3b. Decode before allocate.** `TimelineWorld.LoadLibrary` allocates, then validates, and has eight `Free(); throw` pairs [code]. `MaterializeSteps` repeats the same two pool-bound checks in both of its loops [code].

- Add internal `LibraryError` and a pure check `static LibraryError Check(ReadOnlySpan<byte> library, out int offset)` that validates the manifest, every blob (`TimelineRef.Validate` logic), every step's pool indices and the kind section, before the first allocation. `LoadLibrary` then builds with no failure path. The public constructor still throws `ArgumentException`; message text stays byte-identical where a test or snapshot pins it.
- **Bytes:** about -2,000 [guess].
- **Hashes:** none.
- **Receipt:** R0; `Tl.Fuzz` gains one target: for random mutations of a valid library, either `Check` returns an error or `LoadLibrary` succeeds, and native allocation count before and after a failed load is equal.
- **Kill rule:** any exception message change that a snapshot pins is reverted to the old text.

### Phase 4. Capacity classes (#568)

**Layout.** Keep `Playback` at 8 B. Do not touch its fields: `TlStateHash.Slots` hashes the raw slot bytes [code], so the layout is part of the pinned hashes.

```csharp
public readonly unsafe struct PlaybackView
{
    public readonly Playback* Slots;
    public readonly int* Counts;
    public readonly ulong* Spilled;
    public readonly Playback* Blocks;
    public readonly int EntityCount;
    public readonly int SlotsPerEntity;

    public Playback* RunOf(int entity)
        => (Spilled[entity >> 6] & (1ul << (entity & 63))) == 0
            ? Slots + (nuint)entity * (nuint)SlotsPerEntity
            : Blocks + *(int*)(Slots + (nuint)entity * (nuint)SlotsPerEntity);

    public Playback* SlotOf(int entity, int slot) => RunOf(entity) + slot;
}
```

- An entity with at most `SlotsPerEntity` slots lives in its inline run, exactly as today.
- When it needs more, its run moves to a block of the next power of two (up to `MaxSlots`, 256) in one shared arena. The entity's bit in `Spilled` is set and the first 4 B of its inline run hold the block offset. Freed blocks go to a per-size free list. A run returns inline only when its count reaches 0.
- `PlaybackTable.Grow()` is deleted. `Slots.TryAdd`, `Reserve` and `SlotsOf` move one entity's run. `Reserve` for rated sampling becomes local to the entity.
- **KindTable gets the same treatment:** `_capacity` (2 B per row) and `_heap` (8 B per row) become a `Spilled` bit per row, with the heap pointer stored in the first 8 B of the row's inline run. Rows with `Inline == 0` keep a dense pointer array, since they have no inline bytes to borrow.
- **Public API change (owner decision):** `PlaybackView` gains `Spilled`, `Blocks`, `RunOf`. `Slots + entity * SlotsPerEntity` stops being the address of a spilled entity's slots; `SlotOf` stays correct. Generated code already goes through `SlotOf` and `Counts` only [code: `EmitLegacyLoop`, `EmitKind`, `EmitFusedKernel`].
- **Bytes:** about +5,000 [guess].
- **Hashes:** none expected. Slot bytes and their order within an entity are unchanged; block addresses are not hashed.
- **Receipt:** R0. Count-based memory test: 40,000 entities with 4 playbacks plus one with 150. `main`: 40,001 x 256 x 8 = 81.9 MB. After: 40,001 x 36 B + 2,048 B + 5 KB bitset = 1.45 MB [derived]. Assert native bytes at most 1.6 MB. Rated test: one entity at 2x with 4 playbacks leaves every other entity's run address unchanged.
- **Risk:** a held `Playback*` across a spill. `TickRated` re-reads the pointer from `Reserve` [code]; `Slots.TryAdd` refreshes `_items` [code]. Generated loops enqueue requests rather than adding slots, so they never spill mid-loop.
- **Kill rule:** tick-only median at 4M x 4 worse than `main` by more than 3%. The branch is per entity, not per slot, so it should not show; if it does, hoist the two base pointers and keep the branch on the bit only.

### Phase 5. Make every per-tick loop follow visits

Six small changes. Each is independent and can land or be reverted alone.

- **5a. `TimelineWorld.Advance` walks `_playing`.** Same word and bit scan as `KindStore.TickPass` [code]. After stepping an entity, clear its bit unless some slot still `Moves`, is `Parked`, or is `Stopping`. Parked must stay: it resumes on a direction flip with no request (`PlaybackRules.Resumes`) [code].
- **5b. Emitted legacy loops walk `_playing`.** In `EmitLegacyLoop` and `EmitFusedKernel`, replace `for (__e = 0; __e < __n; __e++)` with the bit scan the emitter already writes for kinds. The slots the loop then skips are exactly the ones its existing `continue` conditions skip.
- **5c. Dense dispatch replaces the dictionary.** Delete `SystemRegistry.Filters` and the body of `SystemDispatch.FilteredSteps`. A system binds once per library generation, outside the loops:

```csharp
public unsafe struct SystemSteps
{
    public int* StageBase;
    public int* StepStart;
    public SystemStep* Steps;
}

public static SystemSteps Bind(TimelineWorld world, ulong system, ReadOnlySpan<ulong> pairKeys)
```

  Per visit: `g = StageBase[timeline - 1] + stage`, then the range `Steps[StepStart[g] .. StepStart[g + 1]]`. Two loads [derived]. The table is native, owned by the world, freed in `Free()`.
- **5d. One record per kind window.** Merge `_firstStage`, `_stageCount`, `_stepArrays` into `struct WindowSteps { LibraryStep** Steps; int FirstStage; int StageCount; }` (16 B). In `StepOf`, return `Steps[0]` when `StageCount == 1` and skip `StageAt`.
- **5e. Marker tables move onto the world.** A native array indexed by timeline id replaces the static `ConditionalWeakTable`, the `lock` and the `Dictionary` in `MarkerTableRegistry`. The two public static methods stay as stateless forwards so emitted code and the API snapshot do not change.
- **5f. Stable O(n) sorts.** `RequestQueue.Sort` becomes the LSD radix from Phase 1 over `(entity, slot)`, with a scratch block the queue owns and a one-pass "already sorted" exit. `Events<T>.Drain` and `SortEvents` use a bottom-up stable merge with owned scratch. Stability preserves `Sequence` order, so results are identical to the insertion sorts.
- **5g. `KindStore` setup and clear.** `Build` assigns rows in one pass over entities per owner (iterate the bits of `_mask[e] & bits`), with no `List<int>`, `ToArray` or `int[] rowIn`. `ClearPulses` walks `Active` words instead of every row.

- **Files:** `TimelineWorld.Systems.cs`, `SystemDispatch.cs`, `TlModule.cs`, `KindStore.cs`, `MarkerTableRegistry.cs`, `Requests.cs`, `Events.cs`, `TimelineWorld.Library.cs`, `SystemEmitter.cs`.
- **Public API change (owner decision):** `SystemDispatch.FilteredSteps` is replaced by `Bind`; `SystemSteps` is new. Both are consumed only by generated code.
- **Bytes:** about +2,000 [guess].
- **Hashes:** none expected. Entities are still visited in ascending order and slots in slot order.
- **Receipt:** R0, regenerated emitter snapshots reviewed line by line. Count-based, under the existing `TL_CHECKED` conditional: a probe counter of entities visited. 1,000,000 entities, 10 playing: one `Advance` and one legacy `Run` each report 10 (on `main`: 1,000,000). Sort test: 15,000 requests in descending entity order produce the same applied order as `main`, with a move counter at most 4 x 15,000 (on `main`: about 56M [derived]). Perf: the legacy A-shape from #540, median of 5.
- **Kill rule:** 5c is reverted if the #540 A-shape improves by less than 1.5x; it is byte-neutral, so the revert costs nothing. Any hash movement reverts the sub-step that caused it.

### Phase 6. Row-range workers

Design in Section 6. Summary of the change:

- `KindRules.TickEntity` callers, the emitted `Kind_i` loops and the emitted legacy loops take a `[firstWord, endWord)` range of `_playing` or `Active` words instead of scanning everything. The sequential entry points call them with the full range. Unity schedules the same range kernels as job batches.
- New public `sealed unsafe class TlWorkers : IDisposable`, instance-owned, no static state. .NET only; Unity never references it.
- Requests and events raised inside a parallel region go to per-chunk segments and are concatenated in chunk order.
- `KindTable.Append` stops writing `MaxLength` and `Overflows`; `KindStore.GetStats` computes them by scanning, cold.
- A system gets a parallel `Run` overload only if none of its consumers takes a context by `ref`. Otherwise the generator reports a diagnostic naming the consumer and parameter, and the system runs sequentially.
- **Bytes:** about +7,000 [guess].
- **Hashes:** none. Asserted, not assumed.
- **Receipt:** R0. All 13 hashes at worker counts 1, 2, 3, 8, 11. `GC.GetTotalAllocatedBytes(true)` delta across 1,000 warm ticks at 8 workers equals 0 (this covers worker threads; your current 0 B test covers the main thread only [briefing]). Perf: 4M x 4 tick-only, median of 5, at 8 workers no slower than single-thread / 4. Do not expect better than 11.1 ms on the measured machine [derived].
- **Kill rule:** less than 2x over one worker at 8 workers on 4M x 4, or any hash that depends on worker count: the pool does not merge. The range kernels stay, because Unity needs them.

### Phase 7. Generator: fewer bytes, real incrementality

`SystemEmitter.cs` (74 k) and `SystemReader.cs` (65 k) are 139 k [briefing], which is 28.5% of 487,962 if they count toward `src` [derived]. I cannot see how `source_budget.py` classifies `Tl.Gen.CSharp`.

- **7a. The kind loop moves into Core once.** Today `EmitKind` prints the whole loop (row scan, sampling, routes, pulse bookkeeping, idle scan) per kind per system. Put it in Core as a generic over a struct body, the same pattern `KindRules` already uses with `where TRows : struct, IKindRows` [code]:

```csharp
public unsafe interface IKindBody<T> where T : unmanaged
{
    bool Feed(int route, KindEntry* entry, Playback* slot, LibraryTimeline* timeline, FrameFlags flags, int entity, out T value);
    void Combine(ref T total, in T next);
    void Apply(in T total, int entity);
    void Idle(int entity);
}

public static unsafe class KindLoop
{
    public static void Run<T, TBody>(in KindPass pass, ref TBody body, int firstWord, int endWord)
        where T : unmanaged where TBody : struct, IKindBody<T>
}
```

  The generator emits only the body struct: the `switch` over feeders, the two user calls, the column pointers. Both hosts then run the identical loop, which removes the class of bug where `Idle` existed on .NET and was ignored in Unity.
- **7b. Delete `NewStyle.Rewriter`.** `SystemReader` learns two equivalences directly: `TlKindAttribute` is a kind attribute (`NewStyle.IsKindAttribute` already accepts both names [code]) and a first parameter `TlFrame<C>` is the frame of pair `(TlNoSettings<C>, C)` (the emitter already wraps it through `KernelShape.TlFrames` [code]). The auto system reaches the reader as a `SystemDecl`, not as parsed source text.
- **7c. Per-type incremental models.** Replace the `CompilationProvider` join with `ForAttributeWithMetadataName` for `TlKind`, `TimelineKind`, `TimelineSystem`, `TlState`, `Feeds`, `Pulse`, plus one syntax predicate for `ITlTrack<>` implementers. Each yields an equatable model of strings. Assignment (`Assign`, `AssignKinds`) runs on the collected models.
- **7d. One identity file.** `Identity.cs` holds FNV-1a, the stable-name grammar and the layout grammar over plain strings. It is compiled into both Core and the generator by source link. Reflection and Roslyn each supply only an adapter that lists name parts and fields.
- **Bytes:** about -12,000 [guess]: 7a -6,000 net (emitter -9,500, Core +3,500), 7b -5,000, 7c 0, 7d -1,000.
- **Hashes:** none expected. 7a moves the loop without reordering any operation. 7d must reproduce today's keys exactly, or every baked library stops matching.
- **Receipt:** R0. 7a: every emitted snapshot diff is explained in the PR; the four pack tests call the generated `TlAuto_Tl_Pack_*.Run` again (the branch hand-inlined them [briefing]). 7c: a generator-driver test edits an unrelated method body and asserts the per-type step reports `Cached` or `Unchanged` (the tracking name `Tl.Type` already exists [code]). 7d: twelve golden types (nested, generic, enum field, fixed buffer, explicit layout) give the same `ulong` from the reflection adapter in Core tests and the Roslyn adapter in generator tests, and equal the keys in the published conformance vectors.
- **Kill rule:** 7a is dropped if Burst or Mono fails to specialise the struct body (the Unity team checks one kind with the Burst inspector: no call through an interface, no boxing). 7d is dropped if any golden key differs from a key already baked into a published vector; report the type and both strings instead.

### Phase 8. Developer experience

Design in Section 7. Changes:

- **8a. Derived kinds.** Three public marker structs: `Tl.Sum<T>`, `Tl.Set<T>`, `Tl.While<T>`. The generator synthesises the kind for each `(template, T)` it meets in a `[Feeds]` or `[Pulse]` attribute and feeds it into the existing kind pipeline.
- **8b. `Reset`.** `RequestOp.Reset`, `PlaybackTable.Reset(int entity)`, `Requests.Reset()`. It zeroes the entity's slots, purges its kind rows, drops its queued game pulses, and adds nothing to the closing list. `StopAll` keeps its meaning.
- **8c. The unconnected clip field warning (#593).** A normal Roslyn warning, suppressible with `NoWarn` or `#pragma`, so it needs no new attribute. Its bytes are paid by the Phase 2 generator deletions.
- **8d. No seed library.** `TimelineWorld.Find(string name)` binary-searches the manifest, which `LoadLibrary` already requires to be sorted ordinally [code]. Generated timeline id constants become optional sugar. The compile step stops depending on bake output.
- **8e. One host-parity fixture.** A library, a request script and expected column bytes per tick, executed by both hosts. It must include `Idle`, an `Idle` kind fed from another assembly, and a rewind through an assigning `Idle`, which are your stated Unity gaps [briefing].
- **Public API additions (owner decision):** `Sum<T>`, `Set<T>`, `While<T>`, `RequestOp.Reset`, `PlaybackTable.Reset`, `Requests.Reset`, `TimelineWorld.Find`.
- **Bytes:** about +9,000 [guess].
- **Hashes:** none. Everything is additive; no existing kind changes.
- **Receipt:** R0. `demos/hello-side` rewritten with `Sum<Teleported>` produces the same column bytes per tick as the current demo. A test builds the silent `SideSwing` version and expects the warning. #594 repro: `Reset(row)` then `Start` in one step, and the new occupant receives zero pulses from the old one. A clean clone builds with no `.tlib` on disk.
- **Kill rule:** 8a is dropped if the synthesised kind for `Sum<T>` is not byte-identical in behaviour to the hand-written kind in the demo across 600 ticks.

### Budget ledger

| Phase | Net bytes in `src` | Label |
|---|---|---|
| 0, 1 | 0 | |
| 2 | -45,000 to -70,000 | guess |
| 3 | -5,000 | guess |
| 4 | +5,000 | guess |
| 5 | +2,000 | guess |
| 6 | +7,000 | guess |
| 7 | -12,000 | guess |
| 8 | +9,000 | guess |
| **Total** | **-39,000 to -64,000** | guess |

Projected `src`: 424,000 to 449,000 of 520,000 [derived from guesses]. If Phase 2 frees nothing because the dead code turns out to be reachable from tools, the total is +6,000 and still fits the 32,038 B headroom [derived]. The order is chosen so the deletions land before the additions.

---

## 4. The deletion list

Sizes are [guess] unless the briefing gave the file size. Each row is one commit.

| Delete | Where | Consequence |
|---|---|---|
| The whole 5.0 stack (never merged) | `World.cs`, `World.Kinds.cs`, `World.Systems.cs`, `Library.cs`, `LibraryWriter.cs`, `Playbacks.cs`, `Requests5.cs`, `Kinds.cs`, `Kernels.cs`, `Threads.cs`, `Clock.cs`, `MonoidClassifier.cs`, `EmitWorldRun`, `EmitWorldKind`, `EmitWorldKindBinders` | TLIB v3 and `Run(World, Direction)` never exist publicly. `KindDecl.CombineKind`, `ApplyKind` and their ops lose their only reader |
| `PairTable` | `Data.cs` | The process-global consumer registry is gone. `Seal`, `Install`, `DigestOf` have no caller in the paste |
| `TimelineRef.ExecuteDispatch`, `ExecuteWindow`, `Resolve`, `Uses`, `PairOf` | `Data.cs` | `TimelineRef` becomes a validated-bytes view only |
| `TimelineTable` | `InternTable.cs` | No process-wide intern table. `TimelineWorld.Reference`, `IsLive`, `Default` leave the public API |
| `SlotView`, `LaneSegment`, `LaneEncoding` | `BankView.cs`, `Lane.cs` | The segmented bank format of 3.0 has no reader in Core |
| `Checked`, unused `Fail` members | `Checked.cs` | `TL_CHECKED` column checks for the removed lane API go. Keep the conditional pattern for the Phase 5 probe |
| `Inspection` bank and intern snapshots | `Inspection.cs` | `tlb inspect` loses intern and bank views. Keep only what the CLI still prints (I cannot see it) |
| `MarkerTableRegistry` state | `MarkerTableRegistry.cs` | Becomes two forwarding methods |
| `SystemRegistry.Filters`, `FilterKey` | `TlModule.cs`, `SystemDispatch.cs` | No managed dictionary on the tick path |
| `PlaybackTable.Grow` | `PlaybackModel.cs` | Replaced by per-entity spill |
| `KindTable._capacity`, `_heap` dense arrays; `MaxLength`, `Overflows` hot writes | `KindTable.cs` | Stats become a cold scan |
| Own wrap, next, hold, park branches | `KindRules.TickSlot` | One rule in `PlaybackRules.Move` |
| Bake analysis and slot model | `BakeReader.cs`, parts of `TypeAnalyzer.cs`, `JobReader.cs`, `Jobs.cs` | 11 diagnostics stop. No emitted code changes, because none was emitted from them |
| `UnityJobEmitter` | `UnityJobEmitter.cs` | `GeneratorCli` calls `JobEmitter.Emit` for both backends; output is already identical [code] |
| `NewStyle.Rewriter` and the tree replacement | `NewStyle.cs` | The generator stops building a second compilation |
| Seven duplicate FNV-1a loops | several | One in `Identity.cs` |
| `EmitKind` loop text, `EmitIdle` | `SystemEmitter.cs` | Loop lives once in Core |

Not deleted, and why: the legacy consumer path (`ExecuteActive`, `ExecuteBatch`, `OnClipStart`, `OnClipEnd`, `OnMarker`, `RunFused`). K5 says keep its results exactly [briefing], pinned hashes depend on it, and removing it is a public API decision you have not made. It is the largest remaining block of emitter bytes. If the owner retires it later, `EmitLegacyLoop`, `EmitFusedKernel`, `EmitCloseLoop`, `EmitStepCase`, `EmitDirectStep`, `EmitFlush`, `AnalyzeDirect` and the fold-lane half of `MaterializeSteps` go with it.

---

## 5. Data layout, before and after

**Per playback:** 8 B before, 8 B after [code]. The information floor is 4 B (16-bit id, 16-bit tick). I keep 8 B because flags, rate, phase and channel are live state and the raw bytes are hashed.

**Per entity, uniform shape** (4 inline slots, one two-float kind reachable by all, inline capacity 4, entry 16 B):

| Item | Before | After | Source |
|---|---|---|---|
| slots | 32 | 32 | 4 x 8 |
| `Counts` | 4 | 4 | |
| `_playing` | 0.125 | 0.125 | 1 bit |
| playback `Spilled` | 0 | 0.125 | 1 bit |
| `_mask`, `_offset` | 12 | 12 | 8 + 4 |
| `_rows` | 4 | 4 | one reachable kind |
| table `Entity`, `Length` | 6 | 6 | 4 + 2 |
| table `_capacity`, `_heap` | 10 | 0 | 2 + 8 |
| inline entries | 64 | 64 | 4 x 16 |
| `Active`, `Enabled`, row `Spilled` | 0.25 | 0.375 | bits |
| **Total** | **132.375** | **122.625** | [derived] |

That is 7.4% less [derived]. At 4M entities: 529.5 MB to 490.5 MB [derived]. This shape was never where the memory problem is.

**Same shape with inline capacity 1 for crowd kinds.** After Phase 4 a spilled row costs one in-band pointer, so `KindOverride.Inline = 1` becomes a reasonable setting for kinds where most entities hold one span. Inline entries fall from 64 B to 16 B and the entity total is 74.6 B [derived], 44% less than today. That is a library setting, not code. The cost is one pointer chase for rows holding more than one entry.

**Mixed shape** (4M entities at 4, 100 players at 150):

- Before: stride 256 for all. 4M x 256 x 8 = 8,192 MB slots + 16 MB counts = 8,208 MB [derived].
- After: 4M x 36.25 B = 145 MB, plus 100 x 256 x 8 = 0.2 MB of spill blocks [derived].
- **56.5x less [derived].**

**Bytes first touched at setup**, 4M entities, one kind:

- Before: 576 MB (Finding 5).
- After: minus `_capacity` 8, `_heap` 32, managed temporaries 48 = 488 MB, 15% less [derived].
- With inline capacity 1: inline 64 MB instead of 256, total 296 MB, 49% less [derived].
- If setup time is linear in bytes touched [guess], your 492 to 537 ms [briefing] becomes about 418 to 456 ms, or 253 to 276 ms with inline 1 [derived]. Receipt: the Phase 0 setup baseline, median of 5.

---

## 6. Threading design

**Unit of work.** A range of 64-entity words of the `_playing` bitset (for the tick pass) or of a kind table's `Active` bitset (for a kind pass). Word alignment means no two workers write the same bitset word. 64 entities x 32 B of slots is 2,048 B, a whole number of cache lines [derived], so no two workers share a slot line either, provided the slot block is allocated 64-byte aligned. Today `PlaybackTable` aligns it to 8 [code]; change that to 64.

**Split.** One pass over the bitset computes popcounts and cuts `workers x 8` chunks of roughly equal set-bit count. At 4M entities that pass reads 0.5 MB [derived]. Workers take chunks with one `Interlocked.Increment` each. Which worker runs which chunk does not matter, because of the next paragraph.

**Why results are bit-identical.** Everything a row's work writes belongs to that row: its playback run, its kind table rows, its column elements. Within a row the order is unchanged: slots in slot order, entries in entry order, `Combine` in entry order, then `Apply`. Float operations are never reordered across rows because no float operation spans rows. So thread count and chunk assignment cannot affect any value.

**The exceptions, and what each becomes.** These are every place in the pasted code where row work writes something shared [code]:

| Shared write | Today | Under threads |
|---|---|---|
| `RequestQueue` (`_count`, `_sequence`) from consumers | one queue | Each chunk appends to a segment in its worker's buffer. After the region, segments are concatenated in ascending chunk order and `Sequence` is renumbered from the queue's base. Chunks are ascending row ranges and the sequential loop enqueues in ascending row order, so the queue content is identical to a sequential run |
| `Events<T>` header (`Count`, `Sequence`) | one buffer | Same scheme |
| System context passed `ref` | shared | Not allowed in a parallel run. The generator emits the parallel overload only when no consumer takes a context by `ref` |
| `KindTable.MaxLength`, `Overflows` | written in `Append` | Removed from the hot path |
| `KindTable.Grow` (row heap) | native alloc | Unchanged. Native allocation is thread safe and is not managed memory |
| Playback spill arena (rated sampling) | global `Grow` today | A spin gate around the block allocator. Rare path. Block addresses are never observed by game code or hashes |
| `KindStore.DroppedPulses` | written in `MergePulses` | Unchanged. `MergePulses` stays single-threaded, after the region, in queue order |
| `ClosingList` | appended by requests | Unchanged. `ApplyRequests` stays single-threaded, before the region |

**Phases of one tick.** Sequential: clear closing list, apply requests. Parallel: tick pass over `_playing` ranges. Sequential: merge game pulses. Then each system: parallel legacy region, then one parallel region per kind, with a sequential concatenation of requests and events after each region.

**0 B warm.** `TlWorkers` creates its threads once. A run stores a function pointer and a pointer to a stack struct of unmanaged fields; there is no delegate, closure or array allocation:

```csharp
public sealed unsafe class TlWorkers : IDisposable
{
    public TlWorkers(int workers)
    public void Run(delegate*<void*, int, int, int, void> body, void* state, int* chunkEnds, int chunks)
}
```

Workers block on a semaphore between runs after a short bounded spin. 5.0's pool spins 400 x `SpinWait(512)` and then polls with `Wait(1)` [code], which is a plausible cause of the 10.6 to 21.0 ms spread you saw at 8 workers [guess].

**Unity.** Unity does not use `TlWorkers`. It schedules the same range kernels as job batches. A chunk in ECS is already a disjoint row range, so one batch per ECS chunk satisfies every rule above.

**Ceiling.** Threads move the tick-only shape toward 11.1 ms at 4M x 4 on the measured machine and no further [derived]. Past that point more workers add nothing.

---

## 7. Developer experience design

### The model, as six axioms

1. A **clip** is data on a time interval.
2. A **track** is a pure function from a frame to a value.
3. A **kind** folds the values of one tick into one (`Combine`) and applies the result to a column (`Apply`). Optionally it says what the column is when nothing arrived (`Idle`).
4. A **column** is state on a row. It has exactly one writer.
5. **Playback** is one total function of slot, timeline and direction (`PlaybackRules.Move`).
6. **Requests** are the only way playback changes between ticks. They apply in `(entity, slot, sequence)` order.

Everything in the runtime is one of these or a table that makes one of them fast. A newcomer should meet 1, 2 and 4 only. Today they must also write 3 by hand and invent a value type that mirrors the column. That mirror is where the silent 0 comes from.

### The change: three derived kinds

The value type becomes the column type, and the kind is named, not written.

| Marker | Accepts | Combine | Apply | Idle |
|---|---|---|---|---|
| `Sum<T>` | spans and pulses | field-wise `+` | `column += total`, field-wise | none |
| `Set<T>` | spans and pulses | last entry wins | `column = total` | none |
| `While<T>` | spans | field-wise `\|` for bool and integers, `max` for floats | `column = total` | `column = default` |

`Sum<T>` on a struct with a non-numeric field is a generator error naming the field. A column written by `While<T>` or `Set<T>` and by any other kind or consumer is a generator error naming both writers (axiom 4). Hand-written kinds keep working unchanged.

### Example 1: teleport, then add a sideways move

Before (five declarations; the new field must be added in four places):

```csharp
[TlClip("Hello/Teleport")]
public struct TeleportClip { public float Distance; public float Side; }

[TlState]
public partial struct Teleported { public float Forward; public float Side; }

public struct Move { public float Forward; public float Side; }

[TlKind(Accepts = KindEntries.Pulses)]
public struct TeleportKind
{
    public static void Combine(ref Move total, in Move next) { total.Forward += next.Forward; total.Side += next.Side; }
    public static void Apply(in Move total, ref Teleported column) { column.Forward += total.Forward; column.Side += total.Side; }
}

public struct TeleportTrack : ITlTrack<TeleportClip>
{
    [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
    public static Move JumpAtClipStart(in TlFrame<TeleportClip> frame)
        => new Move { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };
}
```

After (three declarations; the new field is added to the clip, the column, and one expression):

```csharp
[TlClip("Hello/Teleport")]
public struct TeleportClip { public float Distance; public float Side; }

[TlState]
public partial struct Teleported { public float Forward; public float Side; }

public struct TeleportTrack : ITlTrack<TeleportClip>
{
    [Pulse(typeof(Sum<Teleported>), On = ClipEvent.Start)]
    public static Teleported Jump(in TlFrame<TeleportClip> frame)
        => new Teleported { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };
}
```

### Example 2: the ability window

Before (the column keeps `true` after the clip unless you remember `Idle` or author an "off" clip):

```csharp
[TlKind]
public struct DoubleJumpKind
{
    public static void Combine(ref bool total, in bool next) => total |= next;
    public static void Apply(in bool allowed, ref Player player) => player.CanDoubleJump = allowed;
    public static void Idle(ref Player player) => player.CanDoubleJump = false;
}

public struct DoubleJumpTrack : ITlTrack<DoubleJumpClip>
{
    [Feeds(typeof(DoubleJumpKind))]
    public static bool Allow(in TlFrame<DoubleJumpClip> frame) => true;
}
```

After (true while a clip covers the tick, default otherwise, on both hosts, by construction):

```csharp
[TlState]
public partial struct DoubleJump { public bool Allowed; }

public struct DoubleJumpTrack : ITlTrack<DoubleJumpClip>
{
    [Feeds(typeof(While<DoubleJump>))]
    public static DoubleJump Allow(in TlFrame<DoubleJumpClip> frame) => new DoubleJump { Allowed = true };
}
```

### Each headache, and what removes it

| Briefing 7.x | Removed by |
|---|---|
| 1. Silent 0 on a new field | Value type is the column type (8a); warning for a clip field no track reads (8c) |
| 2. Flag stays true; `Idle` ignored in Unity | `While<T>` (8a); one kind loop for both hosts (7a); parity fixture (8e) |
| 3. Recycled row gets the old End pulse | `Reset(entity)` (8b). `StopAll` means "interrupt", `Reset` means "this row is a new entity" |
| 5. Kind name clashes | Already fixed in the paste: kinds are keyed by full type name (`KindDecl.Id`, and the duplicate error says so) [code] |
| 7. Per-timeline seek, reverse, speed | Already in the paste: `RequestOp.Seek`, `Reverse`, `Speed` and channel variants [code] |
| 9. Seed `.tlib` | Name lookup at load (8d) |
| 4, 6, 8 | Unity setup, CLI export, Timeline window. Outside the code I was given. No design from me |

---

## 8. What Unity must provide

**What Core exposes for Unity. All unmanaged, pointer-based, no statics.**

- `Playback`: 8 B, layout frozen (it is hashed).
- `PlaybackRules.Move` and the request rules over `IPlaybackSlots` [code].
- `KindRules` over `IKindRows` [code]. Unity supplies the `IKindRows` struct for its storage.
- `KindLoop.Run<T, TBody>` with a `KindPass` of pointers (7a).
- Range kernels taking `[first, end)` rows (Phase 6).
- `LibraryView`, `KindSectionView`, `SystemSteps`, `TlStateHash`.

**What Unity must satisfy. Please answer each with yes, no, or what exists instead.**

1. **Language level.** Which C# version compiles the vendored Core? The pasted Core uses C# 10 to 12 features (Finding 13). If the answer is C# 9, name the step that down-levels, and add a CI leg that compiles Core for netstandard2.1 at that version.
2. **Per-entity playback capacity.** Inline capacity equals `SlotsPerEntity`; overflow is per entity. An `IKindRows.Reserve` call must never move another entity's slots.
3. **Row order inside a chunk.** Rows processed in ascending in-chunk index, one batch per chunk, no kernel writing outside its chunk.
4. **A stable row order across hosts.** For hash parity, requests and events from jobs must be concatenated in the same row order .NET uses. ECS chunk order is not that order. What key defines "row" in the Unity parity tests today?
5. **Burst float mode.** Every kernel and every generated kind body compiled with strict float mode and standard precision, so multiply-add is never fused and nothing is reassociated. Without this, bit-exact parity with .NET cannot be promised on any CPU with FMA.
6. **Burst specialisation.** One generated `IKindBody<T>` checked in the Burst inspector: no interface call, no boxing.
7. **Zeroed native buffers.** Kind tables and spill blocks handed to Core are zero-filled by the caller.
8. **Markers without statics.** A per-world marker table pointer (5e). How does Unity resolve marker payloads in Burst today, given the .NET path is a static managed registry?
9. **Destroyed entities.** Queued game pulses addressed to a destroyed entity are dropped, matching `Reset` (8b).
10. **Parity fixture.** The 8e fixture runs in PlayMode and compares column bytes per tick, including the three `Idle` cases.
11. **Thread invariance.** One test: 1 worker and N workers give the same hash.

**Where my design needs something Unity may not have:** item 4 (a cross-host row order) and item 5 (strict float mode on user kind code). If either answer is no, cross-host bit-exactness is limited to shapes where requests and events are not raised from jobs, and to arithmetic without multiply-add.

---

## 9. Disagreements, and what I refuse to promise

**Where the code contradicts the briefing (code wins):**

1. The pasted source is the 5.0 branch and does not compile as shown. The briefing says it does.
2. "Core compiles as C# 9": the paste uses C# 10 to 12 (Finding 13).
3. "No static mutable state" and "unmanaged data only on the runtime path": `MarkerTableRegistry`, `TlGenerated`, `PairTable`, `TimelineTable`, `TimelineWorld.Default`, `SystemRegistry.Filters`, and `KindStore`'s `Dictionary`, `HashSet` and `List` fields all break one or both [code].
4. Section 7 item 7 says per-timeline seek, reverse and speed are missing. They are implemented [code], and #577 is in your closed list.
5. Section 7 item 5 says kinds are identified by name. They are identified by full type name [code], and #582 is closed.
6. #385 and #387 say the delegate allocation in `TimelineRef.Validate` is gone. One remains per call [code].
7. Section 5 item 4 asks whether the generator is where the bytes go. Yes: 139 k of 488 k, 28.5% [derived], if it counts toward `src`.

**Requirements that conflict, and which I drop:**

- "Pinned hashes never change" against "fix the rewind divergence" (Finding 2). I drop the fix if a hash moves. That keeps a known wrong behaviour pinned; the owner should see the table and choose.
- "Public API is frozen" against "no static mutable state". I drop `TimelineWorld.Default`.
- "Replace only at 2x or better" against capacity classes, which win memory, not speed. The rule needs a memory arm: at least 2x less memory in a named shape, with speed within 3%. Phase 4 is 56.5x in its shape [derived].
- "Shipped tools has 362 B of headroom" against "#593 needs a warning". Solvable only by deleting generator code first. Whether Phase 2's generator deletions count toward that budget depends on `source_budget.py`, which I cannot see.

**What I refuse to promise:**

- 100000x of anything. The arithmetic is in Verdict (a).
- Any tick time below 0.69 ns per visit (tick only) or 0.98 ns per visit (value produced) at 4M x 4 on the measured machine, without reducing bytes per visit.
- That 24.5 GB/s holds on another machine. It is derived from one of your measurements.
- Any speed number before Phase 0 produces baselines on `main`. You have no trusted ns-per-visit figure for 4.0 on .NET, and neither do I.
- That Phase 2 frees the bytes I estimated. I cannot see who else reaches those internals. The kill rule covers it.
- Anything about Unity. I have not seen a line of it.
- That every byte figure marked [guess] is within 30%. The budget script decides, phase by phase.

**The short version.** Keep 4.0. Close 5.0. Delete what is dead. Make one playback rule. Give each entity its own capacity. Drive every loop from the playing set. Split by row ranges. Let the kind be named instead of written. The honest result is 1.6x on the tick, 57x on memory in the shape that hurts, zero cost for idle rows on every path, and a model a newcomer can hold in three declarations.

