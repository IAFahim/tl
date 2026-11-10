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

The sample's `foreach` has no braces, so add them or the new line will not compile:

```diff
 foreach (var (playerTransform, teleported) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Teleported>>())
+{
     playerTransform.ValueRW.Position.x = teleported.ValueRO.Offset;
+    playerTransform.ValueRW.Position.z = teleported.ValueRO.Side;
+}
```

Set `Side` on the clips in your timeline: `-2` on the first clip and `3` on the second (in JSON: `"Side": -2`). The result:

```
tick  position (x, y, z)
  30  (5, 0, -2)       clip 1 starts: 5 forward, 2 to the side
  90  (13, 0, 1)       clip 2 starts: 8 forward and 3 to the other side
 119  (5, 0, -2)       clip 2 ends with Come Back on: its move is undone
```

In the Hello Tl scene the cube starts at y = 0.5, so there you will see `(5, 0.5, -2)`. Only x and z are changed.

**If the side stays 0,** one of these is missing: the track still returns the old `float` (step 3), `Apply` does not write `Side` (step 4), your system does not read `Side`, or the clip's `Side` is still 0 in the timeline. The complete version, with the timeline JSON and a test that checks the output above, is `demos/hello-side` (`dotnet run --project demos/hello-side -c Release`).

## Reusing a player's slot

In this console host a player is a row number. `StopAll(row)` on a player that is in the middle of a clip fires that clip's End pulse (flagged `Interrupted`) during the next `Tick`, so a clip with Come Back on "comes back" as it is cut short. If a new player already sits in that row in the same step, the come-back lands on the new player. Tested with 100 players and two rounds of destroy and respawn: respawning in the same step gave 6 wrong players, respawning one step later gave 0.

So stop the old player in one step, and reset the column and `Start` the new player after that step's `Tick` (it then starts a step later). In Unity a new entity is a new row, and a destroy-and-respawn in the same frame did not leak (tested). See issue #594.

## An ability window: "can double jump now"

A clip can switch a flag on the player for as long as it plays. Tl only keeps the flag; your own system makes the decision (`if (abilities.ValueRO.CanDoubleJump && jumpPressed)`).

```csharp
using Tl;

namespace HelloAbility
{
    [TlClip("Hello/DoubleJumpWindow")]
    public struct DoubleJumpWindowClip
    {
        public bool CanDoubleJump;
    }

    public struct DoubleJumpWindowTrack : ITlTrack<DoubleJumpWindowClip>
    {
        [Feeds(typeof(DoubleJumpKind))]
        public static bool WhetherDoubleJumpIsAllowed(in TlFrame<DoubleJumpWindowClip> frame)
            => frame.Clip.CanDoubleJump;
    }

    [TlKind]
    public struct DoubleJumpKind
    {
        public static void Combine(ref bool total, in bool next) => total |= next;

        public static void Apply(in bool allowed, ref Abilities player)
            => player.CanDoubleJump = allowed;
    }

    [TlState]
    public partial struct Abilities
    {
        public bool CanDoubleJump;
    }
}
```

Where each thing lives: the clips (when the window starts and ends, and its value) are in the baked timeline, shared by everyone who plays it. The flag itself is the `CanDoubleJump` field of the player's own `Abilities` column. A `[Feeds]` method runs every tick while its clip plays and its result is combined (`Combine`, here OR) and written once by `Apply`.

The timeline has two clips: ticks 30 to 60 with `CanDoubleJump` ticked, and ticks 60 to 150 with it unticked. Result:

```
step  can double jump
   0  False
  30  True
  60  False
a player who never plays the timeline: False
```

- Before the window the flag is false, because a column starts at its default (zero). Nothing is "baked as false"; it is computed every tick.
- Keep the unticked clip. A column keeps the last value it was given when no clip covers a tick, so without the "off" clip the flag would stay true after the window. A kind can declare `Idle(ref Abilities player) => player.CanDoubleJump = false` to reset it on the plain .NET host, but Unity currently ignores `Idle` silently (IAFahim/tl#590), so the off clip is the form that works on both.
- In Unity a player needs the timeline (a Playable Director) to get the `Abilities` column at all; a player without one is never matched by a query for it.

The runnable, tested version is `demos/hello-ability` (`dotnet run --project demos/hello-ability -c Release`).

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

`tests/Tl.Packs.Tests/HelloAbilityTests.cs`: the flag in `demos/hello-ability` turns true at tick 30 and false at tick 60, and stays false for a player who never plays the timeline.

Full course: https://github.com/IAFahim/tl.unity#readme
