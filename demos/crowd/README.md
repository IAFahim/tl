# Demo H: a crowd of 625 (no engine)

`dance.json` (120 ticks, loops) is the Unity Crowd demo's dance on the Motion pack: two hops (`Move`, ticks 0-30 and 60-90), one spin
(`Spin`, 30-60) and a red-to-yellow glow (`Tint`, the whole loop). `crowd.scene.json` puts 625 dancers on a 25 x 25 grid; each starts
the dance at its own tick, `(x * 7 + z * 11) % 120`, so the field ripples. At step 300 the whole world runs backward for 120 steps, then
forward again. `make_crowd.py` writes `dance.json`, `tl.project.json` and the scene.

## Run it

```sh
dotnet build packs/Motion -c Release
dotnet run --project tools/Tl.Bake -- run demos/crowd/crowd.scene.json --steps 600 --checkpoints 150 \
  --assembly packs/Motion/bin~/Release/net10.0/Tl.Pack.Motion.dll
```

| Step | Running hash |
|---|---|
| 150 | 18310795104460496812 |
| 300 | 8426876472808293853 |
| 450 | 16975299878586844118 |
| 600 | 18108985316199147936 |

## What this shows, and what it does not

Every dancer is one playback; the hash covers all 625 playbacks and their `Pose` and `Tint` after every step, so one wrong dancer anywhere
changes it. The same JSON gives the same hashes in Unity (Burst jobs), where `packs/Motion/Tests/MotionSceneTests.cs` runs it against
`crowd.tlib`, the library `tl bake demos/crowd/tl.project.json` writes (a test keeps the two equal).

Motion sets values (a position, a yaw) instead of adding to them, so a rewind cannot restore a value from before a clip: after the
backward run the crowd does not return to the hashes of the same ticks going forward. That is how the pack is written, not a Tl
limit (the Inventory demo, which adds and removes, rewinds to an empty bag). The scene keeps the rewind so both hosts are checked on the
backward path too.

For speed (ns per entity at 10,000 and 100,000 dancers) see the Unity Crowd demo.
