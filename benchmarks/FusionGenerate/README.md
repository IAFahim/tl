# FusionGenerate

> Retained as history: `samples/Compiled` was removed at commit `5c05aa5`; the `--source` path below names the input at this README's original commit.

This experimental generator recognizes the exact `PulseClip(float Amount)` and `PulseTrack.Blend` declarations in `samples/Compiled/Timeline.cs` and emits a kernel for one fixed operation: add every active work's `Clip.Amount` to a `float` accumulator in authored track order for forward movement, or subtract it for backward movement. Blends use `first + (second - first) * factor` in that order. Any change to the supported payload shape or blend law is rejected rather than compiled as if it were equivalent.

Run it from the repository root:

```sh
dotnet run --project benchmarks/FusionGenerate -- --source samples/Compiled/Timeline.cs --output benchmarks/FusionHour/Generated/FusedPulse.g.cs --batch carry
```

`--source` defaults to `samples/Compiled/Timeline.cs`. `--output` defaults to `benchmarks/FusionHour/Generated/FusedPulse.g.cs`. `--batch` selects `scalar`, `carry`, or `runs` and defaults to `carry`. The scalar mode composes the scalar core, carry keeps playback fields in locals across the span, and runs dispatches a balanced region tree before processing each maximal adjacent run in that region. `--backend` selects `tree` or `dense` and defaults to `tree`. Dense emission supports at most 4096 ticks and three active works, stores ordered per-tick operands in primitive static span data, and does not support region-run batching.

The measured variants are reproduced with these option pairs:

```sh
dotnet run --project benchmarks/FusionGenerate -- --batch scalar --backend tree --output /tmp/tl-fused-v1.g.cs
dotnet run --project benchmarks/FusionGenerate -- --batch carry --backend tree --output /tmp/tl-fused-v2.g.cs
dotnet run --project benchmarks/FusionGenerate -- --batch runs --backend tree --output /tmp/tl-fused-v3.g.cs
dotnet run --project benchmarks/FusionGenerate -- --batch carry --backend dense --output /tmp/tl-fused-dense.g.cs
```

Every output keeps the same scalar API. The batch option changes only the `ReadOnlySpan<uint>` overloads. The tree backend emits exact blend arithmetic in each region body. The dense backend materializes one float operand per active work and tick, keeps work order, performs each accumulator operation separately, and uses a zero count for gaps so padding values are never added. Both backends retain the current `Playback` lifecycle and use the prototype's checked little-endian 8-byte ABI mint.

Run `dotnet run --project benchmarks/FusionGenerate -- --self-test` to check that changed blend arithmetic, a changed clip payload shape, and an unrelated source are rejected.

This remains benchmark-only code. Count the generator and generated artifact sizes separately from `src`; the current generator is about 33 KB and emitted variants range from about 10 KB to 44 KB. Those costs are experimental footprint, not additions to the public source budget.
