# tl 5.0 status — M10 ledger (plan §10 + §9)

Grounded in the working copy at `D:/GitHub/tl` on 2026-11-10. Every file
named below was read there; statuses reflect what is on disk, not the
plan text. The 5.0 cutover is additive: no 4.0 file in `src/Tl.Core/`
has been deleted, and the new 5.0 sources sit alongside the 4.0 tree
(untracked `??` in `git status`). Prior timings are quoted from
`benchmarks/PhaseClock/results/`; no new timings are quoted here
because this worker did not build or run code per the M10 assignment —
the full-pipeline numbers come from running `--worldbench` (§ WorldBench).

## Milestone ledger M0–M10

| Milestone | Scope | Status | Landed files (evidence) | Pinning tests / receipts |
|---|---|---|---|---|
| M0 | Gate zero + §0 kernel experiment | done | `docs/gate-zero.md`; `tests/Tl.Core.Tests/GateZeroTests.cs`; `benchmarks/PhaseClock/README.md`; `benchmarks/PhaseClock/Kernels.cs`, `Cases.cs`, `Bench.cs`, `Checker.cs`, `Data.cs`, `Probe.cs`; `benchmarks/PhaseClock/results/bench-final.txt`, `results/baseline-receipt.json`, `results/baseline.log` | 9/9 parity shapes at 97 ticks in `results/bench-final.txt` (uniform-dense 0.050 ns/entity); baseline folded hot 14.9567 ns/entity in `results/baseline-receipt.json`; 16 §1 rows classified (13 already-fixed, 3 real, 0 paste-damage) in `docs/gate-zero.md` |
| M1 | Library v3 data (tlib3) + writer/verifier | done | `src/Tl.Core/Library.cs` (`Library.Verify`, `LibraryResult`, `Tlib3Timeline`/`Tlib3Step`/…, `LaneValue`, `LaneClass`); `src/Tl.Core/LibraryWriter.cs` (`TimelineSpec`, `StepSpec`, `LibraryWriter.Write`); `tests/Tl.Core.Tests/LibraryV3Tests.cs` | 7 facts: writer→verify round trip, constant-lane detection, 64-byte alignment, empty library, truncation/offset rejection, corrupt-buffer error enum, stable re-verify; `PublicApi.approved.txt` adds `Tl.Library*`, `Tl.Tlib3*`, `Tl.TimelineSpec`, `Tl.StepSpec` |
| M2 | Phase clock + playbacks/cohorts | done | `src/Tl.Core/Clock.cs` (`Clock.Step`, `Direction`, `Edge`, `Row`); `src/Tl.Core/Playbacks.cs` (`Playbacks`, `PlainPlayback`, `SoloPlayback`, `AddCohort`/`AddSoloCohort`, `Advance`/`AdvanceSolo`, `Remove`); `tests/Tl.Core.Tests/ClockPlaybacksTests.cs` | 13 facts: forward/reverse restoration, loop wrap, end/start edges, zero duration, cohort sort by phase then row, remove, non-looping removal, solo rate stepping, stable radix, invalid-request counting |
| M3 | Lane kernels + stable radix + requests queue | done | `src/Tl.Core/Kernels.cs` (`PhaseToTick`, `BroadcastDense`/`BroadcastSparse`, `LaneDense`, `LaneGatherScalar`, `RunBroadcast`, `StableRadix`, `Run`); `src/Tl.Core/Requests5.cs` (`Requests5`, `Request5`, `RequestOutcome`, `StableRadixSort`); kernel facts in `tests/Tl.Core.Tests/WorldV5Tests.cs` | `StableRadixSortsStably` (`3,1,3,0,2` → `3,1,4,0,2`), `BroadcastDenseMatchesScalarLoop` (off 37, 1024), `LaneKernelsMatchScalarOracle`, `RunBroadcastMatchesScalarOracle`; `Requests5` lands as a standalone queue (not yet consumed by `World`, see M4) |
| M4 | World sequential pipeline (observe → visits → accumulate) | done | `src/Tl.Core/World.cs` (`World(Library,int)`, `Tick(Direction)`, `Accumulator`, `Frame`, `Report`, `Requests`, `Events<T>()`, `TickReport`); `tests/Tl.Core.Tests/WorldV5Tests.cs` | 7 oracle shapes at 97 ticks (uniform fwd/rew, mixed fwd/rew, unique fwd/rew, four-pairs) hash-equal to `OracleRun`; warm-tick 0 B allocate (uniform + mixed); 64-cohort ratio ≤2.5; invalid starts counted not thrown; events drain |
| M5 | Kinds monoid runtime | done | `src/Tl.Core/Kinds.cs` (`IKind`, `IKind<T>`, `Kind<T>`, `KindEntries<T>`, `KindTable<T>`, `IntSumKind`, `FloatSumKind`); `tests/Tl.Core.Tests/KindsMonoidTests.cs` | 9 facts: order-independent `Combine`, touched iteration (incl. word boundary), entries rewind, reset/idle semantics, `ApplyAll` with/without idle, stable id/order; `PublicApi.approved.txt` adds `Tl.Kind*`, `Tl.IKind*` |
| M6 | Generator monoid classifier | partial | `src/Tl.Gen.CSharp/MonoidClassifier.cs` (`MonoidClassifier.Classify`, `IrResult`, `IrKind`); `tests/Tl.Gen.CSharp.Tests/MonoidClassifierTests.cs`; `tests/Tl.Gen.CSharp.Tests/Tl.Gen.CSharp.PublicApi.approved.txt` adds `Ir*` + `Classify` | Classifier landed and tested; binding emit not landed: no `SystemEmitter`/`JobEmitter` call site (zero references to `MonoidClassifier`/`IrResult` outside `MonoidClassifier.cs` itself), so no emitted monoid binder wires `KindTable<T>.Combine`/`Apply` yet (see Gaps) |
| M7 | Public API surface + netstandard2.1 guardrails | done | `tests/Tl.Core.Tests/PublicApi.approved.txt` (+37 new types: `Clock`, `Kernels`, `World`, `Library*`, `Kind*`, `Playbacks`, `Requests5`, `Threads` not in surface as `ParallelApply` is separate file); `src/Tl.Core/Tl.Core.csproj` (`netstandard2.1;net10.0`); `src/Tl.Core/Kernels.cs` + `src/Tl.Core/LibraryWriter.cs` (`#if NET10_0_OR_GREATER` `Vector` branches with scalar fallbacks) | New sources contain zero `//` comments (grep clean); `Parallel.For` on the netstandard2.1 surface (`src/Tl.Core/Threads.cs`); `TreatWarningsAsErrors` on `Tl.Core` and `PhaseClock` projects |
| M8 | Threads (`ParallelApply`) | partial | `src/Tl.Core/Threads.cs` (`ParallelApply.ForEachChunk`, `ParallelApply.ParallelAccumulate` via `Parallel.For`) | Helper landed with no `World.Tick` call site (zero references to `ParallelApply` outside `Threads.cs`); sequential `World.Tick` is the only measured pipeline — parallel visits/accumulate not yet wired, hash order still radix-total |
| M9 | Unity shim (spec + vendor list) | partial | `docs/v5-unity.md` (M9 vendor list: `Clock.cs`, `LibraryView.cs`, `Library.cs`, `Kernels.cs`, `World.cs`, `LibraryWriter.cs`, `Polyfills.cs`, `BurstDiscard.cs`; scalar-fallback rationale; parity-job design) | Spec landed; shim code (`TlV5World`, `TlBurstJobs`, `TlBakeMenu`, `WorldV5ParityTests`) lives in `com.tl.unity`, not this repo; no PlayMode batchmode receipt (`TL-PARITY` hashes vs .NET `RunWorldHash`) recorded here (see Gaps) |
| M10 | Full-pipeline benchmark + status ledger | done | `benchmarks/PhaseClock/WorldBench.cs` (`WorldBench.RunWorld`); `benchmarks/PhaseClock/Program.cs` (`--worldbench`); `benchmarks/PhaseClock/PhaseClock.csproj` (`ProjectReference` to `Tl.Core`); `docs/v5-status.md` (this file) | Code landed; run verification (`--worldbench` prints both ns/entity + oracle PASS) is for Main (this worker did not build/run); § WorldBench below is the method |

