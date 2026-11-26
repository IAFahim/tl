# Lazy slot ticks and horizon glide

Two mechanisms skip work on ticks where a playback only moves by one.

- **Lazy slot ticks** (.NET, kind-free `TimelineWorld.Advance`, opt-in): a slot is stepped only on the tick where something other than "tick moves by one" happens. A calm tick reads two bytes per entity and writes nothing.
- **Horizon glide** (tl.unity kind tick job, S4): one cached horizon per entity, the minimum over its slots. It uses the public `Horizon`, `GlideKernel`, `SlotWord` and `KindCalm` helpers, which stay in Core for that host.

Neither changes an observable tick, the Unity scheduler or a pinned hash.

## API and ownership

`new PlaybackTable(entityCapacity, slotsPerEntity, horizonLimit)` opts in with any limit from 1 to 255; the value itself is no longer a cap, because a slot knows its own boundary. The two-argument constructor uses 0, which is eager stepping. `HorizonLimit` reports the table's remaining eligibility.

While a table is lazy, the stored `Tick` of an ordinary slot is relative to the table clock, so raw slot bytes are not readable from outside. Everything public returns real values: `Find`, `IsPlaying` and `CountOf` decode what they return and keep the table lazy. Obtaining a raw pointer does not: `PlaybackTable.View` and `PlaybackTable.Playing` restore real ticks and set `HorizonLimit` to 0 for good, so retained pointers can be read and edited under the existing rules. Generated systems do not escape: they read `PlaybackTable.SystemView`, `PlaybackTable.SystemPlaying` and `TimelineWorld.SystemView`, walk slots through the table pointer and take each tick from `PlaybackView.TickOf`, which adds the lazy lag in a register. A table driven by a generated system stays lazy. Glide in `system` mode, 20,000 x 100, capacity 4, one core, ABAB, median ms, hashes equal: duration 120 26.3-26.8 (eager after the old escape) -> 21.9-23.7, duration 3000 31.0-31.5 -> 21.3-23.6.

Public `TimelineWorld.View` exposes mutable library pointers; obtaining it makes that world step every table eagerly. Borrowed `AdoptView` libraries are never eligible. Owned byte-loaded libraries are eligible until a mutable view escapes.

Table and world mutations and disposal require exclusive ownership, as before. Independent tables can advance on separate threads in one world.

## Encoding and invariant

An ordinary slot has a nonzero timeline, Rate 0 and no flag other than Reverse. An inert slot has Paused or Ended without StartPending, Stopping, StopAged, Began or ReenterPending; it is a fixed point of `Step`.

- **Tick.** A lazy table has a 16-bit clock that counts its ticks. An ordinary slot stores `tick - clock * dir` in 16-bit arithmetic, `dir` being its direction of travel under the current world direction, so `tick = stored + clock * dir` is exact on every tick and the bytes do not change while the slot is calm. Every other slot stores its tick as an eager table does.
- **Due.** One `ushort` per slot, parallel to the slot run (inline run and spill block): the low 16 bits of the tick on which the slot must really be stepped. Ordinary: `now + 1 + calm`, where calm is `Duration - 1 - Tick` moving up and `Tick` moving down. Live but not ordinary: `now + 1`. Inert or empty: `now + 65,535`.
- **Next.** One `ushort` per entity: a lower bound of the earliest due among its slots.

A tick compares `Next` of 64 entities at a time with the tick number. For each hit that is playing it compares the entity's dues, collects the due slots of up to 64 entities, prefetches them, and then steps each one with the unchanged `PlaybackRules.Step`: decode, step, encode, new due. An entity none of whose slots is due (its `Next` was only a lower bound) gets its first slot stepped early, which is a real step and therefore exact, and its playing bit recomputed.

Exactness rests on three facts. Stepping a slot reads and writes only its own 8 bytes and the library. A due may be early but never late, and an early visit is a real step, so it is harmless. Every live slot that is not ordinary is due on every tick. Durations are at most 65,535, so a due never needs a cap and the 16-bit wrap is safe.

Requests decode the entity before a record is applied and encode it afterwards with a revisit on the next tick, including on exceptional exits. A table restores real ticks when a view or the playing words escape, a `KindStore` is attached, the library reloads, or the world, library generation or direction changes; it encodes again on the next lazy tick. Both transitions visit every slot once, so a caller that flips direction every tick pays an eager tick each time. A slot naming no timeline of the loaded library throws the same message and leaves the same half-stepped state as an eager table.

## Memory and execution

Opt-in storage is 2 bytes per inline slot, 2 bytes per entity and 2 bytes per spill-block slot, allocated with the table and released with it; `NativeBytes` includes it. A default table allocates none. This is 25 percent more table memory, not a saving: at 20,000 entities with 200 active playbacks, 8 MB on top of 32 MB. Measured process-private memory grows by 16 to 40 percent, because the due lane of spill blocks is a second array that grows by doubling.

