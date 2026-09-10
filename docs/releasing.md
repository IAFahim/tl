# Release artifacts

`release-artifacts` and `publish-nuget` are separate manual workflows. Building a GitHub prerelease cannot publish a NuGet package. NuGet publication requires its own dispatch with `license_and_owner_decisions` set to true.

The artifact workflow accepts an existing tag. It checks that the tag is `v` plus the package version and that the checked-out commit is the tag target. It builds with the repository commit and tag ref supplied explicitly to MSBuild, packs every package twice, normalizes ZIP timestamps without recompressing entries, and requires the two normalized package sets to be byte-identical.

The package contract verifies:

- `Tl.Runtime`, `Tl.Gen.CSharp`, `Tl.CSharp`, `Tl.Compiler`, and `Tl.Gen.C` exist at the tag version.
- Every package identifies the exact repository commit and tag ref.
- The package dependency graph and required package paths match the approved graph.
- `Tl.Runtime`, `Tl.Compiler`, and `Tl.Gen.C` have portable symbol packages with embedded source.
- The generator portable PDB with embedded source stays under the build-only `tools` path.
- A clean consumer builds and runs from `Tl.CSharp` alone under the JIT and NativeAOT.
- Generator, compiler, Roslyn, and C backend assemblies do not enter either application output.

The workflow uploads one uncompressed Actions artifact containing the five packages, three symbol packages, a deterministic archive of the package-only NativeAOT smoke executable, the generation report, `RELEASE-MANIFEST.json`, and `SHA256SUMS`. The executable archive preserves its mode through GitHub artifact download. The manifest contains no wall-clock time and orders files by ordinal name.

Run the same pipeline on a branch before tagging:

```sh
eng/release-artifacts --candidate local artifacts/release
```

Run the immutable release pipeline from the tagged checkout:

```sh
eng/release-artifacts --tag v1.0.0-alpha.2 artifacts/release
```

Source is embedded in portable PDBs. A SourceLink package is intentionally absent because embedded source satisfies offline symbol inspection without adding another restore-time dependency. This can change when remote source navigation has a concrete consumer receipt.
