# Generator path feasibility report

Commit base: `5198e080a8e50409351b48ead6511840ead946dc`

## Result

The current C# generator cannot be adapted by renaming `TrySeek`. It combines lifecycle validation, frame selection, operation calls, and playback commit in one emitted method. It also owns a C#-specific schedule model that bypasses `Tl.Compiler`. Alpha.3 needs a neutral ordered-occurrence plan, a separate C# binding, a total selector, generated typed executors, and commit after all selected stages succeed.

The executable in `experiments/Alpha3/Generator` is a real analyzer consumer. At the base commit it proves these facts from the emitted assembly and behavior:

- the facade exposes `Start` and `TrySeek`, and does not expose `Select` or `Complete`;
- `Data` borrows the playback and operation slots with `ref` fields;
- `TrySeek(+1)` invokes the authored `Seek` immediately and commits playback in the same call;
- zero movement succeeds without an operation call;
- the sample lowers one track and one clip to one generated region;
- the three analyzer files total 11,414 UTF-8 bytes for this small timeline.

This is evidence about the existing generator. The proposed declarations and generated members below are a migration recommendation; they are not implemented by this branch.

## Current production path

```text
partial ITimeline declaration
  -> TimelineIncrementalGenerator candidate/source closure
  -> HeterogeneousReader Roslyn analysis
  -> C#-specific HeterogeneousTimeline
  -> HeterogeneousEmitter region lowering and source
  -> Start / Data / DynamicData / TrySeek / dynamic routers
  -> SeekCore selects, calls hooks and Track.Seek, then commits
```

`TimelineIncrementalGenerator.cs` collects all candidate timelines and combines them with a compilation-wide input. Its key includes the relevant source closure, parse options, and the path and content hash of every metadata reference. This preserves correctness for options and references, but an edit to one candidate can recompute the collected analysis and shared outputs.

`Analysis/HeterogeneousReader.cs` recognizes constant-shape calls in `Define`, flattens one include level, and validates tracks, clips, hooks, overlaps, and `Seek` slots. It admits up to 256 distinct track or clip kinds and 65,536 instances. Its result contains C# type names, constructor expressions, and `in`/`ref`/`out` slot spellings.

`Model/Heterogeneous.cs` is therefore a language binding and a semantic plan mixed into one model. `HeterogeneousEmitter.cs` lowers regions again, executes the captured C# constructors in static initializers, lazily registers a process-local dynamic route, and emits eager forward and reverse calls. `Tl.Compiler.TimelinePlan` is not used by this path. The current neutral plan only carries a string operation ID, numeric payload indexes, intervals, and regions; it has no ordered occurrence, slot schema, neutral type/constant description, frame requirements, or binding.

The generated `SeekCore` also preserves the alpha.2 behavior that alpha.3 replaces: unstarted/stopped and out-of-range movement return `false`; finite overshoot rejects the whole movement; hooks and track consumers run inside selection; and a process-local `ushort` registry controls dynamic dispatch.

## Concrete source declaration

The asset/schema set must be explicit at build time. A runtime asset handle cannot reveal arbitrary component types to Roslyn or Unity. The track binding must also compile before generation. The smallest tested ordinary-C# shape infers settings at `Track`, selects the job at `Use`, and infers the clip later at `Clip`:

```csharp
public readonly ref struct Builder
{
    public TrackRef<TTrack> Track<TTrack>(in TTrack track)
        where TTrack : unmanaged => default;

    public void Clip<TTrack, TJob, TClip>(
        in TrackRef<TTrack, TJob> track,
        in TClip clip,
        uint start, uint end)
        where TTrack : unmanaged
        where TClip : unmanaged
        where TJob : unmanaged, ITimelineJob<TTrack, TClip> { }
}

public readonly ref struct TrackRef<TTrack> where TTrack : unmanaged
{
    public TrackRef<TTrack, TJob> Use<TJob>() where TJob : unmanaged => default;
}

public readonly ref struct TrackRef<TTrack, TJob>
    where TTrack : unmanaged
    where TJob : unmanaged { }

public readonly partial struct DamageOnlyTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack(2)).Use<DamageJob>();
        builder.Clip(damage, new DamageClip(7), 0u, 10u);
    }
}

public readonly partial struct DamageAnimationTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack(2)).Use<DamageJob>();
        var animation = builder.Track(new AnimationTrack(1)).Use<AnimationJob>();
        builder.Clip(damage, new DamageClip(7), 0u, 10u);
        builder.Clip(animation, new AnimationClip(1), 0u, 10u);
    }
}
```

