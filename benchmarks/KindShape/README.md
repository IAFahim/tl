# KindShape

Owner-shape kinds on the .NET host through the generated path (issue #652): 500 looping timelines of 30-60 ticks, 16 tracks each (8 lift, 4 locomotion with priorities, 2 damage pulse, 2 stat pulse), blends on tracks 0-3 and 8-9 with a 5-tick overlap, `--live` tracks' worth of ticks covered per track, `--active` playbacks per entity started at staggered ticks. One step is `TimelineWorld.Tick` plus the `Movement` and `Vitals` systems of `tests/Tl.Systems.Tests/KindDomain`. Clip values are integers and priority ties carry equal speeds, so every Combine order gives the same bits and the state hash compares hosts and builds.

```sh
dotnet run --project benchmarks/KindShape -c Release -- --rows 10000,50000 --active 40,100 --live 8 --runs 5
```

`--domain pure` runs the same timelines against `benchmarks/KindShape/PureDomain.cs` (namespace `KindPure`, issue #655): its span feeders read only the frame and their fold, `Apply` takes `WalkStat` and `JumpPower`, and `Locomotion` and `Lift` declare `Associative = true`, so they read precombined span values. Build with `-p:TlBaseline=true` to drop the opt-in (the stage-1 per-span path, and the only form that compiles on commits before #655). `--domain entity` (the default) runs `KindDomain` as before. Either way the store reaches only the chosen domain's kinds, and `spanValueBytes` reports the shared precombined tables.

Defaults: 40 warmup steps (the JIT has tiered up by then, so warm allocation reads 0 B) and 16 measured steps per run.

Each `cell` line prints the median over `--runs` runs of the per-run median step, the run range, ns per active playback, kind table bytes per entity (`kindBytesPerRow`), inline playback bytes per entity, the shared span table, resident anonymous memory per entity (`residentBytesPerRow`: Linux `RssAnon` after warmup minus before setup, first run only; it counts every native and managed allocation, including kind rows that spilled out of their inline capacity, so run one cell per process when you compare it), setup and warm allocation, and the state hash.
