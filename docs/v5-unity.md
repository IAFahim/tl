# v5 Unity shim (M9, plan §10)

Target: `com.tl.unity` (IAFahim/tl.unity), Unity 7. The shim vendors a
Burst-compatible subset of `Tl.Core` and runs the same v5 `World` as the
.NET build, bit-identical by construction. Nothing here changes `tl` CLI
behavior.

## Ship list

Vendored from `src/Tl.Core/` (one source list, § Re-sync), compiled into
the `Tl.Core` asmdef as netstandard2.1:

- `Clock.cs` — `Direction`, `Row`.
- `LibraryView.cs` — `TimelineId` (ushort, 1-based, 0 = none).
- `Library.cs` — tlib3 structs (`Tlib3Timeline`/`Tlib3Step`/…), `VerifyCore`,
  `LaneValue`. Managed pin stripped by the shim (§ Burst-incompatible).
- `Kernels.cs` — `PhaseToTick`, `BroadcastDense`/`BroadcastSparse`,
  `LaneDense`, `LaneGatherScalar`, `RunBroadcast`, `StableRadix`, `Run`.
  The `System.Numerics.Vector` branches are `#if NET10_0_OR_GREATER`, so
  Unity (netstandard2.1) always compiles the scalar fallbacks — those
  fallbacks are the Burst kernel bodies.
- `World.cs` — `World(Library, int)`, `Tick(Direction)`, `Requests`,
  `Accumulator`, `Frame`, `Report`. Zero-alloc warm tick is preserved:
  all scratch arrays are preallocated.
- `LibraryWriter.cs` — `TimelineSpec`, `StepSpec`,
  `LibraryWriter.Write(IReadOnlyList<TimelineSpec>) -> byte[]`. The single
  writer: editor bake and `tl bake` both call it (§ Editor bake).
- `Polyfills.cs`, `BurstDiscard.cs` — netstandard2.1 shims; the
  `Unity.Burst.BurstDiscard` attribute is already defined there.

Shim-owned (new code in `com.tl.unity`, not vendored):

- `TlV5World.cs` — owns the `NativeArray<byte>` library blob and the
  `World`; builds `Requests` from fixed input arrays.
- `TlBurstJobs.cs` — scalar `IJobFor` wrappers over the Kernels bodies with
  `[ReadOnly][NoAlias]` `NativeArray`s (the `[NoAlias]`-style contract:
  distinct `NativeArray` per lane, Burst proves no aliasing, scalar loop
  body unchanged).
- `TlBakeMenu.cs` (Editor) — `Tl/Bake Library` menu.
- `WorldV5ParityTests.cs` (Tests) — § Parity receipts.

## Re-sync

One source list, reused the `install/unity.ps1` way. `install/unity.ps1`
already keeps every external input in one ordered map (`$packs`) and
validates the result with a marker line (`TL-INSTALL imported …`). The
shim does the same for Core:

- Source list lives at `com.tl.unity/Tools~/tl-core.sources`, one
  `src/Tl.Core/<file>` per line, in the order above.
- A sync step (installer step 4, next to `$packs`) reads the list from the
  resolved package, copies each file from the `tl` checkout into the
  package, and prints `TL-INSTALL core <file> ok` per file. Any missing
  file fails the install — no silent drift, same as `$packs` failing on an
  unreadable git URL.
- The vendored copies keep the `Tl` root namespace; the shim adds its own
  `Tl.Unity` types in separate files, never edits a vendored body.

## Burst-incompatible APIs and scalar replacements

| API | Why not Burst | Scalar replacement |
|---|---|---|
| Managed `Library` pin (`GCHandle.Alloc(Pinned)` over `byte[] _bytes`, finalizer/`Dispose`, `Blob => _bytes.AsSpan()`) | Burst jobs cannot touch managed objects or GC handles | Shim `LibraryBlob`: `NativeArray<byte>` copied once at load; `VerifyCore` runs on `(byte*, int)` with the same checks (magic `0x42494C54`, version 3, 64-byte alignment, SHA-256 trailer). Managed `Library.Verify` stays edit-time only. |
| `Span<T> Drain()` (`WorldEvents<T>.Drain()`, `Events<T>.Drain()`) | ref structs / `Span<T>` cannot cross a job boundary | `DrainTo(NativeArray<T> dst) -> int` copies the raised records out and clears. `Raise` stays callable from jobs (its `Grow` is already `[BurstDiscard]`). |
| `Parallel` (`Parallel.For` over visits) | Compiles on netstandard2.1 but Burst cannot schedule TPL work | `IJobFor` over the visit range plus `IJobFor` over rows, both with `[ReadOnly][NoAlias]` arrays and an explicit dependency chain. Same accumulation order as the sequential sort-then-scan (radix order is total, so the hash is stable). |

