# Unity ECS and Burst

## Install

Add the package from Unity Package Manager with the repository subdirectory URL:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity
```

The .NET `Tl.CSharp` package targets .NET 10 and its build generator runs through MSBuild. NuGetForUnity can copy NuGet assemblies, but it does not run that generator and cannot convert .NET 10/C# 14 code into Unity's C# 9 Burst subset. Unity projects use the native UPM package.

## Compatibility

| Lane | Editor | Entities | Collections | Burst | Status |
| --- | --- | --- | --- | --- | --- |
| Stable floor | 6000.0 | 1.4.3 | resolved by Entities | resolved by Entities | Declared compatibility target; CI hardware lane pending |
| Preview | 6000.7.0a5 (`a15235a53881`) | 6.7.0 | 6.7.0 | 2.0.0 | EditMode, PlayMode, Burst AOT, and Standalone Linux player passed |

Unity documents Entities 1.4.3 as released for Unity 6000.0. The package declares that dependency and uses only the C# 9 unmanaged subset shared by both lanes. Preview validation does not substitute for the pending stable-editor lane.

## Runtime shape

Each generated timeline owns one `ushort` ID, direct forward and backward scalar kernels, generated input and output pointer contexts, and optional immutable blob data for ECS storage and tooling. The hot scalar kernel reads immediate constants and invokes concrete track operations. It does not traverse the blob, allocate, reflect, box, create delegates, or consult a managed registry.

`Playback` is a 12-byte unmanaged value owned by the entity. Generated contexts borrow component fields for one immediate call. Construct and consume them within the same `IJobEntity.Execute` invocation. Never store a context in a component or retain it across structural changes, job scheduling, callbacks, or storage relocation.

`TimelineReport` separates generated UTF-8 source bytes, immediate static data bytes, typed blob bytes, and runtime managed heap bytes. Blob bytes cover the root and array elements; Unity allocator headers and alignment are outside that figure. Persistent `BlobAssetReference<TimelineBlob>` values must be disposed by their owning world or baking artifact.

## Verification

The committed project at `tests/Tl.Unity.Project` consumes only the local UPM package. Its runtime suite executes a generated heterogeneous timeline through a Burst `IJobEntity`, checks exact forward and backward receipts and failure atomicity, proves all contexts are unmanaged, and measures zero managed bytes across 1,048,576 warm public calls. Its editor suite inspects Unity's player compilation graph and every assembly reference for forbidden compiler, generator, and Roslyn assemblies.

```sh
unity --no-banner --format json test tests/Tl.Unity.Project --mode EditMode --output /tmp/tl-unity-editmode.xml --timeout 600
unity --no-banner --format json test tests/Tl.Unity.Project --mode PlayMode --output /tmp/tl-unity-playmode.xml --timeout 600
```

The standalone player gate invokes `TlUnityBuild.Build`, verifies `lib_burst_generated` contains the generated ECS job, scans the player output for forbidden build assemblies, and runs the player until it prints `TL_UNITY_PLAYER_OK`.
