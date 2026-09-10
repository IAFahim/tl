# tl roadmap

## Start here on any machine

Chat history and local worktrees are disposable. The canonical live state is the [Project 6 Workflow view](https://github.com/users/IAFahim/projects/6/views/4), the [v1.0.0-alpha.2 release issue](https://github.com/IAFahim/tl/issues/11), each workstream issue, its linked pull request, and its remote atomic claim.

```sh
git clone https://github.com/IAFahim/tl.git
cd tl
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
cat AGENTS.md
cat plan.md
gh issue view 11 --comments
gh pr list --state open
gh project item-list 6 --owner IAFahim --limit 1000
git ls-remote --heads origin 'refs/heads/workstream-claims/*' 'refs/heads/claims/*' 'refs/heads/issue-transactions/*'
```

Resume only the branch and exact checkpoint recorded by its issue and Project fields. Use `eng/agent-work` to claim it before editing. Put the complete instruction on the issue before delegating; a private agent message needs only the issue reference. Record each changed decision before dependent edits, and publish one validated atom before beginning the next. If work must stop red, publish a recoverable checkpoint with the exact failures.

An issue transaction normally lives only for one helper command. If one remains after confirmed process or machine loss, recover its exact object with `eng/agent-work recover-lock <issue> <exact-transaction> <reason>`. Never recover a transaction while its owner process may still be running.

For a stacked change, pass its published dependency explicitly: `eng/agent-work start <issue> <kind> <scope> <description> origin/<dependency-branch>`. The claim records the resolved commit, so a new machine never guesses which parent was intended.

The repository roadmap records architecture, release gates, recovery procedure, and durable links. It does not duplicate the Project's mutable status columns.

v0.6 is the released baseline. Its implementation, measurements and verification remain in the [archived report](verification/v0.6/plan.md) and [verification record](verification/v0.6/README.md).

v1.0.0-alpha.1 introduced the breaking generated-runtime line: heterogeneous tracks, automatic generation from partial ITimeline declarations, separate borrowed Input/Output contexts, Frame callbacks, explicit Before/After hooks and a runtime-ID hub without manual Bind.

v1.0.0-alpha.2 is the current candidate. It adds one-install C# packaging, deterministic generation and memory reports, the first language-neutral plan and C11 backend, batch alias atomicity, PMU evidence, and durable GitHub coordination. Follow the [implementation plan](../plan.md), [API contract](v1.0-alpha-api.md), [execution checklist](v1.0-alpha-checklist.md), and [release issue](https://github.com/IAFahim/tl/issues/11).

The active breaking API work removes registry lookup from statically named timelines in [#15](https://github.com/IAFahim/tl/issues/15) and replaces destination/span movement with world-clock `Start` plus signed relative simulation seek in [#16](https://github.com/IAFahim/tl/issues/16). Their issue records supersede older API examples until the migration is merged.

The performance target is below 3 ns/tick for hot sequential or batched public workloads on the reference machine. Correctness, ordered effects, ownership, zero warmed allocation and the 250,000-byte source/path cap remain release gates. Random seeking, larger working sets and callback work are reported separately because their physical cost is content-dependent.

Unity ECS integration follows the core ABI rather than changing it. Timeline payloads and playback state are already unmanaged. Burst qualification requires a dedicated package and test matrix because Burst supports a different C# and runtime subset from .NET 10.

The first portability backend is C11. Its contract becomes the native foundation for C++, Rust, engines, and FFI bindings. The current neutral slice carries identity, runtime ID, looping, authored track indices, payload handles, clip windows, and stable operation IDs. Regions, movement facts, typed constant values, slot access, canonical serialization, and shared semantic lowering remain tracked compiler work. C consumers implement named operations; arbitrary C# method bodies are never presented as portable. The C ABI fixes layout, alignment, ownership, status values, and versioning. Cross-endian serialization is not yet defined.

Historical experiments remain evidence for earlier designs and measurements. They do not substitute for measurements of the v1 public API.
