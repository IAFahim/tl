# v1.0.0-alpha.3: total timeline jobs

Design owner: [issue #27](https://github.com/IAFahim/tl/issues/27). Live work: [Project 6](https://github.com/users/IAFahim/projects/6/views/4). Base: `a267a3b1ab59d37b78814449db51f7d5a8656ec1`. Package/tag target: `1.0.0-alpha.3` / `v1.0.0-alpha.3`.

This is the breaking design and delivery contract. It does not claim that the existing generator implements it. The alpha.2 package version remains until production migration and release validation succeed. The previous plan remains available in Git at the base commit.

## What changes

A timeline selects which operations must execute. It does not receive gameplay components while selecting. Generated typed jobs consume that selection over compatible entity rows. The .NET and Unity adapters call the same authored `Execute(in Frame<Track, Clip>, in input, ref output)` operation.

The compiler knows the supported operation signatures. Designers choose immutable track values, clip values, windows, and ordering without editing those operations. Adding a new operation type requires compilation; editing data of an existing supported type does not inherently require translating new gameplay code.

One timeline can contain TA..TX track types and CA..CX clip types. A job receives only its `(TTrack, TClip, inputs..., results...)` pairing. All those jobs share one pending frame and one committed playback position for that entity's timeline. The generated coordinator advances time; an operation job or independently authored system never advances it on its own.

Normal playback has one public operation: `Tick(gameTick, delta = 1)`. Default playback is ready at local position zero. There is no mandatory `Start`, `Bind`, `Compile`, `InMemory`, or per-tick failure boolean. Empty assets do nothing. Finite timelines stop independently at their boundaries. Signed movement executes every available crossed frame.

## Evidence before implementation

| Boundary | Existing production | Preserved reference | Alpha.3 requirement |
| --- | --- | --- | --- |
| Lifecycle | `Start` and `TrySeek`; invalid movement rejects | Total `Tick`, default ready, independent clamping | Generator emits the total contract |
| Work execution | Whole timeline invokes consumers per entity | Selection, Damage stage, Animation stage, completion | Arbitrary ordered operation occurrences |
| .NET data | Borrowed generated contexts | Borrowed component columns, validated at construction | Generated schema-specific query over caller storage |
| Unity | Existing signed-seek package and player qualification | Real IJobEntity source generation, compile-only at preservation | New selection/filter/order path executes under Burst |
| SIMD | Historical scalar specializations | No SIMD claim | Measured eligible kernels; scalar fallback preserves semantics |
| Designer data | C# declarations; incomplete neutral plan | Three hard-coded assets | Validated language-neutral schedule and separate bindings |

Run the portable reference with:

```sh
dotnet run --project experiments/Alpha3/Reference/Proof.csproj -c Release
```

The preserved receipt is 10,000 entities with default playback, mixed completion, clamping, empty assets, loops, signed order, and setup validation; 0 B over 2,560,000 entity-ticks. Its measured struct sizes are Playback 4 B, Selection 16 B, TimelineState 24 B. These are reference measurements, not a frozen production ABI or performance result. The reference does not prove arbitrary stage ordering, overlapping clips, complete frame flags, or generation by TL.

The three bounded workstreams produced these additional receipts:

| Workstream | Evidence | What remains unproved |
| --- | --- | --- |
| [Ordered operations](docs/alpha3/operation-order.md), PR #28 | Independent forward/reverse trace parity for A-B-A, opposing orders, simultaneous clips, gaps, 256 tracks and mixed completion; 0 B warmed across 100 passes | Production sparse layout, full frame ABI, reduced scans, timing |
| [Unity execution](docs/alpha3/unity-execution.md), PR #29 | Actual Editor EditMode 4/4; six Burst job producers and system entry; enabled filtering, dependencies, missing-component/stale-marker case, borrowed-frame lifetime | Generated jobs, missing scheduler-marker gate, stable Editor, player/IL2CPP, allocation and timing |
| [Generator path](docs/alpha3/generator-path.md), PR #30 | Existing real analyzer generates the old API; proposed builder/catalog syntax compiles; incompatible clip mapping is a compiler error | Production total Tick, schema queries, shared neutral lowering and Unity materializer |

The preserved production baseline builds with zero warnings/errors and passes 129 tests; the benchmark collector passes 7/7. None of these tests establishes a speedup for the new architecture. The Unity evidence is specifically Editor 6000.7.0a5, Entities/Collections 6.7.0 and Burst 2.0.0 on Linux x64. Those preview results do not qualify an untested stable release.

## Two-hour execution budget

The session starts at **2026-09-10 19:25:49 UTC**, or **2026-09-11 01:25:49 Dhaka**. The hard stop is **21:25:49 UTC / 03:25:49 Dhaka**. This budget includes planning and tests; it must not restart after every design change.

| Window | Owner | Deliverable and exit condition |
| --- | --- | --- |
| 0–10 min | Manager | Publish current integration history, issue, remote plan branch, and runnable reference; claim three bounded workstreams |
| 10–30 min | Three agents + manager | Falsify ordering, generator signatures, and Unity job execution assumptions; each publishes runnable evidence and limitations |
| 30–40 min | Manager + reviewers | Freeze one API and stage law; publish exact production atoms, owned files, and acceptance commands; reject unproved shortcuts |
| 40–75 min | Runtime, generator, Unity owners | Implement only the agreed end-to-end slice; first actual generated selector + shared operation + .NET query receipt by minute 60; no new speculative algorithm |
| 75–100 min | Review and integration | Add adversarial ordering/alias/empty/loop tests, run actual Unity receipt, inspect allocation and emitted code; take one measured optimization only if there is time |
| 100–120 min | Manager | Code freeze; exact-commit validation, package/API checks, push all work, review PRs, publish result or explicit blockers |

**Stop rules:** if the first real generated vertical slice is not green by minute 75, finish and push that slice and its failing boundary instead of broadening the API. If Unity/Burst or package checks are unqualified by minute 100, the release remains blocked. A release name never substitutes for passing receipts. Experiments that disprove an assumption are retained with the reason; they do not become shipping code by being copied into `src`.

The design request is complete when the selected contract, three reconciled reports, concrete implementation tasks, and recovery instructions are pushed. Shipping alpha.3 is the subsequent gate and requires the production checks below. Do not call a completed design a completed release.

## Irreducible runtime laws

1. **Definition:** validated immutable asset data owns intervals, typed payload references, operation identities, and authored order. It contains no entity state.
2. **Selection:** definition plus committed position and movement direction determines the pending frame and ordered operation occurrences. Selection reads no gameplay component and performs no gameplay effect.
3. **Execution:** each selected occurrence calls its statically known operation with borrowed data from the same entity. Its observable order is the authored order forward and the exact opposite occurrence order backward.
4. **Commit:** after the frame's operations finish, playback advances to its pending position. Repeating commit on a completed frame has no effect.

For per-entity state `s` and valid immutable definition `d`, repeated selection with identical arguments is identical. A fresh selection replaces an unexecuted selection; it does not append work. The public host owns select/execute/commit sequencing so users do not need to call those phases manually.

A multi-frame call is a fold of this protocol over available frames. It is not one destination sample, an unordered set of effects, or an aggregate that skips side effects.

In Unity, separate typed jobs/systems participate in one dependency chain. Selection precedes every occurrence, and commit depends on all required occurrences for that frame. A delayed second job cannot leave the first job's timeline already committed. Delta five repeats five complete select/execute/commit rounds; it does not run five Damage frames before five Animation frames. .NET observes the identical rule. Each occurrence gets shared local/game tick, cycle and direction from the pending frame, plus its own track/clip metadata.

## Total movement

| Situation | Required behavior |
| --- | --- |
| Assigned asset, default playback | First `Tick(..., 1)` executes local frame 0 |
| Default/empty asset | No callbacks and no allocation |
| `delta == 0` | Playback and components unchanged |
| Finite forward overshoot | Execute the remaining frames, then clamp at duration |
| Finite reverse overshoot | Execute reverse frames down to local zero, then clamp |
| One completed row among live rows | Only that row stops; live rows still execute |
| Looping definition | Wrap local position and execute every crossed occurrence |
| Empty query | Immediate no-op, including extreme deltas |
| Invalid authored windows/schema/payload | Diagnostic at generation/import/setup before execution |
| Consumer throws | Exception propagates; already executed effects are not silently rolled back |

`gameTick` is the external cursor before movement. Forward +3 from 200000 supplies 200000, 200001, 200002 to operations; reverse -3 from 200003 supplies 200002, 200001, 200000. The uint clock wraps explicitly. No wall clock is read by TL. The host owns the application clock even when every finite timeline has completed.

Local uint position is separate from signed long Cycle. Finite assets have Cycle zero. Looped playback increments or decrements the cycle only when crossing its boundary; at the signed counter limits arithmetic wraps explicitly in two's complement. This total policy replaces the old checked-overflow rejection and needs production min/max, wrap and rewind receipts. Preserve the complete current clip/timeline/completion flags while migrating. The prototype's four-byte Playback is not the production size promise.

For a loop of duration D > 0, cursor position p in [0,D), and cycle c, Frame.Cycle identifies the cycle of the emitted frame, not the cursor after commit:

| Movement | Emitted (TimelineTick, Cycle) | Committed (position, cycle) |
| --- | --- | --- |
| Forward, p+1 < D | (p, c) | (p+1, c) |
| Forward, p+1 = D | (p, c) | (0, unchecked(c+1)) |
| Reverse, p > 0 | (p-1, c) | (p-1, c) |
| Reverse, p = 0 | (D-1, unchecked(c-1)) | (D-1, unchecked(c-1)) |

A forward last frame at long.MaxValue reports that cycle and commits long.MinValue. Reversing immediately reports the same last frame at long.MaxValue and restores the old cursor. Duration one follows the same rules; duration zero emits no frame. Production tests must cover these exact coordinates, signed counter limits, uint.MaxValue duration and multi-loop walks.

Do not negate `int.MinValue` in `int`, iterate billions of empty steps, or schedule billions of empty Unity job chains. Finite work bounds come from the longest remaining live timeline. Looping replay with real effects is inherently proportional to the requested work; only a separately proven algebraic operation may collapse it.

Clamping loses overshoot information. Therefore `Tick(+n); Tick(-n)` is not universally identity. Exact restoration additionally requires the same executed frames, reverse operation order, reversible consumer operations, and appropriate event history. Floating-point subtraction is not a general inverse of floating-point addition.

## Public surface and shared consumer code

[The complete runnable reference](experiments/Alpha3/Reference/README.md) includes frame/state types, shared authored jobs, .NET query execution, Unity components, IJobEntity jobs, ISystem scheduling, and entity setup. [The declaration compile receipt](experiments/Alpha3/Generator/README.md) supplies the proposed builder/catalog types and labels its handwritten generated-surface placeholders. The selected consumer shape is:

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

public readonly partial struct DamageOnlyTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack(2)).Use<DamageJob>();
        builder.Clip(damage, new DamageClip(7), 0u, 10u);
    }
}

