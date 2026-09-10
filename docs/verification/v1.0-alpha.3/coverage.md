# Alpha.3 coverage evidence

`dotnet-coverage` 18.11.0 collected Release coverage on Linux with strict production-only settings. All 172 tests passed: 36 core, 16 compiler, 62 C# generator, and 58 C generator tests.

```sh
dotnet-coverage collect "dotnet test tl.slnx -c Release --no-build --no-restore -p:NuGetAudit=false -m:1" -f cobertura -s eng/coverage.settings.xml
```

| Assembly | Lines | Branches |
|---|---:|---:|
| `Tl.Core` | 77 / 77 (100%) | 46 / 46 (100%) |
| `Tl.Compiler` | 330 / 330 (100%) | 238 / 238 (100%) |
| `Tl.Gen.CSharp` | 1,095 / 1,150 (95.22%) | 861 / 982 (87.68%) |
| `Tl.Gen.C` | 565 / 565 (100%) | 116 / 116 (100%) |
| Total | 2,067 / 2,122 (97.41%) | 1,261 / 1,382 (91.24%) |

The retained Cobertura report has SHA-256 `65527594f84ec168614b0620965af9b764544cf93f3e7a4424eced4747b719ac`. The production-only settings have SHA-256 `ac6f6eaa04abe0f4929219523d0f2c0c204cef68e0ffc4f0ce87696fdaa63aa2`.

The 100% gate is open. Uncovered sequence points and branches remain in:

- `src/Tl.Gen.CSharp/Analysis/JobReader.cs`
- `src/Tl.Gen.CSharp/CompileGenerationCache.cs`
- `src/Tl.Gen.CSharp/JobEmitter.cs`
- `src/Tl.Gen.CSharp/TimelineIncrementalGenerator.cs`

This is a measured release blocker. The candidate does not claim 100% coverage.
