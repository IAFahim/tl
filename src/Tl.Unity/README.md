# Tl for Unity

`Tl.Unity` is the C# 9 runtime boundary for materialized Unity ECS and Burst timeline jobs. Add it through Unity Package Manager:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity
```

The package declares Unity 6000.0 and Entities 1.4.3. The current generated-jobs receipt uses Unity 6000.7.0a5, Entities 6.7.0, Collections 6.7.0, and Burst 2.0.0. That preview receipt does not qualify the declared stable package range.

Author timelines and catalogs with the shared declarations:

```csharp
public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack()).Use<DamageJob>();
        builder.Clip(damage, new DamageClip(10), 0u, 3u);
    }
}

public readonly partial struct Combat : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
        => builder.Schema<DamageRows>().Asset<Attack>();
}
```

Run the Tl C# materializer with `--backend unity-entities` before Unity imports scripts. The physical `.g.cs` outputs contain immutable timeline data, one shared catalog state component, enableable schema markers, logical-slot component wrappers, Burst-compatible selection, typed operation jobs, and commit scheduling. A system owns the external game clock, initializes the generated catalog scheduler in `OnCreate`, and calls its `Tick`; operation jobs borrow only the values named by their authored `Execute(in Frame<TTrack,TClip>, in inputs..., ref results...)` signature. Slot wrappers are keyed by parameter name and value type, so two roles with the same value type remain separate ECS columns.

`TimelineState` stores stable asset identity, local position, and signed loop cycle. Selection is total for zero, forward, and reverse movement. Finite timelines clamp independently and looping timelines carry cycle and boundary flags. `Frame<TTrack,TClip>` and `TimelineFrame` are call-scoped borrowed values; generated jobs never retain them in scheduled fields.

The runtime/player package contains no Tl compiler, generator, Roslyn assembly, reflection binding, managed registry, or runtime compilation. Authoring and materialization tooling stay outside player assemblies. Generated files are deterministic physical inputs to Unity's Entities source generator and Burst pipeline.

The canonical generated ECS sample and its executable receipts live in `tests/Tl.Unity.Project`; sample and test source is not shipped in this package.