The first lazy tick after a table is created, thawed or changes direction encodes every slot. After every playback starts in one tick, that first tick measured about 4 to 6 times an eager first tick (both run tier-0 code). A tick on which most slots are due costs about 11 ns per due slot, so with timelines shorter than about 8 ticks (.NET 10) or 16 ticks (netstandard2.1) eager stepping is faster. A lazy table therefore watches itself: after 8 settling ticks it counts the slots it stepped over the next 16 ticks, and when that exceeds the number of slots it holds (more than one slot in 16 due per tick, tables of 64 slots or more), it restores real ticks and steps eagerly for 32 ticks, then encodes again and re-measures. Each consecutive failed probe doubles the eager stretch up to 4,096 ticks; a passing probe resets it to 32. Results are unchanged, because the eager and lazy paths are exact against each other.

The due compare uses `Vector128` on .NET 10 and `System.Numerics.Vector<ushort>` on netstandard2.1 when the runtime accelerates it (a .NET 8 or 9 host loading the netstandard2.1 asset), with a scalar loop otherwise; all three give identical results. .NET 10 prefetches the gathered slots; netstandard2.1 has no prefetch instruction, so it reads every gathered slot once before stepping any, which lets the cache misses overlap. The kernel allocates nothing, keeps no static state and holds no slot pointer across a call.

## Measured

`benchmarks/Glide`, AMD Ryzen 5 8500G, Linux, .NET 10, one thread, inline capacity 4, 1,000 looping timelines, warmup 8,192 ticks, median of 7 rounds of 256 ticks, one process per cell. Milliseconds per `Advance`. "Eager" is the same build with lazy ticks off, "main" is `46f702d2` (the commit before this change) with lazy ticks off and with its entity-level glide at limit 255. Every arm allocated 0 B in the timed rounds, ended equal to an analytic oracle, and produced the same state hash as the other three arms of its cell. Setup (start requests for every playback) took 106 to 130, 239 to 245 and 388 to 404 ms at the three shapes, equal across arms.

**.NET 10 asset (`net10.0`)**

| Entities x active | Duration | Eager ms | Lazy ms | Speedup | main eager ms | main glide 255 ms | Lazy first tick ms | Eager first tick ms | Lazy private MB | Eager private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10,000 x 40 | 120 | 0.700 | 0.071 | 9.8x | 0.734 | 0.650 | 28.9 | 8.3 | 22.3 | 19.3 |
| 10,000 x 40 | 600 | 0.689 | 0.017 | 40.5x | 0.719 | 0.500 | 29.5 | 7.9 | 22.4 | 19.3 |
| 10,000 x 40 | 3000 | 0.640 | 0.004 | 163.8x | 0.715 | 0.443 | 30.3 | 8.0 | 22.6 | 19.6 |
| 20,000 x 100 | 120 | 3.462 | 0.399 | 8.7x | 3.577 | 4.925 | 110.0 | 19.1 | 59.6 | 44.4 |
| 20,000 x 100 | 600 | 3.342 | 0.108 | 30.9x | 3.451 | 2.946 | 109.2 | 18.2 | 59.6 | 44.4 |
| 20,000 x 100 | 3000 | 3.332 | 0.027 | 123.9x | 3.443 | 2.256 | 113.7 | 18.2 | 59.9 | 44.7 |
| 20,000 x 200 | 120 | 6.604 | 0.796 | 8.3x | 7.194 | 8.422 | 207.2 | 35.5 | 108.8 | 77.6 |
| 20,000 x 200 | 600 | 6.424 | 0.252 | 25.5x | 6.312 | 7.428 | 206.8 | 35.2 | 108.9 | 77.7 |
| 20,000 x 200 | 3000 | 6.391 | 0.061 | 104.1x | 6.798 | 4.820 | 206.0 | 34.2 | 109.1 | 77.9 |

**netstandard2.1 asset** (what Unity, .NET 8 and 9 hosts load), same machine and runtime:

| Entities x active | Duration | Eager ms | Lazy ms | Speedup | main eager ms | main glide 255 ms | Lazy first tick ms | Eager first tick ms | Lazy private MB | Eager private MB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10,000 x 40 | 120 | 0.665 | 0.136 | 4.9x | 0.773 | 0.689 | 29.3 | 8.0 | 22.3 | 19.2 |
| 10,000 x 40 | 600 | 0.703 | 0.040 | 17.6x | 0.731 | 0.527 | 29.9 | 8.2 | 22.3 | 19.2 |
| 10,000 x 40 | 3000 | 0.698 | 0.014 | 49.1x | 0.760 | 0.471 | 30.1 | 8.0 | 22.6 | 19.5 |
| 20,000 x 100 | 120 | 3.331 | 1.156 | 2.9x | 3.622 | 4.860 | 108.3 | 18.7 | 59.5 | 44.3 |
| 20,000 x 100 | 600 | 3.375 | 0.270 | 12.5x | 3.581 | 3.002 | 111.0 | 18.1 | 59.6 | 44.4 |
| 20,000 x 100 | 3000 | 3.365 | 0.086 | 39.1x | 3.521 | 2.305 | 110.8 | 18.4 | 59.8 | 44.6 |
| 20,000 x 200 | 120 | 6.971 | 2.806 | 2.5x | 6.519 | 9.500 | 206.3 | 34.8 | 108.7 | 77.5 |
| 20,000 x 200 | 600 | 6.707 | 0.871 | 7.7x | 6.312 | 7.471 | 206.0 | 35.8 | 108.8 | 77.6 |
| 20,000 x 200 | 3000 | 6.863 | 0.234 | 29.4x | 6.859 | 4.916 | 206.7 | 34.3 | 109.1 | 77.9 |

