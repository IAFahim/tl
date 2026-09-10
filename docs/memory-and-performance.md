# Memory and performance

## What is measured

`tl` reports source generation, runtime data, and execution separately because they occupy different resources.

| Quantity | Meaning |
| --- | --- |
| Generated source bytes | UTF-8 bytes written into the consuming project's intermediate directory |
| Static data bytes | Exact target `sizeof` sum for emitted authored track and clip values |
| Shared dispatch value bytes | Declared generated module and schema-map storage before runtime object padding |
| Registry retained bytes | Actual unmanaged pages and 32-bit metadata allocated by the process registry |
| Managed allocation | Bytes allocated by warm playback on the calling thread and by BenchmarkDotNet |
| Managed assembly bytes | Built IL and metadata file size |
| Native binary bytes | Published NativeAOT or target-native artifact size |
| JIT native code bytes | Tier-specific machine code for a selected method |

The generated `TlGenCompile.report.txt` records source artifacts and per-timeline shape. Generated timelines expose `TrackCount`, `ClipCount`, `RegionCount`, and `StaticDataBytes`. `Timeline.RegistryRetainedBytes` reports actual registry allocation.

## Registry bound

On a 64-bit process, the sparse registry retains one 2,048-byte root page and one 2,048-byte leaf page for each populated group of 256 IDs. Timeline duration, loop bit, module, and ordinal are encoded in the pointer-sized slot, so there is no per-timeline unmanaged metadata allocation.

For `N > 0` IDs whose populated page count is `P`, the registry bound is:

```text
2048 + 2048P bytes, where 1 <= P <= 256
```

Sequential registration gives `P = ceil(N / 256)`. All 65,536 IDs therefore require 526,336 registry bytes on a 64-bit process. A 32-bit process additionally allocates one explicit metadata record per timeline and reports its actual total through the same counter.

Compiled definitions intentionally live for the registry lifetime. This is bounded immutable storage, not per-frame growth. Future unloading would require safe reclamation and generation-aware handles; adding `Free` alone would make concurrent readers unsafe.

## Warm playback

The current exact-schema C# path retains no frame, input, output, or playback reference and allocates 0 B after warmup. Generated contexts are ref structs holding borrowed references to caller-owned storage. Static payload values are emitted once. Public scalar operations perform ownership/lifecycle validation, position normalization, a specialized region block, direct operations, and playback reconstruction.

The reference-machine sequential results are below the 3 ns gate. Random seeking is slower because region selection is data dependent. Batch operations amortize validation and route selection and are reported as throughput.

Linux PMU measurements show negligible branch-miss rates on the sequential fixtures. This means branch hints are not a useful blanket optimization. The retained lifecycle-mask change was accepted because it removes about one branch per tick, preserves exact receipts, and improves the complete public benchmark. [Raw alpha.2 performance evidence](../benchmarks/Alpha/results/v1.0.0-alpha.2/README.md) records the exact source, environment, generated hashes, JIT assembly, counters, and three BenchmarkDotNet processes.

## Unity borrowed pointers

The Unity backend takes addresses only from typed `in` and `ref` component fields supplied to one `IJobEntity.Execute` invocation. A generated context is created, consumed by one synchronous scalar call, and discarded before `Execute` returns. It is never stored in a component, blob, static field, closure, callback, job value, or returned playback. No structural change or scheduling boundary may occur while the context exists.

Every pointer retains its source type and is dereferenced only as that type. Unity owns the alignment of the typed component reference; the generated kernel performs no byte reinterpretation, pointer arithmetic, or packed access. Blob arrays use Unity's typed `BlobBuilder` allocation and are read through their declared element types.

Generated kernels preserve authored operation order and make no no-alias assumption. Writes through an output alias are visible to later ordered operations. The caller must serialize concurrent writes to the same storage. ECS dependency tracking supplies that exclusion for queried writable components, while generated constants and blob assets remain immutable and safe for concurrent reads. Persistent blobs belong to the creating world or baking artifact and must be disposed with that owner.

## No-spike policy

Playback code must remain free of managed allocation, blocking locks, lazy initialization, dynamic code generation, runtime compilation, and unbounded work. Registration and source generation are cold paths and report their costs separately. A scheduler or threading extension must preallocate queues and state, define backpressure, and prove that its synchronization does not enter the generated scalar kernel.

The release gate covers BenchmarkDotNet allocation, explicit current-thread allocation receipts, forced compacting GC through managed aliases, concurrent immutable playback, registry capacity, module capacity, NativeAOT execution, generated C compilation, and process-lifetime registry accounting. Platform backends add target-specific leak and allocator instrumentation.