## Deletion ledger (plan §7 file fates)

Additive cutover: every 4.0 file below is still present; the column
shows the v5 successor that landed beside it. Nothing is deleted yet —
a `landed-replaced-by` row means the replacement is compiled and
tested alongside, not that the 4.0 path was removed.

| 4.0 file | Landed v5 replacement | Still present in 4.0? |
|---|---|---|
| `src/Tl.Core/TimelineWorld.cs`, `TimelineWorld.Library.cs`, `TimelineWorld.Systems.cs` (`TimelineWorld(byte[])`, `Advance`, `Tick`, `ApplyRequests`, `LibraryView`) | `src/Tl.Core/World.cs` (`World(Library,int)`, `Tick(Direction)`, `Accumulator`) | yes |
| `src/Tl.Core/PlaybackModel.cs` (`PlaybackTable`, `Playback`, `EntityRef`) | `World` internals (`_pbRow`/`_pbTimeline`/`_pbPhase`) + `src/Tl.Core/Playbacks.cs` cohorts + `src/Tl.Core/Clock.cs` | yes |
| `src/Tl.Core/PlaybackRules.cs` (per-entity `Step`) | `Clock.Step` + `Kernels.PhaseToTick` + `World.Tick` run-per-phase | yes |
| `src/Tl.Core/Requests.cs` (`RequestQueue`, `RequestRecord`, `ClosingList`) | `src/Tl.Core/Requests5.cs` (`Requests5`) + `World.RequestApi` direct `Start`/`Stop` + `WorldEvents<T>` | yes |
| `src/Tl.Core/Data.cs` (TLB v6 `TimelineRef.Validate`, pair table) | `src/Tl.Core/Library.cs` (`Library.Verify`) + `src/Tl.Core/LibraryWriter.cs` for the v5 tlib3 path only; TLB v6 kept for 4.0 | yes |
| `src/Tl.Core/LibraryAssembler.cs` (`Emit`) | `LibraryWriter.Write` for v5 tlib3 only; 4.0 assembler kept | yes |
| `src/Tl.Core/KindTable.cs`, `KindStore.cs`, `KindSection.cs`, `KindPayload.cs`, `KindRules.cs` (unmanaged kind store) | `src/Tl.Core/Kinds.cs` (`Kind<T>`, `KindTable<T>`) managed monoid alongside | yes |
| `src/Tl.Core/InternTable.cs` (`TimelineTable` + `TimelineAsset.Load` interning) | no replacement; v5 `World` never touches `TimelineTable` (`docs/gate-zero.md` row 5: interning is the 4.0 `TimelineAsset` path) | yes |
| `src/Tl.Core/Events.cs` (`Events<T>`) | `World.Events<T>()` + `WorldEvents<T>.Drain` in `World.cs` alongside | yes |
| `src/Tl.Core/Lane.cs`, `LibraryView.cs`, `MarkerFrame.cs`, `MarkerTableRegistry.cs`, `Inspection.cs`, `StateHash.cs`, `PairLayoutDigest.cs`, `Compiled.cs`, `BankView.cs`, `Checked.cs`, `Mathematics.cs`, `Polyfills.cs`, `BurstDiscard.cs`, `Hooks.cs`, `TimelineSystem.cs`, `SystemDispatch.cs`, `KindAttributes.cs`, `TlModule.cs`, `CoderApi.cs`, `AssemblyInfo.cs` | none (kept; 4.0 path unchanged) | yes |
| `src/Tl.Gen.CSharp/SystemEmitter.cs`, `SystemReader.cs`, `JobEmitter.cs`, `UnityJobEmitter.cs` | `src/Tl.Gen.CSharp/MonoidClassifier.cs` is a classifier-only helper (`Classify`); it is not called from the emitter yet (see Gaps) | yes |

