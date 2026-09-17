# Value pooling for baked frame slots (#149 prototype)

The baked timeline format stores track/clip payload floats inline in every frame slot. This prototype tests issue #149's hypothesis: storing each field's unique authored values once per asset in a tiny pool and referencing them from slots by byte index (ushort beyond 256 unique values) shrinks slots enough to improve scan throughput more than the added index-to-pool indirection costs.

The project is a standalone model; it does not reference `Tl.Core` and is not added to `tl.slnx`.

## Model fidelity

The inline slot mirrors `FrameSlot<TTrack, TClip>` from `src/Tl.Core/Data.cs` field for field with one realistic pair type, track = 2 floats (`Multiplier`, `Bias`), clip = 1 float (`Amount`):

| Slot | Fields | Bytes/slot | Full slots per 64 B line |
|---|---|---:|---:|
| `InlineSlot` (today) | `Track`, `First`, `Second`, 4 window uints, `TrackIndex` | 36 | 1 |
| `PooledByteSlot` | 4 window uints, 4 byte pool indices, `TrackIndex` | 24 | 2 |
| `PooledUshortSlot` | 4 window uints, 4 ushort pool indices, `TrackIndex` | 28 | 2 |

Pools are per-field: `Multipliers`, `Biases`, `Amounts` (First and Second share the Amount pool because it is the same clip field). Byte indexing is only valid while every pool holds at most 256 unique values, so pool size 1000 is measured for inline and pooled-ushort only; that boundary is the width cliff the proposal must survive.

All storage is unmanaged (`NativeMemory.AlignedAlloc`, 64-byte alignment), matching the runtime's unmanaged data-path discipline. The scan reproduces `FrameSlot.ToFrame`'s read shape over a stage program whose slots all contain the fixed query tick 300: 25% of slots have `FactorSpan == 0` (First only), 25% have `FactorSpan == 1` (constant 0.5 blend), and 50% ramp across the window; the consumer arithmetic is `sum += amount * multiplier + bias`. All three variants execute the identical arithmetic order, so outputs are bit-identical.

## Method

- BenchmarkDotNet, in-process emit toolchain, 8 warmup + 12 measurement iterations of 250 ms, MemoryDiagnoser, `JsonExporter.Full`, Median column, `OperationsPerInvoke` equal to the row count so Mean/Median are ns/row.
- Fixtures: deterministic xorshift assignments (fixed seeds), pool sizes 10, 256, 1000, 100k and 1M rows. Every benchmark's `GlobalSetup` re-asserts scan-checksum parity between inline and both pooled variants on the exact measured fixture; `--parity` prints the standalone receipt with checksum bits.
- No best-sample selection, no dead outputs (every benchmark returns a checksum that BenchmarkDotNet consumes), warm managed allocation must read 0 B.

## Reproduction

```sh
export PATH="$HOME/.dotnet:$PATH"
dotnet build benchmarks/ValuePooling/ValuePooling.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/ValuePooling -c Release --no-build -- --parity
dotnet run --project benchmarks/ValuePooling -c Release --no-build -- --run <label>
```

Raw JSON and the per-run README land in `results/<label>/bdn/` and `results/<label>/README.md`.