`ITimelineJob<TTrack,TClip>`, both `TrackRef` types, `Builder.Track`, and `Builder.Clip` are runtime declarations, so the first compiler pass never depends on a generated extension or a partial declaration in another assembly. The `Clip` constraint makes C# check the full pairing. The normal experiment build accepts `DamageJob + DamageTrack + DamageClip`; compiling with `INVALID_MAPPING` adds `DamageJob + DamageTrack + OtherClip` and fails with CS0315. The generator still diagnoses a track that is never bound to a clip and validates the supported static `Execute` signature.

The smallest asset-set declaration accepts the manager's proposed `ITimelineSet`: `TimelineSetBuilder.Include<TTimeline>` is a runtime method with one inferred-free generic argument. The generator derives and groups the per-asset schemas:

```csharp
public readonly ref struct TimelineSetBuilder
{
    public void Include<TTimeline>() where TTimeline : unmanaged, ITimeline { }
}

public readonly partial struct Combat : ITimelineSet
{
    public static void Define(scoped TimelineSetBuilder builder)
    {
        builder.Include<DamageOnlyTimeline>();
        builder.Include<DamageAnimationTimeline>();
    }
}
```

The generator derives each asset schema's columns from the statically resolved `Execute` methods, groups identical schemas, and emits typed views under `Combat.Query`. It diagnoses an asset listed twice, an unlisted runtime route, and conflicting slot roles, modes, or types. The declaration does not repeat `Resistance`, `Health`, or `Pose`; those remain ordinary parameters in shared operation code.

```csharp
public readonly partial struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        in Resistance resistance,
        ref Health health)
        => health.Value -= frame.Direction * frame.Clip.Amount
            * frame.Track.Multiplier * resistance.Scale;
}

public readonly partial struct AnimationJob : ITimelineJob<AnimationTrack, AnimationClip>
{
    public static void Execute(
        in Frame<AnimationTrack, AnimationClip> frame,
        ref Pose pose)
        => pose.Frame += frame.Direction * frame.Clip.Step;
}
```

Slot identity is `(declared role, neutral type)`, not CLR type alone. By default the parameter name is the declared role: `bodyPose` and `aimPose` are different columns even when both have type `Pose`, while the same role and type can be shared across operations after access-mode compatibility is validated. An ambiguous rename, same role with conflicting type, or incompatible `in`/`ref`/`out` use is a generation diagnostic in the first slice; explicit alias mapping can be designed later.

For .NET, the set generates two typed borrowed views rather than one fixed all-component query:

```csharp
var query = new Combat.Query();
query.DamageOnly(damageTimelines, resistances, health)
    .Tick(gameTick: 200000, delta: 1);

query.Mixed(mixedTimelines, resistances, health, poses)
    .Tick(gameTick: 200000, delta: 1);
```

`DamageOnly` accepts an entity with `Resistance` and `Health` and does not require `Pose`. `Mixed` requires all three components. Both views validate equal row membership and length, prohibited writable aliasing, asset membership, and schema compatibility before the first selected row. A mixed asset passed to the damage-only view fails setup before Damage executes. It cannot partially apply Damage and then discover that Pose is missing, and it cannot commit that frame.

For Unity, distinct roles of the same value type become distinct generated role component wrappers, and the wrapped field is the canonical ECS storage. The operation receives `in` or `ref` directly to that field; no per-tick component copy is introduced. The generated setup API adds the correct role wrapper, and the chunk schema gate checks those wrapper types. Same-role sharing uses one wrapper.

The exact public builder and view names still require API approval. The semantic requirement to freeze is the explicit `timeline set -> asset types` relation, derived per-asset schemas, role-aware typed columns, and whole-schema validation before selection. Generating a query from whatever asset ID appears at runtime is not viable.

`experiments/Alpha3/Generator/ProposedShape.cs` compiles these runtime declarations, builders, jobs, timelines, and set plus a separate partial that models the generated `Combat.Query` views and a complete consumer. The user's `Combat.Define` does not call any generated member. The TL generator adds its own partial before the C# compilation completes; no second generator needs to inspect that output.