public readonly struct DamageRows;
public readonly struct MixedRows;

public readonly partial struct Combat : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<DamageRows>().Asset<DamageOnlyTimeline>();
        builder.Schema<MixedRows>().Asset<DamageAnimationTimeline>();
    }
}

var catalog = new Combat.Query();
var query = catalog.MixedRows(timelines, resistance, health, poses);
query.Tick(gameTick: 200000, delta: 1);
query.Tick(gameTick: 200001, delta: -1);
```

`ITimelineJob<TTrack,TClip>` identifies a pairing; ordinary C# parameters define any supported finite set of inputs and outputs. It does not attempt variadic generic `in`/`out` syntax. `ref` updates the caller's component; `in` is a borrowed read-only alias, not a copy of gameplay state. A job requiring evolving state uses `ref`. An `out` operation, if retained in the final signature grammar, must assign its result on every executed occurrence; skipped occurrences leave existing caller storage unchanged.

`Track(settings)` infers the track type, `Use<TJob>()` binds behavior, and `Clip(track, payload, start, end)` infers and checks the clip pairing. This avoids the invalid assumption that C# infers omitted generic arguments after `Track<TJob>(settings)`. The ordinary declaration types exist before generation; no generated type is needed to read their definition.

The catalog names schema groups and asset membership. Execute signatures determine component requirements, so a damage-only query does not require Pose. A role/name plus type identifies a logical slot; access is an effect joined across its operations. Damage's `ref Health health` and Animation's `in Health health` share the same health slot. Two differently named Pose roles remain distinct. Ambiguous mappings are compile/import diagnostics. A runtime number alone cannot infer arbitrary C# component types.

Unity's generated catalog entry is `Combat.Tick(ref systemState, gameTick, delta)` inside an ISystem; .NET's borrowed schema query has the same Tick operation. The Unity host borrows ECS storage through job parameters rather than storing .NET spans in jobs. Authored operation signatures and domain semantics are shared; host scheduling arguments and declaration language versions are explicit adapter differences.

Unity's thin IJobEntity wrapper calls the same shared Execute bridge. A generated IJobChunk selector visits every timeline chunk, clears pending selection and any present stage markers, then checks the current complete component and scheduler schema before enabling work. Missing scheduler markers must not bypass clearing. Disabled markers exclude operation jobs; completion advances only rows whose entire selected schedule ran. The tested IJobEntity clear requires both markers and therefore does not yet prove this stronger production gate. Disjoint schema-marker queries are an optimization candidate, not a prerequisite or an implemented result.

`Frame` contains borrowed track and resolved clip values, local/game tick, authored track index, full boundary flags and signed long Cycle. Direction belongs in flags with a computed convenience property, not a stored int. No ordinal or active-count field is added. Generated code can eliminate unused fields from machine code; do not claim source-level size reduction without layout/codegen evidence. Unity's Frame implementation must use its qualified language/Burst subset; C# 14 ref-field or static-abstract syntax in the declaration proof is not a Unity compatibility receipt.

## Ordered batching, not type sorting

A type mask answers whether an operation occurs. It cannot encode `A -> B -> A`. A scheduler that runs all A operations and then all B operations is wrong for that sequence. Timelines `A -> B` and `B -> A` also cannot share one global type order.

The correctness baseline uses ordered **occurrences** with an internal stage/position identity. At each stage, compatible operations may batch across independent entity rows. Reverse movement traverses occurrences backward. Blend resolution produces one typed frame for its authored track occurrence, not two unrelated calls.

The selected representation is an immutable shared ordered occurrence slice with a small per-entity selection. The array position supplies the internal stage ordinal. At stage s, each typed job processes compatible rows at that stage, then a dependency barrier precedes s+1. The proof measured 8 B occurrences, 8 B frame slices and 12 B selections, and exposed O(E × K × S) visits and up to K × S typed passes for E entities, K operation kinds and S stages. These are a correctness baseline and a measured layout, not an optimality claim.

Production lowers sparse region boundaries rather than copying the proof's duration-sized table. Do not allocate a frame queue of entities × active clips or a 256-entry buffer per entity. Reduce repeated scans only after preserving the independent oracle's exact traces. SIMD is limited to operations with compatible layouts, independent lanes and unchanged evaluation semantics; arbitrary callbacks are not automatically vectorized.

Cross-entity or global effects are a separate dependency domain. Passing `in`/`ref` alone does not prove independence if a body reads mutable statics, follows shared pointers, or writes another entity. The first parallel executor supports row-local borrowed components and immutable shared data. Broader effects require an explicit deterministic ordered phase or a proven reduction; arbitrary C# purity is not asserted by a marker interface.

The 256-track limit bounds authored track indices to 0..255; a count of 256 needs more than eight bits. Multiple hooks can create more operation occurrences than tracks. Do not use a byte for occurrence count without a separate validated bound.

## Data, schemas, and lifetime

- .NET queries borrow component columns or operate on validated native chunks. They validate equal entity membership, length, and prohibited overlap at setup. If callers can change row asset routes, Tick rechecks route/schema compatibility before applying any effect; constructor validation cannot cover later mutation. Typed schema-specific state may remove that repeated check only if it actually makes the invalid assignment unrepresentable. Query creation does not allocate a hidden frame queue. A default query is empty.
- Unity owns ECS component storage and job dependencies. TL supplies immutable asset data, per-entity playback/selection data, and generated operation filters. No span, managed byref, or ref struct is stored in a scheduled job.
- Borrowed frames live only inside Execute. Blend scratch is bounded by the current operation/region, with a demonstrated stack limit or caller/host-owned chunk scratch. Large payloads must not produce unbounded stack allocation.
- Required schema membership is checked during import/entity creation/query setup. If external structural changes remove required components, a generated schema gate must disable the whole affected timeline or report the mutation at that boundary before partial work can run. Merely letting each IJobEntity query filter independently is insufficient.
- Stable asset identity is separate from a uint catalog-local route. Save files, networking, and mod references do not store registration-order IDs. Catalog identity accompanies routing across package/catalog boundaries. Typed facades may eliminate routing; dynamic assets retain an explicit generated routing cost.
- Route zero is empty; values 1..65,536 cover the requested nonempty assets. Arbitrary uint values are not legal asset constructors. A ushort cannot encode that domain plus empty. Stable imported asset IDs are resolved before executing the closed catalog.
- Definition lifetime must outlive every scheduled reader. Publication exposes a complete immutable definition. Reclaiming/swapping it requires host fences; no per-frame lock is added.

## Compiler and portability

```text
C# declarations or designer asset data
    -> frontend + typed language binding
    -> validated neutral ordered schedule and constant data
    -> C# kernel/query backend
    -> .NET compiler

