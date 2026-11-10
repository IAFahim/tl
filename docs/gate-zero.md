# Gate zero (M0) — plan §1 re-check against the repo plus §0 receipts

Every §1 row below was read in the current checkout (main `1b3709fb`,
src untouched) and is classified **REAL** (defect still present),
**PASTE-DAMAGE** (row text does not match any code in the repo),
or **ALREADY-FIXED** (row describes a defect the code as read no
longer has, with file:line proof). One row's name has drifted:
`TimelineRef.Validate` and `TimelineWorld.BuildTimeline` are the two
distinct methods the row names; no row failed to resolve to a real
symbol. Pinning tests live in `tests/Tl.Core.Tests/GateZeroTests.cs`.

## §0 baseline receipts (frozen)

### i9 README numbers

`README.md` tl-numbers block, 4.0 systems, one step per call
(i9-14900K, 32 cores, .NET 10.0.12, Release; best of 5 in-process
runs after per-shape steady-state warm-up; hot = cache-resident,
cold = DRAM-streaming; bit-exact forward and backward, 0 B warm):

| shape | hot ns/entity | cold ns/entity | warm alloc B |
| --- | ---: | ---: | ---: |
| folded lane through a generated system | 9.42 | 9.46 | 0 |
| folded lane rewound through the same system | 9.45 | 9.50 | 0 |
| fold-fed compose through a generated system | 10.86 | 10.79 | 0 |
| frame-consumer dispatch through a generated system | 10.58 | 10.57 | 0 |
| four pairs through one generated system | 21.84 | 22.03 | 0 |
| one playback per entity (2-playback reference) | 8.88 | 8.94 | 0 |
| two playbacks per entity | 17.26 | 17.21 | 0 |
| 64 consumers, 2 active per tick | 372.47 | 372.19 | 0 |

### Local PhaseClock baseline receipt

`benchmarks/PhaseClock/results/baseline-receipt.json`: AMD64 Family
25 Model 120 Stepping 0, AuthenticAMD, 12 cores, Windows
NT 10.0.22631.0, .NET 10.0.12, rows 1024, reps 3, parity
DoubleRunMatch true, checksum 18150555008925706741,
TwoVsOne 1.7462175278332857, generated 2026-11-10T10:20:19Z.
Baseline command: `benchmarks/Systems --receipt
results/baseline-receipt.json --reps 3 --warmup-ms 200`.

| scenario | hot ns/entity | cold ns/entity | warm alloc B | gate | baseline ns | ratio |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| folded | 14.9567 | 15.1367 | 0 | <=1.03x (no regression) | 0.4400 | 33.99 |
| folded-backward | 20.4285 | 14.5966 | 0 | — | — | — |
| fold-fed | 37.7167 | 19.9982 | 0 | <=0.15x | 76.2033 | 0.4949 |
| frame | 18.6096 | 15.8691 | 0 | <=0.25x | 0.8520 | 21.84 |
| four-pair | 29.4220 | 31.1066 | 0 | <=0.70x | 3.2099 | 9.166 |
| 1-playback | 21.3806 | 20.2545 | 0 | — | — | — |
| 2-playback | 37.3352 | 30.3467 | 0 | <=2.2x of 1-playback (live) | — | — |
| 64-consumer | 1512.0331 | 1466.8915 | 0 | <=1.5x of same-system 2-consumer (manual) | — | — |

Local CPU is ~1.6x slower than the i9 receipt (folded 14.96 vs 9.42,
four-pair 29.42 vs 21.84, 2-playback 37.34 vs 17.26).

### PhaseClock §0 experiment verdict (frozen)

`benchmarks/PhaseClock/README.md` + `results/bench-final.txt`:
kernel-only prototype on identical fixture data; scalar oracle is
the parity reference. Gates: parity PASS on all 9 shapes
(scalar hash == vector hash, 97 ticks), ISA-invariance PASS
(`DOTNET_EnableAVX2=0`, 4-wide, same hashes), alloc 0 B warm.
Projections met or beaten on every shape.

