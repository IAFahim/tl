# v1.0.0-alpha.2 implementation plan

Date: 2026-09-10
Base release: `v1.0.0-alpha.1` at `5c05aa5`
Target package version: `1.0.0-alpha.2`
Target immutable tag: `v1.0.0-alpha.2`
Status: signed-seek C# candidate under review

This file records the accepted architecture, measurable targets, release gates, and recovery path. GitHub Issues and [Project 6](https://github.com/users/IAFahim/projects/6/views/4) own live work. [Issue #11](https://github.com/IAFahim/tl/issues/11) owns release completion. The [API contract](docs/v1.0-alpha-api.md), [migration guide](docs/v1.0-alpha-migration.md), and [release checklist](docs/v1.0-alpha-checklist.md) define the reviewable surface and proof.

## Product

`tl` compiles declarative, heterogeneous timelines into deterministic playback kernels. A partial `ITimeline` declaration is enough to request generation. The generated typed facade is the primary runtime API. A non-generic runtime ID facade exists for systems that select definitions dynamically.

The accepted C# shape is:

```csharp
using Tl;

public readonly record struct Scale(float Value);
public readonly record struct Offset(float Value);

public readonly struct MotionTrack : ITrack<Offset>
{
    public void Blend(in Offset first, in Offset second, float factor, out Offset result)
        => result = new(first.Value + (second.Value - first.Value) * factor);

    public static void Seek(
        in Frame<MotionTrack, Offset> frame,
        in Scale scale,
        ref float position)
        => position += frame.Direction * frame.Clip.Value * scale.Value;
}

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var motion = builder.Track(new MotionTrack());
        builder.Clip(motion, new Offset(1f), 0u, 8u);
    }
}

public static class Simulation
{
    public static bool Tick(uint gameTick, in Scale scale, ref float position)
    {
        var playback = Attack.Start(gameTick);
        var data = new Attack.Data(ref playback, in scale, ref position);
        return Attack.TrySeek(ref data, 1);
    }
}
```

`Start(gameTick)` anchors local position zero to an external game tick. `TrySeek(ref data, delta)` consumes a signed simulation delta. Positive values move forward, negative values move backward, and zero is an identity operation. A successful magnitude greater than one replays every crossed local frame in order. It is not a snapshot jump.

The generator owns `Attack.Data`, `Attack.DynamicData`, typed playback adaptation, track dispatch, validation, and generated diagnostics. Runtime authoring and explicit binding are absent. One signed operation owns movement in both directions.

## Runtime contract

1. `Playback<TTimeline>` belongs to its generated timeline type. The compiler rejects accidental cross-timeline typed calls.
2. `Playback` is the explicit dynamic-routing state. Both playback forms are 16-byte values with a frozen field layout.
3. `Frame<TTrack,TClip>` carries the borrowed track and resolved clip, game tick, local timeline tick, authored track index, signed cycle, and direction in flags. It carries no redundant signed direction integer.
4. Generated data containers borrow caller storage. Read-only slots are `ref readonly`; mutable and produced slots are `ref`. The container is stack-only and cannot retain those references.
5. `ITrack<TClip>`'s static `Seek` receives one typed frame followed by its explicitly declared slot parameters. User code never receives the generated data container. It may declare any finite set of generator-supported read-only and mutable slots.
6. Generated typed calls have no registry lookup, runtime schema binding, boxing, delegate, reflection, managed allocation, or indirect callback in the warm path.
7. Dynamic calls validate the runtime ID and schema before executing. Dynamic routing is an explicit flexibility and latency tradeoff.
8. Empty timelines and finite timelines reject positions outside their legal domain before callbacks. Position arithmetic is checked in `long`; looping timelines derive a signed `long` cycle from the resulting position.
9. Engine-detected failure returns `false` without effects.
10. User-code exceptions propagate. Effects from already executed frames remain visible.
11. A multi-frame seek is ordered scalar execution. If user code throws, the executed prefix remains visible.
12. Input references are live read-only aliases rather than snapshots. Explicit aliasing observes writes in program order.
13. Forward then inverse movement restores state only when the consumer operations themselves form a valid inverse. The engine preserves order and direction; it cannot manufacture reversibility for arbitrary side effects.
14. Generation is deterministic and culture-independent. Unchanged inputs produce byte-identical sources.

## Architecture

```text
C# declarations
    -> Tl.Gen.CSharp frontend
    -> Tl.Compiler language-neutral plan
    -> validation and normalization
    -> C# backend
    -> generated typed and dynamic kernels
    -> C# compiler
    -> JIT or NativeAOT
```

`Tl.Runtime` owns the stable playback ABI, frame contract, declaration syntax, IDs, and compact dynamic registry.

`Tl.Compiler` owns the immutable language-neutral plan, fixed-width records, semantic validation, and versioned extension contract. It contains no Roslyn symbols, C# expressions, source syntax, or language-specific type spellings.

`Tl.Gen.CSharp` owns C# discovery, semantic analysis, diagnostics, generated C# binding, and IDE incremental generation. The analyzer/source-generator package is build-time only and must not enter application runtime or NativeAOT output.

`Tl.Gen.C`, Unity/Burst, C++, Rust, and other backends consume the neutral plan through independently versioned packages. Arbitrary behavior is represented by stable operation IDs whose implementation is supplied by each language binding. Cross-language support never pretends to translate arbitrary C# method bodies.

Compiled definitions are immutable and retained for their declared lifetime. Publication exposes a complete descriptor. Read-only playback may run concurrently after publication. Any future reclamation or ID reuse requires a generation token and safe-reclamation proof.

## Kernel strategy

Generation removes facts that authoring already proves:

- timeline type, loop trait, track and clip types
- region cuts, active track set, blend count, payload index
- clip and timeline boundary flags for each signed movement
- typed slot shape and access modes
- direct track operation targets
- scalar signed-seek direction when the delta is a literal

Small timelines use emitted basic blocks and direct calls. Larger shapes may use compact interval, rank, tree, or table representations when measured code and data cost wins. Sequential playback keeps only state that reduces total work. Random access falls back to a total location strategy.

Single-frame latency and multi-frame throughput use different generated shapes. The public operation remains signed seek. Literal and runtime delta paths may specialize internally while preserving identical ordered effects.

Generated code is selected from deterministic shape rules. The compiler does not run hidden benchmarks during generation because results would depend on editor load, hardware, tiering, and unavailable target architecture. Thresholds require retained A/B evidence and must account for source bytes, native code bytes, static data, and hot-set behavior.

## Performance contract

The primary performance target is less than 3.000 ns per executed frame for the generated typed `SumTimeline` signed-seek matrix on the declared reference machine. It is a narrow, reproducible target rather than a universal callback guarantee.

The fixed matrix contains:

- literal and runtime `+1`
- literal and runtime `-1`
- alternating `+1/-1`
- literal and runtime `+5`
- literal and runtime `-5`
- five repeated scalar `+1` calls
- five repeated scalar `-1` calls
- literal and runtime `+64`
- direct oracle, typed generated facade, and dynamic generated facade

Every benchmark consumes success, playback, and output state; passes exact semantic parity first; reports per executed frame; and allocates zero managed bytes in the warm path.

At implementation commit `12119ca583884390c523049a3309ce5a3d244730`, median-of-three BenchmarkDotNet process medians on the reference i9-14900K and .NET 10.0.12 are:

| Workload | Direct | Typed | Dynamic |
| --- | ---: | ---: | ---: |
| Literal +1 | 2.247 ns | 1.674 ns | 6.950 ns |
| Literal -1 | 2.269 ns | 1.780 ns | 8.363 ns |
| Runtime +1 | 2.468 ns | 2.170 ns | 6.987 ns |
| Runtime -1 | 2.481 ns | 2.495 ns | 7.332 ns |
| Alternating one | 4.431 ns | 2.562 ns | 7.141 ns |
| Literal +5 | 1.534 ns | 1.448 ns | 2.397 ns |
| Literal -5 | 1.520 ns | 1.412 ns | 2.443 ns |
| Runtime +5 | 1.732 ns | 1.457 ns | 2.398 ns |
| Runtime -5 | 1.731 ns | 1.486 ns | 2.396 ns |
| Repeated +1 ×5 | 1.515 ns | 2.494 ns | 6.910 ns |
| Repeated -1 ×5 | 1.578 ns | 2.780 ns | 7.282 ns |
| Literal +64 | 1.564 ns | 1.411 ns | 1.421 ns |
| Runtime +64 | 1.933 ns | 1.411 ns | 1.416 ns |

All 117 `SumTimeline` measurements report 0 B allocated. The dynamic scalar path is measured above 3 ns and is not the primary latency target.

A separate heterogeneous Combat fixture measures real animation and damage callbacks:

| Stream | Direct | Typed | Dynamic |
| --- | ---: | ---: | ---: |
| Forward +1 | 3.778 ns | 4.165 ns | 6.039 ns |
| Alternating +1/-1 | 8.005 ns | 7.696 ns | 20.258 ns |

All 18 Combat measurements report 0 B allocated. These results define the honest workload boundary: callback bodies, active tracks, blending, branches, memory dependencies, and ordered effects establish a physical floor. The library cannot promise arbitrary consumers below 3 ns.

[Signed-seek evidence](benchmarks/Alpha/results/signed-seek/README.md) retains raw JSON, logs, PMU counters, generated hashes, code sizes, and Tier-1 assembly. NativeAOT correctness is a release gate; NativeAOT throughput is reported separately when a controlled harness exists.

## Measurement rules

- Verify exact traces before timing.
- Measure direct oracle, typed, and dynamic forms with the same authored timeline, ticks, inputs, effects, and receipts.
- Use three fresh BenchmarkDotNet processes with 16 warmups, 12 target iterations, 250 ms requested iteration time, MemoryDiagnoser, and full JSON.
- Pin one logical CPU and record topology, governor, turbo state, SDK, runtime, benchmark version, affinity, source commit, tree, generated hashes, and commands.
- Retain cycles, instructions, branches, branch misses, code bytes, generated bytes, static data, registry bytes, and managed allocation as distinct quantities.
- Keep validation, playback transition, and observable outputs inside the timed operation.
- Label per-frame replay throughput as throughput. Do not describe it as one-call scalar latency.
- Never use best-sample selection, timer subtraction, dead outputs, constant-folded receipts, unchecked failure, different fixtures, or reordered floating-point effects.
- Hosted CI proves correctness and artifact integrity; it is not the authority for sub-3 ns comparisons across unlike machines.

## Memory and size

Production source size is:

```text
sum(UTF-8 file content bytes + UTF-8 relative path bytes + one separator byte)
```

for production files under `src`. The hard cap is 200,000 bytes. This C# signed-seek branch measures 154,651 bytes before the C and package-layout integration workstreams land. The final merged candidate must measure the complete tree again. Generated application source, IL, native code, retained timeline data, and runtime working set are reported separately.

For `L` independently selectable stored values, fixed-width selection requires at least `ceil(log2 L)` bits. A timeline kind upper bound of 256 requires eight bits. Selecting up to 65,536 dynamic timelines requires sixteen bits. A count representing every integer from zero through 256 requires nine bits. Narrowing a field helps only when packing and load costs improve after alignment.

Unique payload information cannot be losslessly deduplicated below its entropy. Equivalent plans, constants, rows, and operation sequences may be interned after exact equality. Signed zero, NaN payloads, padding, alignment, and deterministic ordering are part of that proof.

The 200 KB library cap does not imply that all generated game code or payload data fits L1. Report source, generated code, native text, static data, and runtime hot sets independently. Select specialization only while its instruction-cache cost beats shared execution.

## Correctness matrix

Release validation covers:

- start at arbitrary game ticks while local position begins at zero
- zero, `+1`, `-1`, alternating, literal/runtime `±5`, and larger replay
- finite beginning/end rejection and empty timelines
- looping normalization and cycle capacity
- gaps, one-frame clips, entry/stay/exit priority, overlap blending
- heterogeneous tracks and multiple borrowed inputs/outputs
- typed and dynamic semantic parity
- generated data access modes and compile-time rejection of invalid slots
- exception prefix effects
- stack-local and managed-heap byref safety through compacting GC
- deterministic generation and stale-output deletion
- zero warm managed allocation
- capacity and module capacity
- concurrent read-only playback after publication
- exact Playback, typed Playback, and x64 Frame ABI layouts
- isolated package consumption under managed JIT and NativeAOT
- absence of Roslyn and generator assemblies from runtime artifacts

One hundred percent line coverage is not a substitute for this boundary matrix. Coverage is evidence for missing cases; semantic oracles, ABI receipts, allocation receipts, NativeAOT execution, assembly, and hardware counters prove the properties that matter.

## Release gates

The candidate may publish only when all gates are green on the same reviewed commit:

1. Source budget and benchmark collector unit tests.
2. Release build with zero compiler warnings.
3. Full solution tests.
4. Alpha correctness, capacity, and module-capacity receipts.
5. Mixed heterogeneous sample.
6. Signed-seek benchmark verification.
7. Linux x64 NativeAOT publish and execution.
8. Isolated package-only typed and dynamic execution under managed JIT and NativeAOT.
9. No generator, compiler, or Roslyn assemblies in runtime output.
10. JetBrains Inspect Code findings classified; real defects fixed.
11. Raw benchmark, assembly, PMU, generated, source-size, and checksum evidence retained.
12. Public API approval, migration guide, README, package versions, tag, and release metadata agree.
13. Independent review passes.

The C backend signed-seek ABI and Unity/Burst integration are separate workstreams. Issue #16 remains open until those accepted cross-language receipts land. Completion of the C# typed playback contract closes issue #15.

## Coordination and recovery

| Workstream | Issue |
| --- | --- |
| C# batch alias safety | [#2](https://github.com/IAFahim/tl/issues/2) |
| C backend totality and widths | [#3](https://github.com/IAFahim/tl/issues/3) |
| IDE incremental generation | [#4](https://github.com/IAFahim/tl/issues/4) |
| Language-neutral plan | [#5](https://github.com/IAFahim/tl/issues/5) |
| Correctness and coverage | [#6](https://github.com/IAFahim/tl/issues/6) |
| Unity ECS and Burst | [#7](https://github.com/IAFahim/tl/issues/7) |
| Packages and artifacts | [#8](https://github.com/IAFahim/tl/issues/8) |
| Multi-PC coordination | [#9](https://github.com/IAFahim/tl/issues/9) |
| Physical-floor performance | [#10](https://github.com/IAFahim/tl/issues/10) |
| Release | [#11](https://github.com/IAFahim/tl/issues/11) |
| Typed playback | [#15](https://github.com/IAFahim/tl/issues/15) |
| Signed simulation seek | [#16](https://github.com/IAFahim/tl/issues/16) |
| Separate the C# generator package | [#18](https://github.com/IAFahim/tl/issues/18) |
| Compact generated schema routing tables | [#19](https://github.com/IAFahim/tl/issues/19) |
| Make Release static analysis exact and warning-free | [#22](https://github.com/IAFahim/tl/issues/22) |

A new machine recovers with:

```sh
git clone https://github.com/IAFahim/tl.git
cd tl
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
cat AGENTS.md
cat plan.md
cat docs/roadmap.md
gh issue view 11 --comments
gh issue view 16 --comments
gh pr list --state open
gh project item-list 6 --owner IAFahim --limit 1000
git ls-remote --heads origin 'refs/heads/workstream-claims/*' 'refs/heads/issue-transactions/*'
```

Every workstream records its exact branch, owner, machine, commit, validation, remaining failure, and next atom on its issue. Green atoms are committed, pushed, and checkpointed before another atom begins. Pull requests are reviewed by another agent. The manager alone merges after required checks and evidence pass.