Short timelines, 20,000 x 100, 64 ticks per round, same method:

| Duration | net10.0 eager ms | net10.0 lazy ms | net10.0 lazy vs eager | netstandard2.1 eager ms | netstandard2.1 lazy ms | netstandard2.1 lazy vs eager |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 5.805 | 21.914 | 3.77x slower | 5.747 | 23.713 | 4.13x slower |
| 2 | 4.779 | 11.398 | 2.39x slower | 4.683 | 12.334 | 2.63x slower |
| 4 | 3.869 | 5.854 | 1.51x slower | 3.903 | 7.199 | 1.84x slower |
| 8 | 4.025 | 3.267 | 1.23x faster | 3.601 | 4.201 | 1.17x slower |
| 16 | 3.403 | 2.005 | 1.70x faster | 3.467 | 3.278 | 1.06x faster |
| 32 | 3.385 | 1.123 | 3.01x faster | 3.457 | 2.195 | 1.57x faster |

## Verification and reproduction

`GlideWorldTests` compares a lazy table with an eager one byte for byte after every tick, through trusted internal storage so that no public view disables the path under test: dense and partial words, spilled runs that shrink back inline, a tick that fills the hit buffer to its last lane, 200,000 ticks across the 16-bit clock wrap with paused slots and direction flips, world switches, reload, borrowed libraries, rejected and throwing requests, 10,000 request streams, warm allocation on the calling thread, and tables advancing on separate threads. The checked build counts visited entities, so the test asserts that calm ticks visit none. The fuzz law `T1-LazyAdvance` drives a lazy and an eager table with the same random requests and no generated system.

Eleven seeded faults in the kernel each fail these suites: a calm one tick too long in either direction, a spill that forgets dues, `Land` not lowering `Next`, a revisit one tick late, a dropped vector lane, no record for a due entity without a due slot, a playing bit never cleared, a decode one tick off, a thaw that ignores entity order, and a removed playing-bit check. A hit buffer two lanes smaller is not caught: it overruns one lane of stack that no managed test can observe, and the buffer is sized for the worst case.

Run the Core tests in Release, with `TlCoreTfm=netstandard2.1`, with `TlChecked=true`, and with `DOTNET_EnableAVX2=0` and `DOTNET_EnableHWIntrinsic=0` for the Vector128 and scalar compare. The benchmark under `benchmarks/Glide` measures `Advance` on both runtime assets, checks the final state against an analytic oracle and reports rounds, allocation, setup and memory.

## Kind calm (S4)

`KindCalm.Build` turns a library's kind event bits into two byte tables per kind timeline: the number of ordinary forward or backward steps before the next tick whose event bit fires in that direction, capped at 255. The host allocates the `Length` bytes per direction plus one start index per timeline, once per library bind; the tables are derived, never hashed or serialized. Kind-free timelines keep the motion calm. `Horizon.OfKindRun` is `OfRun` with that calm, so a positive result proves that `KindRules.TickEntity` would only move ticks by one and make no row call for that many steps. `KindGlideTests` checks the table against a brute-force scan, every flag byte at every tick of the kind fixture, and random request/direction streams at limits 1/2/7/255 against `TickEntity` with recorded row calls and entry bytes. The .NET `KindStore` does not use it yet; tl.unity's `TimelineKindTickSystem` does (`TimelineHorizon`, `TimelineGlide`, see tl.unity `docs/KINDS.md`).

`SlotWord.Glide` and `GlideKernel.RunScalar` are unsafe primitives. Call them only on live, caller-owned little-endian 8-byte playback storage, under a previously established positive horizon, with the same direction, unchanged library and no intervening mutation. They do not check timeline boundaries.

## Unity result (S4 + S5)

Measured in Unity 7000.0.0a16 with Burst, 11 workers, safety checks off, median of 5 fresh processes per arm, against tl.unity main `3b44b20`: 4M x4 (duration 3000) 38.15 -> 31.40 ms per tick (tick job 14.02 -> 9.86 ms, kind jobs 24.89 -> 21.96 ms with tl.unity's direct window step), duration 300 39.80 -> 31.74 ms, 10k x150 2.87 -> 1.94 ms; with editor-default safety checks 48.4 -> 39.1 ms. Hashes identical, 0 B on the main thread over 1000 warm ticks. This is 1.22x at 4M x4, not the 2x replacement gate: the tick job still reads and writes every row's in-chunk playback bytes, so it sits near the memory floor. Lazy slot ticks are the answer to that floor; the Unity port is tracked in IAFahim/tl#647.
