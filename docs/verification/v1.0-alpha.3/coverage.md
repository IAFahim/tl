# Alpha.3 coverage evidence

`dotnet-coverage` 18.11.0 collected Release coverage on Linux with strict production-only settings. All 133 tests passed: 36 core, 16 compiler, 32 C# generator, and 49 C generator tests.

```sh
dotnet-coverage collect "dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false" -f cobertura -s eng/coverage.settings.xml
```

| Assembly | Lines | Branches |
|---|---:|---:|
| `Tl.Core` | 77 / 77 (100%) | 46 / 46 (100%) |
| `Tl.Compiler` | 330 / 330 (100%) | 238 / 238 (100%) |
| `Tl.Gen.CSharp` | 1,027 / 1,146 (89.62%) | 778 / 980 (79.39%) |
| `Tl.Gen.C` | 562 / 567 (99.12%) | 107 / 118 (90.68%) |
| Total | 1,996 / 2,120 (94.15%) | 1,169 / 1,382 (84.59%) |

The retained Cobertura report has SHA-256 `3706696050f6425815776fc113463a97a84de1b23620cf97d8771a18f7299a10`. The production-only settings have SHA-256 `ac6f6eaa04abe0f4929219523d0f2c0c204cef68e0ffc4f0ce87696fdaa63aa2`.

The 100% gate is open. Uncovered sequence points and branches remain in:

- `src/Tl.Gen.CSharp/Analysis/DeclarationDiagnostic.cs`
- `src/Tl.Gen.CSharp/Analysis/JobReader.cs`
- `src/Tl.Gen.CSharp/CompileGenerationCache.cs`
- `src/Tl.Gen.CSharp/GeneratorCli.cs`
- `src/Tl.Gen.CSharp/JobEmitter.cs`
- `src/Tl.Gen.CSharp/JobTimelinePlanAdapter.cs`
- `src/Tl.Gen.CSharp/Model/Jobs.cs`
- `src/Tl.Gen.CSharp/TimelineIncrementalGenerator.cs`
- `src/Tl.Gen.C/CEmitter.cs`

This is a measured release blocker. The candidate does not claim 100% coverage.
