# Alpha.3 coverage evidence

Microsoft `dotnet-coverage` 18.11.0 collected Release coverage with the production-only filters in `eng/coverage.settings.xml` at commit `849b73b109fd58d2819d2be6070821ccba46d168`.

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

Cobertura SHA-256: `23be4fff49a17d34c588cb6347271f94a9c553e9a2cc0095f61ca92d9f10af31`.

Settings SHA-256: `ac6f6eaa04abe0f4929219523d0f2c0c204cef68e0ffc4f0ce87696fdaa63aa2`.

The final receipts close Unity backend selection, no-catalog CLI failure, cross-asset operation-slot consistency, multi-region emission, and one-frame blend factors. Structurally impossible branches were removed only where earlier validated reader invariants make the alternate state unrepresentable. No production assembly, source path, line, branch, or file is excluded to raise the result.

Coverage is one release receipt. It does not prove semantic correctness, concurrency, allocation, determinism, package contents, NativeAOT, Unity/Burst compatibility, or performance; those gates retain independent oracles and artifacts.
