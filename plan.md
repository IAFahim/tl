# v1.0.0-alpha.3: total timeline jobs

Design owner: [issue #27](https://github.com/IAFahim/tl/issues/27). Live work: [Project 6](https://github.com/users/IAFahim/projects/6/views/4). Base: `a267a3b1ab59d37b78814449db51f7d5a8656ec1`. Package/tag target: `1.0.0-alpha.3` / `v1.0.0-alpha.3`.

This is the breaking design and delivery contract. It does not claim that the existing generator implements it. The alpha.2 package version remains until production migration and release validation succeed. The previous plan remains available in Git at the base commit.

## What changes

A timeline selects which operations must execute. It does not receive gameplay components while selecting. Generated typed jobs consume that selection over compatible entity rows. The .NET and Unity adapters call the same authored `Execute(in Frame<Track, Clip>, in input, ref output)` operation.

The compiler knows the supported operation signatures. Designers choose immutable track values, clip values, windows, and ordering without editing those operations. Adding a new operation type requires compilation; editing data of an existing supported type does not inherently require translating new gameplay code.

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

Do not negate `int.MinValue` in `int`, iterate billions of empty steps, or schedule billions of empty Unity job chains. Finite work bounds come from the longest remaining live timeline. Looping replay with real effects is inherently proportional to the requested work; only a separately proven algebraic operation may collapse it.

Clamping loses overshoot information. Therefore `Tick(+n); Tick(-n)` is not universally identity. Exact restoration additionally requires the same executed frames, reverse operation order, reversible consumer operations, and appropriate event history. Floating-point subtraction is not a general inverse of floating-point addition.

## Public surface and shared consumer code

[The complete reference](experiments/Alpha3/Reference/README.md) includes all frame/state types, shared authored jobs, .NET query execution, Unity components, IJobEntity jobs, ISystem scheduling, and entity setup. The public shape under review is:

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

var query = new CombatQuery(timelines, resistance, health, poses);
query.Tick(gameTick: 200000, delta: 1);
query.Tick(gameTick: 200001, delta: -1);
```

`ITimelineJob<TTrack,TClip>` identifies a pairing; ordinary C# parameters define any supported finite set of inputs and outputs. It does not attempt variadic generic `in`/`out` syntax. `ref` updates the caller's component; `in` is a borrowed read-only alias, not a copy of gameplay state. A job requiring evolving state uses `ref`. An `out` operation, if retained in the final signature grammar, must assign its result on every executed occurrence; skipped occurrences leave existing caller storage unchanged.

The generated query name comes from its compiled asset/schema set. That set must be defined in import/build metadata; discovering some random runtime asset ID cannot reveal arbitrary C# component types. A row may hold any asset compatible with the query's generated operation/component contract. A complete example must include how that set is declared before claiming the API has zero hidden boilerplate.

Unity's thin IJobEntity wrapper calls the same shared Execute bridge. Selection enables the operation marker for the current stage; disabled markers exclude that operation job. The selector must still visit rows with disabled markers, and completion must only advance rows whose entire selected schedule ran. The wrapper does not rename the domain operation to a different Try method.

`Frame` contains borrowed track and resolved clip values, local/game tick, authored track index, and flags. Direction belongs in flags with a computed convenience property. No ordinal or active-count field is added. Full clip/timeline boundary behavior and any retained cycle information must be specified and tested before replacing the existing frame ABI; the smaller reference must not silently erase them.

## Ordered batching, not type sorting

A type mask answers whether an operation occurs. It cannot encode `A -> B -> A`. A scheduler that runs all A operations and then all B operations is wrong for that sequence. Timelines `A -> B` and `B -> A` also cannot share one global type order.

The correctness baseline uses ordered **occurrences** with an internal stage/position identity. At each stage, compatible operations may batch across independent entity rows. Reverse movement traverses occurrences backward. Blend resolution produces one typed frame for its authored track occurrence, not two unrelated calls.

The ordering workstream must choose and prove the smallest representation. It must measure both retained bytes and traversal complexity. Avoid a frame queue of `entities × active clips` and avoid allocating a 256-entry selection buffer per entity just because 256 tracks are supported. Immutable shared schedules plus small per-entity positions are the default candidate.

Cross-entity or global effects are a separate dependency domain. Passing `in`/`ref` alone does not prove independence if a body reads mutable statics, follows shared pointers, or writes another entity. The first parallel executor supports row-local borrowed components and immutable shared data. Broader effects require an explicit deterministic ordered phase or a proven reduction; arbitrary C# purity is not asserted by a marker interface.

The 256-track limit bounds authored track indices to 0..255; a count of 256 needs more than eight bits. Multiple hooks can create more operation occurrences than tracks. Do not use a byte for occurrence count without a separate validated bound.

## Data, schemas, and lifetime

- .NET queries borrow component columns or operate on validated native chunks. They validate equal entity membership, length, and prohibited overlap once at setup. Query creation does not allocate a hidden frame queue. A default query is empty.
- Unity owns ECS component storage and job dependencies. TL supplies immutable asset data, per-entity playback/selection data, and generated operation filters. No span, managed byref, or ref struct is stored in a scheduled job.
- Borrowed frames live only inside Execute. Blend scratch is bounded by the current operation/region, with a demonstrated stack limit or caller/host-owned chunk scratch. Large payloads must not produce unbounded stack allocation.
- Required schema membership is checked during import/entity creation/query setup. If external structural changes remove required components, a generated schema gate must disable the whole affected timeline or report the mutation at that boundary before partial work can run. Merely letting each IJobEntity query filter independently is insufficient.
- Stable asset identity is separate from a process-local ushort route. Save files, networking, and mod references do not store registration-order IDs. Typed facades may eliminate routing; dynamic assets retain an explicit generated routing cost.
- Default asset needs an explicit empty representation. Supporting all 65,536 nonempty assets plus empty requires an extra state/bit or a wider handle; do not reserve ushort zero and still claim 65,536 live values.
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

Do not implement an entire portable codec during this timebox. New C work, backend repository extraction, GUI editor construction, and arbitrary C# translation remain deferred. Define the neutral seam needed by the slice and preserve the reviewed C ABI until a separate migration is approved.

Roslyn generators do not consume other generators' output in the same pass. A TL-emitted IJobEntity declaration therefore needs deterministic source materialization before Unity's compilation, or TL must emit a complete IJobChunk adapter itself. Pick one qualified path; do not promise IDE generation alone solves the Unity handoff. Existing .NET incremental generation remains automatic while editing. Unity updates use the editor's import/compilation cycle, with unchanged output preserving content and timestamps.

## Implementation atoms after design review

These define dependency and acceptance, not a second mutable task board. Issue #27 records the chosen owners, exact branches, published heads, and transitions.

| Atom | Owned production boundary | Dependency | Acceptance |
| --- | --- | --- | --- |
| A: total state/selection | `src/Tl.Core/Playback.cs`, `Compiled.cs`; core receipts | frozen movement and frame contract | default/empty/zero/mixed completion/finite extremes/loops; selection has no consumer calls |
| B: ordered schedule | minimal `src/Tl.Compiler` records/validator; compiler receipts | ordering prototype | A-B-A and opposing orders, gaps, overlap order, reverse, 256 tracks; deterministic immutable plan |
| C: real generated bridge | C# reader/model/emitter and generator tests | A/B | authored declarations compile into selector, typed Execute bridge, and query; no handwritten fixture dispatch hidden as generator output |
| D: .NET column executor | generated query emitter, mixed sample and runtime receipts | C | 10,000 mixed rows; same traces as independent per-entity oracle; borrowed inputs, live ref outputs, 0 B warmed |
| E: Unity executor | Unity generator/materializer, runtime adapter, ECS sample/tests | A/B/C | real IJobEntity filter and dependency behavior; complete-schema gate; actual Burst execution matches oracle |
| F: candidate qualification | API approvals, README/migration, package versions, release evidence | A–E green | exact-commit build/tests/AOT/package/Unity/budget/inspection/benchmark checks; independent review |

A/B can proceed independently after contract freeze. C consumes their reviewed contracts. E can prepare host tests while C is implemented. One agent owns the C# emitter at a time; do not assign colliding broad refactors. The reference is an oracle/example; migrating its hard-coded asset enum into production is forbidden.

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