## Selection, execution, and completion boundary

The generated host keeps the public convenience operation total:

```csharp
query.Tick(gameTick, delta);
```

Internally each crossed frame is exactly:

```text
validate row schema
  -> select immutable occurrence slice and pending position
  -> execute occurrence ordinal 0 across compatible rows
  -> barrier
  -> execute occurrence ordinal 1 across compatible rows
  -> ...
  -> complete advancing rows
```

Selection reads only immutable asset data and committed timeline state. A fresh selection replaces an unexecuted selection. A gap produces an advancing selection with zero occurrences. Empty/default and completed finite rows produce no work. Finite movement clamps while executing every remaining crossed frame; loops execute every crossed frame. Zero movement changes nothing. The generated host folds this protocol for multi-frame deltas, so callers never guess queue capacity and do not manually coordinate phases.

The ordered-occurrence proof at `f73c041ac19ef0454316f63716780dce317660ce` supplies the representation Atom C should consume: each selected frame borrows one immutable occurrence slice, array order is stage identity, `ushort` stores a count that includes 256, and each occurrence stores operation ID, authored track index, flags, and payload index. This represents `A-B-A`, opposing asset order, and exact reverse traversal without storing a per-entity occurrence queue.

The demonstrated executor scans `E` rows for every generated operation type `K` at every stage `S`, so dispatch is `O(E * K * S)` plus the real occurrence calls. On Unity it also implies up to `K * S` typed job passes and stage barriers for a frame if implemented literally. That is a correctness baseline, not evidence for the sub-3-ns goal. Atom C should emit the scalar baseline first, retain stage identity, and measure indexed active-row compaction or chunk masks only after oracle parity. Operations with shared or unclassified effects use deterministic entity-major scalar execution; typed signatures alone do not prove row independence.

## Unity schema gate

Unity must validate the whole asset schema before it enables any operation stage. Filtering each generated `IJobEntity` independently is unsafe: a mixed row missing `Pose` would match Damage, apply it, then disappear from Animation.

The smallest general gate is a generated `IJobChunk` selection pass over every chunk containing timeline state. It tests the component type handles required by the catalog once per chunk, derives the present schema mask, and compares every row's asset-required schema ID or mask before producing an advancing selection. Only a passing row can receive the stage/operation enable state. Generated typed operation jobs then borrow ordinary component references and construct `Frame` inside `Execute`. Completion advances only rows whose complete selected stage chain ran.

This gate is recomputed after the host's structural-change dependencies on every tick; it does not trust enableable markers left from a previous archetype. A structural change moves an entity to a new chunk, and the next selection pass derives membership from that chunk. The lower-bound gate cost is `O(chunks * C + E)`, where `C` is the distinct component-type set in the catalog and `E` is its timeline rows, before stage dispatch. Caching a chunk schema fingerprint is an optimization only if invalidation by Unity's structural version is proven. The materialized Unity source must use the Unity workstream's qualified generation path; a Roslyn generator cannot feed another generator in the same compilation pass.

## Frame and cycle compatibility decision

Alpha.3 should retain the current public `Frame<TTrack,TClip>` information for the first migration: borrowed Track and resolved Clip, `GameTick`, `TimelineTick`, signed `long Cycle`, `TrackIndex`, and the byte `FrameFlags` values for clip/timeline boundaries, completion side, looping, and reverse. Direction remains derived from `Reverse`. Stage ordinal and active count stay internal.

The 4-byte playback in the reference is therefore only a prototype measurement. It does not establish a production ABI. Loop selection computes `Cycle` with floor division for negative positions, and the selector must carry enough pending coordinates to construct the same frame at execution without gameplay data. Finite frames use cycle zero. Blend resolution returns one resolved clip for its authored track occurrence; the frame does not expose two calls. Boundary flags are derived for every crossed frame before the occurrence slice executes and require parity tests against current signed-seek fixtures.

Dynamic asset identity is also separate from frame state. If zero is empty, a `ushort` handle supports 65,535 nonempty values. Supporting 65,536 nonempty assets plus empty requires a wider handle or an additional validity state. Process registration order is never serialized as stable asset identity.