### Deletion sweep — DeleteSweep (2026-11-10)

`python3 benchmarks/source_budget.py` → `src: 72 files; 577278 content bytes + 2199 path bytes = 579477/520000 bytes` (over **59,477 B**). **Files deleted this change: 0 (0 B freed).** Every assignment candidate and every kept-list file has at least one caller that cannot be mechanically rewired in this change: no landed 5.0 successor covers its surface, and rewiring would change a 4.0 public API pinned by packs/tests/tools or require editing generator-emitted snapshots. Corroboration: `tests/test_dead_code.py` passes (every src/ type is referenced outside its declaration; the only scanner-blind names — `BurstDiscard`, `TimelineSlotKeys` — are live via short-name attribute application, e.g. `Events.cs:130 [Unity.Burst.BurstDiscard]`).

| File (content bytes) | Verdict | Caller blocker |
|---|---|---|
| `TimelineWorld.cs` (327), `TimelineWorld.Library.cs` (23,979), `TimelineWorld.Systems.cs` (4,281) | blocked | 50 ref-files incl. `src/Tl.Gen.CSharp/SystemEmitter.cs` (emits `Run`/`RunFused`/`Kind` signatures, e.g. SystemEmitter.cs:311,403,583) + checked-in snapshots (`TlSystemsD10/Kinds/Snapshot.g.cs`, `TlSystemsPackage.g.cs`, `TlSystemsRunner.g.cs`) + 25+ test files, benchmarks, `demos/crowd`. `World` has no consumer/run path. |
| `PlaybackModel.cs` (9,568) | blocked | `PlaybackTable` 40 files (`GateZeroTests`, `PlaybackRebindTests`, 4 `Tl.Packs.Tests` suites, `SystemEmitter`, snapshots, benchmarks, crowd demo); `PlaybackFlags` 29 files. No slot/closing equivalent in `Playbacks`. |
| `PlaybackRules.cs` (14,395) | blocked | `SystemEmitter`-emitted `Crossings`/`Advance`/`Unpark`/`IsParked` calls in 3 snapshots + `Runner.g.cs`; `PlaybackRulesTests`, `PlaybackRebindTests`, `InventoryTests`, `ControlRunTests`. |
| `Requests.cs` (7,719) | blocked | `ClosingList` emitted by `SystemEmitter` (3 snapshots + `Runner.g.cs`); `RequestQueue` pinned by `GateZeroTests`; `RequestOp` used by `tools/Tl.Bake/RunCommand.cs` + `tools/Tl.Gen.Tlb/TlScene.cs` + 3 Core tests. `Requests5` is unconsumed by any runtime path. |
| `Data.cs` (27,990) | blocked | TLB v5/v6 readers: `TimelineRef` in 7 tools files (`Tl.Bake.Bench`, `Tl.Bake.Oracle`, `Tl.Bake.Tests` ×2, `Tl.Gen.Tlb` ×2 incl. the TLB writer `TimelineBakerFast`, `TlbTrace`) + `InternTable`, `TimelineWorld*`; `TypeKey` → `SystemReader`/`TypeAnalyzer`; `SlotRow` → `JobReader`, `TlbTrace`, `Inspection`; `PairTable` → `GateZeroTests`. `Library.Verify` is tlib3-only. |
| `LibraryAssembler.cs` (15,806) | blocked | `tools/Tl.Bake` (`RunCommand`, `LibraryPack`, `KindsPack`) + `Tl.Bake.Tests` ×2 + `Tl.Packs.Tests` ×4 + `Tl.Systems.Tests` ×3 (`LibraryPart` ×10, `LibrarySettings` ×9). `LibraryWriter` writes tlib3 from new specs, not 4.0 TLIB v1/v2 parts. |
| `KindPayload.cs` (6,470), `KindSection.cs` (8,102), `KindTable.cs` (8,761), `KindRules.cs` (15,000), `KindStore.cs` (27,082) | blocked | all wired into `SystemEmitter` + checked-in kind snapshots + `tools/Tl.Bake` (`KindTickTests`), `KindLibraryTests`, `KindLayout`/`KindCatalog`, 4 Packs suites, `KindRun`/`KindSampleHash`/`KindGroupRun`/`KindUnityHash` suites. `Kinds.cs` is a managed monoid, not a span-compatible replacement. |
| `InternTable.cs` (14,681) | blocked | `TimelineTable` used by `TimelineWorld.cs` (`Reference`/`IsLive`), `Checked.cs`, `Inspection.cs`, `tools/Tl.Tlb/TlbTrace.cs`. No replacement landed. |
| `Events.cs` (4,153) | blocked | `samples/Runner` (`GameLogic`, `Program`) + 13 test files (`EventsStorageTests`, `Toolbox`, 8 Systems suites) + snapshot; `EventRecord` pinned by `EventsStorageTests`. `World.Events<T>` is instance-based, not the `global::Tl.Events<T>` surface emitted today. |
| Kept-list `LibraryView.cs` (4,373), `MarkerTableRegistry.cs` (1,865), `TlModule.cs` (2,239), `SystemDispatch.cs` (3,474) | blocked | all emitted by `SystemEmitter` and/or in checked-in snapshots (`LibraryViewReader`, `MarkerTableRegistry.TryResolve`, `ITlModule`/`TlGenerated`, `SystemDispatch`) + `ViewBuilder` tests + benchmarks. |
| Kept-list `BankView.cs` (1,056), `Lane.cs` (5,822), `Inspection.cs` (3,360) | blocked | `SlotView` cluster: `Inspection` uses `LaneEncoding`+`SlotView`; `InternTable` uses `SlotView`; `Inspection` also used by `TimelineBakerFast.cs:2244` bake-failure diagnostics (user-visible text). |
| Kept-list `CoderApi.cs` (3,797), `Hooks.cs` (225), `Compiled.cs` (1,233), `KindAttributes.cs` (960), `TimelineSystem.cs` (466), `MarkerFrame.cs` (910), `Playback.cs` (398) | blocked | authoring surface of 4 packs (`ITlTrack`, `IBlend`, `TlFrame`) + `NewStyle`/`SystemReader`/`TypeAnalyzer` generator + `tools/Tl.Bake` catalogs + generated code. |
| Kept-list `StateHash.cs` (1,155), `PairLayoutDigest.cs` (2,129), `Mathematics.cs` (1,001), `Polyfills.cs` (5,379), `BurstDiscard.cs` (126), `Checked.cs` (7,866), `AssemblyInfo.cs` (764) | blocked | `StateHash`: 4 pack suites + demo + `ThreadInvarianceTests`; `PairLayoutDigest`: `tools/Tl.Gen.Tlb/TlbLayouting.cs` + `TlbLayoutFamilyTests`; `float3`: public API in `packs/Force`, `packs/Motion`, `Tl.Packs.Tests`, `ForceContract`, `PublicApi.approved.txt`; `BurstDiscard`: `[Unity.Burst.BurstDiscard]` on `Events.cs:130`, vendored per `docs/v5-unity.md`; `NativeAlloc`: `GateZeroTests`; `Checked`: `IlScanCheckedTests` + `Tl.Bake` packs + new `Library.cs`; `AssemblyInfo`: 15 `InternalsVisibleTo` grant internals to 4+ test assemblies (removal breaks test builds). |

