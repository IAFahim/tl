# tl roadmap

v0.6 is the released baseline. Its implementation, measurements and verification remain in the [archived report](verification/v0.6/plan.md) and [verification record](verification/v0.6/README.md).

v1.0.0-alpha.1 is the breaking generated-runtime line: heterogeneous tracks, automatic generation from partial ITimeline declarations, separate borrowed Input/Output contexts, Frame callbacks, explicit Before/After hooks and a runtime-ID hub without manual Bind. Follow the [implementation plan](../plan.md), [API contract](v1.0-alpha-api.md) and [execution checklist](v1.0-alpha-checklist.md).

The performance target is below 3 ns/tick for hot sequential or batched public workloads on the reference machine. Correctness, ordered effects, ownership, zero warmed allocation and the 200,000-byte source/path cap remain release gates. Random seeking, larger working sets and callback work are reported separately because their physical cost is content-dependent.

Unity ECS integration follows the core ABI rather than changing it. Timeline payloads and playback state are already unmanaged. Burst qualification requires a dedicated package and test matrix because Burst supports a different C# and runtime subset from .NET 10.

Historical experiments remain evidence for earlier designs and measurements. They do not substitute for measurements of the v1 public API.
