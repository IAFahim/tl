# tlbake

`tlbake` compiles designer-authored timeline JSON into canonical TLB1 binary assets for the [tl](https://github.com/IAFahim/tl) runtime.

```sh
dotnet tool install tlbake --add-source <directory containing tl.tools.nupkg>
tlbake boss.json boss.tlb --assembly MyGame.Domain.dll
tlbake boss.json boss.tlb --assembly MyGame.Domain.dll --kernel Kernels.g.cs
tlbake --strip boss.tlb boss.ship.tlb
tlbake --report boss.tlb
```

- Deterministic: identical inputs produce byte-identical assets.
- `--assembly` names the assemblies containing the track/clip structs; the
  assembly name must match the assembly that ships those types at runtime
  (pair keys hash assembly-qualified names).
- `--cache <dir>`: content-keyed cache; hits preserve timestamps, failures are
  never cached.
- Install the matching `Tl.CSharp` package for the runtime and the build-time
  consumer binding. Unity consumes the `com.iafahim.tl` UPM package from the
  [tl.unity](https://github.com/IAFahim/tl.unity) repository.
