# Contributing

Read [AGENTS.md](AGENTS.md), [architecture](docs/architecture.md), and [extension contract](docs/extending.md) before changing code. Open an issue for a public ABI, plan-schema, semantic, package-boundary, or benchmark-fixture change so the invariant and migration cost is visible first.

Keep pull requests focused on one observable result. State the trigger, previous behavior, resulting behavior, validation, size delta, and target limitations. Performance work includes exact receipts and a same-machine baseline/candidate comparison. Backend work includes deterministic output, negative validation, artifact sizes, and a real target compiler or runtime receipt.

Run the complete validation block in [AGENTS.md](AGENTS.md) before requesting review. Update public API approval, migration notes, reports, and package identity when those surfaces change. Do not commit generated scratch, credentials, machine-local configuration, or benchmark results without an explicit verification-record path.
