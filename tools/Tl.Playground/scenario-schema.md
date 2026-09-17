# Scenario config schema

Preset scenario files under `tools/Tl.Playground/presets/` are JSON objects with this shape. The
same files load in the playground site (embedded into `Play.Core`), the wasm smoke host, and the
native receipt host, which additionally asserts the embedded bytes equal the committed files.

| Field      | Type             | Meaning                                                        |
| ---------- | ---------------- | -------------------------------------------------------------- |
| `crowd`    | int              | Row count, 1..2048.                                            |
| `pattern`  | string           | Initial position distribution: `uniform`, `staggered`, `waves`. |
| `dir`      | string           | Step direction: `fwd` or `back` (`backward` accepted as an alias). |
| `play`     | bool             | Autoplay at 10 fps after load.                                 |
| `scale`    | number           | `Play.ScaleTrack` payload (`Scale`).                           |
| `amountA`  | number           | Clip A payload (`Amount`).                                     |
| `windowA`  | `[start, end]`   | Clip A half-open tick window inside `[0, duration]`.           |
| `amountB`  | number           | Clip B payload (`Amount`).                                     |
| `windowB`  | `[start, end]`   | Clip B half-open tick window. Overlapping windows blend.       |
| `timeline` | object           | The real flat-schema-v1 authoring document (`duration`, `loop`, `tracks`) baked by `TimelineBaker.BakeJson`. |

The `timeline` object is the single source of truth for `duration` and `loop`; the query-string
embed contract (`?loop=`) writes into it. The site URL params use the same field names, with
`windowA=start:end`; see #107's embed contract.
