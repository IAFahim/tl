# SingleTimeline — when is the timeline fast, and why

One looping 64-tick timeline, three runners that must produce identical effects:

| runner | what it is | one track | three tracks |
| --- | --- | ---: | ---: |
| direct | hand-written playback; the ceiling | 0.24 ns/tick | 1.41 ns/tick |
| facade + kernel | the shipped path: `tlbake --kernel` code bound by content hash | 6.68 ns/tick | 12.13 ns/tick |
| facade + interpreter | same asset, kernel binding defeated | 7.45 ns/tick | 15.17 ns/tick |

The sample bakes its assets in process (`TimelineBaker.BakeJson`), asserts the kernel lane actually binds (`TimelineKernels.Bound`), asserts all checksums match, and exits nonzero on any failure. The committed `Kernels.g.cs`/`ThreeKernels.g.cs` register the exact content hashes of the in-process bake; if the bake ever drifts, the sample fails loudly.

Run:

```sh
dotnet build -c Release
dotnet run -c Release --no-build -- one
dotnet run -c Release --no-build -- three
```

Measured on i9-14900K, .NET 10.0.12, best of 5 runs x 2,000,000 ticks. Regenerate the kernels with `tlbake <json> <out.tlb> --assembly bin/Release/net10.0/SingleTimeline.dll --kernel <name>.g.cs` after changing the authoring JSON.
