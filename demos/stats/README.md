# Demo E: stats and buffs over time (no engine)

`duel.json` (600 ticks) hits the hero for 12 at ticks 60, 120 and 180, poisons it for 1 every 20 ticks from 240 to 480 and heals
20 at 500. `haste.json` (300 ticks, channel `haste`) adds 10 Attack while it plays. The scene (`stats.scene.json`) starts Duel at
step 0 and Haste at steps 100 and 250; the second start replaces the first on its channel, so the bonus never stacks.

## Run it

```sh
dotnet build packs/Stats -c Release
dotnet run --project tools/Tl.Bake -- run demos/stats/stats.scene.json --steps 600 --checkpoints 50 --show \
  --assembly packs/Stats/bin~/Release/net10.0/Tl.Pack.Stats.dll
```

| Step | Health | Attack (Base + Bonus) | Running hash |
|---|---|---|---|
| 200 | 64 | 20 | 15215298177589687405 |
| 450 | 53 | 20 | 17573985342977318404 |
| 600 | 72 | 10 | 13171269908726092120 |

Health is not clamped by the pack (a clamp cannot be undone, so rewind would drift); a game clamps when it draws.

## What is checked

`tests/Tl.Packs.Tests/SceneRunTests.cs` (the three checkpoints and the final hash) and `packs/Stats/Tests/StatsSceneTests.cs`
(the same scene JSON in a Unity world gives the same checkpoint and final hashes).
