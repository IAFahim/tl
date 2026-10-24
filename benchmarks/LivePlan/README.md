# LivePlan

ColumnSet playback (`Timeline.Apply` over a fold-fed dispatch consumer) keeps a
per-shape live plan per closed pair type so a timeline id change no longer
re-runs consumer key thunks, rebuilds fold feed ranges, or scans the ColumnSet
for column bindings.

## Verdict

#486: four immutable native plan slots keyed by ColumnSet shape hold the
header (epoch, set count, column bound, memo count, chain length), the set
keys, the column bindings, and the fold feed indexes. A warm id change
refreshes memo lanes from the folded view and walks the pair chain once to
detect registry growth; bindings run only when a shape first binds or the
chain grows. The alternating-id arms — one id change per row — improve 25%
(5,062 -> 3,804 us and 5,096 -> 3,804 us median at 100k rows); run-grouped and
single-id arms hold at parity. A per-id `Volatile.Read` of the registry epoch
was measured at ~20 ns per id change (5,877 us) and is replaced by the
data-dependent chain walk. The parity arm (alternating batch vs per-row
singles, 8,192 rows) reports zero mismatches on both main and this change.

## Reproduction

```sh
cd LivePlan
dotnet run -c Release -- --verify
dotnet run -c Release -- --artifacts results/486-fixed
```

Baseline receipts under results were produced the same way against unmodified
main at 7ae98870 in a clean checkout of this repository.

## Receipts

- `results/486-baseline` — unmodified main 7ae98870, five shapes, raw
  BenchmarkDotNet JSON (InProcess job rows; the manual-config job cannot spawn
  child processes in the sandbox and reports NA).
- `results/486-fixed` — this change at the #486 head, same host, same shapes.

| Shape | main 7ae98870 | live plans | delta |
| --- | ---: | ---: | ---: |
| ColumnOne | 993.2 us | 968.4 us | -2.5% |
| ColumnRuns8 | 971.6 us | 955.6 us | -1.6% |
| ColumnAlternating8 | 5,062.1 us | 3,804.2 us | -24.9% |
| ColumnRuns8Waves | 961.5 us | 926.4 us | -3.7% |
| ColumnAlternating8Waves | 5,096.2 us | 3,803.8 us | -25.4% |