## Neutral plan and C# binding

`Tl.Compiler` should own deterministic schedule meaning and validation through records equivalent to:

- stable asset identity, looping, duration, and ordered authored tracks;
- neutral operation and payload IDs with byte-exact constant blobs;
- clips and resolved blend policy;
- region/frame occurrence slices in authored order;
- occurrence operation ID, authored track index, payload reference, and static flags;
- operation slot requirements by declared role, neutral type ID, and read/write mode;
- validated width and alignment limits.

It must not contain Roslyn symbols, fully qualified C# spellings, syntax text, or constructor expressions. `Tl.Gen.CSharp` owns a `CSharpBinding` beside that plan: operation/track/clip/component symbols, source locations, safe display names, typed constant emitters, and the exact generated invocation.

The current static fields execute arbitrary accepted constructor expressions at type initialization. That is not a portable or deterministic constant-data contract. The frontend should accept an explicit bounded constant grammar, normalize primitive and enum bits, preserve signed zero and NaN payload bits where promised, encode composite unmanaged values by reviewed field order and endianness, and reject nonconstant constructors. Hashes locate candidates; byte equality confirms deduplication. Large clip/blend payloads need immutable asset storage or host-owned scratch, not unbounded stack copies.

## Exact production migration

The production work should land in dependency order, with one owner for the C# emitter.

1. **Core state contract.** Change `src/Tl.Core/Playback.cs` and `src/Tl.Core/Compiled.cs`. Remove `Started`/`Stopped` from normal typed playback, define explicit empty asset identity, pending selection and completion semantics, preserve the full Frame/cycle contract, and keep borrowed frames call-scoped. Dynamic routing can remain isolated as a compatibility layer; it cannot drive the typed catalog path.
2. **Neutral ordered plan.** Extend `src/Tl.Compiler/TimelinePlan.cs` and `ValidatedTimelinePlan.cs`. Add neutral slot/type/constant records and deterministic ordered occurrences produced once by validation. Keep the 256 authored-track limit distinct from occurrence count and diagnose width overflow. The C# emitter must consume this lowering instead of reproducing regions.
3. **Frontend and binding split.** Replace the mixed records in `src/Tl.Gen.CSharp/Model/Heterogeneous.cs` with a neutral-plan result plus C# binding. Update `Analysis/HeterogeneousReader.cs` to read `ITimelineJob<TTrack,TClip>.Execute`, `ITimelineSet` declarations, role-aware ordinary `in`/`ref` arguments, and the bounded constant grammar. Preserve source-located diagnostics and the SemanticModel/syntax-tree regression test.
4. **Selector emitter.** Split `HeterogeneousEmitter.cs`. Emit immutable asset tables, total per-frame selection, movement folding, full Frame coordinates/flags, and completion with no operation or gameplay-column access in the selector. Remove `Start`, `TryStop`, typed `TrySeek`, and eager hook calls from the approved alpha.3 facade.
5. **Typed executor emitter.** Emit one .NET borrowed view per derived timeline-set schema and direct typed `Execute` calls. Consume occurrence ordinal as internal stage identity, reverse the slice on rewind, insert barriers, and commit after the complete chain. Diagnose unsupported hooks or cross-entity classifications rather than silently changing their order. Emit the per-chunk Unity schema gate and wrappers through the qualified materialization path.
6. **Incremental and CLI graph.** Refactor `TimelineIncrementalGenerator.cs` so declaration analysis, neutral plan, timeline-set binding, and each backend artifact have stable semantic comparers. Keep compilation-wide aggregation only for outputs that require it, such as a set route table. Make analyzer and `GeneratorCli` consume the same normalized plan/binding serializer and content hash. Unchanged materialized output preserves bytes and timestamps.
7. **Receipts and migration.** Replace alpha.2 assertions in `SeekReaderTests.cs`, `GeneratedSeekTests.cs`, `GeneratedSeekRuntimeTests.cs`, `CompatibleRoutingTests.cs`, and the public API approvals. Extend `HeterogeneousCompositionTests.cs` for ordered repeats/blends and `IncrementalGeneratorTests.cs` for catalog-local invalidation and analyzer/CLI byte parity. Add damage-only versus invalid mixed-schema tests, default/empty/zero/clamp/loop extremes, A-B-A/opposing order/reverse/256 tracks, exception-before-commit, warmed allocation, and .NET/Unity oracle parity. Migrate `tests/Tl.Alpha` and `samples/Mixed` only after the new generated path is green.

