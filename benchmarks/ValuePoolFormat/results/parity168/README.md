# Slot row 24 B receipts (issue #168)

Branch `perf/168-slot-row-24`, base `59b4972` (origin/main). The v2 32 B occurrence row repacked to 24 B as format Version 3: every field width is unchanged, `trackIndex` moves from byte 24 into the former reserved `u16` (byte 6), the trailing 8 B of padding drop, and stride/offset alignment moves 16 -> 8. `BakeCacheKey.ToolVersion` bumped `tlbake-bake-v3` -> `tlbake-bake-v4`.

## Playback parity

- `pre.txt` — captured with the BASE build (`59b4972`, v2 32 B rows) over `../parity-corpus/`.
- `post.txt` — identical harness, CANDIDATE build (v3 24 B rows).
- `corpus-sha256 41E0941888C208827DF12770ACCCE99D0D6D68D88FADC5A08C2EFB4F801ABEA7` for both files: 49 authored documents, 23 bake cleanly and replay bit-identically (lane effect tables forward/backward, per-position typed query frames for all 12 corpus pair types, IEEE float bits), and the 26 negative diagnostics pin identical messages. `ParityCheck compare` prints `PASS assets=23 sha256=41E09418...`.

The corpus was regenerated from the post-rename `tests/tlb_cli` fixture paths (`extract_corpus.py` updated; document contents identical to the pre-rename extraction, so only two document names changed). The #155 receipts in `../parity/` predate that rename.

## Baked sizes

- `sizes-pre.txt` (base build) / `sizes-post.txt` (candidate build) / `sizes.csv` — per-asset total bytes.

Every asset shrinks by exactly 8 B per occurrence row; nothing else in the image changes (header, pair table, pools, programs, metadata tail are byte-identical layouts). Corpus total 12,141 -> 11,573 B (-568 B, -4.7%); per-asset deltas range -8 B (single-occurrence assets) to -104 B (13-occurrence asset). The metadata tail dominates small assets, so the hot-region share of the saving is larger than the total-byte share.

## Loader compatibility

Version 2 images (1.0.0-alpha.8, 32 B rows, 16-aligned strides) still load: the loader relocates `trackIndex` from byte 24 to byte 6 inside its private block copy so the read path stays single-layout, and `tlb report`/`tlb strip` accept v2 through the same gate. Covered by `LegacyVersionTwoAssetsLoadAndPlayIdentically` (query frames and measured lane tables bit-compare v2 vs v3 images of the same authored data).

## Timing

Warm playback is receipted in `../../../FusedAdvance/results/168-slot-row-24/` and `../../../PairHandles/results/168-slot-row-24/` (same machine, interleaved two-round A/B; neutral to slightly favorable, no regression). The per-tick query path reads slot rows every tick, so it was A/B'd here too: `../168ab-base*/` vs `../168ab-cand*/` (Intel Core i9-14900K, .NET 10.0.12; `168ab-*` unmarked = full run with LaneApply, `-r2`/`-r3` = QueryScan-only interleaved rounds). 100,000 ticks per op; median of per-run medians over three rounds:

| QueryScan | v2 (us) | v3 (us) | delta |
|---|---:|---:|---:|
| DualAlpha | 318.9 | 307.0 | -3.7% |
| BlendWindow | 374.3 | 385.6 | +3.0% |
| MixedProgram | 875.4 | 882.4 | +0.8% |

Between-round spread on the SAME build reaches ±5% on these long scans (e.g. candidate DualAlpha 306/306/334 us), so all three deltas sit inside the run-to-run noise band with no consistent direction across pair types; no receipt shows a repeatable regression.

## Repro

```sh
export PATH="$HOME/.dotnet:$PATH"
python3 benchmarks/ValuePoolFormat/ParityCheck/extract_corpus.py
dotnet build benchmarks/ValuePoolFormat/ParityCheck/ParityCheck.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/ValuePoolFormat/ParityCheck -c Release --no-build -- compare benchmarks/ValuePoolFormat/results/parity-corpus benchmarks/ValuePoolFormat/results/parity168/pre.txt
PARITY_SIZES=1 dotnet run --project benchmarks/ValuePoolFormat/ParityCheck -c Release --no-build -- capture benchmarks/ValuePoolFormat/results/parity-corpus benchmarks/ValuePoolFormat/results/parity168/sizes-post.txt
```

`pre.txt` is the frozen base-build receipt; a base checkout at `59b4972` regenerates it.
