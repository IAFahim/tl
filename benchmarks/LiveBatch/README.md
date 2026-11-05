# LiveBatch (retired)

Retired post-cutover (#541): the harness measured the old 3.x batch path
(`Timeline.Apply`/`Prepare`/`Play`/`ColumnSet`, removed with Appendix A) and
cannot run against the 4.0 systems API. Its job — the old-vs-new gate
baseline — is done and recorded; the receipts below are kept as the
committed evidence. Follow-up work lives with P0 under issue #544.

Receipts: `results/p0-baseline/` (BenchmarkDotNet reports, parity + PMU logs).
Verdict at retirement: A 45.27 us, B_Apply 7,636.15 us, B_PreparePlay
7,568.64 us, B_Chunk128 7,729.26 us, C 83.98 us, D 320.99 us medians at
100k rows, 0 B allocated; forward checksums A `DBE54FE28E8A19F8`,
B `CF64E5BE9CEE2D77`, C `6AB48FD27BDE2D77`, D `B0903A4A34400000`.
See the receipt log for the full tables.
