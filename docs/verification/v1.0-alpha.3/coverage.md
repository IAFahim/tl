# Alpha.3 coverage evidence

Microsoft `dotnet-coverage` 18.11.0 collected Release coverage with the production-only filters in `eng/coverage.settings.xml` at commit `9b1807686b9035b19a63133cd4f416ab1721e29b`.

```sh
dotnet-coverage collect "dotnet test tl.slnx -c Release --no-build --no-restore -p:NuGetAudit=false -m:1" -f cobertura -s eng/coverage.settings.xml -o /tmp/tl-alpha3-coverage.xml
```

All 221 tests passed: 36 core, 16 compiler, 58 C backend, and 111 C# generator tests.

| Assembly | Lines | Branches |
| --- | ---: | ---: |
| `Tl.Core` | 100% | 100% |
| `Tl.Compiler` | 100% | 100% |
| `Tl.Gen.C` | 100% | 100% |
| `Tl.Gen.CSharp` | 100% | 100% |
| Total | 2,740 / 2,740 (100%) | 1,628 / 1,628 (100%) |

Cobertura SHA-256: `ddf400d7341d8f92359d2d6ba250aadc22550f7aab069c628d5131f40243d544`.

Settings SHA-256: `ac6f6eaa04abe0f4929219523d0f2c0c204cef68e0ffc4f0ce87696fdaa63aa2`.

The final receipts close Unity backend selection, no-catalog CLI failure, cross-asset operation-slot consistency, multi-region emission, and one-frame blend factors. Structurally impossible branches were removed only where earlier validated reader invariants make the alternate state unrepresentable. No production assembly, source path, line, branch, or file is excluded to raise the result.

Coverage is one release receipt. It does not prove semantic correctness, concurrency, allocation, determinism, package contents, NativeAOT, Unity/Burst compatibility, or performance; those gates retain independent oracles and artifacts.
