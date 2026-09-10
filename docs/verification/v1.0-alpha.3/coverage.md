# Alpha.3 coverage evidence

`dotnet-coverage` 18.11.0 collected Release coverage on Linux with strict production-only settings. All 129 tests passed: 35 core, 13 compiler, 32 C# generator, and 49 C generator tests.

```sh
dotnet-coverage collect "dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false" -f cobertura -s eng/coverage.settings.xml
```

| Assembly | Lines | Branches |
|---|---:|---:|
| `Tl.Core` | 73 / 77 (94.81%) | 45 / 46 (97.83%) |
| `Tl.Compiler` | 309 / 330 (93.64%) | 215 / 238 (90.34%) |
| `Tl.Gen.CSharp` | 1,027 / 1,146 (89.62%) | 778 / 980 (79.39%) |
| `Tl.Gen.C` | 562 / 567 (99.12%) | 107 / 118 (90.68%) |
| Total | 1,971 / 2,120 (92.97%) | 1,145 / 1,382 (82.85%) |

The retained Cobertura report has SHA-256 `0be8e3120fbfcb5e1a178ac22ff4b32639792e97ce126d6733324b030c61e16d`. The production-only settings have SHA-256 `ac6f6eaa04abe0f4929219523d0f2c0c204cef68e0ffc4f0ce87696fdaa63aa2`.

The 100% gate is open. Uncovered sequence points and branches remain in:

- `src/Tl.Core/Compiled.cs`
- `src/Tl.Compiler/OrderedTimelinePlan.cs`
- `src/Tl.Compiler/TimelinePlan.cs`
- `src/Tl.Compiler/ValidatedOrderedTimelinePlan.cs`
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
