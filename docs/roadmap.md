# tl roadmap

The next milestone is **v0.1 extraction**, not another optimization round.
Use the [v0.1 handoff](v0.1.md) for implementation tasks and the
[semantics](semantics.md) for behavior. The [README](../README.md) is the product
entry point. This roadmap supersedes the earlier phased API plan.

## Research checkpoint

At `ccd344a`, the repository contains the redesigned three-hook prototype,
8-byte playback, per-work states, generated-table playback, bake v3, explicit
AOT binding and the completed four-item optimization queue. The queue retained
cursor and blend-scratch improvements, retained dedup only as an opt-in and
rejected prefix counts. Skew-tree experiments are also closed.

These are research results. `src` is still empty scaffolding, the fastest bake
is specialized to benchmark consumers, and the handoff identifies correctness
and integration work required for a library release. The 1.06–1.09 ns Fused16
result does not cross the earlier sub-1 ns target or prove impossibility.

## Now: extract and freeze v0.1

1. Record the current receipts and move reusable runtime code into `Tl.Core`.
2. Fix duration narrowing, dispatch lifetime/publication, closure validation and
   authoring-handle ownership before freezing those paths.
3. Extract `Tl.Gen` with a general C# adapter, validated bake selection and a
   working consumer outside the generator/benchmark assemblies.
4. Add reproducible generation, public API tests, allocation checks and an actual
   NativeAOT smoke app to CI.
5. Build local package artifacts, update the README and record the v0.1 freeze.

Keep the measured runtime tables, optional cursor, bounded blend scratch and
opt-in dedup. Any additional optimization needs a reproduced regression or new
evidence. No automatic iutq migration or merge belongs to this milestone.

## Later

Unity authoring integration, WASM execution and other language adapters follow
v0.1 with target-specific contracts and tests. Further speed work remains possible;
it must preserve the selected semantics or be exposed as an explicitly different
mode. A sampling-only path must not be compared as equivalent full-state playback.
