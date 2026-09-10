# Tl for Unity

`Tl.Unity` is the C# 9 runtime boundary for checked-in Unity ECS and Burst kernels. Add it through Unity Package Manager:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity
```

The package declares Unity 6000.0 and Entities 1.4.3. Unity 6000.7.0a5 with Entities 6.7.0, Collections 6.7.0, and Burst 2.0.0 passes the local EditMode, PlayMode, Burst AOT, and Standalone Linux player gates. The stable 6000.0 editor was unavailable and must be tested separately.

Generated timeline facades expose `Start(gameTick)`, one signed `TrySeek(ref data, delta)`, and `TryStop`. Playback is a 16-byte unmanaged value with a signed position and external game-tick anchor. Multi-frame deltas replay every crossed frame. Direction and independent start, interior, end, completion, and loop facts reach one track operation through `Frame<TTrack,TClip>`.

Generated data structs borrow typed component storage for one synchronous call. Create and consume them inside `IJobEntity.Execute`; do not retain them across callbacks, structural changes, scheduling boundaries, or storage relocation. Ordered operations preserve live aliasing.

The package contains no runtime authoring API, managed registry, Tl compiler, Tl generator, or Roslyn assembly. The checked-in kernels use immediate constants and direct calls. Optional `TimelineBlob` data mirrors neutral track and clip records for ECS storage and tooling and is absent from the hot path.

`TimelineReport.GeneratedSourceBytes` counts generated UTF-8 source. `StaticDataBytes` counts immediate track and clip values. `BlobBytes` counts the typed root and array elements without allocator headers. `RuntimeHeapBytes` is zero. Dispose persistent blob assets with their owning world or baking artifact.
