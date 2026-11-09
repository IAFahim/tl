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
