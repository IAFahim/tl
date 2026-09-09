# v1.0.0-alpha.1 decisions

| Decision | Result |
| --- | --- |
| Track identity | A timeline may contain up to 256 distinct closed track/clip kinds. `TrackIndex` identifies authored instances independently with 16 bits. |
| Context identity | Parameter names, exact types, and `in`/`ref`/`out` modes define slots. Ambiguous reuse is a build error. |
| Playback ownership | The owner ID is stored in `Playback`; the naturally aligned value is 12 bytes. |
| Dispatch | Core calls a static abstract method on the generated input type. Generated code dispatches to a direct specialized kernel. |
| Topology | Definitions must be visible at build time. Runtime component values remain dynamic. |
| Composition | Include decorates a build-visible base; hooks execute in stable declared order. Cycles and conflicting loop declarations are rejected. |
| Output reads | Writable slots preserve caller storage. An `out` callback overwrites it only when that callback runs. |
| Lifetime | Compiled IDs are process-local, never reused, and live for the registry lifetime. |
