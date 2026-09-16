# Release artifacts

`release-artifacts` and `publish-nuget` are separate manual workflows. Building a GitHub prerelease cannot publish a NuGet package. The publish job runs only through the protected `nuget-production` environment on the owner's explicit dispatch; the MIT license decision is recorded in [issue #64](https://github.com/IAFahim/tl/issues/64) and the 1.0.0-alpha.6 packages are published on nuget.org.

The artifact workflow checks out the fully qualified `refs/tags/<tag>` ref. It rejects a branch with the same short name, checks that the tag is `v` plus the package version, and requires the checked-out commit to be the tag target. It rejects tracked and untracked source changes, performs two isolated restores and builds with the repository commit and tag ref supplied explicitly to MSBuild, normalizes ZIP timestamps without recompressing entries, and requires the two complete package sets to be byte-identical.

The package contract verifies:

- `Tl.Runtime`, `Tl.Gen.CSharp`, `Tl.CSharp`, and `Tl.Bake` exist at the tag version.
- Every package identifies the exact repository commit and tag ref.
- Every package path and dependency group matches the approved graph exactly.
- `Tl.Runtime` has a portable symbol package with embedded source.
- The generator portable PDB with embedded source stays under the build-only `tools` path.
- A clean consumer builds and runs from `Tl.CSharp` alone under the JIT and NativeAOT.
- Generator and Roslyn assemblies do not enter either application output.

The workflow uploads one uncompressed Actions artifact containing the four packages, one symbol package, a deterministic archive of the package-only NativeAOT smoke executable, the generation report, `RELEASE-MANIFEST.json`, and `SHA256SUMS`. The executable archive preserves its mode through GitHub artifact download. The manifest contains no wall-clock time and orders files by ordinal name.

The NuGet workflow rebuilds and verifies that exact tagged input in an unprivileged job. It then uploads one immutable Actions artifact scoped to the workflow run. Only the protected publish job receives an OpenID Connect token. That job downloads the same-run artifact, verifies its checksums, rejects any package outside the fixed four-package and one-symbol-package sets, and pushes each named package explicitly.

Run the same pipeline on a branch before tagging:

```sh
eng/release-artifacts --candidate local artifacts/release
```

Run the immutable release pipeline from the tagged checkout:

```sh
eng/release-artifacts --tag v1.0.0-alpha.7 artifacts/release
```

Source is embedded in portable PDBs. A SourceLink package is intentionally absent because embedded source satisfies offline symbol inspection without adding another restore-time dependency. This can change when remote source navigation has a concrete consumer receipt.
