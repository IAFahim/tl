# K0 Unity ECS numbers

## Versions
- UnityEngine.Application.unityVersion = 7000.0.0a12 (e1_headers.cs)
- com.unity.entities 6.7.0 (Packages/manifest.json and packages-lock.json; package.json in cache "version": "6.7.0"); entities.graphics 6.7.0
- Probe compiled into Assembly-CSharp (no asmdef in Assets root). allowUnsafeCode=0, so probes use plain nested structs; eval is also non-unsafe (pointers read via reflection + IntPtr ctor).

## Numbers (every value read from an eval result)
| # | Quantity | Value | Eval file |
|---|---|---|---|
| 1 | 8-byte elem, [Cap(0)] | SizeInChunk 16, ElementSize 8, BufferCapacity 0 | e1_headers.cs |
| 2 | 8-byte, Cap(1) | SizeInChunk 24, Cap 1 | e1_headers.cs |
| 3 | 8-byte, Cap(2) | SizeInChunk 32, Cap 2 | e1_headers.cs |
| 4 | 8-byte, Cap(4) | SizeInChunk 48, Cap 4 | e1_headers.cs |
| 5 | 8-byte, no attribute | SizeInChunk 144, BufferCapacity 16 | e1_headers.cs |
| 6 | 12-byte, Cap(0) | SizeInChunk 16, ElementSize 12, Cap 0 | e1_headers.cs |
| 7 | 12-byte, Cap(1) | SizeInChunk 28, Cap 1 | e1_headers.cs |
| 8 | 12-byte, Cap(2) | SizeInChunk 40, Cap 2 | e1_headers.cs |
| 9 | 12-byte, Cap(4) | SizeInChunk 64, Cap 4 | e1_headers.cs |
| 10 | 12-byte, no attribute | SizeInChunk 136, BufferCapacity 10 | e1_headers.cs |
| 11 | Chunk.kChunkSize / kBufferSize (= kChunkBufferSize) | 16384 / 16320 (public consts via reflection, not inferred) | e2_chunk.cs |
| 12 | Chunk.kMaximumEntitiesPerChunk | 128 | e2_chunk.cs |
| 13 | Entity only (8 B), +4, +8, +16, +32, +56 B pad (totals 8..64) | ChunkCapacity 128 (EntityArchetype.ChunkCapacity == ArchetypeChunk.Capacity) | e2_chunk.cs |
| 14 | total 128 B/entity (Entity + 120 pad) | 127 | e2_chunk.cs |
| 15 | total 256 B/entity | 63 | e2_chunk.cs |
| 16 | total 512 B/entity | 31 | e2_chunk.cs |
| 17 | Largest accepted entity (power-of-two pad combo) | pad 16120, total 16128 incl. Entity, chunk capacity 1 | e2b_max.cs |
| 18 | CreateArchetype rejection | ArgumentException "Archetype too large to fit in chunk. Instance size 16384 bytes.  Maximum chunk size 16320. Components in new archetype: ..." (first reject at total 16132) | e2b_max.cs |
| 19 | Overflow buffer (cap 2 -> 10 adds) | len 10, Capacity 16, heap ptr 0x7f317ad08d00 vs inline 0x7f16b3ac0490 | e3_trim.cs |
| 20 | After RemoveRange to len 1, before Trim | Capacity still 16, ptr still heap | e3_trim.cs |
| 21 | After TrimExcess | Capacity 2, ptr 0x7f16b3ac0490 == original inline ptr (distance 0); sibling inline buffer ptr distance 32 B; value preserved | e3_trim.cs |

Notes / caveats
- Header = 16 bytes (SizeInChunk(cap 0) - 0). SizeInChunk = 16 + cap*elem for caps 1,2,4 (8B: 24/32/48; 12B: 28/40/64). Default capacity = floor(128 / elemSize): 16 for 8 B, 10 for 12 B (128/12), SizeInChunk 16+128 / 16+120.
- Entity sizes are totals including the 8-byte Entity. 128 B total gives 127 = floor(16320/128); 256 -> 63; 512 -> 31. No extra system components appeared in a fresh World created with `new World("K0")`.
- Item 17 boundary is composition dependent: the error reports "Instance size" larger than the sum of component sizes (e.g. 16132 B total reported as 16384 instance size; 16328 total -> 16384), i.e. per-component alignment padding is counted. The bisection over my power-of-two decomposition is therefore not strictly monotonic; 16128 is the largest I verified accepted, and the absolute ceiling is 16320 B (kBufferSize) minus alignment overhead. Not tested with a single large component.
- TrimExcess chunk-address method: ArchetypeChunk exposes only m_Chunk (a ChunkIndex, not a pointer), so the chunk base address was not obtainable. Used the sibling method: an unoverflowed buffer on a sibling entity in the same chunk (same chunk confirmed, Count 2) was 32 B from the trimmed pointer (< 16 KB => inline), and the trimmed pointer equals this entity's original inline pointer exactly.

## Implications for the plan
- Per-buffer-type overhead is a flat 16-byte header in the chunk (before elements); capacity-0 buffers cost 16 B/entity and always live on the heap when non-empty.
- The 128 entities-per-chunk cap still holds in Entities 6.7 (kMaximumEntitiesPerChunk = 128); it binds for entities of <= 127 B total (64 B total still 128). Chunk usable space is 16320 B, so entities above ~127 B lose density (127/63/31 at 128/256/512).
- Default (no attribute) buffer capacity gives 144 B/136 B in-chunk for 8/12 B elements, so an unannotated buffer is as large as a 128 B entity by itself; use explicit [InternalBufferCapacity].
- TrimExcess does return data inline when the length fits the internal capacity (cap goes back to 2, pointer back to the original inline slot); removing elements alone does not (capacity stays 16 on heap).
- Hard ceiling on an entity: about 16 KB (CreateArchetype throws ArgumentException above the 16320 B budget, counting alignment padding).
