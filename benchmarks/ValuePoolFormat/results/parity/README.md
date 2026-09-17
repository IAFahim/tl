# Value pool format receipts (issue #155)

Branch `feat/155-ushort-value-pools`, base `7f5934e` (perf/148-fused-advance). The baked format pools every unique authored struct value per pair type and slots reference pool entries by fixed-width `ushort` index. Receipts in this folder:

- `parity/pre.txt` — playback receipt captured with the PRE-CHANGE build (base `7f5934e`) over `../parity-corpus/` (49 authored documents: 13 standalone fixtures/presets/samples and 36 embedded authoring docs; 23 bake cleanly and are replayed, 26 are negative diagnostics whose messages are also pinned).
- `parity/post.txt` — identical harness, POST-CHANGE build. `corpus-sha256 49BFF273E0EE2FDB78F99074F2A0AF7E7EB8FE62205E033045B0BBE2A7D5B013` for both files: playback is bit-identical (IEEE float bits) across the whole corpus.
- `sizes-post.txt` / `sizes.csv` — per-asset baked size, pre-change (from the #149 survey receipts) vs post-change.

Per document the harness records: lane effect tables forward and backward (`L <pair> F/B`, one hex word per IEEE `float` bit pattern, over all positions 0..duration) and per-position typed query frames for all 12 corpus pair types (`Q <tick> <pair> <flags> <trackIndex> <clipLength> <withinClip> <trackBits> <clipBits>`, bits as hex). Diagnostics record the exact message (`B re <message>`).

Repro:

```sh
export PATH="$HOME/.dotnet:$PATH"
python3 benchmarks/ValuePoolFormat/ParityCheck/extract_corpus.py
dotnet build benchmarks/ValuePoolFormat/ParityCheck/ParityCheck.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/ValuePoolFormat/ParityCheck -c Release --no-build -- compare benchmarks/ValuePoolFormat/results/parity-corpus benchmarks/ValuePoolFormat/results/parity/pre.txt
PARITY_SIZES=1 dotnet run --project benchmarks/ValuePoolFormat/ParityCheck -c Release --no-build -- capture benchmarks/ValuePoolFormat/results/parity-corpus benchmarks/ValuePoolFormat/results/parity/sizes-post.txt
```

`compare` prints `PASS assets=23 sha256=49BFF273...` and exits 0 when the post-change build reproduces the frozen pre-change receipt exactly.

`sizes.csv` compares per-asset `tlb/total-bytes` pre-change (from the #149 survey bakes) and post-change (this harness). Baked assets grow +46..+95 B each (+2163 B over the 23 valid assets, ~+21.7%): the uniform 32 B slot row, the 64 B header, 48 B pair entries, and the pool tables all add hot bytes, and this corpus's few unique values never amortize. This is the size trade the owner accepted for #155 (the #149 survey projected +44..+136 B); the identity metadata tail remains strippable. Residual per-asset tail differences also reflect assembly-name length: the survey baked against `Tl.Bake.Tests.dll`, this harness against `ParityCheck`.
