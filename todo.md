# todo

## Frozen bridge — build-time baking of game timelines (THE plan, not built)

The goal: hit build → timelines authored in game code get baked into real
code by Waffle. At runtime the call sees "already generated", plays the
baked kernel, never builds tables. The timeline is frozen forever; only
the input/result data (the player etc.) changes at runtime. `.Build(...)`
stays as the dynamic fallback for content that only exists at runtime.

Three pieces, none built yet (the 1 ns kernel itself is proven — bake v3
receipts in docs/benchmarks.md):

1. **Discovery** — the generator must see frozen timelines at build time.
   Constraint: a C# lambda can only be baked if it is pure constants, so
   frozen timelines must be declared in a generator-readable form (data
   file — the shape `Tl.Gen`'s plan model was aimed at — or attributed
   static tables). Runtime-valued authoring cannot be baked, by logic.
2. **User code in the kernel** — the generator emits calls to the
   consumer's own `IForward`/`IBackward` (their `TResult`), not the fixed
   `FrozenSink` the sandbox kernels accumulate. Tables, blends, states
   bake around the consumer calls.
3. **Seamless runtime linkage** — baked kernels register into the same
   index registry, so `Timeline.Forward(index, in pb, in input, ref
   result, tick)` works unchanged: baked index → 1 ns kernel, `.Build`
   index → interpreter. Call sites never know which.

Why the generator is a build-time CLI, not a Roslyn source generator:
baking per-tick floats requires EXECUTING the authoring to get real
table values. A source generator can only see syntax; our MSBuild-target
console project (rendering with the Waffle package) compiles and runs
the fixture code, then emits the constants. Same reason iutq's frozen
generator reads baked binary blobs, not source.

## Pending

- v0.3 region-stable Tracks — agent in flight (`tl-v03`, branch
  `v0.3-tracks`); review ladder + sweep when it lands.
- src/Tl.Core mirror of v0.3 after sandbox review.
- Three-way bench (tl vs iutq vs iutq-next) — first iutq legs invalid
  (ran during agent build storm); re-run sequentially on a quiet machine,
  one fixture shape for all three engines.
- CI status unverifiable via gh API from this machine; all pushes gated
  by local suites.