| Shape (plan §0) | Kernel | Measured ns/entity | Plan projected | vs local 4.0 |
| --- | --- | ---: | --- | --- |
| Folded, uniform phase, dense rows | `BroadcastDense` (AVX2, 8-wide) | 0.050 | 0.15–0.4 | 300x vs 14.96 |
| Folded, mixed phase, sparse rows | `RunBroadcast` (phase-sorted runs, scalar scatter) | 0.480 | 1.0–2.0 | 31x |
| Folded, uniform phase, sparse rows | `BroadcastSparse` | 0.432 | 1.0–2.0 | 35x |
| Unique phases (plan §12 risk: runs of length 1) | `LaneGatherScalar` | 1.234 | 1.0–2.0 baseline | 12x |
| Lane, dense both | `LaneDense` | 0.061 | upside | — |
| Rewound | `BroadcastDense`, dir −1 | 0.048 | same as forward | identical |
| Four pairs | 4x `BroadcastDense` | 0.205 | 4x single | 143x vs 29.42 |
| Two playbacks | 2x `BroadcastDense` | 0.094 | 2x single | — |
| 64 cohorts, 2 active | 2x `BroadcastDense` + 64-entry mask scan | 0.112 | 2x single | cost follows active |

Direct-call probe (conservative): `BroadcastDense` 0.103,
`RunBroadcast` 0.647 (delegate path 0.050/0.480); true scalar
oracle 4.43 ns for uniform-dense. Kill list for M6: manual 8-wide
gather deleted (2.29 ns, wrong under SSE); `Avx2.Gather`/`Scatter`
not on this .NET's intrinsics surface (CS0117) — gather stays
scalar. N=1024 is L1/L2 resident; cold/DRAM and phase-entropy
sweeps are M6 work. Full hashes in `results/bench-final.txt`
(e.g. uniform-dense scalar=vector=5a7c42b6b1067325).

## §1 row classifications

