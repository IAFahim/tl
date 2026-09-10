# Alpha.3 coverage evidence

`dotnet-coverage` 18.11.0 collected Release coverage on Linux. All 129 tests passed: 35 core, 13 compiler, 32 C# generator, and 49 C generator tests.

```sh
dotnet-coverage collect "dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false" -f cobertura
```

| Assembly | Lines | Branches |
|---|---:|---:|
| `Tl.Core` | 94.81% | 97.83% |
| `Tl.Compiler` | 93.64% | 90.34% |
| `Tl.Gen.CSharp` | 88.54% | 78.86% |
| `Tl.Gen.C` | 99.12% | 90.68% |
| Complete report, including tests | 94.98% | 84.00% |

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
- generated `CompileManifestJsonContext` serializer sources
- `src/Tl.Gen.C/CEmitter.cs`

This is a measured release blocker. The candidate does not claim 100% coverage.
