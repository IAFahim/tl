# Total playback API reference

This standalone reference supersedes the lifecycle policy in `tl-unified-api-reference`. It is manually written reference compiler output, not a new production generator or a released tl API.

## Public contract

`new TimelineState(Attack.Asset)` assigns an immutable baked asset and leaves playback at its valid default position, zero. There is no start operation, started flag, initialized flag, binding operation, or per-tick success boolean. A default asset is empty. The asset's representation is private to the reference; these three small fixtures do not establish production ID encoding or capacity.

`CombatQuery` borrows caller-owned .NET component columns. Its constructor validates lengths and memory overlap once; malformed raw storage produces a setup exception before execution. The same row index must identify the same entity in every column. The constructor neither copies components nor allocates a frame queue. A default query is empty and ticking it is a no-op. It cannot be retained across an asynchronous call.

`CombatQuery.Tick(gameTick, delta)` runs all requested, available simulation ticks, grouping each stage across entities. Finite timelines clamp independently at either boundary. Completed entities do not prevent other entities from advancing. Looping assets wrap local position. Zero delta is a no-op. The .NET loop stops early when no entity can advance.

Game time is supplied by the scheduler. `gameTick` is the external cursor before the movement: forward +3 from 200000 supplies 200000, 200001, 200002 to operations; reverse -3 from 200003 supplies 200002, 200001, 200000. The uint game clock wraps explicitly. Local loop position does not count total elapsed cycles. Saturation discards overshoot; it is not an invertible operation, and a live clock is not an event history for rollback.

The shared per-step protocol is `Timeline.Select`, the typed `Execute` operations, and `Timeline.Complete`. Selection is an immutable pending value and never processes gameplay data or changes committed playback. Repeated selection before completion replaces the pending selection from the same committed position. Completion applies and clears it; repeated completion is a no-op.

`DamageJob` and `AnimationJob` each have an authored `Execute(in Frame<...>, ...)` and a reference-generated `Execute(in TimelineState, ...)` overload. Both hosts call the latter; it constructs borrowed frames and calls the authored method. The extra in/ref parameters are ordinary C# parameters, not a fixed-arity generic interface.

## Unity

`TimelineComponent.Value` contains the same TimelineState as .NET. Typed enableable tags select the operation jobs. The selection job ignores tag enable state so disabled operations can become active. Completion clears the tags after executing the stage. Shared frames are borrowed only inside Execute and never stored in scheduled jobs.

`CombatSystem` owns advancement of a single application GameClock component and schedules the same stages. Create that clock once per World; set Delta to 1 for normal updates or to a signed catch-up/rewind amount. An absent clock prevents the system from updating. GameClock is application scheduling data, not a per-entity timer.

The fixture entity creation helper installs the complete component schema and disables the operation tags before publication. The example assumes that schema and exclusive pipeline ownership remain stable until its jobs finish. It does not define behavior for removing required components or manually scheduling consumers outside the pipeline.

## Verification

```sh
dotnet run --project Proof.csproj -c Release
dotnet build UnityProof.csproj -c Release -p:UnityProbe="$UNITY_PROJECT" -p:UnityEditor="$UNITY_EDITOR" -p:UnityGenerators="$ENTITIES_GENERATORS"
```

The executable covers 10,000 entities, mixed completed and active timelines, no required Start, empty assets/queries, zero allocations over 2,560,000 warm entity-ticks, forward/reverse ordering, looping, int.MinValue/int.MaxValue finite overshoots, borrowed storage validation, and idempotent completion. This reference has a 4 B Playback, 16 B Selection, and 24 B TimelineState under Unsafe.SizeOf; those are not the production ABI or claimed minimum sizes.

The Unity source compiles against installed Unity assemblies with the real Entities source generators and no warnings/errors. It has not been run through Unity Editor/player IL postprocessing or Burst execution. Supply the three environment-specific paths to the project; no installed Unity binaries are committed.

## Remaining scope

This is a complete small example, not the complete arbitrary-asset scheduler. The fixture has a fixed dependency order: damage before animation forward, reversed on rewind. Arbitrary order, multiple tracks using the same operation, overlapping clips, the full lifecycle frame metadata, designer import and automatic TL generation still need implementation and qualification. Generalized scheduling must retain per-entity ordered stages; grouping all operations solely by type is insufficient.

The borrowed Frame layout is a reference boundary, not a measured optimized representation. No SIMD or sub-3-ns result is claimed. Mirror checks use exactly representable fixture numbers and do not establish general floating-point reversibility. Unity schedules one chain per requested tick and lacks the .NET early-exit optimization; large requested deltas need scheduler optimization before this can serve as a production batch API.
