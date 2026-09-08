# todo

## PICK UP HERE (written 2026-09-08 ~12:15, machine quiet)

### v0.3 region-stable Tracks — nearly done, needs: sweep results → docs → merge

Branch `v0.3-tracks` in worktree `/home/i/GitHub/tl-v03`, based on main `c55fdf3`.
Two commits, tree green (build + full `--verify`/`--verify-edges` incl. the
new `indexed/sliced views` battery):

- `3e8632a` v0.3 core: WorkSlot + thin Tracks + engine region materialization
- `f499444` v0.3 receipts: indexed/sliced view battery (agent died silently
  before its perf gates; reviewer verified tree green post-mortem)

Ladder receipts DONE (core 4, TrackViewDecomp, medians, ns/tick):

| step              | v0.2 Jit | v0.3 Jit | v0.2 NoTiering | v0.3 NoTiering |
|-------------------|---------:|---------:|---------------:|---------------:|
| Calls (floor)     |    11.60 |    10.44 |          18.32 |          17.18 |
| Walk              |    20.85 |    15.09 |          39.46 |          16.75 |
| IndexRead         |    26.13 |    17.03 |          28.47 |          23.76 |
| StateRead         |    25.12 |    18.26 |          26.91 |          23.72 |
| ClipRead          |    26.92 |    18.09 |          29.53 |          30.18 |
| ClipReadBlend     |    28.06 |    19.16 |          32.85 |          32.05 |

Headlines: full consumption −33% (target ≤18 hit), blends −32% (≤20 hit),
floor −10%, NoTiering traversal cliff GONE (39.46 → 16.75). Honest misses:
Walk ≤13 and IndexRead ≤16 stretch targets by 2.1/1.0 ns (residual ≈
1.2 ns/work thin-TrackWork construction + bounds checks; disasm diagnosis
optional). NoTiering ClipRead flat (+0.65) — note in docs.

REMAINING, in order:
1. Sweep — a run was in flight at write time (ApiShape + CursorShape seq +
   BlendShape, core 4). If its numbers are already in docs/benchmarks.md
   below, skip. Otherwise re-run, machine QUIET (no builds elsewhere):
   ```sh
   cd /home/i/GitHub/tl-v03/benchmarks/Dispatch
   taskset -c 4 dotnet run -c Release --no-build -- --filter '*ApiShape*'
   taskset -c 4 dotnet run -c Release --no-build -- --filter '*CursorShape*.Sequential=True*'
   taskset -c 4 dotnet run -c Release --no-build -- --filter '*BlendShape*'
   ```
   References (v0.2 same-session A/B, core 6): PlaybackSingle 23.91/35.19,
   PlaybackParamsFour 19.83/26.94, PlaybackBackwardSingle 23.28/34.47,
   HubDispatch 26.37/36.50, cursor-512-seq scan 31.03 / cursor 13.56,
   blend stack 47.13/219 (cliff), buffer 35.30/212, zero-blend 31.56/49.72.
   Expect: consuming arms DOWN (that's the point), NoTiering blend cliff
   improved or gone. Investigate any arm regressing >10%.
2. Docs: append "v0.3: region-stable materialization" to docs/benchmarks.md
   (ladder table above + sweep + honest readings incl. the two stretch
   misses and the flat NoTiering ClipRead).
3. Merge: `git merge v0.3-tracks` into main, re-run
   `dotnet run -c Release --no-build -- --verify` + `--verify-edges`,
   `dotnet test tests/Tl.Core.Tests`, build tl.slnx, push.
4. Clean up: `git worktree remove /home/i/GitHub/tl-v03`, delete branch
   after merge.

### After v0.3 lands — three-way bench (tl vs iutq vs iutq-next)

FIRST ATTEMPT INVALID: iutq legs ran during an agent build storm and
measured 120-134 ns where the pinned quiet receipts are 3.06-5.55 ns.
Protocol: machine quiet, runs SEQUENTIAL, `taskset -c 4`, same day:
1. `cd /home/i/GitHub/iutq/bench && taskset -c 4 dotnet run -c Release -p:SkipFixtureGeneration --project Iutq.Bench -- --filter '*TimelineBenchmarks*'`
2. `/home/i/Work/iutq-next/engine/bench/Iutq.Bench` — `*PreparedQueryBenchmarks*`
3. tl: `*TrackViewDecomp*` + `*ApiShape*` + `*Frozen*` on merged v0.3 main.
Compare on equivalent operations (one-track sample, crossfade, eight
cursors, one-tick traverse, setup) with contract caveats: tl ticks carry
per-work movement + lifecycle the query engines don't; label all numbers
kernel floors, not frame-time predictions. iutq-next's own campaign
fixtures measured "Legacy" iutq at 95-130 ns (vs 3-5.5 home) — absolute
cross-suite numbers are fixture-bound; only same-fixture comparisons count.

### Then: src/Tl.Core mirror of v0.3

Port thin Tracks/WorkSlot/materialization to `src/Tl.Core` (Tracks.cs,
Internal/Playback.cs), port tests + PublicApi approvals, Tl.Aot smoke.
v0.2 mirror is the pattern (two-agent split worked: src agent + sandbox
agent on separate worktrees/branches, disjoint files).

## Frozen bridge — build-time baking of game timelines (THE plan, not built)

Goal: hit build → timelines authored in game code get baked into real code
by Waffle. Runtime sees "already generated", plays the baked kernel, never
builds tables. Timeline frozen forever; only input/result data changes at
runtime. `.Build(...)` stays the dynamic fallback.

Three pieces, none built (1 ns kernel itself proven — bake v3 receipts in
docs/benchmarks.md):

1. **Discovery** — generator must see frozen timelines at build time. A C#
   lambda can only be baked if pure constants, so frozen timelines must be
   declared generator-readable (data file — the shape `Tl.Gen`'s plan model
   aimed at — or attributed static tables). Runtime-valued authoring cannot
   be baked, by logic.
2. **User code in the kernel** — generator emits calls to the consumer's
   `IForward`/`IBackward` (their `TResult`), not the fixed `FrozenSink`.
3. **Seamless runtime linkage** — baked kernels register into the same
   index registry; `Timeline.Forward(index, ...)` works unchanged: baked
   index → 1 ns kernel, `.Build` index → interpreter.

Why a build-time CLI, not a Roslyn source generator: baking per-tick
floats requires EXECUTING the authoring; source generators only see
syntax. (Same reason iutq's frozen generator reads baked binary blobs.)
Waffle package = the template renderer inside that CLI; bake v3's frozen
kernels actually emit via plain raw strings (Waffle's re-indent broke
byte-identity) — if the bridge lands without Waffle load-bearing, drop
the package.

## Standing notes

- Background agents have died silently twice (power cut; unknown at
  11:56). Liveness check: agent transcript file mtime + `ps aux | grep
  dotnet` + worktree `git status`. If dead: verify tree green yourself,
  commit, continue by hand or relaunch.
- `gh` API unreachable from this machine (timeouts) — CI unverifiable
  remotely; every push is gated by local suites first.
- Historical receipts: docs/benchmarks.md (dispatch, algorithms, frozen
  path, bake v1/v2/v3, AOT, skew DROP, faster-queue results,
  TrackViewDecomp). Product: `src/Tl.Core` = Tl.Runtime 0.2.0.
