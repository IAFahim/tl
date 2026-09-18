# r2-packed-recheck: reproducibility receipt for the packed RunEndTwo dead end

Pristine rebuild (interleave stashed, whole TimelineSet at HEAD) measured PairBlocks8Waves 20.1/19.9 us in the same minutes-window; interleave restored on top of the split cascade measured 202.0/196.3 us (Raw JSON under results/). Confirms the packed-run-detection pathology is tied to the interleave build and reproduces across builds; see r2-runsplit README and the issue dead-end table.
