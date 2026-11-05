# LiveBatch

Batch playback shapes A-D through the real `Tl.Gen.CSharp` generator on
BenchmarkDotNet. Every consumer below is an `ITrack<TTrack, TClip>` declared in
`Domain.cs`; the analyzer emits the `TlConsumerBinding` thunks, exactly as a
game build does. No hand-written `PairRuntime<,>.Consume` thunks.

Fixtures are duration-97 looping timelines, 8 variants per shape (pattern 0-7),
100,000 rows per benchmark call, ids cycling variants, clocks cycling ticks.

| Arm | Shape | Call |
| --- | ----- | ---- |
| `A_Folded` | A: folded | `Timeline<LiftTrack,LiftClip>.Apply(ids, clocks, fwd, lift)` — Fold-only consumer, one pooled float lane |
| `B_Apply` | B: fold-fed compose, new kernel | `Timeline<ArcTrack,ArcClip>.Apply(ids, clocks, fwd, in columns)` over a caller-assembled `ColumnSet` (height + pushes) |
| `B_PreparePlay` | B via split | `Prepare(ids, columns)` + `Play(state, clocks, fwd)` + `Release` per call |
| `B_Chunk128` | B per-128-chunk | `ColumnSet` Apply in 128-row slices |
| `C_Frame` | C: frame consumer | `Timeline<PushTrack,PushClip>.Apply(ids, clocks, fwd, total)` — `ExecuteActive(in Frame, ref float)`, no Fold |
| `D_FourPairs` | D: four pairs, one system | four `Apply` calls over one four-track timeline |

Shape A carries a real blend window (clips 0-64 and 33-97 on one track, linear
`IBlend`); B blends by first-wins; C and D are full-span clips. Shape D's four
pairs live on one timeline: four tracks, four pair types, one id/clock stream.

## Verdict

P0 baseline receipts for the roadmap 4.10 gates (same machine, net10.0).
The `--verify` parity arm reports exact forward checksums per shape; see the
receipt log.

| Arm | Median @100k rows (Default job) | Alloc |
| --- | ---: | ---: |
| A_Folded | 45.27 us | 0 B |
| B_Apply | 7,636.15 us | 0 B |
| B_PreparePlay | 7,568.64 us | 0 B |
| B_Chunk128 | 7,729.26 us | 0 B |
| C_Frame | 83.98 us | 0 B |
| D_FourPairs | 313.98 us | 0 B |

Medians agree with the InProcess cross-check rows (44.00 us / 7,622.03 us /
7,617.84 us / 7,692.65 us / 85.20 us / 320.99 us). The Default job is the
primary receipt: clean child process, zero managed bytes on every arm. The
InProcess rows attribute 11-26 B/op to the B arms and 2 B/op to D; the
`--verify` warm loop (`GC.GetAllocatedBytesForCurrentThread` over 128 runs)
retains 0 B on all six arms, so the InProcess bytes are harness attribution,
not benchmark allocation.

PMU (this machine, P-core, `perf stat`, user counters only):

| Shape | ns/row (perf window) | cycles/row | IPC | branches/row | miss rate |
| --- | ---: | ---: | ---: | ---: | ---: |
| a | 0.443 | 2.5 | 3.08 | 0.85 | 0.064% |
| b-apply | 75.8 | 491 | 3.40 | 248 | 0.015% |

The perf-window ns/row cross-checks the BenchmarkDotNet medians (0.453 and
76.4 ns/row).

Forward checksums (8,192 rows x 97 frames, from the receipt log):
A `DBE54FE28E8A19F8`, B `CF64E5BE9CEE2D77`, C `6AB48FD27BDE2D77`,
D `B0903A4A34400000`.

## Reproduction

```sh
dotnet run --project benchmarks/LiveBatch -c Release -- --verify
dotnet run --project benchmarks/LiveBatch -c Release -- --artifacts results/<stamp>
```

PMU counters, where available:

```sh
taskset -c <P-core> perf stat -e cycles,instructions,branches,branch-misses,cache-references,cache-misses -- \
  ./bin/Release/net10.0/LiveBatch --counters b-apply --counters-delay 6000
```

Shapes: `a`, `b-apply`, `b-prepareplay`, `b-chunk128`, `c`, `d`.

## Receipts

- `results/20261105-000208/` — raw BenchmarkDotNet JSON (`JsonExporter.Full`,
  CSV, HTML, GitHub markdown) for the six arms on this machine, the console
  log with the parity checksums (`parity.log`), and PMU captures
  (`pmu-a.txt`/`.perf.txt`, `pmu-b-apply.txt`/`.perf.txt`).
