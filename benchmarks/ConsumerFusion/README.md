# Consumer fusion with runtime inputs, clip state, and effects

The experiment demonstrates 1.195 ns/tick for a simple sum and 1.843 ns/tick for state-aware runtime-input work in sequential batches of eight. Random movement and observable callbacks cost more; the complete results and limits are below.

This experiment follows the v0.5 sum-only result. It tests an explicit per-work operation contract against the same runtime and general compiled Pulse timeline. It does not analyze arbitrary consumer C# or change the published packages.

Three unmanaged consumers share their actual business methods across all engines:

| Consumer | Observable work |
|---|---|
| SumConsumer | Ordered float addition/subtraction of every resolved clip |
| StateConsumer | Runtime Scale/Bias on Stay, track offsets, Enter/Stay/Exit counts |
| EffectConsumer | StateConsumer work plus an ordered non-inlined callback, audit hash, and optional failure after visible effects |

The generated kernel supplies known payloads and track offsets directly, computes the required movement-derived state, and invokes a static operation on the concrete consumer type. Scalar entry points are scalar; batches carry playback values across ticks. The ordinary C# compiler/JIT compiles each operation body, including its runtime input reads and effects. Fusion does not grant permission to remove observable calls or change float arithmetic order.

## What the limitation means

`input.Scale` is available at runtime, so the generated code can multiply by it. It cannot replace that multiplication with a constant derived only from the authored timeline. `ClipState` depends on movement, so a consumer that reads it requires the corresponding facts. An effectful call still occurs, in order, and a thrown exception must leave exactly the same preceding result mutations.

The explicit contract is per work. Existing arbitrary callback bodies that inspect an entire Tracks view, slice it, or perform once-per-callback setup/completion are not automatically translated into it. This experiment manually supplies adapters over the same business method to give all engines equivalent behavior. A production adapter generator and backend-selection interface remain separate work.

For example, these are real calls supported by the experiment:

```cs
var input = new ConsumerInput(1.25f, 0.125f);
var result = new StateConsumer();
var playback = FusedPulse.Start();

playback = FusedPulse.Forward(in playback, in input, ref result, 1u);
input = input with { Scale = 2f };
playback = FusedPulse.Forward(in playback, in input, ref result, 2u);
```

The result is Sum = 3.5, Stays = 2, TrackOffsets = 2. The authored amount is known; the two different Scale values are read at execution time. Runtime input is supported, but the multiplication and addition remain real work.

This existing callback shape needs an additional boundary operation before it can use the per-work contract without changing behavior:

```cs
result.TickCallbacks++;
foreach (var work in tracks.Slice(0, tracks.Count / 2))
    result.Sum += work.Clip.Amount;
```

Calling that body separately for each active track would change both the callback count and the selected subset. A once-per-tick phase and an explicit selection operation could express it. Arbitrary callback translation is not implemented here; the limitation is in this prototype's contract, rather than a rule that C# cannot compile such behavior.

Timeline eligibility is still restricted to the supported Pulse payload, track storage, blend law, and literal authoring grammar. The operation contract accepts unmanaged input and result types; an operation body may still allocate or access external state. Zero allocation is a measured property of these consumers, not a consequence of using the interface.

## Receipt

