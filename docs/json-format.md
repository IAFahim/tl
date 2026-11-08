# Portable JSON, schema 1

Tl data lives in three JSON documents. `tl bake` turns them into binary (`.tlb` per timeline, `game.tlib` per project); `tl decode` turns the binary back into the same JSON. Unity's `Tl > Export` writes the same timeline JSON through the same packer (`TimelineJson.Export` in `Tl.Gen.Tlb`).

Every document starts with `"schema": 1`. Any other value, a missing value where it is required, an unknown key or a duplicate key fails with a message that names the field.

## Timeline (`<name>.json`)

```json
{
  "schema": 1,
  "name": "push",
  "duration": 180,
  "channel": "body",
  "next": "rest",
  "tracks": [
    {
      "name": "force",
      "namespace": "Tl.Pack.Force", "type": "ForceTrack", "data": { "Origin": { "x": 0, "y": 0, "z": 0 } },
      "clips": [
        { "name": "shove", "namespace": "Tl.Pack.Force", "type": "ForceClip", "start": 0, "end": 60,
          "data": { "Direction": { "x": 1, "y": 0, "z": 0 }, "Falloff": [1,0.9,0.8,0.7,0.6,0.5,0.4,0.3,0.2,0.1,0,0,0,0,0,0] } }
      ]
    }
  ],
  "markers": [ { "namespace": "Tl.Pack.Force", "type": "Hit", "tick": 30, "data": { "Amount": 5 } } ]
}
```

- `schema` is optional in a timeline (a timeline without it bakes the same bytes); when present it must be `1`.
- Root keys: `name`, `duration` (ticks, at most 65,535), `loop`, `hold`, `next` (timeline name), `channel` (channel name), `tracks`, `markers`.
- Types are `namespace` (dotted, may be empty) plus `type`. A generic type spells its arguments with their namespaces: `TlNoSettings<Game.Hit>`, `IndependentTrack<System.Int32>`. No assembly name is needed: pair keys use the full type name only.
- `data` sets struct fields by name. Numbers must fit the field and be finite; `bool` is `true`/`false`; enums are member names; `TimelineId` fields are timeline names; nested structs are objects; `TlCurve` is an array of exactly 16 numbers; a `fixed` buffer is an array of exactly its length. Fields left out are zero.

## Project (`tl.project.json`)

```json
{
  "schema": 1,
  "tickRate": 60,
  "channels": ["body", "voice"],
  "timelines": ["push.json", "moves/*.json"],
  "kinds": { "Game.Damage": { "gamePulses": 4 } },
  "packs": [ { "name": "Tl.Force", "assembly": "Tl.Pack.Force", "version": "0.1.0", "layout": "<digest>" } ]
}
```

`schema` and `timelines` are required. `kinds` keys are kind full type names (or group names). Channel order sets channel numbers (1-up). Timeline globs are relative to the project file and never match the project file itself. Layout of the packed result: `docs/library-layout.md`.

## Scene (`*.scene.json`)

```json
{
  "schema": 1,
  "project": "tl.project.json",
  "entities": [ { "name": "cube", "states": [ { "namespace": "Game", "type": "Health", "data": { "Value": 10 } } ], "play": [ { "timeline": "push" } ] } ],
  "requests": [ { "step": 40, "entity": 0, "op": "Pause", "timeline": "push" } ]
}
```

Each request names the fields its operation needs (`timeline` for timeline operations, `channel` for channel operations, `tick` for `Seek`, `speed` for `SetSpeed*`, `reverse` for `SetReverse*`) and nothing else. Scene JSON is validated today; `tl run` that executes it is phase 7.

## Commands

```sh
tl bake tl.project.json -o game.tlib --assembly Game.dll     # whole project (also: a folder)
tl bake push.json -o push.tlb --assembly Game.dll            # one timeline without channel or next
tl decode push.tlb -o push.json --assembly Game.dll          # one timeline; stdout without -o
tl decode game.tlib -o decoded/ --assembly Game.dll          # every timeline plus tl.project.json
```

Decode is exact or it fails. It rebuilds each timeline from the TLB cold metadata (`docs/tlb1-layout.md`, "Cold metadata tail"): authored track and clip order, each clip label bound to its window, track and clip type arguments, channel and next names. It writes every value through its fields, refuses a value whose bytes do not survive that (for example a struct with nonzero padding bytes, named with the byte offset), and re-bakes the JSON it wrote, failing unless the bytes are identical. A stripped TLB (`tl --strip`) has no metadata and cannot be decoded.

What `game.tlib` does not store, so `tl decode game.tlib` cannot give back: the project's `kinds` and `packs` entries (copy them from the authoring project; decode says so), channel names no timeline uses (decode fails rather than renumbering), and the original timeline file names (each timeline is written as `<name>.json`).

## Migration from the earlier format

No compatibility layer is kept.

- `library.json` is now `tl.project.json` and needs `"schema": 1`. Rename the file; the old name is not read.
- The TLB cold metadata tail is version 2 (64-byte header, track and clip tables, generic type names, channel and next labels). Tails of version 1 are rejected by every metadata reader (`tl --report`, `tl decode`, `Tl.Tlb`); playback never reads the tail, so old files still play, but rebake them to inspect or decode them. Every TLB and `game.tlib` byte fixture was re-pinned; hot sections and all state hashes are unchanged (13943614770702343869, 6607956939547305625, 12545803591333190611).
- `TlbMetadataView` gained `Tracks` and `Clips`; type rows of generic types now carry their arguments in `Name`.
- Kinds are identified by full type name everywhere (library kind rows, `tl.project.json` `kinds` keys, `KindStore.IndexOf`/`Pulses<T>`, generated systems, the Unity registry and kind ids), so a pack's `Tl.Pack.Force.ForceKind` and a game's `ForceKind` coexist (IAFahim/tl#582). Write `"Game.Damage"` where you wrote `"Damage"`. The generated `KindCapacity` constants keep the short name unless two kinds share it.
