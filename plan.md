# v1.0.0-alpha.2 implementation plan

Date: 2026-09-10
Base: `5c05aa5`, released as v1.0.0-alpha.1.
Status: v1.0.0-alpha.2 candidate in progress. The generated C# scalar target is met locally; C totality, complete release validation, Unity qualification, and publication remain open.
Working branch: `codex/tl-v1-alpha-plan-20260910`.
Target package version and immutable tag: `1.0.0-alpha.2` and `v1.0.0-alpha.2`.

This file is the durable execution plan and design record. The [release issue](https://github.com/IAFahim/tl/issues/11) owns completion across machines, the [API contract](docs/v1.0-alpha-api.md) defines the surface, the [checklist](docs/v1.0-alpha-checklist.md) tracks proof, and the [alpha.1 record](docs/verification/v1.0-alpha.1/README.md) preserves the released baseline.

| Workstream | GitHub issue |
| --- | --- |
| C# batch alias safety | [#2](https://github.com/IAFahim/tl/issues/2) |
| C backend totality and 16-bit widths | [#3](https://github.com/IAFahim/tl/issues/3) |
| IDE incremental generation | [#4](https://github.com/IAFahim/tl/issues/4) |
| Canonical language-neutral plan | [#5](https://github.com/IAFahim/tl/issues/5) |
| Correctness and coverage matrix | [#6](https://github.com/IAFahim/tl/issues/6) |
| Unity ECS and Burst package | [#7](https://github.com/IAFahim/tl/issues/7) |
| One-install packages and artifacts | [#8](https://github.com/IAFahim/tl/issues/8) |
| Multi-PC coordination | [#9](https://github.com/IAFahim/tl/issues/9) |
| Physical-floor performance matrix | [#10](https://github.com/IAFahim/tl/issues/10) |

## Objective and release gates

Ship a breaking alpha that turns a partial timeline declaration into an automatically generated, heterogeneous timeline. The caller uses one runtime ID API, separate borrowed input/output contexts, and no manual compilation or binding. Runtime state remains unmanaged. Library source plus paths stays below 200,000 bytes.

The performance objective is **less than 3.000 ns per scalar tick through the public runtime-ID entry point**, including its required validation, playback transition, clip resolution, consumer effects, and observable outputs. A private direct kernel, empty callback, constant-only calculation, or batch average cannot satisfy that objective.

The release has three independent gates:

1. Correctness: the agreed observable behavior, invalid-input behavior, lifetime safety, deterministic generation, and NativeAOT receipts pass.
2. Size: all library source and source-path bytes fit the existing strict 200,000-byte budget; generated application code, native code, and retained data are separately reported.
3. Performance: the frozen public-path target workloads below pass the sub-3 ns gate on the declared reference machine. Passing a subset is reported as a subset. Failure leaves the performance goal open.

There is no guarantee that arbitrary user callbacks, cold memory, thousands of active works, or arbitrary random seeks fit 3 ns. This does not waive the named target: if it misses, record the miss and the measured limiting operations. Do not quietly move validation outside the measurement or rename throughput as scalar latency.

## Verified starting point

| Receipt | v0.6 evidence |
| --- | --- |
| Full compiled scalar consumer, direct generated call | 4.489 ns/tick |
| Full interpreter scalar consumer | 15.340 ns/tick |
| Simple compiled scalar sum, direct generated call | 2.473 ns/tick |
| Simple interpreter scalar sum | 11.157 ns/tick |
| Full compiled batch of eight | 4.104 ns/tick |
| Restricted research sequential batch sum | 1.195 ns/tick; different contract |
| Library source budget, rechecked for this plan | 29 files, 177,443 content bytes + 856 path bytes = 178,299 bytes |
| Historical tests | 43 core and 60 generator tests; not rerun during this documentation task |
| Historical native verification | Runtime and named compiled NativeAOT parity passed |

Sources: [release verification](docs/verification/v0.6/README.md), [raw production results](benchmarks/Dispatch/results/v06-production-fusion-20260909/Tl.CompiledBench.CompiledVsInterpreter-report-full.json).

The historical v0.6 baseline used SDK 10.0.400 and runtime 10.0.11. Microsoft released .NET 10.0.12 on September 8, 2026, so the final alpha gate installed SDK 10.0.401 in an isolated toolchain, pinned it in `global.json`, and reran the candidate build, tests, NativeAOT validation, disassembly, and benchmark set on runtime 10.0.12. Historical 10.0.11 numbers remain labeled as historical and are not used as a causal patch-to-patch comparison.

The local CPU is an i9-14900K. The inspected topology maps logical CPUs 4 and 5 to physical core 2. CPU 4 is an initial measurement candidate, subject to affinity permissions and fresh topology inspection.

## Accepted design

| Area | Target |
| --- | --- |
| Declaration | `readonly partial struct Attack : ITimeline` |
| Authoring method | `static void Define(scoped Builder builder)` |
| Track attachment | `builder.Track(new AnimationTrack())` |
| Primary behavior | The track type declares its clip contract and primary directional methods |
| Track contract | `ITrack<TClip>` marker/trait plus generator-validated methods; no variadic interface family |
| Blending | `IBlend<TClip>`, supplied by the track |
| Callback metadata | `Frame<TTrack,TClip>`: Track, Clip, Tick, State, TrackIndex |
| Removed metadata | Ordinal and active count are absent from Frame |
| Extensibility | Explicit Before/After timeline hooks, using `IHook` |
| Composition | A wrapper can include a base timeline and add hooks without editing it |
| External data | Generated `Attack.Input` and `Attack.Output`; no handwritten attributes or context declarations |
| Arity | Any supported finite list of typed in/ref/out operation parameters |
| Caller | `Timeline.TryForward(id, in playback, tick, in input, ref output, out nextPlayback)` |
| Indexed facade | `Timeline.All[id].TryForward(...)`; no heap object per lookup |
| Compilation | Automatic during application build, with cached unchanged output |
| Legacy terminal methods | No public Compile, InMemory, or Bind in normal named-timeline use |
| Other languages | `Tl.Compiler` supplies the first language-neutral plan; `Tl.Gen.C` supplies the first C11 backend; canonical serialization and full semantic convergence remain open |

The fixed public input/output pair does not limit the number of components inside the generated containers. Input contains borrowed read-only references. Output contains borrowed writable references, including read/write state. `out` on an individual callback is different from `ref output` on the public call.

The old standalone-operation `.Then<T>()` sketch is superseded. Merely implementing an interface does not register a hook. There is no automatic enumeration of all matching hook types.

## Smallest semantic rules

1. A definition is an immutable ordered set of typed tracks, half-open clip windows, and explicitly attached hooks.
2. A track instance has one closed track/clip contract. Different tracks in the same timeline may have different contracts.
3. Zero active clips produce no track call. One active clip resolves directly. Two compatible active clips resolve once through Blend. More than two overlapping clips are rejected in this alpha.
4. Forward and backward are separate authored operations. Neither implies a universal inverse for arbitrary output mutation.
5. One resolved active track produces one Frame. Consumers run in authored track order. Blending never duplicates a callback.
6. Before hooks run before track callbacks; After hooks run after their successful completion. Each registration runs once per accepted tick, including gap ticks. Empty tick batches run no hooks.
7. Read/write capabilities come from declared signatures and explicit composition, never from variable names at a call site or runtime object inspection.
8. A pure transition computes the next playback or a typed failure. Validation precedes consumer effects.
9. Runtime-ID mismatch, missing context data, unsupported bindings, stopped/unstarted playback, and capacity failures return false before callbacks. Outputs are unchanged and returned playback equals incoming playback on these engine-detected failures.
10. User-code exceptions propagate. Previous writes and external effects remain visible; Try does not mean rollback or catch-everything.
11. A batch equals ordered scalar execution on the same borrowed data, except that engine validation for the entire batch occurs first. Consumer exceptions preserve the executed prefix.
12. Input references are read-only access, not snapshots. Aliasing input and output observes writes. Sequential feedback is explicit through ref state or caller updates between scalar calls.
13. Generated and interpreted execution expose the same effects and failures for the same definition and inputs.
14. The compiler may share data or code; it may not eliminate, duplicate, or reorder observable callback effects.

Preserve existing movement rules unless a separately recorded breaking semantic decision is made: raw playback tick versus effective frame tick, one-tick Exit priority, gap handling, loop normalization, saturating backward cycle subtraction, and forward cycle overflow. Freeze those rules in oracle tests before changing the engine.

## Decisions to settle in the first vertical slice

These are unresolved engineering details with proposed defaults, not silently approved requirements. P1 must settle and document them before broad migration.

| ID | Question | Proposed default and required evidence |
| --- | --- | --- |
| D01 | Does 256 cap kinds, track instances, or clips? | Treat 256 as closed track/clip kinds per timeline. Keep the separate existing track/clip instance bounds initially. Narrow TrackIndex only after choosing its independent bound; do not truncate instances to a type ID. |
| D02 | How are repeated component types mapped? | Distinct root parameters remain distinct slots. Match inherited contract identities first; permit type-based adaptation only when both sides have a unique candidate. Diagnose ambiguous known graphs; reject ambiguous runtime compatibility. Prove two actors with the same Health type cannot silently share the wrong slot. |
| D03 | How does playback prove its owning timeline? | Carry an owner ID. Retaining uint tick, ushort cycles, a 16-bit owner and four lifecycle bits needs at least 68 bits. Expect a naturally aligned 12-byte value; compare with a packed representation before selecting. Do not keep an 8-byte claim by silently narrowing cycles or omitting ownership. |
| D04 | How does the core hub reach application-generated types? | A fixed core generic protocol implemented by generated context bridges, with inferred generic arguments. Prototype it under JIT and real NativeAOT. Application generation cannot add members to a static class in another assembly. |
| D05 | What is runtime-data scope for this alpha? | Code-authored partial definitions and build-visible assets are required. Late runtime topology loading remains a distinct loading capability with identical playback, not a claim of compile-time precomputation. Do not introduce another public compilation-mode switch. |
| D06 | How far does Include compose? | Initially wrap one base definition and add hooks. Inherit its duration/looping and preserve nesting order. Reject cycles and conflicting definitions. Multiple independently looping children need a separate composition law, not concatenated text. |
| D07 | Which output slots are readable by hooks? | After may read an unambiguous produced output or declared external input; Before cannot depend on output that has not yet been produced. On gap ticks, output references retain caller-initialized values. |
| D08 | How are plans and bindings shared? | Share by proven layout/schema and operation sequence, not by timeline ID. The bridge count must follow actually used contract combinations, not the Cartesian product of all timelines and contexts. |

The ABI prototype must preserve managed byrefs to caller storage even when the component values are unmanaged: an unmanaged struct may still reside inside a movable managed array or object. Never erase a live GC-tracked reference into an unpinned native pointer to save a dispatch instruction.

## Compiler and runtime structure

Use the following separation without duplicating whole compiler stacks:

```text
C# source / build-visible assets
    -> language frontend
    -> typed definition and slot graph
    -> validation and normalization
    -> immutable timeline plan
    -> backend selection
    -> C# source emission
    -> normal C# compilation
    -> JIT or NativeAOT
```

The neutral model must own timeline identity, clip windows, closed type contracts, typed constant values, operation references, access modes, slot identities, hooks, and composition. It must not contain Roslyn symbols, source using directives, C# constructor expressions, or callback-body strings. `Tl.Compiler` now establishes the package boundary and first fixed-width plan records. `Tl.Gen.C` proves that a non-C# backend can consume that boundary. The current C# frontend still has richer internal semantics than the neutral slice, so canonical serialization and one shared semantic plan remain release-program work rather than a completed portability claim. Frontend-owned symbol handles and source locations map diagnostics back to their language. Operation bodies remain opaque code references unless represented by a supported neutral operation algebra; arbitrary C# bodies do not become portable by moving their strings.

The portable compiler split is four orthogonal layers:

1. `Tl.Compiler` owns versioned numeric definition, type, slot, operation and constant IDs plus canonical typed constant bytes, windows, regions, work slots, movement facts and routes.
2. A frontend maps one language into that plan and retains a private binding sidecar for source locations, native type spellings and operation symbols. `Tl.Gen.CSharp` remains the first frontend.
3. A backend consumes the plan plus its language binding and emits local kernels. Planned backends are C#/.NET, Unity Burst, C++, Rust and a stable C ABI; each may choose a different code/data split while preserving the same receipts.
4. A canonical manifest serializer proves frontend/backend separation and supplies deterministic cache identity. Process-local runtime IDs and module registration order never become interchange identities.

Payload constructors, blend bodies and hooks are executable language semantics. Cross-language assets therefore use canonical structural constants and stable operation IDs, with an implementation supplied by each backend. A future portable operation DSL may cover a pure subset; it cannot silently translate arbitrary C# methods. The C ABI must specify version, layout, alignment, endianness, ownership and error values, and cannot expose C# ref structs or generic static-interface calls.

Start by replacing the existing C#-coupled Model/Definition.cs and declaration scanner boundary. Keep the first backend C#, but prove separation with a deterministic canonical plan serializer and a C#-free model test. A second production language backend is outside alpha scope.

Generated inputs/outputs are stack-only borrowed containers. Their concrete constructors and accessors enforce types, roles, and ref modes. A small generated bridge participates in the runtime hub protocol; users do not implement it. The protocol works without reflection or user registration under JIT and NativeAOT when definitions are visible in the same compilation. Cross-assembly schema manifests and inter-generator ordering remain explicit open work.

Known definitions select among:

- Direct blocks for small timelines: fully folded metadata, direction, loop trait, typed payload accesses and direct calls.
- Compact per-track intervals for larger timelines: merge equal adjacent segments and share bounded helpers; omit the active-count prepass entirely.
- Compact indexed or table-driven representation for working sets where code expansion loses.

Do not duplicate one 30–40 KB source/native kernel for each of 65,536 timeline IDs. Separate reusable execution shape from immutable payload data. Full per-ID specialization is a measured choice, not an unconditional rule.

Registration publishes complete immutable descriptors, not partially installed bindings. Sparse paged registry storage keeps unused ID ranges absent. All 65,536 ushort values are usable; validity is separate from the ID. IDs never reuse within a registry lifetime, preserving stale-handle rejection. Reuse would require a generation token and a new size/lifetime proof.

Runtime-created/native-loaded data has explicit ownership and destruction. Compiled static definitions live for the registry lifetime. Construction, publication, retirement and any native deallocation must have a documented synchronization rule. Immutable concurrent playback is separate from concurrent destruction; do not promise both without proving safe reclamation.

## Performance contract

### Alpha target workloads

The alpha gate measures a predictable runtime ID loaded from benchmark state, a dependent caller-owned playback, and complete observable receipts. The ID is not a compile-time literal, but the hot stream uses one owner so branch prediction and tiered PGO can specialize the common exact-schema route.

| Case | Timed path and observable work | Target |
| --- | --- | --- |
| P-Sum | Public scalar and batch-8 calls; region selection, one blend where active, playback state, ordered floating-point sum, success count | <3.000 ns/tick for sequential tiered JIT |
| P-Combat | Heterogeneous Animation/Pose and Damage/Health kinds; four borrowed inputs, three borrowed outputs, hooks, gaps, blending, lifecycle and trace accounting | <3.000 ns/tick for sequential tiered JIT |

Both cases must pass an independent exact receipt before timing, allocate zero managed bytes, and retain public scalar, indexed, public batch, indexed batch, and handwritten scalar-oracle measurements. Random ticks are a separately reported stress case. NativeAOT correctness is an alpha gate; NativeAOT throughput is not claimed.

The current alpha.2 median across three independent run medians on .NET 10.0.12 is 1.383 ns/tick for Sum scalar and 2.188 ns/tick for Combat scalar. Random scalar results are 4.786 and 7.015 ns/tick. Every arm allocates 0 B. The alias-safe batch kernel must be remeasured before alpha.2 claims batch values; alpha.1 measured 1.384 ns/tick for Sum batch-8 and 2.033 ns/tick for Combat batch-8 under the earlier implementation.

### Maximum matrix

The maximum matrix remains active in [#10](https://github.com/IAFahim/tl/issues/10). It requires streams selected among at least 16 compatible definitions with distinct payloads, per-ID playback/output state, P-Sum, a full consumer, and a mixed consumer separately under tiered JIT and NativeAOT. Fixed-ID results do not prove this stronger target.

| Axis | Planned measurements |
| --- | --- |
| Dispatch | Existing direct v0.6 baseline, direct candidate diagnostic, complete public dynamic-ID candidate, indexed facade, runtime interpreter reference |
| Consumers | Ordered sum; full state receipt; mixed types with live inputs; hook read/write effects |
| Movement | Sequential 1; skipping 7; repeated tick; random; backward; loop boundaries; gaps |
| Calls | Scalar; batch 8; batch 64; batch figures explicitly labeled throughput |
| Shapes | Empty; single track; mixed two/four; 16/64/256 kinds where valid; sparse and overlapping clips; long duration |
| Registry working set | 1, 16, 256, 4,096 and 65,536 IDs; distinguish payload diversity from shared execution shapes |
| Runtime | Tiered JIT with default PGO; no tiering; genuine NativeAOT |
| Memory | Warm managed allocation, retained native bytes, setup allocation, generated C# bytes, IL/native bytes, context/frame/playback sizes |
| Hardware | Reference x64 for gate; additional hardware reports separated by architecture and runtime |

Use BenchmarkDotNet with the existing steady-state job as the baseline configuration: 16 warmups, 12 target iterations, 250 ms requested iterations, MemoryDiagnoser and full JSON. Disassembly runs are separate from timing runs. Add actual native instruction/code-size evidence, and perf counters for cycles/instructions/branches/misses when supported.

Freeze one affinity-controlled reference core and keep its SMT sibling free of deliberate experiment load. Record OS, CPU model/microcode/topology, affinity, runtime/SDK, code hashes, flags, and command. Do not run experiments concurrently just because they have different logical CPU numbers; cache, power and memory bandwidth remain shared.

Use at least three fresh-process paired runs with alternating baseline/candidate order for a future maximum-matrix claim. Every claimed target case must have a median below 3 ns in all three runs. Report the BDN distribution and uncertainty; if uncertainty straddles the threshold, collect more evidence instead of declaring victory from the best sample.

Warm state and preallocation may occur in setup. The public-call entry lookup, compatibility validation and playback transition remain inside each timed call. Consume the boolean result, updated playback and all intended output fields. Prevent overflow/saturation from making the consumer dead. Compare exact per-tick traces against an independent oracle before timing, and verify all run outputs afterward.

AOT timing uses BenchmarkDotNet's NativeAOT toolchain: the managed benchmark host builds and measures a native benchmark executable. Pin the ILCompiler version alongside the SDK/runtime, verify the actual executable and preserve its build log. Publishing the entire BDN host as NativeAOT is not this toolchain. The existing Stopwatch harness may provide clearly labeled supporting measurements with timer/batching limitations; it cannot be silently equated to BDN or used alone to claim isolated sub-3 ns latency.

Hosted CI is a correctness and artifact gate, not the authority for sub-3 ns regressions across unlike machines. Update the collector key to include job/runtime, architecture, parameters, fixture/contract version, and environment identity; the older alert contained duplicate names and near-zero ratios.

## Optimization sequence

| Experiment | Hypothesis | Acceptance evidence |
| --- | --- | --- |
| E0: exact baseline | Same-machine reruns isolate code from SDK/CPU variation | Frozen fixture receipts, complete raw results |
| E1: typed hub bridge | One typed, AOT-rooted context dispatch can preserve references without user Bind | Compiling vertical slice, compacting-GC reference test, AOT execution, code size |
| E2: remove ordinal/count | Compact programs no longer need a global active-count scan | Emitted source and assembly omit the scan; full semantic receipts survive |
| E3: Frame versus flattened private ABI | Readable public Frame can inline while a private adapter avoids materialization where necessary | Call-boundary, spill, copy, and code-size comparison on JIT/AOT |
| E4: scalar/direction/lifecycle folding | Known facts disappear before runtime work | No scalar span construction; separate forward/backward; native disassembly |
| E5: region algorithms | Shape-dependent tree/switch/rank/table selection beats one policy | Same traces; sequential and random results; data/code costs |
| E6: sequential cursor | Common case uses a next-boundary check instead of full lookup | All seeks/wraps fall back correctly; size and crossover measured |
| E7: code/data interning | Sharing equal plans and constants preserves cache locality at many IDs | Equality after hashing, signed-zero/NaN/padding rules, retained/native bytes, stress timings |
| E8: hook boundaries | Empty hook lists emit no hook calls; present hooks pay only declared work | Per-tick effect order, gap behavior, no-hook versus real-hook overhead |
| E9: bounded batching | Hoist per-call invariants without changing the public scalar target | Prefix effects, aliasing, batch validation, native code budget |
| E10: final low-level tuning | Specific copies/branches or register spills explain remaining cost | One-variable A/B, annotated assembly outside source, reproducible results |

No blind AggressiveInlining/AggressiveOptimization sweep. No unsafe pointer shortcut across live managed byrefs. No SIMD or reassociation across ordered consumer effects or floating-point reductions. No caching of callback results merely because payloads repeat. All rejected experiments leave a short verdict with measurements, so the next session does not repeat them without new evidence.

## Memory budget and mathematical limits

Keep the exact existing source metric from benchmarks/source_budget.py: content bytes plus UTF-8 relative-path bytes and one separator byte per path, across tracked and non-ignored untracked files under src. The existing gate accepts totals up to 200,000 bytes; plan to stay below that cap. The current headroom is 21,701 bytes. Remove obsolete API, shims, duplicate table emitters and unused dependencies while adding the new model. Do not move production code out of src or minify it to evade the measure.

Provisional allocation of that budget:

| Area | Planning ceiling |
| --- | ---: |
| Runtime/public contracts/native ownership | 45,000 bytes |
| Neutral model, validation and lowering | 35,000 bytes |
| C# frontend and diagnostics | 50,000 bytes |
| C# backend and generated adapter templates | 50,000 bytes |
| Build integration/project/path overhead | 10,000 bytes |
| Contingency | 9,999 bytes |

These are planning ceilings, not current measured component sizes.

At maximum capacities, independently selected timeline IDs require 16 bits and a local kind among 256 requires 8 bits. A count spanning 0 through 256 needs 9 bits; that is why deleting per-frame count is preferable to pretending a byte count covers 256. Ordinal/count are not retained per-clip fields today, so their deletion is not automatically an eight-byte-per-clip saving.

For L distinct stored values, fixed-width selection requires at least ceil(log2 L) bits. A caller context referencing N independently located values generally needs information equivalent to N locations unless an explicit shared layout removes it. At 65,536 timelines, total game code/data is a separate budget from the library's 200 KB source cap. Arbitrary unique payloads cannot all be deduplicated into L1.

Measure actual packing and alignment; smaller integer fields can add unpacking instructions and padding can erase nominal savings. Measure hot-set sizes and cache behavior rather than claiming all timelines fit a cache from source size. Do not promise a universal computable smallest equivalent program for arbitrary callbacks.

## Migration and implementation order

| Phase | Deliverable | Required exit proof |
| --- | --- | --- |
| P0 | Baseline manifest, frozen fixtures, source budget and decision log | Paired-measurement harness works on unmodified v0.6 |
| P1 | Minimal two-kind declaration -> generated Input/Output -> public TryForward -> actual native binary | D01–D08 settled or explicitly scoped; correct borrowed references; measured ABI cost |
| P2 | Language-neutral immutable definition/plan and one semantic validator | Pure tests and deterministic serialized plan, no C# types/strings in neutral model |
| P3 | Typed heterogeneous tracks, clipping, Frame, contexts, diagnostics | Complete consumer example compiles; negative examples diagnose at source |
| P4 | Registry ownership, public Try lifecycle, scalar/batch parity | Failure-before-effects, capacities, aliases, GC and native lifetime tests |
| P5 | Before/After hooks and Include wrappers | Hooks compose without editing base; declared order and context extension tested |
| P6 | Automatic build discovery and caching | Clean package consumer requires only package references and ITimeline; rebuild/deletion/reference-change tests |
| P7 | Measured backend selection and sub-3 ns work | All primary target results, distributions, disassembly and code/data receipts retained |
| P8 | Remove superseded surfaces and migrate repo consumers/docs | No live usage of legacy APIs; warnings/errors/public API checks clean |
| P9 | Pack, package-only JIT/AOT consumer, alpha release ceremony | Version/tag/commit/package identity agree and all gates pass |

P1 precedes a broad rewrite because a beautiful public signature is useless if the generated application types cannot reach the runtime hub safely and cheaply. P2 and P3 may then proceed together when their shared model contracts are frozen. Performance investigations should run one at a time; independent read-only reviews can be delegated when requested.

Affected areas:

- src/Tl.Core/Hooks.cs, Declaration.cs, Timeline.cs, Playback.cs, Authoring, Internal/Playback.cs, Binding.cs, Registry.cs.
- src/Tl.Gen/Model, Analysis/DeclarationReader.cs, validation/lowering, CSharp/KernelEmitter.cs, GeneratorCli.cs, build targets and packaging.
- samples/Compiled and Basic; core/generator/native/package consumer tests; Dispatch/CompiledCompare and verification consumers.
- README, API examples, semantics, roadmap, benchmark report/collector, and release workflows.

The public removal list includes generic Timeline<TTrack,TClip> as the normal hub, the four-parameter ITrack result-as-behavior contract, ordinal/count callback parameters, manual Bind, Compile/InMemory terminals, legacy compiled shims and redundant table-shell entry points. Preserve the independent oracle and useful benchmark fixtures while migrating them; deleting a test because it exposes a regression is not migration.

## Release identity and completion

Use NuGet version 1.0.0-alpha.2 consistently in packages and package-consumer tests. Use tag v1.0.0-alpha.2 on the exact validated commit and mark the GitHub release as a prerelease. Never move v1.0.0-alpha.1 and do not publish a stable 1.0.0.

A release checklist must include release build, all tests, semantic/property batteries, reference-machine benchmark proof, source budget, real NativeAOT binaries, fresh package consumer, repeat-build cache receipt, API approval, license/package metadata review without inventing a license, and source/generated/native size tables.

Publishing the alpha requires release-ready implementation and evidence. The current generated C# sequential scalar gates pass. The release remains open until [#11](https://github.com/IAFahim/tl/issues/11) has exact tagged evidence for the C, package, correctness, source-size, performance, and Unity claims included in the release.

## Execution record

Alpha.1 replaced the homogeneous interpreter with automatically generated heterogeneous kernels, borrowed contexts, a compact runtime-ID registry, module/ordinal routing, composition, and deterministic generation. Alpha.2 currently adds a neutral compiler boundary, a C11 backend, generation and memory reports, one-install C# packaging, PMU evidence, and alias-safe batches. Cross-assembly schemas, true Roslyn incremental ordering, cross-definition interning, the Unity Burst backend, the dynamic-ID performance family, and NativeAOT timing remain tracked by linked issues. Every completed scope must exist as a pushed green commit and issue handoff before this record calls it complete.

## Primary references

- [.NET 10 release/download information](https://dotnet.microsoft.com/en-us/download/dotnet/10.0): current release verification and patch selection.
- [.NET 10.0.12 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md): refresh before changing the benchmark toolchain.
- [C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14): language target.
- [NativeAOT limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/): no arbitrary dynamic managed assembly loading.
- [BenchmarkDotNet toolchains](https://benchmarkdotnet.org/articles/configs/toolchains.html): use the NativeAOT toolchain to measure native benchmark executables from a managed host.
- [C# ref struct rules](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct): borrowed containers and allows-ref-struct constraints.
- [Partial types](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods): partial declarations do not extend a type across assemblies.
- [Unity component reference access](https://docs.unity.cn/Packages/com.unity.entities%401.0/manual/systems-systemapi-query.html): ValueRW aliases component storage; this is not a claim of current Tl Burst compatibility.
