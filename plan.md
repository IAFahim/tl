# v1.0.0-alpha.3 architecture and release plan

Design record: [issue #27](https://github.com/IAFahim/tl/issues/27). Release gate: [issue #35](https://github.com/IAFahim/tl/issues/35). Coordination: [Project 6](https://github.com/users/IAFahim/projects/6/views/4). Package/tag: `1.0.0-alpha.3` / `v1.0.0-alpha.3`.

This file is the enduring architecture, evidence, and release-gate plan. GitHub issues and Project fields own mutable status. The production implementation contains the alpha.3 catalog/query architecture; earlier prototypes and preimplementation decisions remain in Git history and `docs/alpha3`.

## Approved next API

The owner approved [data-authored timelines and typed frame queries](docs/data-authored-api.md) on 2026-09-11. [Issue #56](https://github.com/IAFahim/tl/issues/56) owns the design discussion, future implementation checklist and recoverable workstreams. Assets contain track and clip `data`, timing and order, without mandatory names or per-asset job bindings. Known type pairs drive `Timeline.Query` inside ECS loops or jobs, while a coordinator preserves signed selection, ordered consumers and delayed commit. The .NET entry is `Timeline.Rows(...).Read(...).Write(...).Tick(...)` over borrowed columns.

That document is the approved direction for the next breaking implementation. The remainder of this file describes the released alpha.3 architecture and its evidence. Do not advertise the proposed API as available or transfer alpha.3 performance receipts to it. A separate qualified version is required; the published alpha.3 tag stays fixed.

Implementation is paused by the owner while discussion continues. The documentation-only handoff in [issue #57](https://github.com/IAFahim/tl/issues/57) records the proposal on main for the office PC; the next action is discussion, not prototype or production work. Issue #56 contains the latest authorization and handoff.

## Product contract

A timeline is immutable authored data that selects an ordered set of typed operations. Selection reads no gameplay component and performs no gameplay effect. Generated jobs execute that selection over compatible rows of borrowed storage. A catalog closes asset membership and derives schema-specific component columns from operation signatures.

One timeline may contain up to 256 authored tracks with heterogeneous track and clip types. Every authored track binds one `ITimelineJob<TTrack,TClip>`. Different tracks with the same pair may bind different jobs. A job sees only its typed frame and declared unmanaged `in` and `ref` slots.

The public movement operation is `Tick(gameTick, delta = 1)`. Default state is valid empty state; assigning a generated asset makes it ready at local position zero. There is no mandatory `Start`, `Bind`, `Compile`, `InMemory`, process-global ID, registry, or failure boolean. Finite timelines clamp independently. Nonempty loops normalize position and update signed cycle. Every available crossed frame executes.

## Irreducible laws

1. A validated definition owns intervals, typed payload identities, operation identities, and authored order. It owns no entity state.
2. Definition, committed state, and direction determine one pending frame and its ordered occurrence slice. Equal inputs produce equal selection.
3. Each selected occurrence calls its statically known operation with borrowed values from the same row.
4. Forward execution preserves authored occurrence order. Reverse execution uses the exact reversed slice.
5. Commit occurs once after every selected stage succeeds. A user exception propagates, preserves the executed effect prefix, and prevents commit for that step.
6. A multi-frame call is a fold over single select/execute/commit steps. It cannot skip, regroup, or algebraically collapse observable effects.

For `Tick(G,+N)`, operations observe game ticks `G..G+N-1`. For `Tick(G,-N)`, they observe `G-1..G-N`. Game-tick arithmetic wraps explicitly as `uint`. Local position and signed `long` cycle remain independent.

Empty queries, empty assets, gaps, zero-duration definitions, zero delta, and completed finite movement are total no-ops. Invalid authoring, schemas, bounds, includes, slot signatures, routes, or writable aliases diagnose before user effects. User code exceptions are not converted into status values.

## Ordered row execution

Each region carries an immutable occurrence slice. Its array position is internal stage identity. At one simulation step, the generated .NET query:

1. validates mutable routes against the selected schema;
2. selects at most one frame per row;
3. executes each stage across compatible rows through its concrete typed job;
4. commits each successfully selected row.

This preserves A-B-A, opposing asset orders, hooks, gaps, clips, and blends while allowing one typed stage to process many compatible rows. Type grouping alone is invalid because a type set cannot represent repeated or opposing order.

One active clip is borrowed directly. Two active clips resolve once through `IBlend<TClip>`. The factor spans the overlap intersection and is `0.5f` for a one-frame overlap. `FrameFlags` contains direction and independent clip/timeline/completion/loop facts; `Direction` is derived and consumes no stored integer.

## Data and lifetime

.NET schema queries are ref structs over caller-owned state and component spans. Construction checks equal lengths and prohibited writable overlap. Nonzero ticks recheck caller-mutable routes before effects. A query cannot outlive its borrowed spans and retains no data beyond its ref-struct lifetime.

Generated definitions are immutable static data with process lifetime. The neutral plan deduplicates exact type identity plus canonical payload bytes; the C# frontend currently deduplicates identical normalized type and expression bindings. A hash match never establishes equality. The catalog route domain is zero for empty plus 1..65,536 nonempty assets. Stable external asset identity remains separate from this generated local route.

The supported parallel domain is row-local mutable components plus immutable shared data. Mutable statics, shared pointers, another row's storage, and global effects require an explicit ordered phase or proven reduction.

## Compiler boundaries

```text
C# declarations
    -> Tl.Gen.CSharp frontend and typed binding
    -> Tl.Compiler validated neutral ordered plan
    -> generated .NET timelines and catalog queries

same neutral plan and C# binding
    -> deterministic Unity-compatible physical C#
    -> Unity compiler, Entities generation, Burst

neutral plan and C binding
    -> existing C11 ABI v2 backend
```

`Tl.Runtime` owns declarations, state, movement, frame, and flags. `Tl.Compiler` owns language-neutral identities, operation slots, tracks, clips, hooks, regions, occurrences, payload identities, deduplication, and validation. `Tl.Gen.CSharp` owns Roslyn discovery, concrete types and expressions, diagnostics, and .NET/Unity C# emission. Target backends consume the validated plan and binding; they do not rederive semantics.

Normal Roslyn and supporting IDE builds run incremental generation. `TlGenExport` uses the same reader/emitter for deterministic physical output, cache receipts, reports, and Unity materialization. A cache hit preserves contents and timestamps. No build-time benchmark or autotuning is hidden in generation.

Unity source is materialized before script compilation because ordinary Roslyn generator output cannot feed another generator in the same compilation. The Unity runtime UPM package contains no compiler, generator, Roslyn, reflection binding, or runtime compilation. A separate .NET authoring project owns the toolchain.

The C ABI v2 remains supported in its existing scope. Alpha.3 heterogeneous C catalogs, canonical neutral serialization, designer GUI import, cross-generated declarations, and runtime-loaded arbitrary schemas are deferred.

## Performance and size

Correctness receipts precede timing. Full public query operations consume state and output receipts, use matched direct controls, report scalar latency separately from row throughput, and retain raw BenchmarkDotNet, assembly, PMU, size, and allocation evidence.

The reference-host three-process medians are 1.804 ns for one track, 3.754 ns for A-B-A, 21.230 ns for 16 tracks, 34,250.868 ns for 256 tracks, 1.997 ns for a gap, 2.555 ns for a blend, 2.256 ns for three inputs, and 34.650 ns/entity-tick for 256 mixed rows. All allocate 0 B. `<3 ns` applies only to the named one-track, gap, blend, and three-input fixtures. [Issue #10](https://github.com/IAFahim/tl/issues/10) owns the 256-track generated-code and staged-scheduler cliff.

The optimization order is deletion of redundant work, hoisting immutable facts, scalar single-step specialization, compact schedules and payloads, direct typed calls, and then SIMD only where lanes, layouts, and effect ordering permit it. Branch hints, inlining, unsafe code, and intrinsics require measured end-to-end proof.

Production source plus relative UTF-8 paths must remain at or below 300,000 B. Generated C#, static values, neutral data, state, JIT code, NativeAOT code, executable bytes, scratch, and allocation are separate quantities. A position over `0..D` needs at least `ceil(log2(D+1))` bits; an asset choice among `A` assets plus empty needs at least `ceil(log2(A+1))` bits. These bounds do not erase alignment, pending state, stage identity, or observable work.

## Qualified hosts

.NET qualification uses SDK 10.0.401 and runtime 10.0.12 on Linux x64, including JIT, NativeAOT, package-only consumers, full query receipts, zero allocation, FullOpts assembly, and PMU counters.

Unity qualification covers stable Unity 6000.0.83f1, Entities 1.4.3, Burst 1.8.30 and preview Unity 6000.7.0a5, Entities 6.7.0, Collections 6.7.0, Burst 2.0.0. Both lanes pass 4/4 EditMode and 4/4 PlayMode tests. Stable Mono and IL2CPP players execute generated Burst jobs, print `TL_UNITY_PLAYER_OK`, expose expected Burst symbols, pass native leak scans, and exclude toolchain assemblies. The stable 10,000-row fixture measures 18.006 ns/entity-step, 20 jobs/step, and 0 main-thread managed B.

## Release gate

The final candidate must pass from one exact clean commit:

```sh
python3 benchmarks/source_budget.py
python3 -m unittest discover -s benchmarks -p test_collect.py
python3 -m unittest discover -s tests -p test_release_artifacts.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --capacity
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --module-capacity
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet run --project tests/Tl.UnityAnalyzerReceipt -c Release --no-build
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true -p:NuGetAudit=false
eng/release-artifacts --candidate issue-35 /tmp/tl-alpha3-release
```

Execute the NativeAOT binary in default, capacity, and module-capacity modes. Measure strict production-only line and branch coverage. Run JetBrains Inspect Code and classify every material result. Retain Unity stable/preview EditMode/PlayMode, Mono/IL2CPP, Burst, leak, package isolation, and deterministic UPM evidence. Another agent reviews the exact head after all docs and artifacts agree.

The release set contains five nupkgs, three snupkgs, a NativeAOT smoke archive, generation report, deterministic UPM tgz, manifest, and checksums. Each artifact is built twice or otherwise reproducibly verified. Package version, tag, release title, source commit, repository ref, and manifest identity must agree.

The GitHub prerelease is authorized. NuGet publication remains disabled and requires a separate owner decision. License selection remains a separate owner gate. Never infer either from a green GitHub artifact build.

## Recovery

```sh
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
gh issue view 35 --comments
gh project item-list 6 --owner IAFahim --limit 1000
gh pr list --state open
git ls-remote --heads origin 'refs/heads/workstream-claims/*' 'refs/heads/issue-transactions/*'
```

Read `AGENTS.md`, this file, the issue checkpoint, Project fields, and remote claim before editing. Use `eng/agent-work`. Commit, push, and checkpoint every green atom. The final report names the exact reviewed commit, tests, coverage, allocation, performance limitations, source budget, artifact manifest, host qualification, deferred scope, and any unperformed publication or legal gate.
