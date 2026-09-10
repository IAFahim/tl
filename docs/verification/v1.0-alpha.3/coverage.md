# Alpha.3 coverage evidence

`dotnet-coverage` 18.11.0 collected Release coverage on Linux with strict production-only settings. All 163 tests passed: 36 core, 16 compiler, 62 C# generator, and 49 C generator tests.

```sh
dotnet-coverage collect "dotnet test tl.slnx -c Release --no-build --no-restore -p:NuGetAudit=false -m:1" -f cobertura -s eng/coverage.settings.xml
```

| Assembly | Lines | Branches |
|---|---:|---:|
| `Tl.Core` | 77 / 77 (100%) | 46 / 46 (100%) |
| `Tl.Compiler` | 330 / 330 (100%) | 238 / 238 (100%) |
| `Tl.Gen.CSharp` | 1,095 / 1,150 (95.22%) | 861 / 982 (87.68%) |
| `Tl.Gen.C` | 562 / 567 (99.12%) | 107 / 118 (90.68%) |
| Total | 2,064 / 2,124 (97.18%) | 1,252 / 1,384 (90.46%) |

The retained Cobertura report has SHA-256 `02b51324a6c6b797c667876ea3dd163e1c11e7b25a6b6d9dcd8c3b863cfaf714`. The production-only settings have SHA-256 `ac6f6eaa04abe0f4929219523d0f2c0c204cef68e0ffc4f0ce87696fdaa63aa2`.

The 100% gate is open. Uncovered sequence points and branches remain in:

- `src/Tl.Gen.CSharp/Analysis/JobReader.cs`
- `src/Tl.Gen.CSharp/CompileGenerationCache.cs`
- `src/Tl.Gen.CSharp/JobEmitter.cs`
- `src/Tl.Gen.CSharp/TimelineIncrementalGenerator.cs`
- `src/Tl.Gen.C/CEmitter.cs`

This is a measured release blocker. The candidate does not claim 100% coverage.
