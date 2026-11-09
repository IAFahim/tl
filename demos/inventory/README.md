# Demo D: inventory over time (no engine)

A bag fills from one 600-tick timeline (`loot_run.json`): Give clips add items, a Take clip removes a potion, a Craft clip turns
4 Wood into 2 Plank (consume at its start, produce at its end), and two Pickup markers add 10 Coin and 1 Gem. The scene
(`inventory.scene.json`) plays it for 450 steps, rewinds 450 steps (the bag is empty again: every pulse is signed by the playback
direction), starts it again at step 900 and runs 600 more.

## Run it

```sh
dotnet build packs/Inventory -c Release
dotnet run --project tools/Tl.Bake -- run demos/inventory/inventory.scene.json --steps 1500 --checkpoints 300 --show \
  --assembly packs/Inventory/bin~/Release/net10.0/Tl.Pack.Inventory.dll
```

```
step 900 hash=17985338506880947930
  player Bag: Wood 0, Stone 0, Plank 0, Potion 0, Sword 0, Coin 0, Key 0, Gem 0
...
step 1500 hash=6806057278687917896
  player Bag: Wood 1, Stone 3, Plank 2, Potion 1, Sword 1, Coin 10, Key 0, Gem 1
hash=6806057278687917896 steps=1500 entities=1
```

`tl run` bakes `tl.project.json` in memory (the result is byte-identical to the Inventory pack's pinned `inventory.tlib`), loads
the pack dll, runs its generated system and prints the state hash (`docs/json-format.md`, "Scene").

## What is checked

`tests/Tl.Packs.Tests/SceneRunTests.cs`: empty bag after the rewind, the final bag above, hash 6806057278687917896 on every run.
`packs/Inventory/Tests/InventorySceneTests.cs` runs the same scene JSON in a Unity world (Burst jobs) and expects the same hash.

## Speed

`inventory-fast.scene.json` plays the same timeline at 2x: 300 steps end with the demo bag. `inventory-mixed.scene.json` changes the speed three times (2x, then 0.5x at step 100, then 1.5x at step 260) and 700 steps end with the same bag. Hashes 17812265086550969911 and 15172252175353637725 are pinned in `SceneRunTests` and in the Unity pack test, which runs the same JSON in a Unity world.
