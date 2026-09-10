# tl roadmap

v0.6 is the released baseline. Its implementation, measurements and verification remain in the [archived report](verification/v0.6/plan.md) and [verification record](verification/v0.6/README.md).

v1.0.0-alpha.1 introduced the breaking generated-runtime line: heterogeneous tracks, automatic generation from partial ITimeline declarations, separate borrowed Input/Output contexts, Frame callbacks, explicit Before/After hooks and a runtime-ID hub without manual Bind.

v1.0.0-alpha.2 is the current candidate. It adds one-install C# packaging, deterministic generation and memory reports, the first language-neutral plan and C11 backend, batch alias atomicity, PMU evidence, and durable GitHub coordination. Follow the [implementation plan](../plan.md), [API contract](v1.0-alpha-api.md), [execution checklist](v1.0-alpha-checklist.md), and [release issue](https://github.com/IAFahim/tl/issues/11).

The performance target is below 3 ns/tick for hot sequential or batched public workloads on the reference machine. Correctness, ordered effects, ownership, zero warmed allocation and the 200,000-byte source/path cap remain release gates. Random seeking, larger working sets and callback work are reported separately because their physical cost is content-dependent.

Unity ECS integration follows the core ABI rather than changing it. Timeline payloads and playback state are already unmanaged. Burst qualification requires a dedicated package and test matrix because Burst supports a different C# and runtime subset from .NET 10.

The first portability backend is C11. Its contract becomes the native foundation for C++, Rust, engines, and FFI bindings. The current neutral slice carries identity, runtime ID, looping, authored track indices, payload handles, clip windows, and stable operation IDs. Regions, movement facts, typed constant values, slot access, canonical serialization, and shared semantic lowering remain tracked compiler work. C consumers implement named operations; arbitrary C# method bodies are never presented as portable. The C ABI fixes layout, alignment, ownership, status values, and versioning. Cross-endian serialization is not yet defined.

Historical experiments remain evidence for earlier designs and measurements. They do not substitute for measurements of the v1 public API.
