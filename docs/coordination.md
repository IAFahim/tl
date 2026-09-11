# Multi-agent coordination

## One system of record

Use GitHub Issues for work definitions, pull requests for reviewable changes, and [Project 6's Workflow view](https://github.com/users/IAFahim/projects/6/views/4) for queue state. The view groups cards by the built-in `Status` field. Do not create a second workflow field. This keeps three machines and many agents consistent without copying tasks into chat, local Markdown checklists, or another tracker.

The Project uses these states:

| State | Meaning |
| --- | --- |
| Backlog | Defined but not ready or scheduled |
| Ready | Dependencies settled and acceptance receipts written |
| In progress | One or more workstreams are still changing |
| In review | The only active workstream has an open pull request |
| Done | Issue closed with no active claim |

Pull-request cards use In review while checks and review are open, then Done after the exact head merges. A merged stacked pull request can complete its bounded workstream while its open issue returns to Ready for later integration or release work.

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

Pass a stacked target explicitly. Omitting it targets `main`:

```sh
TL_AGENT=Curie TL_MACHINE=office-1 ./eng/agent-work pr 123 "Emit the C backend" /tmp/pr.md docs/27-alpha3-plan
```

## Worktree protocol

Create a fully labeled issue from a prepared body and place it in Ready before claiming it:

```sh
./eng/agent-work issue "Add a C catalog receipt" area:c kind:feature /tmp/issue.md
```

The repository helper performs remote preflight, an atomic Git-ref claim, worktree creation at `../<repo>-<issue>-<kind>-<scope>`, assignment, Project update, and issue report as one operation:

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

`handoff`, `pr`, and `done` update the Project summary. `pr` also records its exact base and moves its own Project card to In review. The helper refuses to publish a checkpoint with uncommitted files or from a machine, agent, or branch that does not own its remote claim. It adds the issue to Project 6 idempotently, records the latest Agent, Machine, Branch, and Checkpoint activity, pushes the commit, and posts the same recovery data on the issue. A `refs/heads/workstream-claims/<issue>/<kind>/<scope>` remote ref is the compare-and-set lock for one workstream. Legacy `refs/heads/claims/<issue>` and `refs/heads/claims/<issue>/<kind>/<scope>` refs remain readable during migration. Concurrent starts of the same workstream race at the Git server and exactly one can create it; different workstreams under the same issue can proceed independently. Every branch publication atomically advances the branch and rotates the claim under a lease on the claim object observed during validation.

The single Project issue row and ordered issue record are shared by all workstreams, so every helper command that creates a claim or projects state holds `refs/heads/issue-transactions/<issue>`. This issue-level compare-and-set lock serializes the active claim set, Project fields, comments, workstream takeover, handoff, and completion. Agent, Machine, Branch, and Checkpoint are one coherent latest-activity projection; scoped Git refs remain the authoritative list of concurrent owners. A missing transaction or claim is accepted only after a successful remote query returns no matching ref; transport and authentication failures abort. Handoff and completion publish their terminal projection while holding the transaction, delete the exact observed workstream claim, then release the issue transaction as their final action; they perform no later Project or comment write. A successor cannot expose a new claim while a terminal transition is deciding whether other claims exist. Completion requires local HEAD, the claim parent, the remote branch head, and the linked merged pull-request head to be identical. GitHub must report that exact PR merged into its recorded base. A stacked merge completes only that workstream; the issue and integration branch retain their own gates.

Every transition is restartable. The same owner reruns `start` to repair a claim whose worktree, assignment, Project fields, or issue comment was interrupted. A closed issue causes an interrupted start claim to be released under the issue transaction; another machine can perform the same cleanup through an exact-claim `takeover`. Repeating `handoff`, `pr`, or `done` completes the remaining transition without publishing a second branch or closing unrelated work. Handoff accepts its own already-published Ready projection, and completion accepts Ready or Done after re-proving the exact merged commit. Any machine may finish that proven completion cleanup; it cannot alter or release unmerged work. A coordinator recovers a stale claim from its last Project checkpoint by naming the exact current claim object; Git rejects the replacement if ownership changed between inspection and takeover:

```sh
TL_AGENT=Turing TL_MACHINE=office-2 ./eng/agent-work takeover 123 feat c-backend "owner machine lost power" 0123456789abcdef0123456789abcdef01234567
```

The takeover recreates the same workstream branch from its last pushed remote checkpoint under a new compare-and-set owner. Work that was never pushed is not guessed from an inaccessible machine.

An interrupted helper normally releases its issue transaction through an exit trap. Abrupt process or machine loss can leave the remote lock. After confirming the owner cannot resume, recover only the exact observed object, then rerun the interrupted command:

```sh
TL_AGENT=Turing TL_MACHINE=office-2 ./eng/agent-work recover-lock 123 89abcdef0123456789abcdef0123456789abcdef "owner machine lost power"
```

Recovery itself compare-and-set replaces the exact old transaction, records the audit on the issue, and releases the replacement. If audit publication fails, the replacement stays locked; retry with that exact replacement after confirming the failed process exited. Every retry acquires a new replacement under a lease, while its audit retains the original interrupted transaction. If the final response is lost after deletion, rerunning the command succeeds only when the exact durable audit exists. Closed-claim cleanup follows the same rule. A wrong, unrelated, or changed object is rejected. Never recover a live transaction merely because a command is slow.

The equivalent manual protocol remains valid:

```sh
git fetch origin
git worktree add ../tl-123-feat-c-backend -b feat/123-c-backend origin/main
cd ../tl-123-feat-c-backend
```

Each worktree builds into its own local paths. Shared machine-wide benchmark state, package caches, and temporary output receive issue-specific names. Performance experiments run alone on the reference core and post baseline/candidate hashes.

After the pull request merges:

```sh
git worktree remove ../tl-123-feat-c-backend
git branch -d feat/123-c-backend
```

## Pull request handoff

The pull request describes final behavior, generated or public API changes, memory and source-budget deltas, target limitations, and validation. The issue holds chronology and failed experiments. Review checks the code against [repository rules](../AGENTS.md), [architecture](architecture.md), and [memory contract](memory-and-performance.md).

GitHub is sufficient until cross-repository dependency scheduling or organization-wide portfolio reporting becomes a measured problem. If a second tracker is ever introduced, GitHub issue IDs remain canonical and the external system mirrors them automatically.
