# Multi-agent coordination

## One system of record

Use GitHub Issues for work definitions, pull requests for reviewable changes, and one GitHub Project for queue state. This keeps three machines and many agents consistent without copying tasks into chat, local Markdown checklists, or another tracker.

The Project uses these states:

| State | Meaning |
| --- | --- |
| Backlog | Defined but not ready or scheduled |
| Ready | Dependencies settled and acceptance receipts written |
| In progress | One or more claimed workstreams are active |
| In review | Pull request open and complete checks running |
| Done | Merged with evidence |

Recommended labels are `ai`, `area:runtime`, `area:compiler`, `area:csharp`, `area:c`, `area:unity`, `area:tooling`, `area:docs`, `area:ci`, `kind:bug`, `kind:feature`, `kind:performance`, `kind:architecture`, `kind:release`, `blocked`, and `experiment`.

## Issue as executable plan

Every issue answers:

1. What observable problem exists?
2. Which invariant and package boundary own it?
3. What is explicitly outside scope?
4. Which exact receipts prove completion?
5. Which files or APIs are likely to conflict?
6. What data must be posted if the experiment fails?

Issue titles use an imperative statement of the observable outcome, such as `Emit Burst-compatible timeline blobs`. Branches use `<kind>/<issue>-<scope>`, such as `feat/123-c-backend` or `perf/123-scalar-dispatch`. Identity and machine data belong in the claim record rather than the branch name.

An agent claims a bounded workstream with its machine, agent identity, branch, worktree, files, and receipts. One issue may have several workstreams when they contribute to the same outcome. Each receives an independent compare-and-set claim, branch, and worktree. Work with its own outcome, acceptance criteria, or release decision belongs in a linked child issue. Multiple PCs pushing one branch creates races and is forbidden. Linked issues and pull requests are the communication channel and the integration manager orders and merges reviewed changes.

Every green material checkpoint is committed and pushed. Before a handoff, shutdown, or machine change, the agent posts the last pushed commit, remaining work, exact next command, and any local-only state. Another PC must be able to resume from GitHub without access to the previous checkout or chat.

Each pull request links its workstream with `Refs #<issue>`. The final pull request uses `Closes #<issue>` only when every acceptance receipt on the issue is complete.

## Worktree protocol

Create a fully labeled issue from a prepared body and place it in Ready before claiming it:

```sh
./eng/agent-work issue "Add a C ABI receipt" area:c kind:feature /tmp/issue.md v1.0.0-alpha.2
```

The repository helper performs remote preflight, an atomic Git-ref claim, worktree creation, assignment, Project update, and issue report as one operation:

```sh
TL_AGENT=Curie TL_MACHINE=office-1 ./eng/agent-work start 123 feat c-backend "C validation and mirror receipts"
```

Claims created before the atomic protocol can be migrated only from their exact pushed branch, machine, and agent identity:

```sh
TL_AGENT=Curie TL_MACHINE=office-1 ./eng/agent-work adopt 123
```

After committing a tested change from that worktree, publish it and its evidence with:

```sh
TL_AGENT=Curie TL_MACHINE=office-1 ./eng/agent-work checkpoint 123 "C identifiers are total" "dotnet test; gcc and clang strict C11" "second architecture remains"
```

`handoff`, `pr`, and `done` update the Project summary. The helper refuses to publish a checkpoint with uncommitted files or from a machine, agent, or branch that does not own its remote claim. It adds the issue to Project 6 idempotently, records the latest Agent, Machine, Branch, and Checkpoint activity, pushes the commit, and posts the same recovery data on the issue. A `claims/<issue>/<kind>/<scope>` remote ref is the compare-and-set lock for one workstream. Concurrent starts of the same workstream race at the Git server and exactly one can create it; different workstreams under the same issue can proceed independently. Handoff releases only that lock after publishing a recoverable branch checkpoint. Completion releases it only after GitHub contains a merged pull request whose reviewed head is the claimed commit and whose merge commit is present on `origin/main`.

Every transition is restartable. The same owner reruns `start` to repair a claim whose worktree, assignment, Project fields, or issue comment was interrupted. Repeating `handoff`, `pr`, or `done` completes the remaining transition without publishing a second branch or closing unrelated work. A coordinator recovers a stale claim from its last Project checkpoint by naming the exact current claim object; Git rejects the replacement if ownership changed between inspection and takeover:

```sh
TL_AGENT=Turing TL_MACHINE=office-2 ./eng/agent-work takeover 123 feat c-backend "owner machine lost power" 0123456789abcdef0123456789abcdef01234567
```

The takeover recreates the same workstream branch from its last pushed remote checkpoint under a new compare-and-set owner. Work that was never pushed is not guessed from an inaccessible machine.

The equivalent manual protocol remains valid:

```sh
git fetch origin
git worktree add ../tl-123-c-backend -b feat/123-c-backend origin/main
cd ../tl-123-c-backend
```

Each worktree builds into its own local paths. Shared machine-wide benchmark state, package caches, and temporary output receive issue-specific names. Performance experiments run alone on the reference core and post baseline/candidate hashes.

After the pull request merges:

```sh
git worktree remove ../tl-123-c-backend
git branch -d feat/123-c-backend
```

## Pull request handoff

The pull request describes final behavior, generated or public API changes, memory and source-budget deltas, target limitations, and validation. The issue holds chronology and failed experiments. Review checks the code against [repository rules](../AGENTS.md), [architecture](architecture.md), and [memory contract](memory-and-performance.md).

GitHub is sufficient until cross-repository dependency scheduling or organization-wide portfolio reporting becomes a measured problem. If a second tracker is ever introduced, GitHub issue IDs remain canonical and the external system mirrors them automatically.