same neutral schedule + C# binding
    -> Unity-compatible materialized source + immutable data
    -> Unity compilation, Entities generation, Burst
```

`Tl.Compiler` owns schedule semantics and validation, never Roslyn symbols or C# expression strings. `Tl.Gen.CSharp` owns symbol analysis, generated signatures, constant encoding, diagnostics, and .NET/Unity C# source emission. Unity storage adapters may use BlobAssetReference, but the semantic schedule is the same and .NET does not depend on Unity.

The current analyzer target is netstandard2.0 with Roslyn 4.3.1, while the neutral compiler is currently net10-only. Sharing production lowering therefore requires a host-compatible neutral assembly target and analyzer dependency packaging/load tests. Referencing the CLI's net10 assembly from the analyzer is not a valid shortcut. Keep compiler objects out of runtime/player artifacts.

Do not implement an entire portable codec during this timebox. New C work, backend repository extraction, GUI editor construction, and arbitrary C# translation remain deferred. Define the neutral seam needed by the slice and preserve the reviewed C ABI until a separate migration is approved.

Ordinary Roslyn RegisterSourceOutput cannot feed another generator in the same compilation. Current Roslyn also documents experimental RegisterPreCompilationSourceOutput for non-compilation inputs; it cannot inspect SyntaxProvider/CompilationProvider and is not a qualified dependency of TL's older analyzer/Unity host. See the [official cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#pre-compilation-source-generation).

The selected Unity path is deterministic materialization before script compilation: emit physical C# in the qualified subset, then let Entities discover IJobEntity declarations in the following compilation. Write atomically; preserve content/timestamps on a cache hit; remove stale owned outputs; prevent refresh loops. Existing .NET incremental generation remains automatic in a supporting IDE. A generated IJobChunk selector solves schema discovery, not the separate IJobEntity generator handoff.

Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) still lists .NET 10 as active LTS at this review. Keep SDK 10.0.401 pinned for the recorded checks; a toolchain upgrade requires its own qualification.

## Implementation atoms after design review

These define dependency and acceptance, not a second mutable task board. [Runtime #31](https://github.com/IAFahim/tl/issues/31), [neutral schedule #32](https://github.com/IAFahim/tl/issues/32), [generated .NET queries #33](https://github.com/IAFahim/tl/issues/33), [generated Unity jobs #34](https://github.com/IAFahim/tl/issues/34), and [release qualification #35](https://github.com/IAFahim/tl/issues/35) own production execution. They belong to [milestone alpha.3](https://github.com/IAFahim/tl/milestone/2). Issue #27 remains the reviewed design and experiment record. Each implementation issue records its owner, exact branch, published head, and next atom.

| Atom | Owned production boundary | Dependency | Acceptance |
| --- | --- | --- | --- |
| A: total state/selection (#31) | `src/Tl.Core/Playback.cs`, `Compiled.cs`; core receipts | frozen movement and frame contract | default/empty/zero/mixed completion/finite extremes/loops; selection has no consumer calls |
| B: ordered schedule (#32) | minimal `src/Tl.Compiler` records/validator; compiler receipts | ordering prototype | A-B-A and opposing orders, gaps, overlap order, reverse, 256 tracks; deterministic immutable plan |
| C: real generated bridge (#33) | C# reader/model/emitter and generator tests | A/B | authored declarations compile into selector, typed Execute bridge, and query; no handwritten fixture dispatch hidden as generator output |
| D: .NET column executor (#33) | generated query emitter, mixed sample and runtime receipts | C | 10,000 mixed rows; same traces as independent per-entity oracle; borrowed inputs, live ref outputs, 0 B warmed |
| E: Unity executor (#34) | Unity generator/materializer, runtime adapter, ECS sample/tests | A/B/C | real IJobEntity filter and dependency behavior; complete-schema gate; actual Burst execution matches oracle |
| F: candidate qualification (#35) | API approvals, README/migration, package versions, release evidence | A–E green | exact-commit build/tests/AOT/package/Unity/budget/inspection/benchmark checks; independent review |

A/B can proceed independently after contract freeze. C consumes their reviewed contracts. E can prepare host tests while C is implemented. One agent owns the C# emitter at a time; do not assign colliding broad refactors. The reference is an oracle/example; migrating its hard-coded asset enum into production is forbidden.

The first production checkpoint is deliberately finite, nonoverlapping and hook-free: one asset, one schema, one operation, actual generated selection/execution/commit, plus default/empty/zero/gap/both directions/clamp/mixed-completion/0-B receipts. Unsupported definitions diagnose during generation at that checkpoint. That limited checkpoint does not close #33 or qualify alpha.3; loops, blends, hooks, heterogeneous schemas and Unity remain mandatory before release.

## Performance experiment policy

First compare selector-only, direct per-entity oracle, and full query selection+execution+commit on the same data. Then compare the previous generated API when semantics match. Report selection, dispatch, callback, scheduler, and complete operation cost separately.

The below-3-ns goal applies to a declared hot trivial workload on the reference CPU. Report scalar latency and per-entity batched throughput separately. A Unity job schedule time is not a per-entity processing time. A SIMD arithmetic loop that excludes gathers, masks, selection, and commit is not the full result.

Measure sequential and mixed assets, alternating direction, gaps, blending, A-B-A, and 1/16/256 tracks. Working sets include 1, 32, 10,000, and a cache-exceeding row count. Use exact receipts before timing, retained BenchmarkDotNet JSON, warmed allocation measurement, and assembly; use perf counters if access works. Serialize timed runs and record CPU affinity/environment. Short runs are exploratory and must not satisfy the established three-process release evidence gate.

The first optimization order is deletion of redundant work: hoist immutable facts, genuine scalar single-step entry, ordered stage reuse, compact shared payloads/schedules, direct typed calls, then SIMD only for eligible independent operation kernels. Do not apply every intrinsic or inlining hint blindly. No runtime or build-time autotuning benchmarks are hidden inside source generation.

## Source and memory budget

Production source plus relative UTF-8 paths is at most **250,000 B**. The base is **207,337 B**, leaving **42,663 B** before deletions. Run `python3 benchmarks/source_budget.py` after each production atom. Documentation and experiments are separate and must not be included in runtime packages.

Report source bytes, generated C#, IL/native text, unique payload bytes, schedule bytes, per-entity state, scratch, managed allocation, and retained native allocation separately. Deduplication uses exact semantic/bit equality, not hash identity alone. Track stable ordering, signed zero, NaN payloads, alignment and endianness where they matter.

A position with `D + 1` finite boundary values requires at least `ceil(log2(D + 1))` bits. Choosing among `A` assets plus empty needs `ceil(log2(A + 1))` bits. These are representation lower bounds, not a proof that an entire scheduler can reach them without padding, indexes, or lookup cost. For arbitrary effectful replay, at least each observable effect must execute; batching cannot erase that work.

## Release gate

Run the existing release gate unchanged unless a reviewed breaking migration explicitly replaces an obsolete receipt:

```sh
python3 benchmarks/source_budget.py
python3 -m unittest discover -s benchmarks -p test_collect.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --capacity
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --module-capacity
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

