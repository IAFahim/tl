# Tl.Playground

Interactive cookbook and playground for the data-authored API, published at
https://iafahim.github.io/tl/. Issue #116; prototype receipts in #107.

## Layout

- `Play.Core` — engine, no UI. One authored pair (`Play.ScaleTrack`/`Play.AmountClip`, one
  `PairRuntime` consume), `Scenario` (panel state + the #107 query params), authoring-JSON
  rendering, the playback engine over `TimelineSet.Gather(ids).Seek(positions, forward).Apply(effects)`,
  the per-row scalar oracle (`TimelineMovement.Select` + `BakedLane` tables), and the 120-frame
  SMOKE receipt. Presets under `../presets/` are embedded here so wasm hosts need no file system.
- `Playground` — Blazor WebAssembly app: scenario panel, live per-row strip and table colored from
  the real columns, moved/skipped/wrapped aggregates, editable authoring-JSON box with 300 ms
  debounce and inline `[line:column]` diagnostics, the static cookbook pages under `wwwroot/cookbook/`.
- `Playground.Smoke` — headless browser-wasm console host running the SMOKE receipt under Node.
- `Playground.Native` — native host that re-runs the receipt, asserts the checksum pinned in
  `Pin.Checksum` (the wasm receipt), verifies the embedded preset bytes equal the committed
  `presets/` files, and bakes every preset.
- `presets/` — the four preset scenario configs; shape documented in `scenario-schema.md`.
- `deploy/` — Pages deployment notes and the Node boot host for the published app.
- `receipts/` — committed receipt logs.

`Play.Core` references `src/Tl.Core` and `tools/Tl.Gen.Tlb` with `ProjectReference` — the real
runtime code pinned to this branch, never a fork. No Roslyn anywhere.

## Receipts

`SMOKE PASS frames=120 checksum=7131578910045740992` is bit-identical across three hosts:
the wasm console host, the published Blazor app's `Main` booted under Node, and native x64
(pinned and machine-checked by `Playground.Native`). Every frame compares the whole crowd
(positions + IEEE bits of effects) against the scalar oracle. Logs live in `receipts/`.

Performance is never a playground claim; wasm receipts are correctness-only.

## Commands

```sh
dotnet run --project tools/Tl.Playground/Playground.Native -c Release
dotnet build tools/Tl.Playground/Playground.Smoke/Playground.Smoke.csproj -c Release
node tools/Tl.Playground/Playground.Smoke/bin/Release/net10.0/browser-wasm/AppBundle/main.mjs
dotnet publish tools/Tl.Playground/Playground/Playground.csproj -c Release -o /tmp/pg116-publish
cp tools/Tl.Playground/deploy/blazor-node.mjs /tmp/pg116-publish/ && node /tmp/pg116-publish/blazor-node.mjs
```

The playground projects are intentionally not part of `tl.slnx`; CI does not build them.
