# Multi-agent coordination

## One system of record

Use GitHub Issues for work definitions, pull requests for reviewable changes, and one GitHub Project for queue state. This keeps three machines and many agents consistent without copying tasks into chat, local Markdown checklists, or another tracker.

The Project uses these states:

| State | Meaning |
| --- | --- |
| Backlog | Defined but not ready or scheduled |
| Ready | Dependencies settled and acceptance receipts written |
| In progress | One active issue claim owns the files and invariant |
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

An agent claims the issue with its machine, agent identity, branch, worktree, files, and receipts. Several agents may share an issue only when a coordinator records non-overlapping sub-scopes. Each agent still owns a separate branch and worktree; multiple PCs pushing one branch creates avoidable races and is forbidden. The issue is their communication channel and the coordinator integrates reviewed commits.

Every green material checkpoint is committed and pushed. Before a handoff, shutdown, or machine change, the agent posts the last pushed commit, remaining work, exact next command, and any local-only state. Another PC must be able to resume from GitHub without access to the previous checkout or chat.

## Worktree protocol

The repository helper performs the claim, worktree creation, Project update, and issue report as one operation:

```sh
TL_AGENT=Curie TL_MACHINE=office-1 ./eng/agent-work start 123 c-backend "C validation and mirror receipts"
```

After committing a tested change from that worktree, publish it and its evidence with:

```sh
TL_AGENT=Curie TL_MACHINE=office-1 ./eng/agent-work checkpoint 123 "C identifiers are total" "dotnet test; gcc and clang strict C11" "second architecture remains"
```

`handoff`, `pr`, and `done` move the Project through Ready, In review, and Done. The helper refuses to publish a checkpoint with uncommitted files. It adds the issue to Project 6 idempotently, records Agent, Machine, Branch, and Checkpoint fields, pushes the commit, and posts the same recovery data on the issue.

The equivalent manual protocol remains valid:

```sh
git fetch origin
git worktree add ../tl-issue-123 -b ai/123-c-backend origin/main
cd ../tl-issue-123
```

Each worktree builds into its own local paths. Shared machine-wide benchmark state, package caches, and temporary output receive issue-specific names. Performance experiments run alone on the reference core and post baseline/candidate hashes.

After the pull request merges:

```sh
git worktree remove ../tl-issue-123
git branch -d ai/123-c-backend
```

## Pull request handoff

The pull request describes final behavior, generated or public API changes, memory and source-budget deltas, target limitations, and validation. The issue holds chronology and failed experiments. Review checks the code against [repository rules](../AGENTS.md), [architecture](architecture.md), and [memory contract](memory-and-performance.md).

GitHub is sufficient until cross-repository dependency scheduling or organization-wide portfolio reporting becomes a measured problem. If a second tracker is ever introduced, GitHub issue IDs remain canonical and the external system mirrors them automatically.
