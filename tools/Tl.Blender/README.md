# tl bridge (Blender addon)

Exports the selected objects' NLA animation as tl flat-schema-v1 authoring JSON (docs/data-authored-api.md) and bakes each timeline to a `.tlb` asset by invoking the existing `Tl.Bake` CLI (tools/Tl.Bake) as a subprocess. Bake diagnostics flow back into the Blender report. Pure Python, no compiled extensions, no network. The mapping module (`mapping.py`) imports under plain CPython with no `bpy` dependency; the repo's stubbed unit tests live in `tests/test_tl_blender.py`.

Receipted on Blender 5.2.1 LTS (headless, `--factory-startup`). Requires Blender >= 5.0: the addon reads the strip bounds the NLA editor displays, which are `frame_start`/`frame_end` on the 5.x NLA (the 4.x `frame_final_start`/`frame_final_end` pair no longer exists; `frame_*_raw` are only unvalidated setters). Older Blender versions are rejected with a diagnostic.

## Install

1. Zip the addon folder so the archive contains `__init__.py`, `mapping.py` and `bake.py` at its root (for example `cd tools/Tl.Blender && zip -r ~/tl_bridge.zip .`), or copy this folder into the Blender scripts addons directory under a valid Python module name such as `tl_bridge`.
2. Blender: Edit > Preferences > Add-ons > Install, then enable "tl bridge".
3. Open Preferences (sidebar: View3D > N-panel > tl > Preferences) and set the bake command and consumer assemblies.

## Preferences

| Preference | Meaning |
| --- | --- |
| Default track type | Bare type name for tracks without a `tl_track` entry; empty rejects those tracks. |
| Default clip type | Bare type name for strips without a `tl_clip` entry; empty rejects those strips. |
| Namespace | Bare namespace written on every track and clip; empty selects the global namespace. |
| Bake command | Command that starts the CLI, e.g. `dotnet /path/to/Tl.Bake.dll`. Default `dotnet`. |
| Consumer assemblies | Semicolon-separated paths of assemblies holding the track and clip structs, passed as `--assembly <path>`. |

## Workflow

1. Author NLA tracks/strips on the objects to export.
2. Add the type identity on each object's custom property `tl_nla` (see mapping below) or rely on the default type preferences.
3. Select the objects, press the sidebar button "Export tl timelines", choose an output directory.
4. The addon writes `<object name>.json` per object next to the scene start frame mapping, then bakes each to `<object name>.tlb` and reports the CLI output. Mapping defaults and ignored Blender-only settings are echoed in the report; any rejection aborts with a message naming the object, track and strip.

## Mapping (v1)

Scene frames map to ticks 1:1, relative to `scene.frame_start` (tick 0 = scene start). Objects are exported sorted by name; one timeline per object; one bake per timeline.

| Blender | tl JSON | Notes |
| --- | --- | --- |
| Selected object | root timeline | `name` = object name; the file name sanitizes path separators in the object name. |
| `tl_loop` object custom property (bool) | root `loop` | Default false when absent; echoed in the report. |
| NLA track, top-down collection order | `tracks[]` entry | `name` = track name; order preserved. Muted tracks are skipped and reported. |
| Track type | track `type` | `tl_nla.tracks['<track>'].tl_track` > default track type preference > reject. |
| NLA strip, left-to-right | `clips[]` entry | `name` = strip name; authored order preserved. |
| Strip window `[frame_start, frame_end)` | clip `start`/`end` ticks | The bounds the NLA editor displays (5.x `frame_start`/`frame_end`), minus `scene.frame_start`. Bounds must be whole frames at/after the scene start; subframes reject. |
| Clip type | clip `type` | `tl_nla.strips['<strip>'].tl_clip` > default clip type preference > reject. |
| Amount | clip `data.Amount` | `tl_nla.strips['<strip>'].tl_amount` (number) > constant strip `influence` > reject. Animated influence without `tl_amount` is a rejection, not a sample. Emitted as `{"Amount": <value>}`; consumer clip structs must expose an exact field named `Amount` (schema field matching is ordinal). |
| Namespace preference | track and clip `namespace` | Written on both levels; empty = global namespace. |
| Max strip end across included tracks | root `duration` | Clamped to at least 1; above the 65,535-tick cap the object is rejected. |

### Where the custom properties live

Blender 5.x NLA strips and tracks do not support id properties, so the pinned `tl_track`/`tl_clip`/`tl_amount` keys live in one object-level custom property `tl_nla` (a property group persisted in the .blend and editable in the object Custom Properties panel):

```json
tl_nla = {
  "tracks": {"hits": {"tl_track": "BlendTrack"}},
  "strips": {"hit_a": {"tl_clip": "BlendClip", "tl_amount": 2.5}}
}
```

Missing entries behave like missing custom properties (preference fallbacks). Unknown keys, wrong-typed values, and duplicate track or strip names are rejections naming the object, track or strip.

### Settings reported but not mapped

Nothing is silently dropped. For every included strip the report lists: strip mute, `blend_type` when not `REPLACE`, `extrapolation` when not the 5.x default `HOLD`, soft trim (`blend_in`/`blend_out`), `repeat` when not 1.0, and non-`CLIP` strip types. Track-level mutes skip the track and are reported.

## Diagnostics

Mapping rejections abort the export with the offending object, track or strip named. Bake failures surface the CLI's stdout, stderr and exit code in the Blender report; located `[line:column]` diagnostics pass through unchanged. On the current main CLI only duplicate-field diagnostics carry a location; type-resolution diagnostics become located when the #108 fused parser lands. The bridge adds no schema validation: schema diagnostics come from `Tl.Gen.Tlb`.

## Receipts

With the solution built (`dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false`):

```sh
python3 -m unittest discover -s tests -p test_tl_blender.py
mkdir -p /tmp/tl111-r2 && blender -b --factory-startup --python tools/Tl.Blender/receipt/receipt_export.py -- /tmp/tl111-r2
mkdir -p /tmp/tl111-r3 && blender -b --factory-startup --python tools/Tl.Blender/receipt/receipt_invalid.py -- /tmp/tl111-r3
```

Receipt 2 exports a scripted scene and asserts the JSON is byte-identical to `fixtures/receipt_scene.json`, then proves the baked `.tlb` equals the bake of the hand-authored `fixtures/receipt_scene_hand.json`. Receipt 3 round-trips an invalid type name from scene to JSON to CLI diagnostic to bridge report and demonstrates a located `[line:column]` diagnostic through the same path.

## Limitations

- One timeline per object and one JSON per object: the flat-schema-v1 root is a single timeline.
- Blender tracks cannot hold overlapping strips, so time-overlapping clips belong on separate NLA tracks (the bake derives blending per track entry).
- Influence detection uses `use_animated_influence`, strip-local fcurves, and a `data_path` scan of the object's drivers; exotic driver targets are best-effort.
- The default 5.x strip influence is 0.0; set `tl_amount` or the strip influence explicitly.
- The bake step requires `dotnet` plus a built `Tl.Bake.dll` and the consumer assemblies; nothing is downloaded.