Also execute the published AOT binary, new generated batch/oracle/allocation tests, real Unity EditMode/PlayMode/Burst receipts, package-only JIT/AOT consumers, generator exclusion from player/runtime output, deterministic generation/cache invalidation tests, and JetBrains InspectCode. A compile-only Unity probe is not a player qualification. Report exact supported Editor/Entities/Burst versions; no untested stable Unity or IL2CPP claim.

The SemanticModel/syntax-tree ownership issue found during previous inspection requires a regression receipt if it remains present. Do not hide an analyzer exception by suppressing its warning. Coverage is measured and gaps named; neither 100% correctness nor 100% test coverage is assumed.

The candidate's public API approval files, migration examples, README, package versions, generated reports, tag, and checksums must agree. Another agent reviews the exact integration commit. The manager merges after checks and review, then creates an immutable prerelease tag only on that validated commit. NuGet publication and license choice retain their explicit owner-decision gates.

## Recovery and completion

```sh
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
gh issue view 27 --comments
gh project item-list 6 --owner IAFahim --limit 1000
gh pr list --state open
git ls-remote --heads origin 'refs/heads/workstream-claims/*' 'refs/heads/issue-transactions/*'
```

Read the latest issue checkpoint and claim before editing. Use `eng/agent-work`; never push a branch with another active owner. Each atom is committed, pushed, and reported before the next. Source, test commands, result, remaining defects, and next atom live on GitHub. Only local installed tool paths and ephemeral build products remain machine-specific.

The final report must say which outcome occurred: design validated, production vertical slice complete, or alpha.3 released. It includes exact commits/PRs, measured performance and allocation evidence, failures and deferred scope. A completed experiment is productive evidence, but it is not shipped functionality.
