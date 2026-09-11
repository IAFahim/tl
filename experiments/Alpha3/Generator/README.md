# Current generator path probe

This project is a real `Tl.Gen.CSharp` analyzer consumer. It proves that checkpoint `5198e08` emits `Start`, borrowed `Data`, and eager `TrySeek`, while it emits no `Select` or `Complete` member. `ProposedShape.cs` compiles the reviewed .NET settings-first track binding and named `ITimelineCatalog` schemas. `GeneratedSurfacePlaceholder.cs` is a handwritten compile-only stand-in for the future generated query and its complete two-schema consumer; the current generator does not emit it. Its .NET 10/C# scaffolding is not a Unity-language qualification receipt.

```sh
dotnet run --project experiments/Alpha3/Generator/CurrentPath.csproj -c Release
```

The compiler rejects a job paired with the wrong clip type:

```sh
dotnet build experiments/Alpha3/Generator/CurrentPath.csproj -c Release \
  -p:DefineConstants=INVALID_MAPPING -p:NuGetAudit=false -m:1
# expected: CS0315 at the OtherClip call
```

The build writes analyzer output under `obj/generated`. Produce the independent CLI export and byte report with:

```sh
dotnet msbuild experiments/Alpha3/Generator/CurrentPath.csproj \
  -t:TlGenExport -p:Configuration=Release -p:NuGetAudit=false -m:1
```

The CLI output is under `obj/Release/net10.0/TlGenCompile`. Both directories are disposable and untracked.
