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

## Add a sideways move

Goal: while the player jumps 5 forward, it also moves 2 to the side, so the position goes from `(0, 0, 0)` to `(5, 0, -2)`. This is five small changes in `Teleport.cs` and one line in your system. Each change gives you one thing. Skip one and Tl still compiles, but that part stays 0 and nothing tells you why.

**1. Carry both numbers together.** Gives you: one move with two parts, so one kind and one write, not two of each.

```diff
+    public struct Move
+    {
+        public float Forward;
+        public float Side;
+    }
```

**2. Let each clip say how far to the side.** Gives you: a `Side` value on every clip, set per clip in your timeline (negative goes the other way).

```diff
     public struct TeleportClip
     {
         public float Distance;
         public bool ComeBack;
+        public float Side;
     }
```

**3. Make the track send a `Move`.** Gives you: the jump at the start of a clip and, when `ComeBack` is on, the same move undone at the end. `frame.Direction` makes rewinding work for free.

```diff
-        public static float JumpAtClipStart(in TlFrame<TeleportClip> frame)
-            => frame.Direction * frame.Clip.Distance;
+        public static Move JumpAtClipStart(in TlFrame<TeleportClip> frame)
+            => new Move { Forward = frame.Direction * frame.Clip.Distance, Side = frame.Direction * frame.Clip.Side };

-        public static float ReturnAtClipEnd(in TlFrame<TeleportClip> frame)
-            => frame.Clip.ComeBack ? -frame.Direction * frame.Clip.Distance : 0f;
+        public static Move ReturnAtClipEnd(in TlFrame<TeleportClip> frame)
+            => frame.Clip.ComeBack
+                ? new Move { Forward = -frame.Direction * frame.Clip.Distance, Side = -frame.Direction * frame.Clip.Side }
+                : default;
```

**4. Let the kind add both parts.** Gives you: the one place that writes into the column. Because both parts ride in one `Move`, you do not need a second kind.

```diff
-        public static void Apply(in float jumpDistance, ref Teleported player)
-            => player.Offset += jumpDistance;
+        public static void Apply(in Move move, ref Teleported player)
+        {
+            player.Offset += move.Forward;
+            player.Side += move.Side;
+        }
```

**5. Give the column a `Side`.** Gives you: the number your game reads.

```diff
     public partial struct Teleported
     {
         public float Offset;
+        public float Side;
     }
```

**Then, in your own system** (the one that moves the player), read it next to `Offset`. Gives you: the player actually moves sideways.

```diff
 playerTransform.ValueRW.Position.x = teleported.ValueRO.Offset;
+playerTransform.ValueRW.Position.z = teleported.ValueRO.Side;
```

Set `Side` on the clips in your timeline: `-2` on the first clip and `3` on the second (in JSON: `"Side": -2`). The result:

```
tick  position (x, y, z)
  30  (5, 0, -2)       clip 1 starts: 5 forward, 2 to the side
  90  (13, 0, 1)       clip 2 starts: 8 forward and 3 to the other side
 119  (5, 0, -2)       clip 2 ends with Come Back on: its move is undone
```

**If the side stays 0,** one of these is missing: the track still returns the old `float` (step 3), `Apply` does not write `Side` (step 4), your system does not read `Side`, or the clip's `Side` is still 0 in the timeline. The complete version, with the timeline JSON and a test that checks the output above, is `demos/hello-side` (`dotnet run --project demos/hello-side -c Release`).

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

`tests/Tl.Packs.Tests/HelloSideTests.cs`: the sideways-move version in `demos/hello-side` prints `(5, 0, -2)`, `(13, 0, 1)`, `(5, 0, -2)`.

Full course: https://github.com/IAFahim/tl.unity#readme
