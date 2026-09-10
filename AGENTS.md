# Repository rules

These rules apply to every human and automated contributor. More specific `AGENTS.md` files may strengthen them for a subtree but may not weaken the invariants below.

## Work coordination

GitHub Issues and the repository Project are the source of truth for all non-trivial work. `plan.md` defines architecture and release gates; it is not a shared mutable task queue.

Agent chats and local worktrees are disposable. A machine must be able to lose power after any published checkpoint without losing the task, its reasoning, or its recovery path.

- Put the complete task specification on GitHub before delegating it. The issue or a workstream comment contains the problem, invariants, owned files, dependencies, acceptance receipts, current checkpoint, material risks, and exact next atom.
- A private agent prompt contains only the issue URL or number and the instruction to read and claim its recorded workstream. Do not place unique requirements, design reasoning, or handoff state only in chat.
- Before acting, read this file, `plan.md`, `docs/roadmap.md`, the issue and its comments, linked issues and pull requests, Project 6 fields, and the remote atomic claim. If those sources do not define the work completely, update the issue before editing.
- Post a changed decision, scope, dependency, or next atom to the issue before dependent work begins. An agent's final response links the durable issue comment and remote commit; it is never the only report.
- Keep atoms short. Validate, commit, push, and record one atom before starting another. Prefer green checkpoints. If an atom cannot become green in the current session, push a clearly labeled recoverable checkpoint and record every known failing command; do not start the next atom from an unpublished or unexplained state.

- Create or select an issue before editing. The issue contains the problem, constraints, acceptance receipts, affected boundaries, and dependencies.
- Apply the `ai` label to AI-executed work plus one `area:*` and one `kind:*` label. Add the issue to the active GitHub Project.
- Claim a bounded workstream by posting its machine, agent, branch, worktree, owned files, and owned receipts. One issue may contain several independent workstreams and pull requests. Every workstream has its own atomic claim, branch, worktree, and non-overlapping ownership. Split work into a child issue when it has an independent outcome, acceptance criteria, or release decision.
- Branches use `<kind>/<issue>-<scope>`, where kind is `feat`, `fix`, `perf`, `refactor`, `docs`, `test`, `build`, `ci`, `chore`, or `release`. Names describe the shipped change and never contain agent names, model names, machine names, or orchestration terms. Issue titles state an observable outcome as a short imperative phrase. Avoid release codenames, implementation diaries, and vague buckets.
- Several agents may work on one issue through separate claimed branches. Only one active owner may push a workstream branch. A handoff or compare-and-set takeover transfers that ownership; multiple PCs never push concurrently to the same branch. The integration manager reviews overlap and orders or combines the pull requests.
- Rebase or merge the current default branch before final validation. Resolve semantic conflicts using the issue invariants, not by mechanically choosing one side.
- Post concise progress only when evidence or scope changes. Record commands, receipts, measurements, generated-size changes, and blockers in the issue.
- Push every green material checkpoint and before every handoff, delegated wait, shutdown, machine change, or switch to another atom. The issue comment names the last pushed commit, validation state, and any uncommitted work. Never leave another PC dependent on an unpushed checkout or private chat context.
- Open a linked pull request with `Refs #<issue>` when it completes one workstream and `Closes #<issue>` only when it satisfies every remaining acceptance criterion. The PR is the reviewable result; the issue remains the execution record.
- Another agent reviews correctness, architecture boundaries, source budget, and benchmark validity. The implementer does not self-approve.
- Merge only after required checks and review pass. Remove the worktree and branch after merge. Close abandoned experiments with their measurements and reason.
- On startup, every agent fetches origin and reads this file, the issue, linked dependencies, active claims, and open pull requests touching the same paths. On shutdown, it pushes its branch and leaves the issue sufficient for another machine to resume without private context.

Use `eng/agent-work` for issue creation, claims, checkpoints, handoffs, pull requests, and completion. Set `TL_AGENT` and `TL_MACHINE` to stable public team identifiers. The helper updates Project 6 and the issue while it pushes the branch. If GitHub is unavailable, keep working only within the claimed scope, then run the missing helper operation before handing off or starting another issue.

Start a stacked workstream with the optional explicit base, such as `eng/agent-work start 16 feat runtime-state "runtime state ABI" origin/feat/15-typed-playback`. The helper resolves and records the exact base commit before publishing its atomic claim. Omit the base only when `origin/main` is correct or when resuming an existing claim.

The cold-start entry point is `docs/roadmap.md`. It points to the release issue and canonical Project view. Live status belongs in GitHub; repository roadmap files contain recovery procedure, architecture, gates, and links rather than a second mutable status board.

Small typo-only documentation fixes may share their parent issue. Emergency release repairs still receive an issue immediately after containment. No agent creates an untracked private task list as an alternative authority.

## Product contract

