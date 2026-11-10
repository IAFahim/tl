# Hello Tl for plain .NET (no Unity)

The smallest complete Tl program, the same file as the Unity sample `Hello Tl` (`Teleport.cs` is identical): a clip, a track, a kind and a column that teleport a player along X.

```sh
dotnet run --project demos/hello -c Release
```

```
step  offset
  30  5
  90  13
 119  5
```

Tick 30: clip 1 starts, jump +5 and stay. Tick 90: clip 2 starts, +8 (13). Tick 119: clip 2 ends and comes back, -8 (5).

## Add a value: a sideways swing

A new value has to pass through all four parts, or it silently stays 0. Adding `SideSwing` only to the clip and the column builds clean and does nothing, because nothing reads the clip field and nothing writes the column field.

| | What you add |
|---|---|
| 1. Clip | `public float SideSwing;` on `TeleportClip` |
| 2. Track | a method that reads it: `[Pulse(typeof(SideKind), On = ClipEvent.Start)] static float SwingAtClipStart(in TlFrame<TeleportClip> frame) => frame.Direction * frame.Clip.SideSwing;` |
| 3. Kind | a new `SideKind` with `Apply(in float swing, ref Teleported player) => player.SideSwing += swing;` |
| 4. Column | `public float SideSwing;` on `Teleported` |

Then set `"SideSwing": 2` on the clips in the timeline JSON and rebake. Each kind takes one value, so a second value is a second kind, not a second argument to `TeleportKind`. The finished, tested version is `demos/hello-side` (`dotnet run --project demos/hello-side -c Release`):

```
step  offset  side
  30  5  2
  90  13  5
 119  5  5
```

Tick 30 adds 5 forward and 2 sideways. Tick 90 adds 8 and 3 (13 and 5). Tick 119 brings the 8 back (5) and leaves the side at 5, because only the start pulse carries a swing. To bring it back too, add a `[Pulse(typeof(SideKind), On = ClipEvent.End)]` method that returns `-frame.Direction * frame.Clip.SideSwing`.

## Files

- `Teleport.cs`: all of Tl's part. The generator turns it into `Tl.Generated.TlAuto_HelloTl` at build time (the name is the assembly name `HelloTl`).
- `Program.cs`: loads the library, starts timeline 1, steps 150 times, prints each change.
- `timelines/teleport.json`, `tl.project.json`: the timeline as portable JSON (what Unity's **Tl > Export** writes).
- `HelloTl.tlib`: the baked library. Rebake after a change:
  `dotnet run --project tools/Tl.Bake -- bake demos/hello/tl.project.json -o demos/hello/HelloTl.tlib --assembly demos/hello/bin/Release/net10.0/HelloTl.dll`
  (it writes only the library; `--emit-sources <dir>` would add `Kinds.g.cs` and `Timelines.g.cs`).
- `hello.scene.json`: the same run as a scene: `dotnet run --project tools/Tl.Bake -- run demos/hello/hello.scene.json --steps 150 --assembly demos/hello/bin/Release/net10.0/HelloTl.dll` prints hash `4684325832958264737`.

## What is checked

`tests/Tl.Packs.Tests/HelloTests.cs`: the three changes above, the committed library equal to the JSON bake, and the `tl run` hash.

Full course: https://github.com/IAFahim/tl.unity#readme