Closed receipt (#300 finding 3.2, owner decision of September 20, 2026, delegated on the #142 trail). The harness measured the per-work operation contract against the interpreter and normal compiled references for the Sum, State, and Effect consumers over sequential, seeded-random, and repeated tick streams: 54 baseline cases plus the 18-case batch-inlining follow-up, each invocation processing 65,536 ticks from fresh result/playback state with scalar and batch arms seeing identical ticks, inputs, and effects. Every fused arm was faster than both matched references, every MemoryDiagnoser result read 0 B/tick, and both the baseline and the inlined candidate passed 215,273 exact comparisons under JIT and NativeAOT.

Its reproduction tooling was removed under issue #207: the `ConsumerGenerate` generator step is gone, no buildable project remains, and `prepare.py` is retained only as an inert record of the chain. The results stand as receipts — the tables below, the correctness and machine-code proofs, and the retained files under `results/` are the measurement record and have not been re-run since.

## Measured baseline

Measured on September 9, 2026: i9-14900K, logical CPU 4, .NET SDK 10.0.400, .NET 10.0.11, BenchmarkDotNet 0.15.8. Numbers are medians in ns/tick; each scalar arm is a stream of dependent single-tick calls, not an independently measured one-call latency. Batch rows divide complete eight-tick calls by eight. CPU affinity does not isolate unrelated applications or shared package power. No benchmark ran concurrently with an agent build.


### Scalar calls

| Consumer | Stream | Interpreter | Normal compiled | Fused | Compiled / fused |
|---|---|---:|---:|---:|---:|
| SumConsumer | Sequential | 12.614 | 14.269 | 2.466 | 5.79x |
| SumConsumer | Random | 20.197 | 23.527 | 8.381 | 2.81x |
| SumConsumer | Repeated | 14.200 | 15.549 | 2.365 | 6.58x |
| StateConsumer | Sequential | 14.322 | 16.322 | 2.765 | 5.90x |
| StateConsumer | Random | 21.516 | 30.366 | 10.024 | 3.03x |
| StateConsumer | Repeated | 16.806 | 16.123 | 2.742 | 5.88x |
| EffectConsumer | Sequential | 17.477 | 17.898 | 5.160 | 3.47x |
| EffectConsumer | Random | 24.903 | 28.213 | 12.784 | 2.21x |
| EffectConsumer | Repeated | 20.571 | 19.497 | 6.540 | 2.98x |

### Batches of eight

| Consumer | Stream | Interpreter | Normal compiled | Fused | Compiled / fused |
|---|---|---:|---:|---:|---:|
| SumConsumer | Sequential | 8.538 | 6.992 | 2.836 | 2.47x |
| SumConsumer | Random | 17.414 | 16.122 | 7.851 | 2.05x |
| SumConsumer | Repeated | 9.502 | 8.173 | 4.293 | 1.90x |
| StateConsumer | Sequential | 9.785 | 8.862 | 2.957 | 3.00x |
| StateConsumer | Random | 19.327 | 19.623 | 9.553 | 2.05x |
| StateConsumer | Repeated | 12.356 | 11.036 | 4.255 | 2.59x |
| EffectConsumer | Sequential | 13.292 | 12.031 | 4.653 | 2.59x |
| EffectConsumer | Random | 21.417 | 22.188 | 12.529 | 1.77x |
| EffectConsumer | Repeated | 15.923 | 14.242 | 6.026 | 2.36x |

All 54 cases completed. Every fused arm was faster than both matched reference arms. Every MemoryDiagnoser result was 0 B/tick. This is a useful consumer-fusion result, but this baseline does not achieve a universal 1–2 ns/tick. These consumers and the harness differ from the earlier sum-only FusionHour experiment; differences between those reports are not a controlled regression comparison.

Full receipts: [summary CSV](results/measurement/summary.csv), [sum measurements](results/measurement/results/Tl.ConsumerFusion.ConsumerBenchmarks_SumConsumer_-report-full.json), [state measurements](results/measurement/results/Tl.ConsumerFusion.ConsumerBenchmarks_StateConsumer_-report-full.json), [effect measurements](results/measurement/results/Tl.ConsumerFusion.ConsumerBenchmarks_EffectConsumer_-report-full.json).

## Correctness and machine code

The baseline passed 215,273 exact comparisons under both [JIT](results/proof/jit-verification.txt) and [NativeAOT](results/proof/aot-verification.txt). An independent authored-clip-list oracle checks amounts and movement states. Tests also compare every result field and full Playback, input preservation, both directions, edge pairs, zero/negative-zero/infinity/NaN result seeds, scalar/span composition, changing inputs, invalid lifecycle, unknown flags, empty spans, full loops, backward saturation, cycle overflow, and mutations before a second-work or second-tick callback failure. NativeAOT is correctness-checked; the nanosecond tables above measure the JIT.

Tier-1 JIT assembly removes the static operation calls completely for Sum and State. Effect retains its direct Notify calls in order. Scalar kernels inline into the benchmark caller. The baseline span kernels remain separate calls. [JIT assembly](results/proof/jit-disassembly) and [NativeAOT assembly](results/proof/native-disassembly) are archived with the source and execution identities.

| Baseline span kernel | Tier-1 JIT body bytes | NativeAOT body bytes |
|---|---:|---:|
| Sum | 911 | 816 |
| State | 3,382 | 2,927 |
| Effect | 4,166 | 3,597 |

These are individual forward span method bodies, not whole-program sizes. NativeAOT sizes exclude readonly literals and exception metadata; scalar inlining duplicates work in callers. The richer operation bodies consume more instruction space. This proves neither that a whole game fits in L1 nor that specializing every timeline is the best cache tradeoff.

## Integration boundary

The published v0.5 API and all production src files are unchanged. This branch was an executable experiment and its verification harness; the reproduction chain was removed under #207 (see Receipt above). Automatic conversion of arbitrary existing consumers, transparent backend selection through the existing API, a general semantic frontend, ARM64/Burst validation, and a many-timeline code-size policy are not implemented.

The production budget at measurement time was **186,154 / 200,000 decimal bytes**, counting file contents plus relative paths and one newline per path. Keeping this experiment outside src does not make its implementation free: promotion must budget its generator and shared contract, replace/factor existing implementation where appropriate, and separately account for emitted code and native code per timeline and consumer. [Budget receipt](results/proof/source-budget.txt).

## Batch inlining experiment

The baseline assembly identified a remaining call boundary in the eight-tick path. Adding AggressiveInlining only to the two span overloads removed that boundary under Tier-1 JIT. No consumer operations, arithmetic, inputs, lifecycle checks, or benchmark methods changed. A second 18-case run measured both fused shapes; the scalar method bodies were unchanged controls.

| Consumer | Stream | Baseline batch | Inlined batch | Batch change | Scalar control change |
|---|---|---:|---:|---:|---:|
| SumConsumer | Sequential | 2.836 | 1.195 | -57.9% | -0.1% |
| SumConsumer | Random | 7.851 | 7.071 | -9.9% | -1.4% |
| SumConsumer | Repeated | 4.293 | 1.133 | -73.6% | +0.0% |
| StateConsumer | Sequential | 2.957 | 1.843 | -37.7% | +0.1% |
| StateConsumer | Random | 9.553 | 9.335 | -2.3% | +0.1% |
| StateConsumer | Repeated | 4.255 | 1.891 | -55.5% | +0.0% |
| EffectConsumer | Sequential | 4.653 | 4.659 | +0.1% | -0.2% |
| EffectConsumer | Random | 12.529 | 12.942 | +3.3% | -2.8% |
| EffectConsumer | Repeated | 6.026 | 5.845 | -3.0% | -1.4% |

Negative change means less time. The inlined candidate reached **1.195 ns/tick for sequential Sum** and **1.843 ns/tick for sequential State with runtime inputs**. It was reproduced with --inline-batch and retained as the checked-in example. This is eight-tick throughput with complete result/playback receipts, not a claim of 1–2 ns latency for every call.

It is not a universally better switch: Effect/Random increased from 12.529 to 12.942 ns/tick (+3.3%), while its scalar control improved 2.8%. Effect/Sequential was effectively unchanged. The baseline figures were produced by omitting --inline-batch; that preparation chain is removed (see Receipt above), so these runs stand as receipts rather than a repeatable procedure. These results favor inlining for the demonstrated Sum/State consumers; an automatic production policy must consider consumer work and code growth instead of enabling it indiscriminately.

After inlining, the complete benchmark batch caller bodies are 1,055 B for Sum, 3,532 B for State, and 5,236 B for Effect in the diagnostic Tier-1 run. There is no FusedPulse call in those bodies; Effect still calls Notify. Caller sizes and standalone kernel sizes measure different scopes, and code can be duplicated into multiple callers. [Selected JIT assembly](results/inline-batch/proof/jit-disassembly).

All 18 follow-up cases completed with 0 B/tick allocation. The selected output also passed the full 215,273-comparison battery under JIT and NativeAOT. The generation grammar passed eight rejection probes. [Preparation and JIT proof](results/inline-batch/proof/prepare.txt), [NativeAOT proof](results/inline-batch/proof/aot-verification.txt), [all follow-up measurements](results/inline-batch/summary.csv).

The generator was 26,150 C# bytes, the shared experimental contracts 1,195 bytes, and selected emitted Pulse source 32,983 bytes. The 110-byte increase from baseline emission was just the two attributes. These counts exclude benchmark consumers, verification, reports, project files, and dependencies. At the time, simply adding the generator and contracts to the production src would have exceeded its remaining 13,846-byte budget; production integration requires replacing or factoring code.