Hooks are operations in the ordered plan if retained. Treating them as emitter-only before/after lists would recreate a second ordering system. `Include` must flatten to the same neutral occurrence identities with deterministic asset constant ownership.

## Smallest feasible production slice

The first real slice is one finite, nonoverlapping, hook-free C# asset; one declared schema; one operation with one `in` and one `ref` component; a generated total selector; a generated borrowed .NET query; and completion. It must cover default playback, empty, zero, both directions, finite overshoot, a gap, one completed row beside a live row, schema rejection before effects, exact operation count, and 0 B warmed. Unsupported loops, blends, includes, hooks, dynamic routing, and Unity emission receive explicit generation diagnostics in this slice.

That slice is small enough to demonstrate a real path through Core, Compiler, reader, emitter, and query without pretending the handwritten reference is generated. It depends on the ordered occurrence representation and the frozen Frame contract. Loops and resolved two-clip blends are the next generator atom; timeline sets with multiple derived schemas plus Unity materialization follow only after the single-schema query is green. Alpha.3 release remains blocked until every required case and Unity runtime receipt passes.

## Blockers and manager-plan review

The manager plan at `fb70db89e327f8d0c975eb729bc24e7a28a68d02` correctly separates selection, ordered typed execution, and completion; requires an explicit schema set; retains the full Frame question; rejects type-only ordering; and sequences Core/Compiler before generator execution. The implementation atoms are usable with these additions:

- freeze the `ITimelineSet` declaration and its compiled settings-first track binding before Atom C;
- freeze the occurrence-slice/count representation proved by the ordering workstream before the selector and executor share a format;
- retain `Cycle` and all current Frame boundary flags for the first migration, with exact compatibility receipts;
- require a whole-schema gate before selection, including the valid damage-only and invalid mixed cases;
- record the `O(E * K * S)` and `K * S` job/barrier baseline until a measured alternative exists;
- freeze neutral constant encoding and definition lifetime before Unity schedules readers;
- decide the empty-plus-65,536 asset representation without overloading a `ushort` zero sentinel.

The source budget reported by the manager is 207,337/250,000 B. This branch changes only documentation and experiments, so it adds zero production bytes. The shipping implementation still needs deletion-first accounting within that remaining 42,663 B.

## Receipts

Run the current-path probe and retain both generation paths:

```sh
dotnet run --project experiments/Alpha3/Generator/CurrentPath.csproj -c Release
dotnet build experiments/Alpha3/Generator/CurrentPath.csproj -c Release \
  -p:DefineConstants=INVALID_MAPPING -p:NuGetAudit=false -m:1
# the preceding command must fail with CS0315 at the OtherClip call
dotnet msbuild experiments/Alpha3/Generator/CurrentPath.csproj \
  -t:TlGenExport -p:Configuration=Release -p:NuGetAudit=false -m:1
wc -c experiments/Alpha3/Generator/obj/generated/Tl.Gen.CSharp/\
Tl.Gen.CSharp.TimelineIncrementalGenerator/*.cs
```

The first command prints:

```text
current generator: Start + borrowed Data + eager TrySeek
generated schedule: 1 track, 1 clip, 1 region
proposed authoring: Track(settings).Use<TJob>() compiles
```

The analyzer output lives under `obj/generated`; the explicit CLI export lives under `obj/Release/net10.0/TlGenCompile`. Both are disposable. This report claims a validated design path and current-generator evidence, not a production vertical slice or an alpha.3 release.

At this checkpoint the exact probe and positive settings-first declaration passed; the negative mapping failed with the expected CS0315; analyzer and CLI files compared byte-for-byte; all 65 focused generator tests passed; the preserved 10,000-row reference passed with 0 B over 2,560,000 entity-ticks; source remained 207,337/250,000 B; and `git diff --check` passed. The conditional `UnityProof.csproj` build could not run because this machine has no recorded `UnityProbe`, `UnityEditor`, or `UnityGenerators` paths; it failed at the project's explicit environment guard before compilation. No Unity execution claim is made here.
