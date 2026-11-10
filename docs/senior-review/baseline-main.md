# Baseline on main (Phase 0 truth gate)

Source measured: tl `main` at `a4d863ac` (src unchanged by the later docs-only commit `014aed09`). No source, test or tool file was changed. Every number is labelled **measured**, **read** (from code) or **not tested**.

Machine: AMD Ryzen 5 8500G, 6 cores / 12 logical, 31.1 GB, Windows 11, .NET SDK 10.0.401, Release. Caveat: three Unity editors and Unity Hub were running on the machine during all runs, so timings are noisy (spread is shown). Harness workers: `--timelinebench` 4.0 arm is single-threaded (`TimelineWorld.Advance` has no worker knob); throwaway probes are single-threaded.

## 1. Gates (measured)

`dotnet build tl.slnx -c Release -m:1`: 0 warnings, 0 errors.

| Suite | Passed / Total |
|---|---|
| Tl.Core.Tests | 31 / 31 |
| Tl.Tlb.Tests | 17 / 17 |
| Tl.Fuzz | 11 / 11 |
| Tl.Gen.CSharp.Tests | 47 / 47 |
| Tl.Analyzers.Tests | 11 / 11 |
| Tl.Packs.Tests | 38 / 38 |
| Tl.Bake.Tests | 865 / 865 |
| Tl.Systems.Tests (separate run) | 113 / 113 |
| Python `unittest discover -s tests` | 99 run, OK, 1 skipped |

`python benchmarks/source_budget.py` (exit 0):

```
src: 62 files; 486012 content bytes + 1950 path bytes = 487962/520000 bytes
samples: 11 files; 64122 content bytes + 391 path bytes = 64513/66000 bytes
shipped-tools: 34 files; 343553 content bytes + 1085 path bytes = 344638/345000 bytes
portable-json-tools: 4 files; 33772 content bytes + 121 path bytes = 33893/36000 bytes
analyzers: 6 files; 15543 content bytes + 301 path bytes = 15844/24000 bytes
```

## 2. Pinned hashes (read by grep)

