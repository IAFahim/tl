# Current generator path probe

This project is a real `Tl.Gen.CSharp` analyzer consumer. It proves that checkpoint `5198e08` emits `Start`, borrowed `Data`, and eager `TrySeek`, while it emits no `Select` or `Complete` member. `ProposedShape.cs` also compiles the reviewed full-generic track binding, `ITimelineSet`, generated-query analog, and complete two-schema consumer as ordinary C#.

```sh
dotnet run --project experiments/Alpha3/Generator/CurrentPath.csproj -c Release
```

The build writes analyzer output under `obj/generated`. Produce the independent CLI export and byte report with:

```sh
dotnet msbuild experiments/Alpha3/Generator/CurrentPath.csproj \
  -t:TlGenExport -p:Configuration=Release -p:NuGetAudit=false -m:1
```

The CLI output is under `obj/Release/net10.0/TlGenCompile`. Both directories are disposable and untracked.
