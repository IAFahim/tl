# Tl for Unity

`Tl.Unity` is the C# 9 runtime boundary for materialized Unity ECS and Burst timeline jobs. Add it through Unity Package Manager:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity#v1.0.0-alpha.3
```

The package declares Unity 6000.0 and Entities 1.4.3. The stable generated-jobs receipt uses Unity 6000.0.83f1, Entities 1.4.3, and Burst 1.8.30; `eng/test-unity-stable` recreates its isolated project and exact package inputs. A separate preview receipt uses Unity 6000.7.0a5, Entities 6.7.0, Collections 6.7.0, and Burst 2.0.0.

Keep the C# 9 domain and shared job declarations in `Assets/Timelines/DomainJobs.cs` so Unity compiles them:

```csharp
using Tl;

public readonly struct DamageClip
{
    public readonly int Value;
    public DamageClip(int value) => Value = value;
}
public readonly struct DamageTrack : IBlend<DamageClip>
{
    public void Blend(
        in DamageClip first,
        in DamageClip second,
        float factor,
        out DamageClip result)
        => result = new DamageClip((int)(first.Value + (second.Value - first.Value) * factor));
}
public struct Bias { public int Value; }
public struct Trace { public int Total; }

public readonly struct ApplyDamage : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        in Bias bias,
        ref Trace trace)
        => trace.Total += frame.Clip.Value + bias.Value;
}
```

Keep the builder declarations in `Assets/Timelines/Combat.tl`. The materializer reads this file, while Unity does not import its newer declaration syntax:

```csharp
using Tl;

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageTrack()).Use<ApplyDamage>();
        builder.Clip(damage, new DamageClip(10), 0u, 2u);
    }
}

public readonly struct DamageRows { }

public readonly partial struct Combat : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<DamageRows>().Asset<Attack>();
    }
}
```

Download the matching `Tl.CSharp` and `Tl.Runtime` `.nupkg` files from the GitHub prerelease into `tools/packages`, then create a small .NET 10 authoring project beside the Unity project. `Tl.CSharp` supplies the materializer, resolves the compilation references, and imports the `TlGenExport` target. The local package source is required because alpha.3 is distributed through the GitHub prerelease rather than a public NuGet feed. This project is an authoring tool and does not enter the Unity or player assembly graph:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <Nullable>enable</Nullable>
    <TlGenBackend>unity-entities</TlGenBackend>
    <TlGenOutput>$(MSBuildThisFileDirectory)../Game/Assets/Timelines/Generated</TlGenOutput>
    <RestoreSources>$(MSBuildThisFileDirectory)../tools/packages;https://api.nuget.org/v3/index.json</RestoreSources>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../Game/Assets/Timelines/DomainJobs.cs" />
    <Compile Include="../Game/Assets/Timelines/Combat.tl" />
    <PackageReference Include="Tl.CSharp" Version="1.0.0-alpha.3" />
  </ItemGroup>
</Project>
```

Materialize before Unity script import:

```sh
dotnet msbuild Timeline.Authoring.csproj -restore -t:TlGenExport
```

`TlGenBackend` defaults to `csharp`, and `TlGenOutput` defaults to the project's intermediate `TlGenCompile` directory. The project above selects the Unity backend and writes the deterministic `.g.cs` files and cache/report sidecars directly to the Unity Assets directory.

The generated catalog owns the state, enableable schema marker, logical-slot wrappers, and scheduler. Create an entity with the complete generated schema and drive the scheduler from one external clock:

```csharp
using Unity.Burst;
using Unity.Entities;

public struct TimelineClock : IComponentData
{
    public uint GameTick;
    public int Delta;
}

[BurstCompile]
public partial struct CombatTimelineSystem : ISystem
{
    private Combat.Scheduler _scheduler;

    public void OnCreate(ref SystemState state)
    {
        _scheduler.OnCreate(ref state);
        state.RequireForUpdate<TimelineClock>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var clock = SystemAPI.GetSingleton<TimelineClock>();
        _scheduler.Tick(ref state, clock.GameTick, clock.Delta);
    }
}

public static class TimelineBootstrap
{
    public static Entity CreateAttack(EntityManager manager)
    {
        var entity = manager.CreateEntity(
            typeof(Combat.TimelineComponent),
            typeof(Combat.DamageRows),
            typeof(Combat.Role0Bias),
            typeof(Combat.Role1Trace));
        manager.SetComponentData(entity, new Combat.TimelineComponent
        {
            Value = new Combat.State(Combat.Asset.Attack)
        });
        manager.SetComponentData(entity, new Combat.Role0Bias(new Bias { Value = 2 }));
        manager.SetComponentData(entity, new Combat.Role1Trace(new Trace()));
        return entity;
    }
}
```

The physical `.g.cs` outputs contain immutable timeline data, one shared catalog state component, enableable schema markers, logical-slot component wrappers, Burst-compatible selection, typed operation jobs, and commit scheduling. Operation jobs borrow only the values named by their authored `Execute(in Frame<TTrack,TClip>, in inputs..., ref results...)` signature. Slot wrappers are ordered by parameter name and type, so the example generates `Role0Bias` and `Role1Trace`; two roles with the same value type remain separate ECS columns.

`TimelineState` stores catalog-local asset routing identity, local position, and signed loop cycle. Selection is total for zero, forward, and reverse movement. Finite timelines clamp independently and looping timelines carry cycle and boundary flags. `Frame<TTrack,TClip>` and `TimelineFrame` are call-scoped borrowed values; generated jobs never retain them in scheduled fields.

The runtime/player package contains no Tl compiler, generator, Roslyn assembly, reflection binding, managed registry, or runtime compilation. Authoring and materialization tooling stay outside player assemblies. Generated files are deterministic physical inputs to Unity's Entities source generator and Burst pipeline.

Entities 1.4.3 imports `System.IO.Hashing` from one editor-only source file but omits the assembly dependency. The stable gate injects hash-pinned `System.IO.Hashing` and `System.Runtime.CompilerServices.Unsafe` assemblies into its isolated test project's editor plug-ins. This works around the upstream editor-package defect without adding either assembly to `Tl.Unity` or the player graph; a plain Entities 1.4.3 install on this editor is not turnkey.

The canonical generated ECS sample and its executable receipts live in `tests/Tl.Unity.Project`; sample and test source is not shipped in this package.