| # | row | class | evidence | pinning test |
| --- | --- | --- | --- | --- |
| 1 | RequestQueue.Grow | ALREADY-FIXED | `src/Tl.Core/Requests.cs:95-106` copies `_count` records into the doubled block (`16`, else `*2`), frees the old block; spare entries stay unread (`_count` is authoritative; no pointers inside `RequestRecord`). | `GateZeroTests.RequestQueueGrowingPreservesEveryRecordInOrder` |
| 2 | ClosingList.Grow | ALREADY-FIXED | `src/Tl.Core/Requests.cs:153-172` copies `_count` entries of all three arrays (entities, slots, `Playback[8]`), frees the old blocks; `Get` (`Requests.cs:120-125`) reads only `[_count]`. | `GateZeroTests.ClosingListGrowingPreservesEveryEntry` |
| 3 | Events.Grow | ALREADY-FIXED | `src/Tl.Core/Events.cs:131-141` copies `_count` records into the next block; `Raise` grows `16` then `*2` (`Events.cs:85`) before any write past `[0,_count)`. | `GateZeroTests.OwnedEventsGrowingPreservesEveryRaisedValue` (existing overlap: `Tl.Systems.Tests/EventsStorageTests.OwnedEventsStillGrow` counts but does not check values) |
| 4 | PlaybackTable.Grow | ALREADY-FIXED | `src/Tl.Core/PlaybackModel.cs:128-139` clears the new block, then copies each entity's old stride into the doubled stride (`Min(MaxSlots, *2)`); `Reserve` (`PlaybackModel.cs:141-145`) and `Slots.TryAdd` (`:200-212`) never write past the current stride (`count == MaxSlots` returns false). | `GateZeroTests.PlaybackTableGrowingPreservesEarlierSlotsPerEntity` |
| 5 | TimelineTable graveyard | REAL (residual; 4.0-only) | `src/Tl.Core/InternTable.cs:341-351` pairs `NativeMemory.Alloc` node with aligned `AlignedFree` block; drain (`:353-376`) frees only after the heuristic grace `GraveyardGraceLoads = 4096` loads (`:11`, `:115`). Lock-free readers (`TryLive`, `:181-191`) dereference `entry->Block` without a hazard pointer, so a reader pre-empted across 4096 loads can use a freed block. 5.0 `TimelineWorld(library)` never touches `TimelineTable` (interning is the 4.0 `TimelineAsset` path), so no M0 test; 5.0 either drops interning or adds epoch/hazard retirement. | — |
| 6 | MaterializeSteps | ALREADY-FIXED | `src/Tl.Core/TimelineWorld.Library.cs:243-339`: sizing pass and copy pass use identical accounting (`Align16` per step + `Align16(window*2*laneBytes)` fold lanes + marker `Align16(count*stride)`, `stride = Align8(max)` with minimum 8, `:265-279`); pool indices re-checked in both passes (`:254-255`, `:292-293`); every offset is post-`TimelineRef.Validate` (`:139-141`). | — (internal; exercised through `LoaderTests`) |
| 7 | TimelineRef.Validate / BuildTimeline | ALREADY-FIXED | Name drift: `BuildTimeline` is `TimelineWorld.BuildTimeline`, not a `TimelineRef` method (only `Validate` is `TimelineRef.Validate`). `src/Tl.Core/Data.cs:194-257` checks magic/version/duration/pair count ≤ 256/size/alignment/pair-order/pools/stage partition/step bounds/marker section; `TimelineWorld.Library.cs:138-141` validates before building. | — |
| 8 | LibraryAssembler.Emit | ALREADY-FIXED | `src/Tl.Core/LibraryAssembler.cs:312-381`: content dedup, dense manifest ids, names written once, digest over all but the trailing 32 bytes (identical window verified by `LoadLibrary`, `TimelineWorld.Library.cs:97-106`). `tickRate` written at offset 24 (`Put(span, 24, tickRate)`, `:359`) round-trips (`Tl.Gen.Tlb/TlScene.cs:228` reads `U(24)`). Ordinal name sort is a caller contract honored by every in-repo caller (`Tl.Bake/LibraryPack.cs:93`); `Emit` itself does not sort. | — |
| 9 | StableName / FrameRootedOk | ALREADY-FIXED | Hash: `Keying.Of` and Roslyn `StablePairKey`/`StableKey` use identical FNV-1a (seed 14695981039346656037, prime 1099511628211, `|1`) with the same `'\0'` pair separator (`Data.cs:16-27`; `SystemReader.cs:905-919`; bake mirror `TimelineBakerFast.cs:1843-1846`); name spellings match (namespace + `+` nested + `<args,>` generics). `FrameRootedOk` (`SystemReader.cs:633-647`) roots at the actual frame parameter name and admits only `Direction`/`Track`/`Clip` (bare `Direction` only). | `GateZeroTests.PairKeysAreNeverZeroAndPairCompositionMatchesItsSpelling` (pair keys always odd → never the `PairTable` empty sentinel) |
| 10 | EmitFlush __dir | ALREADY-FIXED | `SystemEmitter.cs:969` emits `sbyte __dir = dir != 0 ? (sbyte)-1 : (sbyte)1`; batch `dir` is `__backward ? 1 : 0` (`SystemEmitter.cs:841-842`), matching the non-batch `__backward ? -1 : 1` (`:727-728`); `Frame_` takes `sbyte direction` (`:163`); committed generated proof `TlSystemsSnapshot.g.cs:444-461`. | — |
| 11 | FlushArg / EndBind | REAL (latent; unreachable today) | `FlushArg` returns `ref __rr` / `ref __er` (`SystemEmitter.cs:1024-1027`) which `Flush_` never declares; unreachable only because the reader rejects `ExecuteBatch` taking Requests/Events with TLGEN96 (`SystemReader.cs:760-763`). Runtime would emit uncompilable code if that guard ever lapses. `EndBind`/`EndArgs` ordinals agree (both advance per non-frame param, `SystemEmitter.cs:1033-1087`); `EndBind` hard-codes `__rx0` (`:1045`). Roslyn-emitter row, not 5.0 runtime path: fix in the Gen package (emit per-entity binds like `EndBind`, or delete the dead arms). | — (needs the `Tl.Gen.CSharp.Tests` harness) |
| 12 | PairTable.Probe vs GrowSlots | ALREADY-FIXED | `Probe` (`Data.cs:685-696`) and the `GrowSlots` rehash (`Data.cs:349-368`) both mask with `(cap-1)`; `Slot` is 16 bytes (`Data.cs:311`) so the block `8 + 24*cap` fits exactly: 8 header + 16/cap slots + 8/cap layouts (`Data.cs:313-314`, `:324-328`). Minor note for 5.0: `GrowSlots` never frees the old block (bounded once-only leak by table lifetime). | `GateZeroTests.PairTableProbeFindsKeysInstalledBeforeAndAfterSlotGrowth` (600 installs force the 1024→2048 grow; pre-grow layouts and heads still resolve) |
| 13 | NativeAlloc.AllocZeroed | REAL (minor) | NET branch forwards to `NativeMemory.AllocZeroed` (`Polyfills.cs:49`) and the non-NET branch zeroes explicitly (`:57-62`), but `NativeAlloc.Clear` (`:53`) and the non-NET `AllocZeroed` (`:60`) truncate the length with `(int)` casts, so zeroing above 2 GiB would be partial (`:83` same cast). | `GateZeroTests.AllocZeroedReturnsZeroBytes` (runs, small) + `GateZeroTests.AllocZeroedAboveIntMaxValueReturnsZeroBytesToTheEnd` (skipped by design; 4.0 cannot satisfy it in a unit test) |
| 14 | Inspection / GeneratorCli parse | ALREADY-FIXED | `Inspection.Tables()` snapshots live counters from `TimelineTable`/`PairTable` (`Inspection.cs:67-91`); `GeneratorCli.Main` requires `--compile` first (`GeneratorCli.cs:18`), every flag requires a value (`:27-45`), `--output`/`--backend` validated (`:47-50`). | — |
| 15 | MemberSystems | REAL (fragility; not 5.0 runtime path) | `SystemReader.cs:486-496` treats any single-type constructor-argument attribute as a member system without checking the attribute is `TimelineSystemMemberAttribute` (`TimelineSystem.cs:8-13`, `AllowMultiple = true`): a foreign `[Foo(typeof(X))]` on a consumer type would add `X` as a member system (TLGEN99 unknown-system error or silent misassignment). Roslyn-analysis row; fix in the Gen package by filtering on the attribute symbol already resolved at `SystemReader.cs:92-93`. | — (needs the `Tl.Gen.CSharp.Tests` harness) |
| 16 | MarkerTableRegistry lock | ALREADY-FIXED | All entry points (`Register`, `Clear`, `TryResolve`) take `lock (_gate)` with every `ConditionalWeakTable`/`Dictionary` access inside the lock (`MarkerTableRegistry.cs:15-52`). Pointer validity stays a caller contract (valid until `Dispose`/`Reload`). | — |

## Notes for the 5.0 port (from these rows only)

- Grow/copy/zeroing/hash is byte-true in 4.0 today: every grow copies
  exactly `_count` live entries and frees the old block; every zeroed
  allocation is really zeroed below 2 GiB; probe and growslot hash
  functions agree, and all public pair keys are odd so the pair table's
  zero-means-empty sentinel is safe.
- The three REAL rows are not in the 5.0 runtime path: intern-table
  graveyard grace (4.0-only interning), the `FlushArg` dead arms and
  the `MemberSystems` attribute-identity check (both Gen package).
  M0 takes no action beyond pinning the `AllocZeroed` truncation as a
  skipped expectation for the 5.0 allocator to satisfy.
- No §1 row was PASTE-DAMAGE: each resolves to an existing symbol;
  row 7's name drift (`TimelineRef.BuildTimeline` → the method is
  `TimelineWorld.BuildTimeline`) is recorded above.
