# NuGet quick start

The whole library, from the public registry, in four commands. This folder is the runnable form of the repository README's [Quick Start](../../README.md#3-author-and-bake-with-tlbake), and CI runs it on every build against the **published** packages — nothing here references the repository sources.

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.5        # runtime + build-time consumer binding
dotnet tool install --global Tl.Bake --version 1.0.0-alpha.5 # baker; the command is `tlbake`
dotnet build -c Release
tlbake boss.json boss.tlb --assembly bin/Release/net10.0/NuGetQuickStart.dll
dotnet run -c Release --no-build
```

Output:

```
flawless: health=80 position=2
```

What just happened:

- `Domain.cs` declares the domain: a `DamageTrack`/`DamageClip` pair and what one active frame does (`ApplyDamage` writes into borrowed `Health` storage).
- `dotnet build` runs the `Tl.CSharp` source generator, which binds `ApplyDamage` into the assembly — no registration code anywhere.
- `boss.json` is the designer-authored timeline (one damage clip, ticks 0–1, 2.0 multiplier).
- `tlbake` validates and bakes it into a canonical TLB1 binary asset, resolving type names against the compiled `NuGetQuickStart.dll` (the assembly name must match the one that ships those types at runtime).
- `Program.cs` loads the baked bytes, ticks one entity two frames forward at game tick 200,000, and the generator-emitted binding applies 5 × 2.0 damage per crossed frame: 100 − 20 = 80.

Optional fast path — bake a generated C# kernel and compile it into your assembly:

```sh
tlbake boss.json boss.tlb --assembly bin/Release/net10.0/NuGetQuickStart.dll --kernel Kernels.g.cs
dotnet build -c Release   # Kernels.g.cs is picked up by the SDK glob; the runtime binds it by content hash
```

The Unity version of this walkthrough lives in the [tl.unity end-to-end guide](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md).