| Hash | Where it is asserted on main |
|---|---|
| 13943614770702343869 | `tests/Tl.Systems.Tests/KindUnityHashTests.cs:11` |
| 12545803591333190611 | `tests/Tl.Systems.Tests/TwoAssemblyTests.cs:13` |
| 6607956939547305625 | **No test or source file on main contains it.** Only `docs/json-format.md:90` (added with #579, `18f6dc8c`) and the briefing. Either it is asserted under another form or on tl.unity, or it is an unpinned claim. Needs the owner's answer. |
| Force 7683521458411619355 | `tests/Tl.Packs.Tests/ForceTests.cs:12`, `packs/Force/Tests/ForcePackTests.cs` |
| Stats 12183504288633381751 | `tests/Tl.Packs.Tests/StatsTests.cs:10`, `packs/Stats/Tests/StatsPackTests.cs` |
| Inventory 5645869165897381019 | `tests/Tl.Packs.Tests/InventoryTests.cs:10`, `packs/Inventory/Tests/InventoryPackTests.cs` |
| Motion 14881653450773647005 | `tests/Tl.Packs.Tests/MotionTests.cs:11`, `packs/Motion/Tests/MotionPackTests.cs` |

Scene hashes also found: `tests/Tl.Packs.Tests/SceneRunTests.cs:10-14` (inventory, stats, crowd, inventory-fast, inventory-mixed) and `HelloTests.cs:8` (hello).

## 3. Finding 1 spot check (read)

Finding 1 is ignored per the owner. Eight other sites were opened in the real files (main, plus one on `feat/591-integration`). All are paste loss except one latent item.

| Site | Real file | Verdict |
|---|---|---|
| `TimelineRef.Validate` `Next = Slice(52)` | `Data.cs:212` | present, paste loss |
| `TimelineWorld` library build `Next` | `TimelineWorld.Library.cs:164` | present, paste loss |
| `RequestQueue.Grow` copy | `Requests.cs:95-105` | copy present, paste loss |
| `KindTable.Grow` copy | `KindTable.cs:110-120` | copy present, paste loss |
| `SystemReader.FrameRootedOk` `names.Add` | `SystemReader.cs:656-663` | present, paste loss |
| `TimelineTable` static constructor | `InternTable.cs:73-79` | clears `Entries`, `ById` (fills -1) and `_motion`, paste loss |
| `TimelineTable.PushGraveyard` | `InternTable.cs:341-351` | node fields present, paste loss |
| `KindStore.Rebuild` queued value copy | `KindStore.cs:441-444` | copy present, paste loss |
| `World.PlaybackIndex.EnsureCapacity` (5.0) | `World.Systems.cs:254-256` | `nk[h]=k`, `np[h]`, `nc[h]` present, paste loss |
| `NativeAlloc.AllocZeroed` (non-NET) | `Polyfills.cs:57-62` | clear present, paste loss |
| `UPoly.InitBlock` (non-NET) | `Polyfills.cs:147-148` | **real but latent**: `new Span<byte>(start,count).Clear()` ignores `value`. Both callers (`Data.cs:326,355`) pass 0, so no wrong output today. |

No real compile or corruption defect found.

## 4. Baselines from the existing harness (measured)

Harness: `benchmarks/PhaseClock --timelinebench`. It exists only on `feat/591-integration` (not on main). Its 4.0 arm calls `ApplyRequests` + a harness fold + `TimelineWorld.Advance` on `PlaybackTable`, using Core files that the branch does not modify (the branch only adds files to Core), so the 4.0 arm is main's code. It is not `KindStore.TickPass`. The harness reports the minimum of 3 rounds per process; below is the median of 5 processes (6 runs, first discarded, min to max in brackets). All 6 runs reported `TIMELINEBENCH VERIFY PASS` with the oracle hash matching.

| Shape (4.0 arm, 1 thread) | ms per tick, median of 5 | ns per active visit | setup ms (enqueue + first tick), median of 5 |
|---|---|---|---|
| 4M entities x 4 (16M playbacks) | 68.57 [66.85 - 72.69] | 4.29 | 469.35 [464.52 - 494.44] |
| 1M entities x 1 | 5.50 [5.35 - 5.90] | 5.50 | 73.42 [64.28 - 76.62] |
| 4M x 1 | 20.67 [20.29 - 22.48] | 5.17 | 117.06 |
| 1M x 4 | 16.51 [15.78 - 18.13] | 4.13 | 251.79 |
| 1000 x 150 (harness dense shape) | 0.731 [0.728 - 0.860] | 4.87 | 9.31 |
| 10000 x 150 | 8.37 [7.39 - 8.83] | 5.58 | 104.14 |

The Unity KindTick numbers in the briefing (18.25 ms at 4M x 4) were not reproduced here (no Unity run). The .NET `KindStore.TickPass` at 4M x 4 is **not tested** (no existing .NET harness for it on main).

## 5. New baseline A: native bytes of `PlaybackTable` (measured)

Throwaway program `D:\scratch\phase0\probe\Program.cs` (not in the repo). 40,000 entities, default `PlaybackTable(40000)` (stride 4), each entity 4 playbacks via `Start` + `ApplyRequests`, optionally entity 0 gets 150 more (154 total). Library of 1,000 timelines baked by `TimelineBaker`.

| Case | Stride after load | Slots bytes | Counts | Playing bitset | Table total | Process private delta |
|---|---|---|---|---|---|---|
| with the 150-playback entity | 256 | 81,920,000 | 160,000 | 5,000 | **82,085,000 (82.09 MB)** | 88.6 MB |
| without it | 4 | 1,280,000 | 160,000 | 5,000 | **1,445,000 (1.45 MB)** | 6.2 MB |

Table totals are exact arithmetic from the stride the real code path produced (`View.SlotsPerEntity`, `Grow()` doubling to 256). Private delta is the process counter and includes the request queues and managed objects. The reviewer's 81.9 MB vs 1.3 MB (slots only) is confirmed: 81.92 MB vs 1.28 MB. Ratio 56.8x. `KindTable` bytes: **not tested** (needs a kind-bearing library and generated consumers; no cheap path).

## 6. New baseline B: 1,000-timeline library, 2,000 entities (measured)

What it is: one timeline (1 track, 1 full-window clip, duration 1024, looping) baked once through the real `TimelineBaker.BakeJson` and `LibraryAssembler.Assemble` into 1,000 named parts (37,496 bytes). The 1,000 parts are identical copies, so it exercises library size and per-entity count, not distinct content. 2,000 entities, each with 40 to 150 distinct active timelines (random, seed 12345), total 194,881 active visits, stride grew to 256. No kinds, no markers, no `Next`. Single thread, 20 warmup ticks, 6 rounds x 100 ticks, first round discarded, median of 5 rounds. Two process runs per path.

| Path | ms per tick (run 1 / run 2 median) | ns per active visit | alloc per tick |
|---|---|---|---|
| `ApplyRequests` + `TimelineWorld.Advance` | 0.487 / 0.456 | 2.50 / 2.34 | 0 B |
| `TimelineWorld.Tick` with an attached `KindStore` over a zero-kind library (`TickPass` state machine only) | 0.343 / 0.258 | 1.76 / 1.32 | 0 B |

Not covered: kind entry work (`Kind_i` loops, lane values), because a 1,000-timeline kind library needs generated consumers and a kind pack; **kind work not tested**. Working set is about 4 MB, so this fits cache; it is a best case.

## 7. Fact checks of three reviewer claims

**(i) Three state machines disagree on a backward exit (measured for two paths, read for the third).** Probe `Rules.cs`: non-looping 4-tick timelines, rewind past tick 0. Outcome at the exit from tick 0:

| Timeline | `PlaybackRules.Advance` (`TimelineWorld.Advance`) | `KindRules.TickSlot` (`KindStore` via `TimelineWorld.Tick`) |
|---|---|---|
| Next=B | removed (empty slot) | handed to B, tick 3 |
| Hold | tick 0, flags Ended | tick 3, flags Ended |
| Next=B + Hold | tick 0, flags Ended | handed to B, tick 3 |
| plain | removed | removed |

Confirmed: they disagree. (The kind path's first tick only runs `Begin`, so its traces are one tick behind the Advance path; the table compares the same exit condition.) 5.0 `World.StepSoloOnce` (`World.Systems.cs:1358`): **read, not run**. Backward exit with Next hands over at `_duration[next-1]-1`, with Hold sets tick 0 and `SoloHeld`, otherwise removes. Matches the reviewer's table.

**(ii) 5.0 feeds zero to non-float/int kinds (confirmed, read plus committed generated code).** `SystemEmitter.cs:442-447` `LaneValue` returns null except float and int, and `EmitWorldKind` writes `{value} __v = default;` for null (line 490). The committed generated file `tests/Tl.Systems.Tests/Generated/TlSystemsKinds.g.cs:1011` on `feat/591-integration` contains `global::KindGame.Speed __v = default;` for the struct `Speed { float Value; byte Priority; }`, while float and int kinds get `__acc[__e]` and `(int)__acc[__e]`. A two-float kind takes the same branch. Also in the table path no `__table.Add` is emitted at all. Detail: `BinderIdOf` maps int to 0 and float to 1 only; other value types use `kind.Order`, so the reviewer's "two float kinds share a table" holds, but a two-float struct does not collide.

**(iii) Sorts are insertion sorts (confirmed).** `Requests.cs:62-69` `RequestQueue.Sort`, `Events.cs:95-105` `Events<T>.Drain`, `TimelineWorld.Library.cs:401` `SortEvents` are all insertion sorts (read). Measured, 15,000 requests (100 entities x 150 starts), median of 5 runs, first discarded:

| Entity order | `RequestQueue.SortStable` | whole `ApplyRequests` |
|---|---|---|
| descending | 79.35 ms | 93.09 ms |
| ascending | 0.037 ms | 0.686 ms |

`Events<T>.Drain` was not measured (same algorithm, **not tested**).

Probes: `D:\scratch\phase0\probe\` (Program.cs, Rules.cs, SortProbe.cs, Types.cs). Logs: `D:\scratch\phase0\logs\`.
