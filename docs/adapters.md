# Frontends and backends

`Tl.Gen.CSharp`, `Tl.Compiler`, and `Tl.Gen.C` are separate projects and packages.

| Package | Responsibility |
|---|---|
| `Tl.Compiler` | Language-neutral immutable plan, validation, normalization, and format contract |
| `Tl.Gen.CSharp` | C# syntax discovery, diagnostics, C# binding, and C# kernel emission |
| `Tl.Gen.C` | C11 binding and kernel emission from `Tl.Compiler` plans |
| `Tl.CSharp` | One-reference C# installation that carries the build-only C# generator |

The packages remain in one repository while the neutral format and conformance fixtures are changing together. Their dependency direction already permits independent versioning and later repository extraction. `Tl.Gen.CSharp` is a sibling of `Tl.Gen.C`; no generic `Tl.Gen` assembly exists.

The C# package currently keeps a private language-specific model for typed slots, constants, includes, hooks, and routes that the neutral format does not yet express. Moving those semantic facts into `Tl.Compiler` is required before the C# and C backends can consume the same complete plan.

Waffle was evaluated and excluded. A template layer does not improve emitted code, runtime speed, plan portability, or output determinism. Each backend writes target source directly from validated immutable data.

Generated files belong to the consuming build output. Normal C# compilation runs the incremental analyzer; explicit export produces inspectable source, a manifest, and a generation report. The generator and Roslyn remain build-time assets and never enter application or NativeAOT output.

A new backend consumes the public versioned `Tl.Compiler` contract, supplies a target binding for operation and payload IDs, rejects unsupported plan features before emission, and passes the shared semantic fixtures on its real toolchain. Backend representation stays private; observable receipts, generated sizes, ABI layout, and compatibility are public evidence.

See [architecture.md](architecture.md), [extending.md](extending.md), and [v1.0-alpha-checklist.md](v1.0-alpha-checklist.md).
