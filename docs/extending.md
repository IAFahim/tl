# Extending tl

Scope note: this document describes the intended extension model. The `Tl.Compiler` neutral plan and `TimelinePlan.CurrentFormatVersion` named below are directional — no `Tl.Compiler` package or `TimelinePlan` type exists in the tree today. The shipped authoring and extension lane is the data-authored C# generator ([data-authored-api.md](data-authored-api.md)).

## Choose the boundary

Use `Tl.Runtime` when an extension only advances generated timelines or provides caller-owned state. Use `Tl.Compiler` when it reads, validates, transforms, visualizes, or emits timeline plans. Build a frontend when a new authoring language must create the neutral plan. Build a backend when a target needs different source, data layout, ABI, or execution code.

Domain libraries such as animation, combat, audio, transactions, networking, or deterministic simulation should publish operation IDs and data schemas. Their runtime behavior stays in target-language bindings. Scheduling and threading packages own when and where playback runs; they do not alter timeline semantics.

## Backend contract

A backend receives an immutable plan and a target binding. It must:

- validate its ABI and target restrictions before writing output;
- preserve authored track order, half-open windows, blend factors, direction, lifecycle, loops, and pre-effect import/schema/configuration validation; user-operation exceptions preserve the already executed effect prefix;
- emit deterministic, culture-independent artifacts;
- report every artifact byte count and target data-layout cost;
- keep unsupported features explicit;
- compile and execute the shared conformance fixtures on the real target toolchain.

A backend may choose basic blocks, tables, rank structures, cursors, shared kernels, or fully folded code according to measured target behavior. Representation is private. Receipts are public.

## Operation IDs

Operation IDs name behavior without embedding a source language. Use stable reverse-domain or package-qualified values once plans cross package boundaries. A C# binding may map an ID to a static operation method, while a C binding maps it to forward and backward symbols. Different implementations are compatible only when their observable receipts match.

## Versioning

The plan schema, runtime ABI, and each backend version independently. A frontend writes its compile-time `TimelinePlan.CurrentFormatVersion` into every plan. Validation preserves that instance value, and a backend compares it with its own supported-format constant before emission. This detects both an older frontend loaded with a newer compiler and a newer plan passed to an older backend. Unknown required features fail before emission. Optional metadata may be ignored only when the plan marks it non-semantic.

The C backend and the Unity host already live in separate repositories, extracted at commit `3e67333` ([issue #64](https://github.com/IAFahim/tl/issues/64)) without the neutral plan. Further backend packages can move to their own repositories after the neutral plan has a released schema, canonical serializer, compatibility matrix, and fixture package. The split is then mechanical: depend on `Tl.Compiler`, import the fixtures, retain package provenance, and run conformance in CI.

## Pull request evidence

An extension contribution includes its package boundary, supported targets, deterministic-output test, negative validation tests, native compiler or runtime receipt, artifact-size report, allocation result where applicable, and unsupported-feature list. Hot-path changes also include an exact baseline/candidate comparison and generated or native assembly evidence.
