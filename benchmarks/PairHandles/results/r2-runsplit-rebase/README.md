# r2-runsplit-rebase: kept-atom receipts on the rebased head

Main moved to fafd77c (#187 shared DomainBaker, #188 README showcase) after the
atom was validated; the three workstream commits were rebased onto it (no
conflicts; the kept change touches only TimelineSet.cs, the pair-lane test
receipts, and the PairHandles harness). Full core gate re-run on the rebased
head: tests 572/572, parity PASS, Alpha receipts 0 B, samples/Mixed, --verify,
NativeAOT publish, budget 188,819/300,000.

Medians, us per 100k-row pass, same session:

| Shape | Default | InProcess | Allocated |
|---|---:|---:|---:|
| LaneUniform (gold) | 18.23 | 18.32 | 0 B |
| PairOne | 18.28 | 18.06 | 0 B |
| PairRuns8 | 20.17 | 20.39 | 0 B |
| PairAlternating8 | 81.73 | 79.58 | 0 B |
| PairRuns8Waves | 17.19 | 17.16 | 0 B |
| PairBlocks8Waves | 19.24 | 20.24 | 0 B |
| PairAlternating8Waves | 66.77 | 62.67 | 0 B |

The kept-atom result reproduces on the rebased head: PairRuns8 holds at
~1.10-1.11x gold InProcess (baseline 1.73x), untouched shapes inside their
committed envelopes.
