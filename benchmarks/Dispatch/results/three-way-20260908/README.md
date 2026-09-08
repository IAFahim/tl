# Three-way bench — 2026-09-08, sequential, core 4, quiet machine

Protocol: both iutq legs run back-to-back pinned to core 4 after quiet
check; tl numbers are the same-day v0.3 receipts (ladder + sweep + frozen
sanity, same core). Validity: iutq-next's arms reproduce its Sep 6 pilot
(New one-track 50.7 vs 54.7; Frozen 5.6 vs 6.4) - machine and protocol
sound.

## iutq main AT TIP vs its README pins

iutq main (1e11b5f) measured 114-461 ns on its own TimelineBenchmarks
suite; its README pins 3.06-19.44 ns from artifacts produced on
PRE-fast-lookup code (the waffle-lut worktree's copy, base ace6bb9).
Today's two independent measurements agree with each other (home suite
122.35 vs vendored Legacy copy 125.69 on one-track sample) and not with
the pins. Either the fast-lookup rework (eb3ff67/36bbe2c) regressed the
suite ~40x, or the default path changed. The README pins at tip are
stale either way. Needs the iutq owner's eyes.

## Kernel floors (ns, medians)

| operation                  | tl 0.3.0 | iutq main (tip, today) | iutq-next (today) |
|----------------------------|---------:|----------------------:|------------------:|
| one-track sample/tick*    | 16-18    | 115-125               | 51-81             |
| crossfade                  | ~19      | 146-151               | 81                |
| eight cursors              | per-index| 461-536               | 1190 (noisy)      |
| one-tick traverse          | 15-18    | 126                   | 178               |
| setup/bind per use         | bound once| 85                    | prepared once     |
| frozen/baked kernels       | 1.04-1.09| (waffle-lut branch: 0.165 sampling / 1.33 traverse, Sep 6) | 5.6-13 |

*contracts differ: a tl tick carries per-work Enter/Stay/Exit + lifecycle
+ user callback; iutq/iutq-next sample/traverse are stateless queries.
All numbers are kernel floors (pinned P-core, hot caches), not
frame-time predictions.

## Verdict

With today's receipts, tl 0.3.0's dynamic path is the fastest of the
three (16-18 ns with fuller per-tick semantics), ~3x iutq-next's
prepared queries and ~7x iutq-main-at-tip. tl's frozen tier (1.06 ns,
full semantics) is the fastest kernel measured anywhere in this
comparison; iutq's waffle-lut 0.165 ns sampling is faster still but
carries no movement semantics. CAVEAT: against iutq's PRE-fast-lookup
pinned numbers (3-5.5 ns) tl's dynamic path would NOT be fastest - which
is why the tip regression question above matters and is flagged, not
assumed.
