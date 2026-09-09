# tl roadmap

v0.6 promotes direct per-work specialization into the production runtime and generator. The current contract, evidence, and remaining compiler work are maintained in [plan.md](../plan.md).

The next performance tier is consumer-body source fusion for a deliberately small typed operation algebra, followed by a sequential cursor kernel and a separately tuned batch backend. Each tier must preserve the interpreter oracle, ordered effects, exception timing, lifecycle behavior, zero managed allocation, NativeAOT execution, and the 200,000-byte library source budget.

Unity ECS integration follows the core ABI rather than changing it. Timeline payloads and playback state are already unmanaged. Burst qualification requires a dedicated package and test matrix because Burst supports a different C# and runtime subset from .NET 10.

Historical plans and benchmark implementations remain evidence for rejected designs. They do not define the current API.
