# RowOrigins: what each playback layout costs

Two C experiments for IAFahim/tl#673. They measure how fast the kind step can get on this machine, and prove that time-relative row origins keep every playback control bit-exact.

```bash
gcc -O3 -march=native -ffp-contract=off -fno-fast-math -fopenmp -o layouts layouts.c
gcc -O2 -ffp-contract=off -o controls controls.c
./layouts -e 50000 -a 100 -t 12
taskset -c 4 ./layouts -e 50000 -a 100 -t 1
./controls 3000 5000
```

## `layouts.c`

The owner shape: 500 timelines of 30-60 ticks, and one fused 32-byte row per (timeline, tick). Four kinds:
- a priority pick (last slot wins ties);
- a float sum;
- two integer sums, one of them a pulse.

Apply writes `Motion` and `Vitals` from `WalkStat` and `JumpPower`. Every kernel produces the same state hash, floats included.

| Kernel | Slot | What it does each step |
|---|---|---|
| `today-8B-two-pass` | 8 B `Playback`, out of chunk above 4 slots | today's shape: a tick pass (step, then a pulse lookup), then a kind pass (cell lookup, presence bits, scalar `Combine`), then a vitals pass |
| `8B-fused` | 8 B | tick and kinds in one pass |
| `4B-two-pass` / `4B-fused` | 4 B (timeline, tick) | the same two shapes, half the slot bytes |
| `2B-relative-*` | 2 B origin | row = origin + world frame, so stepping writes nothing. One 32 B row read per slot, and a sparse handover for slots whose row carries the end flag |
| `2B-lean-simd` | 2 B origin | as above, with AVX2 monoid lanes: float add, int add and a max of priority with the slot index |
| `2B-read-only-floor` | 2 B origin | rows only, no handover or pick: the read floor |

The model is calibrated: `today-8B-two-pass` at 12 threads lands near the real Unity main numbers (tl.unity#47), at 7.84 ms vs 6.79 ms for 50k × 100 and 0.67 ms vs 0.80 ms for 10k × 40.

### Ryzen 5 8500G, 12 threads (the Unity analogue), ms per step

| Cell | Unity at start (`703fcd2`) | Unity main now | C, today's layout | C, 2 B lean SIMD | C, read floor | #651 spike, Burst |
|---|---:|---:|---:|---:|---:|---:|
| 10k × 40 | 32.1 | 0.800 | 0.671 | **0.103** | 0.055 | 0.104 |
| 10k × 100 | | 1.557 | 1.705 | **0.242** | 0.123 | 0.184 |
| 50k × 40 | | 3.397 | 3.251 | **0.495** | 0.259 | |
| 50k × 100 | 329.8 | 6.794 | 7.842 | **1.232** | 0.646 | 0.754 |
| 1M × 2 | 161.9 | 6.878 | 4.432 | **1.428** | 0.830 | about 10 (264 B per character) |
| 1M × 4 | | 9.045 | 7.764 | **1.821** | 1.059 | 9.45 |

### One Zen4 core (`taskset -c 4`, the .NET analogue), ms per step

| Cell | .NET at start (`e3649e85`) | .NET main now | C, 2 B lean SIMD | C, read floor |
|---|---:|---:|---:|---:|
| 10k × 40 | 219.8 | 3.159 | **0.512** | 0.260 |
| 50k × 100 | 2462.7 | 22.03 | **6.036** | 3.220 |
| 1M × 2 | 936.3 | 14.47 | **7.156** | 3.201 |

The single-core today's-layout kernel is branchier than .NET main (57.7 ms vs 22.0 ms at 50k × 100), so it is not calibrated for .NET. Use only the 2 B rows there.

## `controls.c`

Compares two models every step, bit for bit, through random controls:
- **Reference:** each slot holds an explicit tick, with reverse, paused, rate 2x, alive, loop and `Next` handover.
- **Origin model:** a dense 2-byte origin per slot, in slot order.
  - Paused, rated and stopped slots become tombstones that read a zero row. Their state lives in a side list.
  - Rated copies are combined first, as tl does today.
  - Rows come in two layouts, forward and reversed, so a reverse playback also moves +1 per step.
  - There are two value tables: one for the world stepping forward, one for rewind. Each row carries an end flag for its direction.

Result: 15,000,000 entity-steps bit-exact, through about 90k each of pause, resume, reverse, rate, seek and stop, 58k starts and 55 world rewinds.

Each of these four mutations is caught at once:
- ignoring the rewind table;
- skipping handover;
- a wrong reversed layout;
- dropping slot order in priority ties.