`Events<T>` (`Events.cs`) is not shipped: events surface only through
`World.Events<T>()` + the replacement `DrainTo`. `KindTable<T>`/`Kind<T>`
are not shipped in this cut (kind consumers keep using the existing pack
shims).

## Parity job design

`WorldV5ParityTests` (PlayMode, batchmode via the same
`Invoke-UnityTests` path as `install/unity.ps1` step 6) mirrors
`tests/Tl.Core.Tests/WorldV5Tests.cs` fixture-for-fixture: same
`LibraryWriter.Write` libraries, same `RunWorldHash` (1024 rows, 97
ticks), same FNV-1a hash over `SingleToInt32Bits` (seed
`14695981039346656037`, prime `1099511628211`). Each case prints
`TL-PARITY case=<name> hash=<ulong:X16>`; CI diffs the Unity receipt
against the .NET receipt from `dotnet test`. Acceptance = identical hash
list, no tolerance.

Oracle cases (config is the receipt — the hash is recomputed by
`OracleRun` on both sides):

- `UniformForwardMatchesOracle` — const lane 40×6f, 1024 pairs phase 0, Forward, 97 ticks.
- `UniformRewindMatchesOracle` — same, Reverse.
- `MixedForwardMatchesOracle` — varying lane 40 (`1 + (i*7%13)*0.5 + (i%5)*0.25`), phase `(i*37+11)%40`, Forward.
- `MixedRewindMatchesOracle` — same, Reverse.
- `UniqueForwardMatchesOracle` — varying lane 1024, phase `i%1024`, Forward.
- `UniqueRewindMatchesOracle` — same, Reverse.
- `FourPairsForwardMatchesOracle` — 4 timelines (40 ticks, const 6/7/8/9), 4096 pairs (1024 rows × 4 timelines, phase 0), Forward.

Kernel oracles (exact array equality, not hashes): `BroadcastDense`
(off 37, value 3.5, count 1024), `LaneKernels` (`LaneGatherScalar` at
frame 13, dir −1; `LaneDense` over the 40-tick lane), `RunBroadcast`
(8 rows, 3 runs, frame 3, dir −1), `StableRadix` (keys `3,1,3,0,2` →
order `3,1,4,0,2`). Warm-tick zero-allocation and invalid-start counting
(`InvalidStartsAreCountedNotThrown`) run in the shim too — they pin the
preallocated-scratch design.

## Editor bake

`Tl/Bake Library` enumerates the same `TimelineSpec` set from the same
project JSON and calls `LibraryWriter.Write`. `tl bake` for a v5 project
writes through the same `LibraryWriter.Write` (single writer), so the
editor `.tlib` is byte-identical to `tl bake <tl.project.json> -o
game.tlib` by construction. Receipt: SHA-256 trailer + total bytes from
both outputs logged side by side; any diff fails the bake.

## Known limits

- No AVX-512 on i9: the `Vector<float>` path never compiles under Unity
  (netstandard2.1), so vector headline numbers from .NET do not transfer.
  Burst scalar jobs on this machine are scalar-first; AVX2 vectorization
  is a later job-side pass, not a kernel fork.
- Accumulation order is pinned to radix order. Parallelizing visits
  inside a tick group requires the `[NoAlias]` disjoint-row split; any
  reordering breaks hash parity and is not allowed without re-oracling.
- Timeline ids are ushort (1–65,535, 0 = none); lanes are `float`.
- IL2CPP player builds keep the managed `Library.Verify` path out of the
  runtime; verify at load with `LibraryBlob` only.
