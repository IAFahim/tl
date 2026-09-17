# Tl.Bake

The `tlb` command (package `Tl.Bake`) compiles designer-authored timeline JSON into canonical TLB1 binary assets for the [tl](https://github.com/IAFahim/tl) runtime. The command was named `tlbake` before 1.0.0-alpha.8.

```sh
dotnet tool install Tl.Bake --global --add-source <directory containing the Tl.Bake nupkg>
tlb boss.json boss.tlb --assembly MyGame.Domain.dll
tlb --watch boss.json boss.tlb --assembly MyGame.Domain.dll
tlb --strip boss.tlb boss.ship.tlb
tlb --report boss.tlb
```

- Deterministic: identical inputs produce byte-identical assets.
- `--assembly` names the assemblies containing the track/clip structs; the
  assembly name must match the assembly that ships those types at runtime
  (pair keys hash assembly-qualified names).
- `--watch` re-bakes on save: it bakes every input once at start, then
  rebuilds only files whose content hash changed, printing JSON events
  (`ready`, `rebuild`, `skip`, `diagnostic`). A directory input watches every
  `*.json` in it; `--debounce <ms>` (default 100) coalesces editor saves.
- `--cache <dir>`: content-keyed cache; hits preserve timestamps, failures are
  never cached.
- Install the matching `Tl.CSharp` package for the runtime and the build-time
  consumer binding. Unity consumes the `com.iafahim.tl` UPM package from the
  [tl.unity](https://github.com/IAFahim/tl.unity) repository.
