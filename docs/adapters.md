# Frontends and backends

The repository keeps runtime, compiler, language binding, and target emission separate.

| Package | Responsibility |
| --- | --- |
| `Tl.Runtime` | Declaration, frame, flags, state, and total movement ABI |
| `Tl.Compiler` | Language-neutral immutable ordered plan and validation |
| `Tl.Gen.CSharp` | C# discovery, diagnostics, typed binding, .NET query emission, and Unity materialization |
| `Tl.CSharp` | One-reference C# install containing runtime plus build-only generator assets |

The neutral plan owns operation identities and slots, tracks, clips, hooks, regions, ordered occurrences, payload identities, deduplication, and validation. A language binding owns concrete type names, constant expressions, and operation symbols. The C# and Unity emitters consume the same validated ordered plan plus C# binding; they do not derive schedule semantics independently.

Normal C# and supporting IDE builds run the incremental analyzer. `TlGenExport` runs the same frontend and backend when a physical, deterministic source snapshot is needed. Unity uses that export before script compilation so the Entities generator can discover the materialized jobs. Generator, compiler, and Roslyn assemblies never enter .NET application, NativeAOT, Unity runtime, or player output.

The existing C backend lives in a separate repository extracted at commit `3e67333`; it consumed `Tl.Compiler` as a sibling of the C# frontend. Its ABI v2 does not yet support alpha.3 heterogeneous catalogs. A C catalog migration must add a target binding for every required operation and payload while preserving the neutral order; it must never claim to translate arbitrary C# bodies.

Waffle was evaluated and excluded. A template layer does not improve runtime code, plan portability, deterministic output, or target diagnostics. Each backend emits directly from validated immutable data.

A new backend must reject unsupported features before writing output, preserve every observable schedule and movement rule, emit deterministic artifacts, report code and data size, and execute shared semantic fixtures on its real toolchain. Backend representation stays private; ABI and receipts are public.

See the [architecture](architecture.md), [extension contract](extending.md), [alpha.3 API](v1.0-alpha-api.md), and [release qualification](https://github.com/IAFahim/tl/issues/35).
