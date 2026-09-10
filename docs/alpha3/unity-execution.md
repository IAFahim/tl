# Alpha.3 Unity execution report

Issue [#27](https://github.com/IAFahim/tl/issues/27) assigns this report the Unity execution boundary. The tested sources live in [`experiments/Alpha3/Unity`](../../experiments/Alpha3/Unity/README.md). They are a handwritten host probe derived from the portable reference, not production TL generation.

The recommendations below record the experiment's proposal. The reviewed [plan](../../plan.md) selects ITimelineCatalog with named schema groups and a production IJobChunk gate over every timeline chunk, including incomplete scheduler-marker schemas. That stronger gate is not implemented by this probe. A uint route is catalog-local, not a stable saved/network identity. Disjoint schema-marker queries remain a possible optimization.

## Result

The alpha.3 selection/operation/commit shape compiles through the real Entities source generators and executes in Burst-scheduled `IJobEntity` jobs on the installed preview Editor.

Environment:

| Boundary | Version |
| --- | --- |
| Unity Editor | 6000.7.0a5, revision a15235a53881, Linux x64 |
| Entities | 6.7.0 |
| Collections | 6.7.0 |
| Burst | 2.0.0 |
| Test Framework | 1.8.0 |

The compile-only reference gate passed with zero warnings and errors:

```sh
dotnet build experiments/Alpha3/Reference/UnityProof.csproj -c Release \
  -p:UnityProbe="$UNITY_PROJECT" \
  -p:UnityEditor="$UNITY_EDITOR" \
  -p:UnityGenerators="$ENTITIES_GENERATORS" \
  -p:NuGetAudit=false
```

Its emitted files contain four generated `IJobChunk` implementations and the generated `CombatSystem` wrapper. This remains compile-only evidence because `dotnet build` does not run Unity IL postprocessing, the Editor, or Burst.

The isolated Editor receipt passed all four tests:

```sh
experiments/Alpha3/Unity/verify.sh /tmp/tl-alpha3-unity-schema-results.xml
```

The NUnit report recorded 4 passed, 0 failed. The jobs use `BurstCompile(CompileSynchronously = true)`, the test requires `BurstCompiler.IsEnabled`, and the Burst JIT manifest contains native entries for all six job-chunk producers (`ClearTimelineStagesJob`, both schema selectors, Damage, Animation, and Complete) plus the generated `CombatSystem.__codegen__OnUpdate` entry point.

## What the receipt proves

The selector ignores enable state so it visits rows whose operation markers begin disabled. Test-only stage counters record one damage and four animation job executions per entity over four forward frames, then the same counts in reverse. Damage changes Health before Animation reads it on forward movement: Attack ends at Health 90 and Pose 380, while HeavyAttack ends at Health 80 and Pose 360. Reversing schedules Animation before Damage and restores both entities to Health 100, Pose 0, and position 0. These exact values fail if the job dependency chain or direction-specific operation order changes.

A completed entity stays at position 4 while a live entity advances from 0 to 1 in the same scheduled update. Completion of one row therefore does not stop another row.

The schema counterexample uses two rows without `PoseComponent`:

- `DamageOnlyAttack` requires Resistance and Health and advances successfully, applies one damage occurrence, and commits.
- `Attack` requires Damage and Animation. It starts with a deliberately stale enabled damage marker. The clear phase removes pending selection and disables all operation markers before schema selection. The full-schema selector cannot match the incomplete archetype, and the damage-only selector rejects its asset schema. Health, playback, and operation counters remain unchanged.

This establishes the required no-partial-work behavior for the tested host. A fixed union-of-all-components query would incorrectly exclude the valid damage-only row. Independent operation queries without the clear/schema gate would allow the stale damage marker to apply Damage before the missing Animation stage.

`Frame<TTrack,TClip>` is byref-like. The operation bridge creates it from local track and clip values, calls the authored typed `Execute` immediately, and lets it die before the `IJobEntity.Execute` invocation returns. Reflection checks every scheduled job and the system for byref-like instance fields. No `Frame`, span, borrowed component reference, or generated context crosses a scheduling boundary.

## Production schema gate

The smallest explicit catalog is one build/import file that names the generated query and its closed asset set. For example:

```json
{
  "query": "CombatQuery",
  "assets": [
    "asset:attack",
    "asset:heavy-attack",
    "asset:damage-only-attack"
  ],
  "components": {
    "Resistance": "ResistanceComponent",
    "Health": "HealthComponent",
    "Pose": "PoseComponent"
  }
}
```

The Unity frontend resolves the stable asset identities, reads each validated neutral operation schedule, verifies the component bindings against the C# operation signatures, and generates `CombatQuery.Tick(ref SystemState, uint gameTick, int delta)`, the marker components, the schema-specific selectors, typed operation jobs, and completion. A complete consumer is:

```csharp
[BurstCompile]
public partial struct CombatSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var clock = SystemAPI.GetSingleton<GameClock>();
        CombatQuery.Tick(ref state, clock.Tick, clock.Delta);
    }
}
```

The neutral schedule contains logical slots and operation IDs. The catalog's C# component names remain in the Unity binding and never enter `Tl.Compiler`.

Generation partitions the catalog by exact required-component schema. Entity import or the generated creation helper installs exactly one enableable schema marker for that partition. Assets may vary dynamically within the same schema. Switching to an asset with a different schema is a synchronized reconfiguration that installs the new component set and schema marker before the next Tick; assigning only a new runtime asset ID is invalid.

Each tick first runs one catalog-wide clear job requiring only timeline state and scheduler markers. It clears pending selection and operation markers even when gameplay components were removed. Then one selector job per distinct schema requires that schema marker and its complete component set. The schema queries are disjoint, so selection visits each valid row once; an incomplete archetype matches none. Typed occurrence jobs require the current operation marker, and a final job commits only selected rows and clears markers.

For `E` entities, `K` component schemas, and `S` ordered stages, this design does O(E) clear and selection work plus the entity visits for selected occurrences. It schedules at least `K + S + 2` dependent jobs per simulated tick. The handwritten probe uses value checks instead of generated schema marker types, so its schema selectors can overlap and cost O(E*K); that fixture shape must not become the production implementation. Grouping occurrence stages can also create a Unity job-count problem. Neither this receipt nor the ordering prototype establishes the below-3-ns target.

## Generator handoff

Ordinary Roslyn source output cannot feed another generator in the same compilation. A TL generator using that phase cannot emit a new `IJobEntity` declaration and expect Entities to transform it in the same pass. Newer experimental pre-compilation output for additional files is a separate, unqualified host capability; it does not support reading Execute symbols through CompilationProvider.

The concrete Unity path is a two-phase precompile importer. It reads the catalog and timeline assets, writes deterministic C# 9/HPC# `.Generated.cs` files before script compilation, and requests one Unity refresh. Unity's following compilation then presents those physical sources to the Entities generator and Burst pipeline. The importer writes atomically, preserves the file and timestamp when content is identical, records input/output hashes, deletes stale owned outputs, and prevents a refresh loop. The existing .NET incremental generator remains an independent IDE path. If only the timeline kernel is generated and `IJobEntity` wrappers are authored source, both generators may run in one compilation because the Entities generator does not need to discover a TL-generated job declaration.

## Review of the manager contract

The plan at `origin/docs/27-alpha3-plan@fb70db8` correctly keeps production migration behind the ordered schedule, generator bridge, complete-schema gate, and real Burst acceptance. The Unity findings sharpen four decisions before contract freeze:

1. Use the explicit catalog and generated per-schema marker/query shape above. Runtime asset IDs select only among the catalog's known definitions and cannot infer components.
2. Retain the full current Frame boundary, including signed cycle and clip/timeline/completion flags, until a separately reviewed ABI change proves every removed field redundant. The 4-byte reference Playback and reverse-only Frame flags are prototype sizes, not compatibility targets.
3. Encode empty separately from every nonempty asset. A uint catalog-local route with zero reserved for empty and nonempty routes starting at one represents the required 65,536 nonempty assets; a ushort cannot represent that domain plus empty. Persisted identity is a separate contract.
4. Keep the plan's job-count and O(E*K*S) warnings as release blockers. The recommended schema markers remove overlapping schema scans, but occurrence-stage scheduling and large signed deltas still need measured design work.

## Limits and failed assumptions

The first Editor compile failed because an assembly referencing Entities must also reference Collections; the asmdef now declares it. The second compile failed when a temporary expression was passed to an `in` parameter; the test now stores the selection in a local first. Both corrections are in the preserved probe.

No PlayMode, player, IL2CPP, stable Unity 6000.0/Entities 1.4.3, allocation, or performance result was run here. The jobs use hard-coded assets and dispatch. General A-B-A and opposing order representation belongs to the separate ordering workstream, and production source generation belongs to the generator workstream. Alpha.3 remains a design with executable feasibility evidence, not a released implementation.
