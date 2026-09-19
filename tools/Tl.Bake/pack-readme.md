# Tl.Bake

The `tlb` command (package `Tl.Bake`) compiles designer-authored timeline JSON into canonical TLB1 binary assets for the [tl](https://github.com/IAFahim/tl) runtime. The command was named `tlbake` before 1.0.0-alpha.8. The output is TLB1 v3 and the loader accepts v3 only; assets baked by earlier alphas (v1, or the v2 layout shipped through 1.0.0-alpha.8) are rejected at load with a diagnostic, so update the tool and re-bake.

```sh
dotnet tool install --global Tl.Bake --prerelease --add-source <directory containing the Tl.Bake nupkg>
tlb boss.json boss.tlb --assembly MyGame.Domain.dll
tlb boss.json              # output defaults to boss.tlb; assembly discovered from the JSON's types
tlb                        # exactly one *.json in the directory
tlb --json --assembly MyGame.Domain.dll
tlb --watch boss.json boss.tlb --assembly MyGame.Domain.dll
tlb --strip boss.tlb boss.ship.tlb
tlb --report boss.tlb
```

- Deterministic: identical inputs produce byte-identical assets.
- `--assembly` names the assemblies containing the track/clip structs; the
  assembly name must match the assembly that ships those types at runtime
  (pair keys hash assembly-qualified names). Explicit `--assembly` always
  wins and skips discovery.
- Lazy forms: `tlb boss.json` defaults the output to `boss.tlb` beside the
  input and discovers the assembly by sweeping DLLs under the launch
  directory for one defining every `(namespace, type)` pair the JSON
  references; bare `tlb` requires exactly one `*.json` in the directory.
  `tlb.db` in the launch directory caches the last resolution per JSON,
  is machine-local (gitignored), and is rewritten whenever it goes stale.
- `--json` prints the authorable `(track, clip)` pairs the assemblies expose,
  with member names, so authoring tools can fill type lists automatically.
- TLB1 v3 stores each pair's unique track and clip values once in per-pair
  value pools and references them from frame slots by fixed-width `ushort`
  index in 24 B, 8-aligned slot rows (at most 65,535 unique values per pool).
  `--report` audits the split, including `pool/<pair>/track-unique-count`,
  `pool/<pair>/clip-unique-count`, and the totals `pool/unique-count` and
  `pool/value-bytes`.
- `--watch` re-bakes on save: it bakes every input once at start, then
  rebuilds only files whose content hash changed, printing JSON events
  (`ready`, `rebuild`, `skip`, `diagnostic`). A directory input watches every
  `*.json` in it; `--debounce <ms>` (default 100) coalesces editor saves.
- `--cache <dir>`: content-keyed cache; hits preserve timestamps, failures are
  never cached.
- Install the matching `Tl.CSharp` package for the runtime and the build-time
  consumer binding. Unity consumes the `com.iafahim.tl` UPM package from the
  [tl.unity](https://github.com/IAFahim/tl.unity) repository.
