# ConsumerGenerate

> Retained as history: `samples/Compiled` was removed at commit `5c05aa5`; the `--source` path below names the input at this README's original commit.

This benchmark-only generator recognizes the current `Pulse` declaration and emits a fused timeline player for an explicit per-work consumer contract. Each active work becomes a direct static `IWorkOperation<TInput, TResult>` call with its authored index, track offset, resolved clip amount, movement state, and effective tick. The generated class keeps scalar and carry-batch paths, without runtime timeline tables, retained views, managed arrays, or region switches.

Run it from the repository root:

```sh
taskset -c 17 dotnet run --project benchmarks/ConsumerGenerate -c Release -p:NuGetAudit=false -- --source samples/Compiled/Timeline.cs --output benchmarks/ConsumerFusion/Generated/FusedPulse.g.cs
```

Both paths preserve `Playback` lifecycle checks, loop accounting, forward overflow timing, backward cycle saturation, last-frame flags, and the runtime outer-window `ClipState` rules. Blend amounts use `first + (second - first) * factor` in that order. Empty batches validate lifecycle and then return the original `Playback` unchanged.

The supported grammar is intentionally narrow: `PulseClip(float Amount)`, `PulseTrack(int offset)` with its current offset field and blend method, literal track offsets, literal clip amounts, and a looping `Pulse.Decls.Pulse` declaration. Unsupported input fails generation.

Use `--self-test` to run the acceptance check and rejection probes:

```sh
taskset -c 17 dotnet run --project benchmarks/ConsumerGenerate -c Release -p:NuGetAudit=false -- --self-test
```

`--inline-batch` requests inlining only for the two generated span overloads. Without it, generation reproduces the original baseline byte-for-byte. This is a measured code-shape option, not a promise that every JIT or every consumer will inline or run faster. The experiment report records both variants.
