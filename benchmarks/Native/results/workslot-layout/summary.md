# WorkSlot layout verification

The internal field order changed; the constructor, values, work ordering and public API did not. A layout test confirms 24 bytes and offsets 0,4,8,12,16,18,20,22. The previous layout was 28 bytes, giving a 4-byte (14.3%) saving per retained work slot without bit packing or unpacking.

A serial BenchmarkDotNet comparison used the existing NativeVsManaged harness, selecting NativeSingle and NativeParamsFour on logical CPU 4. Both setups checked exact managed/native receipts before measurement. Each method returns the result receipt; each invocation performs 65,536 ticks.

| Case | 28-byte slots, median ns/tick | 24-byte slots, median ns/tick |
|---|---:|---:|
| NativeSingle | 17.1635 | 16.8559 |
| NativeParamsFour | 14.0876 | 13.8681 |

Both cases reported zero managed bytes per operation. This is a check against an obvious performance regression, not a claim of a portable 1–2% speedup. The accepted result is the deterministic storage saving. Full reports and logs are retained in baseline/ and candidate/. NuGetAudit=false was scoped to the offline commands.