**Arithmetic.** Need −59,477 B. Candidate pool (the 15 assignment files): 188,314 B content (+ ~525 B path bytes). Safely deletable today: **0 B** — every file's rewiring cost is a generator rewrite (M7), a bake-format switch (M2), or public-API/test surgery. Unblock order, either alone sufficient: **M2** (bake emits tlib3; deletes `LibraryAssembler`+`KindPayload`+`KindSection`+`Data` TLB readers+`InternTable`+`Inspection` = 76,409 B ≥ 59,477) after re-pinning the 861 `Tl.Bake.Tests` byte fixtures in the same change; **M7** (emitter emits v5 bindings; deletes `TimelineWorld*` 28,587 + `PlaybackModel`/`PlaybackRules` 23,963 + `Requests` 7,719 + `KindTable`/`KindStore`/`KindRules` 50,843 + `LibraryView` 4,373 + `MarkerTableRegistry` 1,865 + `TlModule` 2,239 + `SystemDispatch` 3,474 + `Events` 4,153 = 127,216 B ≥ 59,477) after regenerating checked-in snapshots. The sweep itself is plan M10 and must follow both.

**Receipts.** `dotnet test tests/Tl.Core.Tests/Tl.Core.Tests.csproj -c Release -p:TlChecked=true -p:NuGetAudit=false` → 100 passed, 2 skipped, 0 failed; `dotnet test tests/Tl.Gen.CSharp.Tests/Tl.Gen.CSharp.Tests.csproj -c Release -p:NuGetAudit=false` → 71 passed; `python3 -m unittest discover -s tests -p test_dead_code.py` → OK. Budget line unchanged.

