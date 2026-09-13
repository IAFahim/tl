# Frontends and backends

The repository keeps runtime, consumer binding, and target emission separate.

| Package | Responsibility |
| --- | --- |
| `Tl.Runtime` | Declaration, frame, flags, state, and total movement ABI |
| `Tl.Gen.CSharp` | C# consumer-pair discovery, diagnostics, typed binding, and Unity source materialization |
| `Tl.CSharp` | One-reference C# install containing runtime plus build-only generator assets |

Baked assets own type identity, timing, windows, and authored order; `Tl.Core` validates and imports them and owns selection and ordered execution. The generator owns consumer discovery and typed operation binding; its emitters never derive schedule semantics.

Normal C# and supporting IDE builds run the incremental analyzer. `TlGenExport` runs the same frontend and backend when a physical, deterministic source snapshot is needed. Unity uses that export before script compilation so the Entities generator can discover the materialized jobs. Generator and Roslyn assemblies never enter .NET application, NativeAOT, Unity runtime, or player output.

The existing C backend lives in a separate repository extracted at commit `3e67333`. Its ABI v2 does not yet support data-authored assets. A C catalog migration must add a target binding for every required operation and payload while preserving the authored order; it must never claim to translate arbitrary C# bodies.

Waffle was evaluated and excluded. A template layer does not improve runtime code, plan portability, deterministic output, or target diagnostics. Each backend emits directly from validated immutable data.

A new backend must reject unsupported features before writing output, preserve every observable schedule and movement rule, emit deterministic artifacts, report code and data size, and execute shared semantic fixtures on its real toolchain. Backend representation stays private; ABI and receipts are public.

See the [architecture](architecture.md), [extension contract](extending.md), and [release qualification](https://github.com/IAFahim/tl/issues/35).
