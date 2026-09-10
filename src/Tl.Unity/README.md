# Tl for Unity

`Tl.Unity` is the C# 9 runtime boundary for generated Unity ECS and Burst kernels. Add the package with Unity Package Manager:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity
```

The compatibility floor is Unity 6000.0 with Entities 1.4.3. The validated preview lane is Unity 6000.7.0a5 with Entities 6.7.0, Collections 6.7.0, and Burst 2.0.0.

This alpha ships checked-in C# 9 backend output. Timeline IDs are assigned before emission and must be unique across loaded generated assemblies. The Unity authoring generator and cross-assembly ID allocation remain separate work.

The Burst Combat sample has two track and clip kinds in one timeline. Its checked-in generated output contains pointer contexts, direct scalar kernels, an immutable `BlobAsset`, an `IJobEntity` call site, and byte-accounting metadata.

Generated input and output contexts borrow unmanaged component storage for one immediate call. Create and consume them inside `Execute`. Do not retain them across a structural change, scheduling boundary, callback return, or storage relocation.

The player receives only `Tl.Unity` and `Tl.Unity.Entities` from this package. The compiler, Roslyn frontend, and backend remain build tools. NuGetForUnity does not run the Tl generator and is not a supported installation path.

`TimelineReport.GeneratedSourceBytes` counts the generated kernel and blob-builder source in UTF-8 bytes. `StaticDataBytes` counts immediate track and clip values. `BlobBytes` counts the typed blob root and array elements; Unity allocator headers and alignment are outside that value. `RuntimeHeapBytes` is zero for generated scalar playback. Dispose every persistent blob with its owning world or baking artifact.