`src` re-checked after peer edits in the same worktree (outside this sweep's scope, `src/Tl.Gen.CSharp/Model/SystemModel.cs` changed mid-run): 580416 content + 2199 path = 582615/520000 (over 62,615); this sweep deleted nothing, so no byte delta is attributable here.
## Migration table 4.x → 5.0

| Concern | 4.x | 5.0 | Migration note |
|---|---|---|---|
| World vs TimelineWorld / PlaybackTable / Advance | `new TimelineWorld(ReadOnlySpan<byte> library)` + `new PlaybackTable(entityCapacity, slotsPerEntity)`; per frame `world.ApplyRequests(players); system.Run(world, players); world.Advance(players)`; entities are `int`, timelines are `ushort` ids, slots are per-entity (≤256) with `ClosingList` | `var lib = Library.Verify(blob).Library; using var world = new World(lib, rowCapacity); world.Requests.Start(new Row(i), new TimelineId(1), phase); world.Tick(Direction.Forward);` read `world.Accumulator` | Rows are `Row(int)`; ids are `TimelineId(ushort)` 1-based (0 = none); single `rowCapacity` replaces `entityCapacity × slotsPerEntity`; no `ClosingList`; invalid starts counted in `TickReport.InvalidRequests`, not thrown |
| Library.Verify vs TimelineAsset.Load | `TimelineAsset.Load(bytes)` interns bytes into the process-lifetime `TimelineTable` and returns a `ushort` index (`Of`/`Dispose`); `TimelineWorld(byte[])` throws `ArgumentException` on bad magic/version/digest/row (v1/v2 tlib + TLB v6 blobs) | `LibraryWriter.Write(IReadOnlyList<TimelineSpec>) → byte[]` is the single v5 writer; `Library.Verify(ReadOnlySpan<byte>) → LibraryResult {Ok, Library, Error, ErrorOffset}` never throws (catches → `LayoutMismatch`), checks tlib3 magic `0x42494C54`, version 3, 64-byte alignment, SHA-256 trailer, row validation; lanes read via `LaneValue`/`ForwardPlane`/`BackwardPlane` | Rebake all assets to tlib3; handle `LibraryError` enum instead of try/catch; `Library` pins the blob (`GCHandle`) for the world's lifetime (Unity shim copies to `NativeArray<byte>` instead, see `docs/v5-unity.md`) |
| Tick(Direction) vs Advance / Tick | `world.Advance(players, forward: true)` / `world.Tick(players, forward: true)` (kind-store path); per-entity `PlaybackRules.Step` mutates each `Playback.Tick` in place with loop/hold/next/channel semantics | `world.Tick(Direction.Forward)` / `world.Tick(Direction.Reverse)` computes `tick = (phase + frame·dir) mod duration` once per `(timeline, phase)` run via `Kernels.PhaseToTick`, stable-radix sorts by `(timeline,phase)` then `(kindOrder,kindId,timeline,step,row,substep)`, accumulates via `BroadcastDense`/`BroadcastSparse`/`RunBroadcast`, copies `_acc` to `Accumulator` | `Direction` (`Forward=1`, `Reverse=-1`) replaces `bool forward`; no per-entity tick state (rewind is exact by construction); looping is duration-mod, not per-playback flags; `Requests5.Apply` (`Applied`/`InvalidRow`/`InvalidTimeline`) replaces `RequestQueue.SortStable` + `Drain` for the queue path |

## Remaining gaps

1. Generator binding emit. `MonoidClassifier.Classify` returns `IrResult(IrKind, Op, Type)` with `IrKind` `Monoid`/`Affine`/`Opaque` and is covered by `MonoidClassifierTests`, but nothing in `SystemEmitter`/`JobEmitter` calls it and no emitted binder wires `KindTable<T>.Combine`/`Apply` (grep: zero callers outside `MonoidClassifier.cs`). Closes M6 partial → done.
2. Unity parity run. `docs/v5-unity.md` (M9) lands the vendor list (including the netstandard2.1 scalar-fallback argument for `Kernels`) and the `WorldV5ParityTests` design (same `LibraryWriter.Write` libraries, same FNV-1a `RunWorldHash` over `SingleToInt32Bits`, seed `14695981039346656037` prime `1099511628211`, 97 ticks). The shim code and the PlayMode batchmode receipt (`TL-PARITY` hash list diffed against the .NET receipt) are not in this repo. Closes M9 partial → done.
3. Cold sweep. The §0 verdict in `benchmarks/PhaseClock/README.md` is hot/L1-L2 only (N=1024, uniform-dense 0.050 ns/entity delegate / 0.103 probe). Cold/DRAM-streaming and phase-entropy sweeps (duration 40 → 1024+, sparse rows) are still open per `benchmarks/PhaseClock/README.md` ("M6 needs a cold sweep") and `docs/gate-zero.md` §0 notes; `WorldBench` below measures only hot uniform-dense. Closes the 1.0–2.0 claim lock.

## WorldBench (M10)

Shape: one timeline, duration 40, one step covering the full window
`[0, 40)` with a constant lane `6f` (forward and backward, detected as
`LaneClass.Constant` by `LibraryWriter`), 1024 rows started with
`TimelineId(1)` phase 0, `Tick(Forward)`. Kernel arm: PhaseClock
`Kernels.BroadcastDense(acc, 37, 6f, 1024)` on the same shape (offset 37,
as in `Bench`/`Checker`). Oracle: 97 ticks, FNV-1a hash over
`SingleToInt32Bits` via `Checker.HashSlice` (same seed/prime as
`WorldV5Tests.Hash`); expected value per row is `97 × 6 = 582f`. Both
arms must hash-equal to the oracle.

Measure: warmup 2000 ticks, then min of 7 rounds × 10000 ticks,
`ns/entity = Stopwatch.TotalSeconds × 1e9 / ticks / 1024`; sink hash
prevents dead-code elimination (as in `Bench`). Gain is vs the 4.0
folded baseline 14.96 from `benchmarks/PhaseClock/results/` (folded
hot 14.9567 ns/entity in `results/baseline-receipt.json`, 0.050
ns/entity kernel floor in `results/bench-final.txt`).

```sh
dotnet run --project benchmarks/PhaseClock/PhaseClock.csproj -c Release -- --worldbench
```

`WorldBench.RunWorld` prints `WORLD PASS`/`KERNEL PASS` (or FAIL),
both `ns/entity` numbers, and `gain vs baseline 14.96` for each arm.

Measured 2026-11-10 (Ryzen 5 8500G, .NET 10.0.12, Release): WORLD PASS / KERNEL PASS, oracle=world=kernel=5a7c42b6b1067325, world 6.601 ns/entity (2.27x gain vs 14.96), kernel 0.050 ns/entity. World first landed at 49.96 ns/entity (8 radix sorts per tick); sort-once dirty-flag fast path + single-kind visit-sort skip brought it to 6.60. Remaining overhead is per-visit array building; run descriptors (M5 proper) are the next lever. Full solution: build green, 101 Core + 71 Gen + 861 Bake + 28 Packs + 17 Tlb + 11 Fuzz + 11 Analyzers tests pass, 2 skipped M0 pins.
