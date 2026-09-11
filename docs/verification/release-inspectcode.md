# Release Inspect Code

JetBrains Inspect Code 2026.2.1 analyzed the generated Release tree with .NET SDK 10.0.401 and runtime 10.0.12.

```sh
/home/i/.dotnet/tools/jb inspectcode tl.slnx --no-build --no-updates --jobs=1 --severity=WARNING --format=Text --properties=Configuration=Release --caches-home=/tmp/tl-release-inspect-cache --output=docs/verification/release-inspectcode.txt
```

The retained [text report](release-inspectcode.txt) contains no findings.

Five analysis settings have narrow scopes:

- `*.g.cs` files are marked as generated because they are deterministic compiler output and are verified through emitter, generated-runtime, checksum, and NativeAOT receipts.
- `UnusedTypeParameter` is disabled only in `src/Tl.Core/Compiled.cs`. `TrackRef<TTrack>` and the `Before<THook>`, `After<THook>`, and `Include<TTimeline>` builder methods carry compile-time identity in generic arguments and constraints. Removing those parameters changes the typed authoring contract; adding runtime storage or work solely to reference them changes the zero-size marker behavior.
- `UnusedAutoPropertyAccessor` is disabled only for the BenchmarkDotNet shape parameter whose setter BenchmarkDotNet invokes through reflection.
- `CheckNamespace` is disabled only for the two `netstandard2.0` `IsExternalInit` polyfills, which must declare `System.Runtime.CompilerServices`.
- `NotResolvedInText` is disabled only where `ArgumentException.ParamName` intentionally names the validated `plan` argument. A test asserts that public exception contract.

Project `RootNamespace` values describe the intentional source layout. All other WARNING-severity findings remain enabled.
