# v5 Unity parity receipt (M9)

Run date: 2026-11-10. Unity editor: 7000.0.0a16 (newest installed, via
`unity editors --installed --json`; no other Unity 7 editor on this machine).
Backend: Editor CoreCLR, clr-version 10.0.10 (per NUnit XML). The
`System.Numerics.Vector` branches in `Kernels.cs` and `LibraryWriter.cs` are
`#if NET10_0_OR_GREATER`, so Unity (netstandard2.1 profile) compiled the
scalar fallbacks — those scalar bodies are what the hashes below prove.

Vendored into `com.tl.unity/com.tl.unity/Core` (Tl.Core asmdef,
`noEngineReferences`, verbatim from `tl/src/Tl.Core`, uncommitted):
`Clock.cs`, `Library.cs`, `Kernels.cs`, `World.cs`, `LibraryWriter.cs`,
`BurstDiscard.cs`. `LibraryView.cs` and `Polyfills.cs` were already in sync
(line-ending-exact after `--strip-trailing-cr` diff). Vendoring `Tl.World`
shadowed `Unity.Entities.World` inside every `Tl.*` namespace, so the existing
unqualified references in two files were qualified to
`global::Unity.Entities.World` (`Tests/PlaybackRulesTests.cs`,
`Editor/KindReportMenu.cs`); no other `Tl.*` file names `World` unqualified.
Parity tests live at `com.tl.unity/Tests/WorldV5ParityTests.cs` (14 tests,
EditMode, mirroring `tests/Tl.Core.Tests/WorldV5Tests.cs` fixture-for-fixture).

## Hash table (.NET vs Unity, FNV-1a over SingleToInt32Bits, seed 14695981039346656037, prime 1099511628211, 97 ticks)

| Case | .NET (Tl.Core net10.0) | Unity (Editor, 7000.0.0a16) | Match |
|---|---|---|---|
| UniformForwardMatchesOracle (const 40x6f, 1024 pairs, phase 0) | `5A7C42B6B1067325` | `5A7C42B6B1067325` | yes |
| UniformRewindMatchesOracle | `5A7C42B6B1067325` | `5A7C42B6B1067325` | yes |
| MixedForwardMatchesOracle (varying lane 40, phase `(i*37+11)%40`) | `19EFEF6313C65325` | `19EFEF6313C65325` | yes |
| MixedRewindMatchesOracle | `B9EF75341D971325` | `B9EF75341D971325` | yes |
| UniqueForwardMatchesOracle (varying lane 1024, phase `i%1024`) | `D77E142E80A83325` | `D77E142E80A83325` | yes |
| UniqueRewindMatchesOracle | `2627DB56B4AC7325` | `2627DB56B4AC7325` | yes |
| FourPairsForwardMatchesOracle (4 timelines 40 ticks, const 6/7/8/9, 4096 pairs) | `6EE2EE4A060F3325` | `6EE2EE4A060F3325` | yes |

.NET hashes were computed by a scratch console process referencing
`tl/src/Tl.Core` and reproducing `WorldV5Tests` exactly (same
`LibraryWriter.Write` libraries, same `RunWorldHash`, 97 ticks); every .NET
world hash also equaled its in-process scalar oracle, as the Unity tests do.
Unity cases additionally print `TL-PARITY case=<name> hash=<X16>` to the
batch log; all seven lines match the table above.

Kernel oracles (exact array equality, both sides): `BroadcastDense` (off 37,
value 3.5, count 1024), `LaneKernels` (`LaneGatherScalar` frame 13 dir -1,
`LaneDense` over 40), `RunBroadcast` (8 rows, 3 runs, frame 3 dir -1),
`StableRadix` (`3,1,3,0,2` -> `3,1,4,0,2`) — all passed in Unity.
`InvalidStartsAreCountedNotThrown` passed in Unity. `WarmTickAllocatesZeroBytes`
and `MixedWarmTickAllocatesZeroBytes` passed in Unity (delta computed into a
local first, per field notes section 4).

## Batch runs (one scratch project, `file:` manifest entry, lock `_work/unity.lock`, absolute `-testResults`/`-logFile`, no `-quit` with `-runTests`)

- EditMode: exit 0, `error CS` count 0, XML `total=50 passed=49 failed=0 skipped=1`.
  The 14 WorldV5ParityTests all passed; the one skip is the pre-existing
  `KindGame.KindBakingTests.EditingATimelineWithoutChangingACapacityRewritesNothing`,
  which `Assert.Ignore`s when `TL_TLB` is unset (unrelated to this change).
- PlayMode: exit 0, `error CS` count 0, XML `total=30 passed=30 failed=0 skipped=0`
  (identical to the pre-change baseline of 30/30).

Scratch project, lock, and the throwaway .NET hash console were deleted after
the runs. Nothing was committed.

## Known limits of this cut

- The parity run uses the managed `Library.Verify` path in EditMode (GCHandle
  pin works in the editor); the `LibraryBlob` NativeArray path from
  `docs/v5-unity.md` is not part of this cut, and no Burst jobs run yet.
- `TimelineSpec`/`StepSpec` writer is exercised through `LibraryWriter.Write`
  exactly as in .NET; editor bake menu and `TlV5World.cs`/`TlBurstJobs.cs`
  from the plan are not part of this cut.
