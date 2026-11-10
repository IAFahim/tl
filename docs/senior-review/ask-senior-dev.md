# Tl: briefing for the senior reviewer

You will be given the source of the `tl` library (and only that). You will not run anything. You read code and answer like a senior engineer who has to make this the best timeline runtime there is, and who has to be right, because other agents will execute your plan and run every test.

Be blunt. If our design is wrong, say so. If a goal below is impossible, say so and say what the real ceiling is, with the arithmetic. We would rather hear "no, and here is why" than a nice plan that fails at the gate.

---

## 0. Rules for your answer

1. Label every number: **read from the code**, **derived by you (show the arithmetic)**, or **guess**. Never present a guess as a measurement. We have been burned by this (section 6).
2. You cannot run code. So every claim about speed must be a derivation from the code (bytes touched per visit, loads, branches, allocations, cache lines), not "this should be faster".
3. A plan that cannot be verified by someone running it is not a plan. Every step you propose needs a receipt: what to run, what number must come out.
4. A micro-kernel win is not a system win. State which part of the real per-tick path each change touches.
5. Tell us what you could not see. You do not have the Unity side (`tl.unity`). Section 8 lists what we assume about it; give us back a list of what the Unity side must satisfy, so we can take it to that team (that is us, through an agent).
6. Do not trust our own claims in this file without checking them against the code. Where code and this file disagree, the code wins, and tell us.

---

## 1. What Tl is

A timeline engine for games. Data flows one way:

```
Clip  ->  Track  ->  Kind  ->  Column  ->  user systems
```

