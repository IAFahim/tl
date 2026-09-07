# Build-time adapters

The C# adapter is the first backend. Waffle belongs in the build pipeline;
playback consumes ordinary compiled C# and has no Waffle dependency.

```text
designer / application input
          |
          v
      Definition
          |
     validate + plan
          |
          v
        IAdapter
          |
          v
   C# source files -> normal C# build -> application playback
```

`Definition` describes authored tracks, clip windows and typed payload input.
`Plan` holds validated regions, active rows, blend information and the selected
emission strategy. Keep timeline analysis separate from C# syntax emission.
`SourceFile` describes an output name and source text. These are responsibilities
for extraction, not a claim that the empty `src` placeholders implement them.

The initial delivery needs one real C# adapter with a documented build-time
entry point and an external consuming sample. Do not add other language backends
or a complex plugin system now. C# expressions, managed types and emitted names
are C# adapter concerns; another backend will need its own payload mapping.

General generated tables provide the behavior-preserving path. Specialized bake
strategies may emit precomputed tables and straight-line operations only when
valid for the supported consumer and within code/data budgets. The current
FrozenSink benchmark emitter is not a general adapter for arbitrary user hooks.
Unsupported specialization must fall back or diagnose, never silently drop work.

Generated files belong to the consuming build's output directory. Track authoring
inputs, generator/adapter versions and output identity so edits regenerate even
when an old file exists. Output must be deterministic and parallel builds must
not write to the same fixture directory.

Explicit closed-type bindings must be reachable to AOT compilation. A guarded
JIT reflection convenience path does not establish AOT support. NativeAOT and
future Unity/WASM targets need tests on their actual toolchains, including the
consumer code; generating valid C# alone is insufficient.

See the [v0.1 handoff](v0.1.md) for project boundaries and acceptance checks.