`tl` compiles immutable, heterogeneous timelines into deterministic playback kernels. The primary runtime surface is a generated typed timeline facade, timeline-typed playback, generated borrowed contexts, and total operations. A non-generic timeline ID is an explicit dynamic-routing fallback. The signed simulation-seek contract is owned by issue #16 and supersedes older forward/backward/span examples while that breaking migration is active. Authoring is declarative syntax consumed at compilation. Runtime authoring, reflection, binding tables, hidden allocation, and implicit fallback are outside the compiled path.

Production source plus UTF-8 relative paths must remain at or below 200,000 bytes under `benchmarks/source_budget.py`. Every public abstraction must justify its runtime, generated-code, and maintenance cost. Extensions belong in separate packages when they do not strengthen the irreducible runtime.

## Architecture boundaries

- `Tl.Runtime` owns the stable playback ABI, frame contract, syntax surface, IDs, and compact registry.
- `Tl.Compiler` owns the language-neutral immutable plan and versioned extension contract.
- A frontend translates one language into the neutral plan plus a language binding.
- A backend consumes the neutral plan and its binding. It must not introduce assumptions into `Tl.Runtime` for its own convenience.
- `Tl.Gen.CSharp`, `Tl.Gen.C`, Unity/Burst, analyzers, editor tools, visualization, networking, banking, and other integrations are independently versioned packages.
- Language-specific type names, expressions, syntax nodes, and compiler objects never enter the neutral plan.
- Arbitrary behavior is represented by stable operation IDs. Each language binding supplies its implementation. No backend pretends to translate arbitrary code from another language.

## Code rules

- Production code contains no explanatory comments. Names, types, file boundaries, and tests carry the design.
- Prefer immutable values, pure transformations, total functions, explicit ownership, and deterministic order.
- Keep data separate from behavior. Validate once before emission; emit code whose legal states are already constrained.
- Do not add reflection, `dynamic`, boxing, delegates, LINQ, exceptions, managed allocation, locks, or indirect calls to a warm playback path without measured proof and explicit review.
- Unsafe code requires a stated lifetime, aliasing, alignment, and concurrency proof in `docs/architecture.md` or `docs/memory-and-performance.md`.
- Do not retain a managed reference, span, ref struct, generated input/output context, or callback frame beyond its call.
- Do not change ordered effects or floating-point evaluation to win a benchmark.
- Generated files must be deterministic, content-stable, and culture-independent. A cache hit preserves timestamps.
- Diagnostics identify the source location, invariant, and repair. Unsupported input fails compilation; it never silently selects a slower runtime.

## Performance rules

- Correctness receipts precede timing. Every benchmark consumes success, playback, and output state.
- Warm scalar playback must allocate 0 B. Batch results are labeled throughput and never presented as scalar latency.
- Compare one-variable baseline and candidate builds on the same machine. Retain raw BenchmarkDotNet JSON, Tier-1 or NativeAOT assembly, and PMU counters when available.
- Record cycles, instructions, branches, branch misses, code bytes, generated bytes, static data, registry bytes, and managed allocation as distinct quantities.
- Do not use constant folding, dead output, unchecked failure, different fixtures, best-sample selection, timer subtraction, or hidden setup to improve a result.
- Keep a change only when exact receipts pass and evidence supports it. Record meaningful dead ends so they are not repeated blindly.

## Memory and concurrency rules

- Compiled definitions are immutable and live for the registry lifetime. This intentional retention must remain bounded and reported.
- Publication must expose a complete descriptor. Playback may read concurrently after publication.
- Any future destruction or ID reuse requires a generation token and a safe-reclamation proof.
- Runtime and generated data layouts use explicit widths where they cross an ABI. Each native ABI defines size, alignment, version, ownership, failure behavior, and endianness scope.
- Add stress receipts for allocation, retained memory, compacting GC, aliasing, concurrent publication, maximum capacity, and NativeAOT whenever the affected boundary changes.

## Required validation

Run the smallest relevant checks during development and the complete gate before release:

```sh
python3 benchmarks/source_budget.py
python3 -m unittest discover -s benchmarks -p test_collect.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --capacity
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --module-capacity
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

Run JetBrains Inspect Code when available. Classify remaining findings explicitly; do not suppress a real defect or add configuration solely to make a count zero.

## Public API and release rules

- Public API changes require approval-file updates, migration documentation, package version review, and package-only consumer validation.
- A release tag points to the exact validated commit. Package versions, tag, release title, generated checksums, and repository commit metadata agree.
- Never move a published tag. Use the next prerelease identifier.
- Do not choose a license, publish to NuGet, or make legal compatibility claims without the repository owner's explicit decision.
- Do not commit credentials, machine-local settings, generated benchmark scratch, or unreviewed binary artifacts.
- Release readiness is tracked by one milestone and one release issue whose checklist links every blocking issue and exact artifact workflow run.