- **Clip** (`[TlClip]` struct): authored data on the timeline (start, length, your fields).
- **Track** (`ITlTrack<TClip>`): code that turns a clip into a value. `[Feeds]` methods run every tick while the clip plays. `[Pulse(On = ClipEvent.Start|End)]` methods run once.
- **Kind** (`[TlKind]`): combines all values of one type that arrive in one tick (`Combine`), then applies them to the column (`Apply`). Optional `Idle(ref Column)` runs on ticks where no value arrived. One value per kind, so a second value means a second kind or one struct value.
- **Column** (`[TlState]` partial struct): lives on the entity. It keeps its last value when no clip covers the tick (issue #584 is the `Idle` answer to that).
- Timelines are baked offline (`tl bake`) into a `.tlib` (TLIB / TLB1 binary) shared by all entities. Per-entity playback state is small: a row in a `PlaybackTable`.
- Hosts: plain .NET (`TimelineWorld`, `PlaybackTable`, `KindStore`, generated `TlAuto_<Assembly>.Run`) and Unity ECS (vendored copy of Core, generated `ISystem`/`IJobEntity`, Burst). The same bits must come out of both (bit-exact floats; pinned hashes, section 5).

Where data lives (current 4.0): clip data in the baked library (shared); playback state per entity in `PlaybackTable`; per-tick values in a native per-kind `KindTable` (one block made at world start, per-entity slot(s), each entry = 8 B header + value, e.g. 16 B for a 2-float struct) used for one tick; column persists on the entity.

---

## 2. What the owner wants

Verbatim from the owner: make it **"100000x faster, 100000x less memory, 10000x more scalable, perfect, .NET Standard 2.1 fast, Unity ECS fast, world's fastest, most dev friendly, no bullshit, god tier."**

Treat that as a direction, not a spec. Your first job is the reality check:

- Define the baseline for each multiplier (4.0 as shipped on `main`).
- Compute the **floor**: the minimum bytes that must be read and written per *active visit* (one timeline playing on one entity for one tick) and per *idle entity* (zero is the target), given the data model. Use memory bandwidth and cache-line arguments. Say what fraction of that floor we are at today, from the code.
- Say which of the multipliers are physically impossible, which are possible only in specific shapes (e.g. idle entities cost 0), and what the achievable number is for each shape in section 3.
- Then give the plan for the achievable version. If the honest answer is "2x to 5x in these shapes and nothing more", say that.

"Most dev friendly" matters as much as speed. Section 7 shows where devs hurt today.

---

## 3. The workload that counts (the owner's game)

- **Millions of entities** (4M and 16M are our test sizes), most with **few active timelines (1 to 4)**. The whole game is timelines. Cost per tick must follow *active visits*, not entities or capacity.
- **Players:** a player can own up to **1000 timelines**, with **40 to 150 active** at a time. Today the playback table gives every entity the same number of slots; the workaround is two tables. This uniform-slot design is a known weakness (issue #568, "capacity classes").
- **Spawn/destroy churn** (100 players spawning and dying is the normal case). A recycled row on .NET currently gets the old player's End pulse (section 7, #594).
- Must run multithreaded: split by **entity row ranges**, keep canonical per-row order so floats stay bit-identical. Unity: `IJobEntity`/`IJobChunk` with Burst. .NET: `Parallel`-style workers with **0 B warm allocation**.
- Setup time and memory matter (a 16M-playback world is a real case).

---

## 4. Hard constraints (do not break; or tell us exactly which one you want broken and why)

- Core and generated code must compile for **netstandard2.1**, and the Core must also compile **inside Unity 7000.0.0a16 (C# 9 in Assembly-CSharp)** as a vendored copy, and Burst-compile in the hot loops. No features Unity cannot take.
- **Unmanaged data only on the runtime path.** No managed arrays, no GC allocations per tick, no `static` mutable state, no LINQ, no virtual calls in hot loops. (Repo rule. The 5.0 attempt broke it, section 6.)
- **0 B per warm tick**, including worker threads.
- **Bit-exact results across hosts and thread counts.** Pinned hashes that must never change: `13943614770702343869`, `6607956939547305625`, `12545803591333190611`. Pack hashes: Force `7683521458411619355`, Stats `12183504288633381751`, Inventory `5645869165897381019`, Motion `14881653450773647005`. Scene hashes: inventory `6806057278687917896`, inventory-fast `17812265086550969911`, inventory-mixed `15172252175353637725`, stats `13171269908726092120`, crowd `18108985316199147936`, hello `4684325832958264737`. If your design changes the order of float operations, say which hashes would change and whether that is avoidable.
- **Source budget (enforced by `benchmarks/source_budget.py`):** `src` 520,000 bytes, shipped tools 345,000 bytes, samples 66,000. On shipped 4.0 `main`: src 487,962, shipped-tools 344,638 (362 bytes of headroom). A new feature must pay for itself by deleting code. Budget waivers are the owner's call, not yours: tell us the cost, do not assume it.
- **No code comments** anywhere. Small files and short methods are expected.
- Public API is approved/snapshotted; changing it is an owner decision. Say precisely which public types change.
- TLIB/TLB1 baked format: changing it is allowed only if you show the migration and the byte-for-byte conformance story (there are published conformance vectors).

---

## 5. State of the code today (4.0, `main`)

Measured gates on `main`: Bake 865 tests, Core 31, Packs 38, Gen.CSharp 47, Tlb 17, Fuzz 11, Analyzers 11, Systems 113, Python 99. All green. (Counts, not speed.)

Largest source files you will meet (bytes, from the 5.0 branch listing; names are stable): `SystemEmitter.cs` 74k, `SystemReader.cs` 65k, `World.Systems.cs` 50k, `World.cs` 35k, `Data.cs` 28k, `KindStore.cs` 27k, `TimelineWorld.Library.cs` 24k, `TypeAnalyzer.cs` 23k, `Library.cs` 22k, `NewStyle.cs` 20k, `Playbacks.cs` 16k, `KindRules.cs` 15k, `PlaybackRules.cs` 14k.

Known weaknesses of 4.0 (from our own reading, please confirm or refute):

1. Uniform slots per entity: wasteful for "millions with 1 to 4" mixed with "players with 1000". Memory per playback row is not something we have a clean number for. Derive it from `PlaybackTable`.
2. Setup cost at 16M playbacks (about 492 to 537 ms measured on our machine, 4.0).
3. Tick cost shape: we do not have a clean ns-per-active-visit figure for 4.0 that we trust. Derive what the inner loop must do per visit.
4. The generated systems carry a lot of emitted code (emitter and reader are about 140 kB of the budget). Maybe the generator is where the bytes go.
5. K6 ideas already filed for the Unity/.NET kind path, not done: cache a frame-row pointer in span entries (#567), capacity classes (#568), one job per kind (#569), `ExecuteBatch` SIMD over entries (#570), merge pulses immediately before the target system (#571), group per-tick work by kind (#552), idle rows within 1.5x of .NET in Unity (tl.unity #8).

---

## 6. What happened with "tl 5.0" (the rewrite attempt). Read this twice.

The plan (a design doc, "time as a function, phase-sorted cohorts, TLIB v3, lanes, monoid kinds, kernels, threads") was written by another AI and implemented by an agent on branch `feat/591-integration` (tl issue #591, PR #592) with Unity parity on `tl.unity` branch `feat/29-burst-world-parity`. Outcome, honestly:

- The micro-benchmarks looked great (kernels up to about 13x). **They were kernel numbers, not system numbers.**
- The first real `World` was **1.3x to 3.7x slower than 4.0 at millions of entities.** The implementation had drifted from the plan: run descriptors rebuilt every tick, phase-grouped scatter (`BroadcastSparse`) instead of row-ordered gather, an extra copy pass, per-tick allocations, and managed arrays (violating the repo rule).
- After profile-driven fixes (bounds checks of 1.1 to 2.2 ns per visit, copies, allocations, JIT tier-up, a fused kernel with a precomputed values stream, native buffers) the final branch was **independently re-measured by a second agent** (fresh clones of tl `1263c49c` and tl.unity `10fab5e`, Unity 7000.0.0a16, 12 cores, 11 job workers, 6 runs per shape, first run discarded, medians of runs 2 to 6, spread at most 7%):

| Unity, ms per tick (median) | 5.0 BurstWorld | 4.0 KindTick | 4.0 Advance |
|---|---|---|---|
| 4M x 4 (16M visits), all workers | 7.18 (0.449 ns/visit) | 18.25 (1.14 ns/visit) | 33.47 (2.09 ns/visit) |
| 4M x 4, 1 worker | 35.45 | 134.6 | |
| 1M x 1, all workers | 0.99 | 4.02 | 3.27 |
| 1M x 1, 1 worker | 5.68 | 30.27 | |

  Speedup of 5.0 vs 4.0 KindTick: 2.54x (4M x 4, N workers), 3.80x (1 worker), 4.06x (1M x 1, N), 5.33x (1 worker). Caveat from the verifier: the arms do not do identical work. 4.0 KindTick only steps the tick and computes no lane values; 4.0 Advance does not step Tick; 5.0 computes the lane sum. Also each arm's hash is checked against its own scalar oracle, and the hashes differ across arms, so "bit-identical to 4.0" is not what the Unity test asserts.
- .NET (author's `--timelinebench`, 4M x 4): setup at 16M playbacks 5.0 457 ms vs 4.0 487 ms (medians of 4 runs), same final hash `b352044711105725` for both in all 5 runs. 5.0 at 8 workers was noisy (4M x 4 from 10.6 to 21.0 ms across runs). Closest many-timeline shapes in .NET: 150-timeline library, 1000 entities, 4.0 0.847 ms vs 5.0 0.581 ms (1.46x); 10000 entities, 4.0 8.10 ms vs 5.0 1.74 ms (4.72x); note 4.0 is single-threaded there and 5.0 uses 8 workers.
- **Not tested by anyone:** a library of 1000 timelines with 40 to 150 active per entity (a few thousand entities) in either engine; Unity BurstWorld warm allocation with worker threads (the zero-alloc tests cover only the sequential `Tl.World`; .NET 0 B was measured on the main thread only); the four pack hashes and six scene hashes on 5.0.
- **Weakened checks on the branch:** a timing ratio was loosened 2.5 to 7.0 inside the branch and later replaced by an exact-count test (no numeric threshold loosened relative to main; the new `WorldScaleShapeTests` still has a timing assertion `slopeB <= slopeA*3 + 100000`). Non-numeric: `NewStyleAnalyzerTests` lost its pinned-key assertion and two generated-code assertions; the Force, Inventory, Motion and Stats pack tests no longer call the generated `TlAuto_Tl_Pack_*.Run(...)`, they hand-inline the kernels.
- **Unity vendored copy:** `com.tl.unity/Core/World.cs` (469 lines) matches no committed `tl` `World.cs` (the tl branch has 912 lines). The Unity "5.0 path" is `BurstWorld` over a vendored copy, not tl's final `World`. Fresh clone compiles and passes in Unity (EditMode 69 total / 68 passed / 1 skipped; PlayMode 30 / 30; error CS 0).
- **Source budget: src grew from 487,962 (main) to 670,400 / 520,000** (62 to 74 files, +182,438 bytes), samples 66,752 / 66,000, shipped-tools 351,248 / 345,000. The author's own ledger says getting under means deleting the `TimelineWorld` stack (about 112 kB), a public API change.
- The branch at one point only compiled with uncommitted files (fixed: fresh clone now compiles).

Decision recorded on #591: PR #592 is **not merged**; 4.0 stays shipped; no budget waiver.

Lessons we wrote into the rules (AGENTS.md "Replacing an implementation"): profile first; fair A/B on the same machine and worker count, median of 5; measure in Unity with Burst at owner scale; replace only at >= 2x with setup and memory no worse, 0 B, bit-exact, no loosened test, clean-clone compile, and a second agent reruns it; the budget does not grow (delete the old path in the same change).

**What we want from you about 5.0:** is the 5.0 *design* the right one, or was the plan flawed, or only the implementation? What did it get right that 4.0 should adopt without replacing it (threads, fused kernel with precomputed values stream, native buffers, count-based tests)? What is the smallest set of changes to 4.0 that gets the 5.0 wins inside the budget? Would you start from 4.0, from 5.0, or from a third design?

---

## 7. Developer pain (our "headaches"), each a real incident

These are experience problems. "Most dev friendly" means these disappear by design, not by documentation.

1. **Adding a sideways move to the Hello teleport confused a new programmer** (they added a second field and the output stayed 0; they said our code was broken). Cause: values flow through a kind that carries one value type; a second value needs a field on the same struct or a second kind, and an unconnected field gives a silent 0. Issue #593 (warn when a clip or column field is never connected) is blocked by the shipped-tools budget (362 bytes headroom).
2. **The ability window** (double jump allowed only during a clip): the column keeps the last value after the clip ends, so the flag stays true. Fix is either an explicit "off" clip or the kind's `Idle`. `Idle` existed on .NET and was **silently ignored in Unity** until tl.unity #590 was fixed (tl.unity `main` 939a8fb). Silent differences between hosts are the worst kind of bug.
3. **Recycled player rows on .NET:** `StopAll(row)` mid-clip fires that clip's End pulse (flagged `Interrupted`) on the next `Tick`. A player respawned into that row in the same step receives the old player's "come back" move (#594). Intended behaviour, but it is a trap. In Unity a new entity is a new row, no leak.
4. **Unity setup traps:** an entity without a Playable Director gets no column component (silent); `Tl.Playback` is ambiguous (use `Tl.Unity.Playback`); the README's example did not compile because the sample's `foreach` has no braces; the editor must be exactly 7000.0.0a16.
5. **Kinds are identified by name** so two packs can clash (#582 wants full type name); clip mirror and menu stub names clash for same-named clips in different namespaces (tl.unity #28).
6. **No way to see when pulses/markers fire without running** (#586); **`tl run` cannot export a column per tick** (#585); **`tl bake` writes `Kinds.g.cs` and `Timelines.g.cs` beside the project file** (#587); **clips cannot hold text or media references** (#588); **no deterministic frame capture** (#589).
7. **Per-timeline Seek, reverse and speed** are not available (#577). Portable JSON round-trip (#579) and portable kind packs (#580) are open. New-style systems are not generated in ordinary analyzer builds (#581).
8. **Authoring in the Unity Timeline window** has rough edges: markers (#27), invalid-track warnings (#24), the zero-config designer flow plan (#22).
9. **The bake needs a seed `.tlib` to build the first time** (the build needs the file it is about to produce): bake, rebuild, rebake dance.

Two reference examples of the experience we want to be trivial (both exist in `demos/`):

```csharp
public struct Move { public float Forward; public float Side; }

[Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
public static Move JumpAtClipStart(in TlFrame<TeleportClip> frame)
    => new Move { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };
```

```csharp
// clip 30..60 sets a bool true, clip 60..150 sets it false (or Idle resets it)
[Feeds(typeof(DoubleJumpKind))]  Combine: total |= next   Apply: player.CanDoubleJump = allowed
```

Ask yourself: why does a newcomer need to know about kinds, pulses vs feeds, columns, a seed tlib and a code generator to move a cube sideways? What is the smallest model that covers the owner's game and removes these traps?

---

## 8. What you cannot see: the Unity side (assume these, and tell us what breaks)

We will relay your Unity requirements. Current facts about `tl.unity` (you do not have the code):

- It vendors a copy of Tl Core and the bake tools (synced by script, pinned to a `tl` commit) and generates `ISystem`/`IJobEntity` code from the same analysis.
- Authoring: Unity Timeline window (Playable Director + Timeline asset) -> bake in a SubScene -> entities. PlayMode tests run in a SubScene with a Director.
- Runtime: unmanaged ECS only (`ISystem`, `IJobEntity`/`IJobChunk`, Burst); no `SystemBase`, no managed components, no GameObject lookups.
- A Unity "row" is an ECS entity; storage is chunked, so contiguous row ranges are chunk ranges, not one array. Per-tick kind tables are native buffers (`NativeArray`-style, caller-owned).
- Unity 7000.0.0a16 only. C# 9 in the main assembly. One editor process at a time on the build machine.
- Known gaps: `Idle` across assemblies and rewind of an assigning `Idle` untested in Unity.

Give us, as a list: the exact interface Core must expose so Unity can run the same kernels (struct layouts, function shapes, what must be `unmanaged`, what must be Burst-compilable, how rows map to chunks, how threads should split), and every place your design needs something from Unity that may not exist.

---

## 9. What we want back (format)

1. **Verdicts** (one paragraph each, no hedging): (a) what is the real performance ceiling of this data model per shape in section 3, with arithmetic; (b) is 4.0's architecture limiting us or is it the implementation; (c) is 5.0's design right.
2. **Findings from reading the code:** a ranked list of concrete problems with file and method names (hot-loop costs, indirections, memory per playback, anything that scales with capacity instead of active visits, allocations, hash-order or float-order hazards, places where the generator emits more than it needs).
3. **The one-shot plan:** ordered phases. For each phase: files to change or delete, new types (public/internal), the byte budget effect (net bytes; the plan must come out **under** the budget), which pinned hashes could move (ideally none), the receipt that proves it (what to run, what number, what it is compared with), the risk, and a kill rule.
4. **The deletion list** that pays for the new code. Be specific, with the consequence of each deletion.
5. **Data layout:** per-entity and per-playback bytes before/after, with the arithmetic.
6. **Threading design:** how work is split, how canonical order is kept, how 0 B is kept.
7. **Developer experience design:** the smallest model that fixes section 7. Show the before/after code for the two examples.
8. **What Unity must provide** (section 8 list).
9. **What you disagree with** in this briefing, and what you would refuse to promise.

Keep it concrete. Skip praise. If a requirement here contradicts another, say which one you would drop.

---

## 10. Appendix A: every issue, both repos (state as of this file)

Open issues come with their body and the last comment (trimmed). Closed issues are listed by title only; ask if you want the detail. Issues marked CLOSED were verified done (commits on main) and closed today by the owner's assistant.


### tl: 15 open, 253 closed


#### tl#535 Restore the floating 10.0.x CI toolchain after the 10.0.403 CDN rollout completes

## Problem

a11d42b7 (PR #534, #490 evidence) pinned every `dotnet-version` in `.github/workflows/ci.yml` from `10.0.x` to `10.0.402` because the wildcard resolved to the hours-old 10.0.14 runtime / 10.0.403 SDK, which aka.ms serves from `dotnetorigin.blob.core.windows.net` and which returned HTTP 409 on every cold runner while Microsoft's rollout was gated. GA'd patches (10.0.402 SDK, 10.0.13 runtime) resolve from `builds.dotnet.microsoft.com` and serve 200, which is what the pin now installs.

## Change

Flip all five `dotnet-version: 10.0.402` entries back to `10.0.x` once the rollout completes.

## Acceptance receipts

- `curl -s -o /dev/null -w "%{http_code}" https://dotnetorigin.blob.core.windows.net/dotnet/Sdk/10.0.403/dotnet-sdk-10.0.403-linux-x64.tar.gz` returns 200 (or aka.ms no longer redirects new patches to the gated origin).
- All five CI jobs pass on the restored wildcard.
- No other files change.

## Affected boundaries

`.github/workflows/ci.yml` only.

## Dependencies

None; purely time-gated on Microsoft's CDN rollout. Started during the 2026-10-25 10.0.14/10.0.403 rollout.


*Last comment:* Scope update from the #490 landing: the benchmark run on main at 368c3e01 failed on the same dotnetorigin.blob 409, and fuzz-nightly would have followed tonight — those two workflows were the last ones still floating on `10.0.x`. PR #536 (merged 30ff79a3) pinned both to GA 10.0.402, matching the exact-pin convention release-artifacts, deploy-playground, and publish-nuget already follow.  Restore scope stays as written: flip ci.yml's five `10.0.402` entries back to `10.0.x` once the rollout serves the new patches. The auxiliary workflows keep their exact pins per the repo's standing convention, so "no other files change" still holds for this issue.  Toolchain pin state after 30ff79a3: ci.yml  ...[trimmed]


#### tl#537 Ship the Tl 4 playback model

# Ship the Tl 4 playback model (parent)

Parent for Roadmap.impl.plan.md v2. Sections 1-4 + decisions D1-D13 are the scope.

## Owner checklist D1-D13
- [ ] D1 per-world dense ids
- [ ] D2 eager fold at load
- [ ] D3 remove Appendix A API in 4.0
- [ ] D4 netstandard2.1+net10, C#9 surface
- [ ] D5 plain Frame, ushort flags
- [ ] D6 JSON v2
- [ ] D7 consumer surface, caps unchanged
- [ ] D8 playbacks/channels, Run never moves ticks
- [ ] D9 delete first
- [ ] D10 fold lanes per step
- [ ] D11 Unity authoring
- [ ] D12 ITlModule
- [ ] D13 2.1 perf parity

Refs: #56, #356.


*Last comment:* ## Final ledger (all reachable work is on main, green)  Merged to main: #545 (two-track lock), #546 (LiveBatch + parity receipts), #547 (P1 world/2.1/ITlModule), #550 (P2+P3: library v2/v6, playback model, generator, tests), #551 (P4 cutover: old API out, README/Runner/migration/D9). Closed: #538 #539 #541 (+#544 auto-closed). Verified on main: slnx build 0 errors, Core/Systems/Gen/Bake/Tlb suites green, budget PASS, Runner --verify green, release-artifacts COMPLETE. - #540: perf A/C/D gates fail on apples-to-apples data (dispatch floor); options + recommendation recorded; needs your fork call. - #542: tl.unity#3 scaffold PR open; editor receipts need a qualified Unity editor. - #543 (P6): b ...[trimmed]


#### tl#540 P3: Play timelines through generated systems

# P3: Play timelines through generated systems

P3 plain Frame, Playback table, Requests Events, ApplyRequests Advance, generator systems kernels Run wrappers, ExecuteBatch, net10 SIMD fast path, both-TFM gates, Appendix B fixtures.

Parent: #537. Plan: Roadmap.impl.plan.md v2.


*Last comment:* ## Fallback + ownership fixes landed on the stack (all green)  - Null-lane fallback: emitter generates slow Frame+Fold path when `FoldLane == null` (no more throw); fused kernels declare locals (was a missing-prologue bug); new `MissingFoldLaneFallsBackToFrameFold` test proves exact values. - View ownership: `AdoptView` + `_ownsView`; world never frees borrowed views (fixes teardown double-free; smoke exit 0). - Systems 45/45, full battery green (Core 1168+647, Gen 177, Bake 800, Tlb 17), snapshots regenerated via sanctioned env vars. - Gate receipts committed: `benchmarks/SystemsCompare/results/{old-baseline,new-prefast{,-21},new-fastpath{,-21}}` (raw BDN JSON/CSV/MD/HTML + logs).  Perf gat ...[trimmed]


#### tl#542 P5: Unity ECS Burst runtime

# P5: Unity ECS Burst runtime

P5 com.tl.unity package on 2.1 Core: buffers singletons groups jobs TimelinePlayer Slots API, play-mode + Burst + hash tests, IL2CPP sample.

Parent: #537. Plan: Roadmap.impl.plan.md v2.


*Last comment:* Owner decision: drop the qualified-6000.7 path (no 4GB download; verify on Unity 7 only). P5 evidence stands as the 7000.0.0a12 probe: 16/16 package EditMode green + 600-step Runner hash parity bit-exact vs .NET (13344555154516429701). Qualified receipts (6000.7 Burst/IL2CPP/PlayMode) explicitly waived, not silently skipped.


#### tl#543 P6: Unity editor authoring and bake

# P6: Unity editor authoring and bake

P6 TlLibrary Track Clip Marker assets, exporter, bake runner, player authoring + validation, editor-authored Runner byte-identical bake.

Parent: #537. Plan: Roadmap.impl.plan.md v2.


*Last comment:* P6 authoring validation done on Unity 7000.0.0a12 (probe-only, per owner no-download instruction). New in-editor test closes the loop: author JSON --(tlb bake via repo-built Tl.Bake.dll, shelled out from EditMode)--> game.tlib --(load)--> 600-step playback. Receipts: bake exit 0, fresh bytes byte-identical to committed game.tlib (deterministic bake), playback hash 13344555154516429701 bit-exact vs .NET Runner. Full suite 18/18 green (16 package rule tests + parity + bake-roundtrip), error CS 0. Shipped P6 editor UX (in-import bake, hot-reload) still needs the C#9-vs-10 + ImplicitUsings owner calls from #542.


#### tl#552 Group per-tick timeline work by kind

Plan: `Roadmap.Kinds.impl.plan.md` ("plan 2"), which follows `Roadmap.impl.plan.md` (#537). It targets the per-entity dispatch floor measured on #540 (A 21.4x, C 10.1x, D 4.55x): kinds group per-step work by kind instead of visiting every entity, playback and timeline each step.

## Owner checklist (section 3): agents implement the recommended column unless a box is changed

- [ ] **K1** Execution model: Kinds replace per-entity dispatch for new code. `[TimelineSystem]` stays as the column and context provider and the scheduling unit; a system runs its kinds one kind at a time.
- [ ] **K2** Where kind buffers live: On the owner entity itself, not on child entities.
- [ ] **K3** How long entries live: Span entries last from clip start to clip end; pulse entries last one step.
- [ ] **K4** How kinds combine: Game-defined static `Combine(ref T total, in T next)` and `Apply(in T total, …owner parameters)`, found by name (C# 9, no static abstract members). Mode `Each` applies every entry in order, without combining.
- [ ] **K5** Plan 1 consumers (`ExecuteActive`): Keep plan 1's results exactly. Each system's legacy consumers run as one implicit kind in plan 1's order (entity, slot, step, consumer). New behaviour uses kinds. Removing the legacy path is a later decision (K5 phase).
- [ ] **K6** Inline capacity: `tlb` computes it from the library (formula in 4.6), caps it by bytes (64 inline bytes per kind by default) and writes it to `Kinds.g.cs`, only when a number changes. Overrid ...[trimmed]


*Last comment:* Kinds status: K1, K2, K2b, K10, K3, K4, K5 .NET side and `tlb kinds` are on main (0c4ae5a6). The Unity side lands in IAFahim/tl.unity main. Still open: K6 follow-ups (#567-#571), P6 (#543, clip TimelineId reachability), K-B perf judged against this machine's .NET number (2.06 ns) per tl.unity#8.


#### tl#567 K6: Cache a frame-row pointer in span entries

Parent: #552 (plan `Roadmap.Kinds.impl.plan.md`, section 5, phase K6). Design and decisions live in #552; read it first. K6 follow-up: do only on measured evidence.

- **Atom:** 16-byte entries caching the library frame-row pointer; only if K-A shows the library lookup dominates.
- **Acceptance:** K-A improves with bit-equal results, entry layout documented. Decide keep/drop with measured evidence; if kept, full tests + benchmarks + docs as in K2.


#### tl#568 K6: Capacity classes for owners with very different needs

Parent: #552 (plan `Roadmap.Kinds.impl.plan.md`, section 5, phase K6). Design and decisions live in #552; read it first. K6 follow-up: do only on measured evidence.

- **Atom:** several entry types per kind (capacity classes).
- **Acceptance:** worst-case owners keep inline, small owners don't pay for them. Decide keep/drop with measured evidence; if kept, full tests + benchmarks + docs as in K2.


#### tl#569 K6: One job per kind as an option

Parent: #552 (plan `Roadmap.Kinds.impl.plan.md`, section 5, phase K6). Design and decisions live in #552; read it first. K6 follow-up: do only on measured evidence.

- **Atom:** one job per kind for few kinds + huge entity counts.
- **Acceptance:** beats one-job-per-system on that shape, loses nowhere else. Decide keep/drop with measured evidence; if kept, full tests + benchmarks + docs as in K2.


#### tl#570 K6: ExecuteBatch for kinds (SIMD over entries)

Parent: #552 (plan `Roadmap.Kinds.impl.plan.md`, section 5, phase K6). Design and decisions live in #552; read it first. K6 follow-up: do only on measured evidence.

- **Atom:** batch path over entries.
- **Acceptance:** measured speedup with parity receipts. Decide keep/drop with measured evidence; if kept, full tests + benchmarks + docs as in K2.


#### tl#571 K6: Merge pulses immediately before the target system

Parent: #552 (plan `Roadmap.Kinds.impl.plan.md`, section 5, phase K6). Design and decisions live in #552; read it first. K6 follow-up: do only on measured evidence. This is the K8 latency removal.

- **Atom:** per-kind immediate merge instead of next-Tick deferred (K8 latency removal).
- **Acceptance:** same determinism guarantees, documented ordering, latency gone. Decide keep/drop with measured evidence; if kept, full tests + benchmarks + docs as in K2.


#### tl#588 Clips cannot hold text or media references

**What happened.** A film wants "show this caption" and "play this footage". Clips are unmanaged structs, so I stored an integer id (`Line`, `Footage`) and kept the text and file names in a side table (`script/*.json`) that the generator reads. It works, but the timeline JSON on its own no longer says what the film says.

**Wanted.** A supported string table in the library (for example `TlText` ids resolved from the baked library) so a clip can carry a label that survives bake, export and the Unity Timeline Inspector. The same table would serve damage text, dialogue and sound names in games.

Found while dogfooding: demos/film in IAFahim/tl (the release and tutorial videos are Tl timelines).


#### tl#589 (Unity) Deterministic frame capture for demos

**What happened.** To put real Unity footage in the films I wrote `demos/film/unity/FilmCapture.cs`: an EditMode test that enters Play mode with `Time.captureFramerate = 60` (one frame is one Tl tick), renders the main camera to a RenderTexture every second frame, and logs the tick and position. It worked on the first run (Hello Tl: x = 5 at tick 30, 13 at tick 90, 5 at tick 120, the same as the .NET run).

**Wanted.** Ship it as a supported tool in `com.tl.unity` (menu `Tl > Record Frames`, plus a command line entry), so any project can record a timeline-driven scene deterministically and so tests can compare pictures by tick. It needs a real graphics device (not `-nographics`), which should be documented.

Found while dogfooding: demos/film in IAFahim/tl (the release and tutorial videos are Tl timelines).


#### tl#591 Land tl 5.0: run-descriptor World, binding emit, deletion sweep

Problem: tl 5.0 (time-as-function, Library v3, phase clock) exists as an additive stack in `Tl.Core`/`Tl.Gen.CSharp` but the 4.0 code paths it replaces are still shipped, so the 520,000-byte production source budget fails (579,477 bytes), the generator monoid classifier has no emit callsite, `World.Tick` is serial-only, and the Unity shim has no parity receipt. PhaseClock experiment verdict: kernel 0.050 ns/entity, mixed/sparse 0.480 ns/entity vs 4.0 folded baseline 14.96 ns/entity on this machine (Ryzen 5 8500G, .NET 10.0.12) — records in `benchmarks/PhaseClock/results/`.

Invariants: deterministic, bit-exact forward/reverse, 0 B warm allocation, no managed allocation/locks/delegates on warm playback paths, no code comments in `src/`, public API approvals regenerated when the surface changes, TLIB v3 only (v1/v2/TLB-v6 readers removed with 4.0).

Owned files (workstreams):
- `feat/5-world-run`: `src/Tl.Core/World.cs` — M5 run-descriptor Tick, M8 `ParallelApply` callsite.
- `feat/5-gen-emit`: `src/Tl.Gen.CSharp/**` — M7 monoid binding emit via `MonoidClassifier`.
- `feat/5-unity-parity`: `docs/v5-unity-parity.md` + `com.tl.unity` shim — M9 PlayMode batchmode parity.
- `feat/5-deletion-sweep`: `src/Tl.Core` 4.0 files per `docs/v5-status.md` deletion ledger — source budget ≤ 520,000.

Acceptance receipts:
- `python3 benchmarks/source_budget.py` — src line ≤ 520,000.
- `dotnet build tl.slnx -c Release` + `dotnet test tl.slnx -c Release --no-build` — all suites green (101 Core +  ...[trimmed]


*Last comment:* Gate check on 1263c49c / tl.unity 10fab5e (I measured the budget myself in a fresh clone; the other numbers are the author's and are being rerun by a second agent).  Measured by `benchmarks/source_budget.py` on the branch: src 670,400/520,000 (was 661,821 at the last review, so it grew), shipped-tools 351,248/345,000, samples 66,752/66,000. Gate item 5 (budget does not grow, 4.0 deleted in the same change) is not met.  Author-reported and credited: Unity 4M x 4 2.13x, setup 16M roughly equal to 4.0, native buffers, clean-clone compile, count-based test (timing threshold removed).  Decision for now: PR #592 stays unmerged and 4.0 stays the shipped path. No budget waiver. Per docs/budget-ledge ...[trimmed]


#### tl#593 Warn when a clip or column field is never connected (silent 0 in Hello Tl)

**What happens.** A newcomer extending Hello Tl added `SideSwing` to `TeleportClip` and to the `Teleported` column, and nothing moved. The build has 0 warnings and 0 errors, the timeline accepts `"SideSwing": 2`, and the column stays 0. Nothing reads the clip field and nothing writes the column field, but Tl says nothing about either.

**Repro.** `demos/hello/Teleport.cs` plus `public float SideSwing;` in both structs, then `dotnet build` and run: clean build, `SideSwing` always 0. The working version is `demos/hello-side`.

**Proposal.** Two warnings from the generator, in the style of the existing `TLGEN98` ("consumer fits no system; it never runs"):
- a field of a `[TlClip]` struct that no track method of that clip reads (`frame.Clip.<Field>` never appears);
- a field of a `[TlState]` struct that no kind `Apply`, track or consumer writes.

Both need a body scan, so expect false positives for columns written by user systems or Unity jobs: start with the clip-field rule, make it a warning with an opt-out attribute, and add a test that builds the silent version and expects the warning.

**Constraint.** shipped-tools is at 344922/345000 bytes, so this needs a budget decision or has to replace something.

Docs shipped meanwhile: `demos/hello/README.md` ("Add a value") and the tl.unity README after the first example.


#### tl: closed issues (title only)

- #2 Guarantee C# batch alias safety and whole-call atomicity
- #3 Make the C backend total and honor the 16-bit track contract
- #4 Generate timelines incrementally during IDE and Unity authoring
- #5 Define portable timeline assets and backend conformance
- #6 Close the semantic correctness, inverse, and coverage matrix
- #7 Ship and validate a Unity ECS and Burst package path
- #8 Verify one-install C# packaging and reproducible alpha.2 artifacts
- #9 Make GitHub the durable multi-PC and multi-agent control plane
- #10 Reduce the 256-track generated execution cost
- #11 Release v1.0.0-alpha.2 compiler ecosystem
- #15 Expose registry-free typed timeline playback
- #16 Make signed seek replay every simulation frame
- #17 Right-size every bounded runtime and generated value
- #18 Separate the C# generator package
- #19 Compact generated schema routing tables
- #22 Make Release static analysis exact and warning-free
- #27 Design total timeline jobs for .NET and Unity
- #31 Make timeline movement total by construction
- #32 Preserve ordered operations in the neutral timeline plan
- #33 Generate total typed timeline queries for .NET
- #34 Generate synchronized timeline jobs for Unity ECS
- #35 Qualify and publish v1.0.0-alpha.3
- #40 Migrate consumers to generated catalog queries
- #43 Make stacked workstream reviews recoverable
- #49 Make tagged release builds independent of stale outputs
- #54 Explain the generated catalog surface before first use
- #56 Discuss data-authored timelines and typed frame queries
- #57 Reconcile project status with shipped work
- #58 Qualify CI and test dependency updates
- #64 Decide split-repo publication, licensing, and release-tooling rework for tl.c and tl.unity
- #65 Remove the superseded alpha.3 surface
- #67 Ship MagicOnion-standard packages: slim Tl.CSharp, tool-package tlbake, flawless fresh-consumer install
- #70 Ship v1.0.0-alpha.4 to nuget.org
- #75 Ship a .NET grid-influence package that surpasses the reference with PMU receipts
- #76 Add a NuGet quick-start sample that consumes the published packages
- #79 Move the grid-influence library to its own repository
- #81 Force a compacting GC in the dispatch address-stability receipt
- #83 Make the kernel compacting-GC receipt pass in isolation
- #85 Cut kernel warm-path tick cost 4x for multi-row timelines
- #87 Bind metadata-baked kernels and cut mixed warm-path dispatch to direct calls
- #88 Design per-asset playback tables for the data-authored coordinator
- #91 Ship 1.0.0-alpha.6 with the audited repository
- #94 Remediate the repository audit for the alpha.6 release
- #100 Add benchmarked playback samples and the README performance section
- #102 Document the ECS-hosted execution shape and integration cost ladder in README
- #104 Add type-keyed vectorized playback lane: Timeline<T>.Forward/.Backward at the memory floor
- #107 docs: interactive browser playground for the data-authored timeline API
- #108 perf: bake a full game's authoring JSON (20 MB) in under 30 ms
- #111 Export Blender NLA scenes to tl authoring JSON and bake them from a bridge addon
- #113 Slim the frame view: ushort positions, no engine cycle, no game tick, clip length instead
- #115 Bake CLI machine introspection (--json), watch mode, and JSON test config
- #116 Interactive cookbook site on GitHub Pages with live timeline editing
- #121 Ship 1.0.0-alpha.7
- #123 Audit and correct every living document against the shipped alpha.7 surface
- #125 Live authoring page: write C# pairs, get the type JSON, author the timeline JSON, bake and run in one place
- #142 Ship 1.0.0
- #146 Rename the bake CLI command to tlb and consolidate documentation into the README
- #148 Measure a fused Seek+Apply entry point on the typed playback lane
- #149 Pool authored track and clip values behind per-type indices in the baked format
- #150 Return a playable handle from TimelineAsset.Load and drop the Bind step
- #151 Ship v1.0.0-alpha.8 with the tlb command rename and experiment verdicts
- #152 Backfill Project 6 fields after gh token gains read:project
- #155 Pool unique track and clip values per type behind fixed ushort indices
- #157 Consolidate the seven hand-rolled baker copies into one shared implementation
- #160 Add the pair-typed many-timeline lane: Timeline<T,C>.Seek(handles, positions, forward).Apply(effects)
- #162 Update the README and living docs for the 1.0.0-alpha.8 surface and the pair-typed lane
- #167 Cut the pair-typed and set lane row cost toward the single-lane gold path
- #168 Shrink the TLB1 v2 frame slot row and re-receipt the format
- #172 Sync README and live docs with the perf-hunt changes (UTF-8 bake, mixed walk, TLB1 v3)
- #174 Make Timeline<TTrack,TClip> the only public playback API
- #175 Attach-time pair discovery: hosts need an elegant way to know which pairs an asset carries
- #180 Ship the data-driven bake seam: Load returns the timeline index, Timeline.Bake wires host markers
- #184 Ship 1.0.0-alpha.9 with the one-API index surface and the bake seam
- #185 Rewrite the README as a use-case showcase with inline receipts
- #193 Shrink production source to the 128 KB budget and enforce a 200 KB shipped-tooling cap
- #194 Tools perf: batch parallel bake for game start, near-zero GC, SIMD bake stage
- #195 Repo hygiene: benchmarks cleanup, 32 KB samples budget, dead-code sweep outside claimed workstreams
- #200 Fix live playground: scoped bake resolver, editor scroll sync, splitter drift, right console panel
- #203 README: inline the full Showcase pipeline with code, plain C# bodies, condensed literals
- #204 Release 1.0.0-alpha.10
- #205 Show clip names in the authored JSON contract
- #206 Auto-refresh the playground site and README numbers
- #207 Sweep tracked files for necessity
- #209 perf: crowd advance at one million rows beats the published table
- #211 Full branch coverage of the shipped core, SQLite-style
- #214 tlb works with zero arguments: lazy input, output, and assembly discovery
- #215 Evaluate ZString for the tlb tool paths: perf and GC receipts
- #218 Infer missing namespaces with tlb --auto and document type discovery in a Get started guide
- #219 Diagnose data-less clips instead of crashing with ArgumentOutOfRangeException
- #221 Run the README authoring surface in the playground with Live authoring first
- #222 Measure claimed vector-width and bake ceilings against shipped paths
- #224 Document IBake in the README system contract and pin background sub-agent delegation
- #225 Remove dead playground and Blender files from tools
- #228 Follow the JSFiddle four-panel layout on the live authoring page
- #229 Wire Playground.Smoke into the solution gate and CI
- #231 Keep System.Console overloads through wasm trimming for live-authored code
- #237 Replace Seek/Advance with single Step + non-mutating Apply
- #239 Recover the alpha.9 playback floor across every host shape and CPU tier
- #240 Measure playback numbers at steady state with labelled hot and cold columns
- #241 Keep every warm playback method out of tier-0
- #242 Route uniform and wave clocks back to the run kernels
- #243 Make single-row and small-span Apply and Step cost one record lookup
- #244 Ship Vector128 and record-table tiers so non-AVX2 CPUs stay within 2x of AVX2
- #245 Fold wrap and next flags into JIT constants in the gather kernels
- #246 Decide on a shared-clock Apply overload for crowds on one clock
- #247 Vectorize the per-row-id worst case with a flat record arena
- #248 Stop relocating pair bank blocks so hosts can hold stable table views
- #250 Fix finite lanes wrapping instead of clamping in Timeline.Step (smoke red since the Apply/Step split)
- #253 Rare flake: PairHandleLaneTests.UnloadedIndexInCrowdKeepsDiagnostic message selection under shared static state
- #255 Teach frame.Direction / frame.IsBackward everywhere instead of manual FrameFlags.Reverse lookups
- #257 Split apply-only + end-of-frame advance over typed lane columns (Lane<TTrack,TClip>)
- #263 Batched per-entity API: pay the per-call contract once per batch for sparse entities
- #264 Fail-fast playback contract: strip managed diagnostics from the per-entity warm path
- #266 Rename ITrack<TTrack, TClip>.Execute to OnActive
- #267 Rename Step to Advance everywhere, zero compatibility surface
- #272 Re-gate TlJump against the settled playback API (pure Apply, Advance, OnActive)
- #273 Reconcile README.md with the settled playback API after the #239 campaign
- #274 Attribute the per-entity contract cost with PMU counters and Tier-1 disassembly before removing any guard
- #275 Playback misuse fails fast in checked builds and costs zero instructions in Release
- #276 TlJump plays unchanged against main: pure typed Apply paired with Advance, Execute renamed OnActive
- #277 Numbers: label the hand-written comparison as scalar and add the SIMD traffic-floor row
- #279 Bake contexts accept in/ref parameters in any pattern (owner directive)
- #283 BakeReader: a TLGEN74-dropped bake still seeds chains, enabling a false TLGEN74 later
- #286 Finish the per-entity floor: hasNext fold + H5 record advance (0.73/0.76 apply-step)
- #287 Gate the AVX2-only EffectForward/Backward tail loops like the other tier kernels
- #289 One surface: fold Lane<TTrack,TClip> into Timeline<TTrack,TClip> and delete the Lane class
- #290 Playground: clip ending exactly at duration throws ArgumentOutOfRangeException (count = -2)
- #292 Bake consumer-typed parameter receives default instead of the registered consumer instance
- #295 AVX2 double-gather kernel for record walks: worst-fixture <= 0.6 and backward per-entity <= 0.8
- #298 Close 100% test and bench coverage across all shipped methods before the v1.0.0 tag
- #299 Fix and classify the 2,111-finding JetBrains InspectCode HINT sweep
- #300 Dead code, dead links, dead samples, dead benchmark audit + CI prevention legs
- #303 Fuzz and property-test every trust boundary: TLB bytes, authoring JSON, and the pure timeline laws
- #309 Numbers --only runs print double.MaxValue for bake and load instead of skipping them
- #312 Run the playback parity slice on a native ARM64 runner in CI
- #316 Integration-window test wiring: Tl.Fuzz into tl.slnx and the renderer null-Bake unittest
- #325 Pair-isolated fold + no-output consumer surface: Timeline<T,C> plays only its own pair (owner ruling)
- #330 Re-derive the mixed preset receipts on pair-isolated semantics (main red after #323 merged post-#325)
- #332 Teleport dispatch: fire every crossed clip window when a position jumps in one call
- #333 Timeline.Bake accepts explicit ref arguments at the call site
- #336 State the ref-shape consumer purity contract and label the cold fold pass
- #337 Bind same-type Bake arguments distinctly: repeated-type bake parameters all receive the first argument's address
- #338 Mechanically separate the measured consumer shape from live dispatch so the fold cannot execute side-effect-bearing consumer code
- #339 Checked-arm diagnostic for runtime timeline positions outside the loaded asset's domain
- #340 Two-method consumer contract: OnMemo freezes N unmanaged outputs per tick, OnActive composes them with live columns per row
- #346 Ship 1.1.0: two-method consumer contract, BakeRef, range Apply, binding fix, checked diagnostics
- #348 Harden the live-column surface: slot-accurate diagnostics, stack guard, TLGEN gate for unfeedable shapes, silent-skip README correction
- #351 Squeeze every playback operation to its measured floor: PMU-profile the Timeline<TTrack,TClip> surface toward 0.15 ns/row
- #352 Deduplication sweep: collapse parameterized clone families and cut src toward 200,000 B
- #353 Fix every JetBrains Inspect Code finding across the solution
- #356 Widen the consumer surface: memo feeds must not consume the player's live-column budget
- #359 Make Inspect Code resolve the Blazor WASM Playground projects instead of classifying them as artifacts
- #361 Cut the TimelinePair public overload ladder to the canonical consumer surface
- #362 Shrink the ApplyLive stack frame from worst-case 45.5 KB to actual per-pair composition
- #363 Execute the deferred naming renames from the 353 classification record
- #367 Release 1.2.0
- #371 Adopt the upload-artifact v7 features and bump download-artifact to v8
- #372 Cut tlb --auto cold start: MetadataReader table scan instead of per-DLL MetadataLoadContext sweeps
- #375 Bring src back under the 300,000-byte source budget
- #376 Fail fast at typed bind when baked struct layout skews from the consuming types
- #377 Make the Windows authoring loop green: rename-over watch semantics and LF-stable goldens
- #380 Make Play.Core live receipt checksums platform-stable
- #382 Turn on the baked-layout skew guard by default from the C# generator
- #384 Experiment: would Cysharp NativeMemoryArray backing make any path faster?
- #385 Stop the per-step delegate allocation in TimelineRef.Validate
- #387 Reuse the hoisted fail delegate in the Validate pair loop
- #389 Audit game integration correctness and design hazards
- #390 Ship 1.3.0: publish the TLB format v4 package pair
- #392 tl 2.0 vocabulary: Fold, ExecuteActive, Apply-only — retire OnMemo/OnActive/ApplyLanes
- #393 Timeline<,> partial: host pair-keyed playback facades on the pair type
- #396 Perf: single-row / fused-pass fast path for timeline consumption
- #399 Chained TimelineGeneration.Run: make every misuse loud (double registration, unmatched filter, dropped bakes, unvalidated id)
- #400 Baker misses chained layout attributes: baked Layout=0 silently disables the layout fail-fast
- #401 Post-2.0 hardening: ApplyChunk length check, TLB PairCount bound, Checked overlap gap, error-string vocabulary
- #402 Fix the JetBrains Inspect Code warnings accumulated since the 353 sweep
- #404 Grow the process-wide pair and consumer tables (64-consumer wall, 512-pair wall)
- #405 SlotView v2: segmented tables collapse duration-linear memory for long sparse timelines
- #409 Collapse the duplicated lane kernel tails without moving lane throughput
- #413 Expose a read-only runtime introspection snapshot for deep live inspection
- #414 Analyzer package: reject chained TimelineGeneration.Run misuse at edit time (TLGEN85/86)
- #415 Document the TLB1 container byte layout with versioned golden fixtures
- #416 tlb CLI: Unity-style hosting and inspection verbs with a stable JSON contract
- #420 ship the visual timeline viewer on the pinned --inspect/--live JSON contract
- #430 Ship 3.0.0: segmented banks, introspection surface, tlb verbs
- #432 Copy decoded folded lanes through cold inspection
- #435 Feed each consumer's memo-fed columns from its own Fold results in live Apply
- #437 Keep ColumnSet columns and sampled clips valid across a compacting GC
- #439 Reject single-result Apply over a missing or integer fold lane
- #441 Recover the mixed-row playback cost lost in the segmented encoding
- #443 Pin bake arguments for the duration of Timeline.Bake
- #446 Name the Tl.Core version skew when tlb cannot load the game's types
- #459 Keep BakeJson inside the fuzz contract on partitioned-track tails and intern saturation
- #461 Triage and resolve the 2026-10-20 risk review findings
- #463 Adopt the tl roadmap as the staged remediation plan
- #466 Restore mixed-tier bank throughput: the reserved index must not count as a bank hole
- #468 Decode multi-lane segmented banks to their measured values
- #469 Keep permute loads inside bank blocks
- #470 Zero fold scratch and free it on failure
- #471 Fold through a single per-tick path
- #472 Keep asset bytes immutable while blending
- #473 Fold consumers in an order independent of assembly load
- #474 Store lane maps and feed links per view
- #475 Publish bake links with release semantics
- #476 Seal the consumer registry into per-pair arrays
- #477 Detect consumer sets that change after a pair folds
- #478 Fold only requested indices on first use
- #479 Verify content before reviving a dead index
- #480 Diagnose bakes the dispatcher can never call
- #481 Move clocks without folding typed banks
- #482 Diagnose non-float sugar consumers at generation
- #483 Fingerprint enum, fixed-buffer and explicit layouts
- #484 Validate every invariant the TLB1 spec rejects
- #485 Take no lock on dispatch and live playback
- #486 Bind live columns once per column-set shape
- #487 Generate bindings incrementally per declaration
- #488 Prove every playback kernel against the scalar oracle
- #489 Publish bank conformance vectors for host ports
- #490 Split playback into Prepare and Play
- #491 Reject hand-written consumers that declare more than ten owned results
- #493 Retire Roadmap.md now that every §10 atom is filed
- #496 Bank golden snapshot must not depend on process-global intern indices
- #504 Regenerate the TLB1 layout spec for version 5
- #513 Re-check slot keys after bake table rehash and state bake publication in the README
- #520 CLI crashes on an IBake whose consumer identifier cannot be resolved
- #524 Bake suite intermittently fails 'Timeline index N is not loaded' during batch validation probes
- #530 Rest view must report the ABI it was built against
- #538 P1: Run Tl from TimelineWorld on netstandard2.1
- #539 P2: Timeline library with stable ids
- #541 P4: Ship Tl 4.0 with one playback API
- #544 P0: Record decisions and port LiveBatch receipts
- #553 K0: Prove the kind model and measure Unity's numbers
- #554 K1: Bake kind metadata and inline capacities
- #555 K2: Run timeline kinds in .NET
- #556 K3: Run timeline kinds in Unity
- #557 K4: Give each Unity entity exactly its kinds
- #558 K5: Ship kind samples, docs and the legacy-path decision
- #562 K2b: Fold consecutive game pulses per target (opt-in)
- #564 K10: Run kind groups in .NET
- #575 Raise Events<T> from Burst over caller-owned storage
- #576 Linux: demo installer fails at step 10 (PlayMode test prints no result)
- #577 Per-timeline Seek, reverse and speed requests (needed by the playback panel demo)
- #578 Generated plain systems still inline their own copy of the kind-free advance rule
- #579 Round-trip portable timeline JSON without changing baked bytes
- #580 Run portable Force, Stats, Inventory and Motion kind packs on either host
- #581 Generate new-style systems during ordinary analyzer builds
- #582 Identify kinds by full type name so packs and games can share kind names
- #583 Rename [TlState] to [TlColumn]
- #584 A column keeps its last value when no clip covers the tick: kinds need an idle value
- #585 `tl run` cannot export a column per tick
- #586 No way to list when pulses and markers fire without running the timeline
- #587 `tl bake` writes `Kinds.g.cs` and `Timelines.g.cs` next to the project file
- #590 Unity: support Kind Idle in the Unity system generator and re-sync the vendored Core and tools
- #594 StopAll fires the interrupted clip's End pulse in the same Tick: a player respawned into that row in the same step gets the undo


### tl.unity: 5 open, 18 closed


#### tl.unity#9 CI: get both Unity jobs green on the kinds stack

Neither Unity CI job has run green on the K3–K5 stack (#4, #5, #6).

1. **v3 profile gate** (`v3-profile.yml`, `eng/gate-v3.sh unity`): #7 (branch `fix/ci-unity-editor-discovery`) fixes the missing Mono and `unity` command on the self-hosted runner. EditMode now runs 7/8: the project's own allocation test sees 48 B where 0 is expected, and PlayMode does not run. Decide whether that is a real regression or a runner artifact, fix it, then merge #7 and restack #4–#6 onto it.
2. **New `com-tl-unity.yml`** (added in #4): the pin check `eng/sync-tl-core.sh --check` runs on ubuntu, then `eng/test-com-tl-unity.sh` runs on `[self-hosted, unity]`. It passes locally (EditMode 28/28, PlayMode 30/30 on K4) but has not passed on the runner yet. Watch its first run and fix runner-only problems (editor path via `UNITY`, the probe location `TL_UNITY_PROBE`). Set `TL_TLB` on the runner to a tlb that has the `kinds` command, or the capacity-bake test stays skipped.

Rules: batch mode only, one Editor per project path, and never point CI at a developer's open project.


*Last comment:* Both Unity jobs now pick the newest installed Unity 7 editor and install it by name when missing: `unity install 7 --yes --accept-eula --module <platform>-il2cpp` via `python3 eng/unity_editor.py editor` (tl.unity@a1fdeeb). No version is pinned; `UNITY` can override the binary.


#### tl.unity#10 K4 follow-ups: clip TimelineId reachability, editor bake tests, fixture, 6000.x

Gaps left after K4 (#5). Each is real but out of K4's tested scope:

- **Clip `TimelineId` fields**: plan 4.7 says reachable timelines include `TimelineId` fields inside clips. The baker covers StartWith, MayStart and `next` only. That needs plan 1 P6 (IAFahim/tl#543), which has no code yet.
- **Editor bake path untested:** `KindCapacityBake.Bake` (Refresh only on change), the `.json` import postprocessor (deferred with `delayCall`) and the after-reload hook have no tests. Only `Run` is tested, and only when `TL_TLB` is set. Add EditMode tests with a temp library under `Assets/` and a fake tlb script.
- **Unity fixture mismatch:** `Tests/Kinds/Timelines~/game.tlib` includes `run_legacy`, whose `LegacyWalkTrack` exists only in the .NET mirror. So the editor bake cannot run on the full fixture; the test uses a subset. Either give the Unity test game the legacy types (Unity does not dispatch legacy consumers; TLUNITY9105) or drop `run_legacy` from the Unity fixture and re-pin the hash on both hosts.
- **Unverified:** Windows and macOS, and an IL2CPP build of the `KindsRunner` SubScene scene.


*Last comment:* Body edit: removed the older-editor item from Unverified. The package now declares unity 7000.0 (tl.unity@4197c49); only the newest Unity 7 editor is supported and tested.


#### tl.unity#22 Plan: add the package git URL, author in the Unity Timeline window, press Play (zero-config designer experience)

Owner feedback after trying the one-line demo (screenshots: Walker inspector, "Demo Library (Timeline Library Asset)"): this is not what we planned. Score: -100000/10 for the designer experience. The runtime is ready; the authoring and setup experience is a pile of plumbing that only a coding agent can use.

## What the owner wants (the original plan)
1. Add ONE thing to a Unity project: the package git URL. Everything else works. No `tlb` path, no dotnet tool, no clone, no scripts to run, no asmdef marker dance, no "Folder / Timeline Ids / Game Assembly / Namespace / Kinds File / Tlb / Kind Sets" asset.
2. Designers author in **Unity's own Timeline window** (like https://gitlab.com/tertle/com.bovinelabs.timeline and the timeline-heavy libraries in https://github.com/vex-studio): create a Timeline, drag clips on tracks, set values in the Inspector, drag and drop references. Never type names, never write JSON.
3. Unity Timeline **bakes the JSON** (our library data) automatically, and on bootstrap the runtime finds and loads it and the game starts "like magic": entities have their timelines, systems run, press Play and see it.
4. Hot path stays fully unmanaged (ISystem + Burst); designers never touch code; coders add a clip type in a few lines and it shows up in the Timeline window.

## What is wrong today (from the screenshots)
- The demo scene has no camera; Game view is empty. A demo must show something you can see.
- Timeline Player Authoring has "Start With / May Start" as ...[trimmed]


*Last comment:* Reviewed shared-advance atom is on main: tl f14e36a1b67a8ad6c614de6af02cab4a33647d4c and tl.unity 446334f74b9dec1f3d7136a4d191eadbbaca8f98 (same local validation state; Unity push follows core).  The plain generated RunFused now calls PlaybackRules.Advance. Six checked-in plain snapshots were regenerated; no ABI or runtime rule changed. Review removed a redundant Unknown failure branch, retaining the original slot validation location.  Rechecked: solution 900/900, separate Tl.Systems.Tests 77/77; Unity 50 EditMode +54 PlayMode, 0 compiler warnings/errors; sync-tl-core, sync-tl-bake and generator stamp checks pass. All pinned hash assertion tests passed. Two warm, uncontended K runs: A8.0/7.4 ...[trimmed]


#### tl.unity#24 Timeline + > Force: invalid-track warnings and a red 'track has 0 clips' error

Unity 7000.0.0a16 (Linux), com.tl.unity main (dcdc5b9893b7), Timeline 6.7.0, project made by install/unity.sh. Seen in the Editor console after using the Force demo and the Control demo in the Timeline window.

**1. Warning when the + menu is built** (once per stub type, e.g. ForceClip_TlMenu and DashWhenFarClip_TlMenu):
`Timeline Track type 'Tl.Generated.ForceClip_TlMenu' appears to be invalid. Make sure to move the track in its own file named ForceClip_TlMenu.cs, or unexpected errors may occur.`
Timeline's TypeUtility.AllTrackTypes (Editor/Utilities/TypeUtility.cs:104) warns for every TrackAsset subclass whose MonoScript name differs from the type name. The _TlMenu stubs are emitted by Tools~/Tl.Gen.Unity/UnityMirrorEmitter.cs into one generated file (TlUnity.Clips.g.cs), so the check fails. Also logged once: `No script asset for DashWhenFarClip_TlMenu. Check that the definition is in a file of the same name...`. FORCE-DEMO-CHECK.md says the console should have 0 warnings. Suggested fix: emit one file per stub named after its class, or avoid a TrackAsset subclass per stub.

**2. Red error for an empty track.** After + > Force (a new track with no clip yet) both live conversion and Edit-mode preview log `Tl could not prepare Push on Cube: empty tracks with duration > 0 (stages must cover duration): track 1 has 0 clips.` (TlDirectorBaker.cs:35, via TlPreview.Bake and LiveConversionDiffGenerator). This is the normal step 7 of the check doc, before step 9 adds the clip. Suggest ...[trimmed]


*Last comment:* Fixed bugs 1 and 2 in be6d0b3 (main). Bug 3 is not reproduced; leaving the issue open for it.  **1. Invalid-track warning / "No script asset".** Cause confirmed: a Roslyn-generated type has no MonoScript. Timeline's `TypeUtility.AllTrackTypes` warns when `MonoScript.FromType` is null, and adding the stub to a saved `.playable` (`AddObjectToAsset`) logs "No script asset for X". One file per stub (`AddSource("<Class>.cs")`) was tried and changes nothing, and the + menu is purely type-driven, so a per-type entry needs a real type with a real file. The generator no longer emits the `_TlMenu` stubs. `TlMenuStubFiles` (Editor, `[InitializeOnLoad]`) writes one real file per clip mirror to `Assets/T ...[trimmed]


#### tl.unity#29 Burst World parity: bit-exact Burst fast path for the tl 5.0 World

Problem: tl 5.0 ships a run-descriptor World whose sequential `World.Tick` in `Tl.Core` is hash-pinned (uniform dense oracle `5a7c42b6b1067325`, plus mixed, unique, four-pairs shapes in `tl/docs/v5-unity-parity.md` on `feat/591-integration`). The Unity package vendors the sequential World (`com.tl.unity/Core/World.cs`) and passes parity in EditMode, but has no Burst-compiled World: no `IJobParallelFor` over run descriptors, no chunk-parallel accumulation whose hash is independent of thread count. The `docs/v5-unity.md` Known limits in tl say "no Burst jobs run yet" and name `TlBurstJobs.cs` as the shim-owned file.

Scope (this workstream):
- NEW files only under `D:/GitHub/tl.unity` (no edits to vendored `Core/` snapshots, no bake-format changes, do NOT touch `com.tl.unity/Editor/KindReportMenu.cs` — it has an uncommitted local change that belongs to another workstream).
- A Burst-compiled World equivalent: `IJobParallelFor` over run descriptors and chunk-parallel accumulation, one disjoint row range per chunk (per-row addition order preserved), hash independent of thread count.
- Bit-exact hash parity with the .NET oracle per shape: uniform dense, mixed-phase sparse, unique-phase, four-pairs, 64-cohorts-2-active. Oracle hashes from `tl/docs/v5-unity-parity.md` (97 ticks, FNV-1a over `SingleToInt32Bits`, seed 14695981039346656037, prime 1099511628211).
- A Burst parity test class with the same shapes plus thread-count invariance (1 vs 8 threads equal hash).
- A measured recei ...[trimmed]


*Last comment:* ### Commit checkpoint (push-scope fresh run)  - Agent: omp - Machine: nibir-win - Branch: `feat/29-burst-world-parity` off `local/complete` (bbe5d00) - Commit: `d21fe086b48c98a189c71ebdbe7283b0f066d34e` - Push: https://github.com/IAFahim/tl.unity/tree/feat/29-burst-world-parity (origin HEAD:feat/29-burst-world-parity) - Files added (only): `com.tl.unity/Runtime/TlBurstWorld.cs` (+ `.meta`), `com.tl.unity/Tests/BurstWorldParityTests.cs` (+ `.meta`), `com.tl.unity/Tests/BurstWorldLargeBenchTests.cs` (+ `.meta`, measure-only 1M hostile case, carried here), `docs/v5-unity-burst-parity.md`. Zero edits to existing files; `Editor/KindReportMenu.cs` untouched. `WorldV5ParityTests` (14 cases), `Core/ ...[trimmed]


#### tl.unity: closed issues (title only)

- #1 Make tl v3 consumable through NuGetForUnity
- #8 Meet the K-B Unity gate: idle rows within 1.5x of .NET
- #11 Events<T> is not dispatched on Unity (Drain step missing)
- #12 Generator only works when scoped next to the asmdef, and no generator ships in the package
- #13 Plain C# host needs a supported generator for its system Run code
- #14 Unmanaged ISystem route: Tl tick/request/advance systems and KindIndex are managed
- #15 Package test game compiles in user projects and collides with user kinds (Damage, Lift, Locomotion, StatChange)
- #16 Kinds test game.tlib is packed against the .NET assembly, so its pair keys do not match the Unity types
- #17 Start the Linux self-hosted runner so the Unity CI jobs run
- #18 Prebuilt ECS generator output compiles but its components are Unknown Type at run time (Play mode throws)
- #19 Timeline bridge bake writes a second Kinds.g.cs into the library folder (duplicate KindCapacity)
- #20 Kind ids shift when two game assemblies declare kinds
- #21 Generated timeline jobs build their query in OnUpdate and log a runtime warning per system
- #23 Force demo and EcsDemo interfere when imported in the same project
- #25 Events<T> from OnClipStart on ITlTrack never reach TimelineEvent<T> in the zero-config path
- #26 Auto kinds list every auto-system column, so reaching one kind adds all of them
- #27 Author Tl markers (IMarker<T>) in the Timeline window
- #28 Clip mirror and menu stub names clash for same-named clips in different namespaces
