# tl roadmap

## Start on any machine

GitHub is the live source of truth. Read [Project 6](https://github.com/users/IAFahim/projects/6/views/4), [approved next API issue #56](https://github.com/IAFahim/tl/issues/56), linked workstream issues and pull requests, and the remote atomic claim before editing. [Design issue #27](https://github.com/IAFahim/tl/issues/27) and [release issue #35](https://github.com/IAFahim/tl/issues/35) preserve the released alpha.3 baseline.

```sh
git clone https://github.com/IAFahim/tl.git
cd tl
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
cat AGENTS.md
cat plan.md
gh issue view 57 --comments
gh issue view 56 --comments
gh pr list --state open
gh project item-list 6 --owner IAFahim --limit 1000
git ls-remote --heads origin 'refs/heads/workstream-claims/*' 'refs/heads/issue-transactions/*'
```

Resume only the branch and exact checkpoint recorded by its issue and Project fields. Use `eng/agent-work` to claim, checkpoint, hand off, open a pull request, and finish work. Publish each green atom before starting another. Chat, local worktrees, and machine state are disposable.

## v1.0.0-alpha.3

Alpha.3 ships the generated heterogeneous catalog/query architecture:

- total signed `Tick` with default-ready state and independent finite clamping;
- catalog-local typed asset identities without a process registry;
- heterogeneous tracks and clips with explicit typed operations;
- pure per-row selection, ordered stage execution, and delayed commit;
- generated .NET span queries over caller-owned component columns;
- a language-neutral validated ordered schedule below the C# frontend;
- stable and preview Unity Editor ECS/Burst materialization, plus stable Mono and IL2CPP player receipts;
- deterministic NuGet, symbol, NativeAOT, and UPM release artifacts;
- strict 100% production line and branch coverage;
- a 250,000-byte production source-plus-path cap.

Production workstreams [#31](https://github.com/IAFahim/tl/issues/31), [#32](https://github.com/IAFahim/tl/issues/32), [#33](https://github.com/IAFahim/tl/issues/33), and [#34](https://github.com/IAFahim/tl/issues/34) are integrated. [Issue #35](https://github.com/IAFahim/tl/issues/35) owns the exact release candidate, final review, artifacts, and GitHub prerelease. The [implementation plan](../plan.md), [API contract](v1.0-alpha-api.md), [release notes](v1.0-alpha.3.md), and [migration guide](v1.0-alpha-migration.md) describe the current system.

The measured `<3 ns` result is deliberately narrow. Full generated `Catalog.Query.Tick` reaches 1.804 ns for one track, 1.997 ns for a gap, 2.555 ns for a blend, and 2.256 ns for three input columns on the reference machine. A-B-A is 3.754 ns, 16 tracks is 21.230 ns, mixed routing is 34.650 ns/entity-tick, and the 256-track staged fallback is 34,250.868 ns. [Issue #10](https://github.com/IAFahim/tl/issues/10) remains open for the 256-track code-size and scheduler cliff. No universal 3 ns claim is valid.

## Next work

The owner approved [data-authored timelines and typed frame queries](data-authored-api.md) on 2026-09-11. [Issue #56](https://github.com/IAFahim/tl/issues/56) owns the live design discussion and future implementation checklist. Freeze the intended consumer surface before production edits: data-only assets without mandatory names or job bindings, build-known type pairs, borrowed .NET rows, nested Unity queries, direct and job runners, and a coordinator that preserves ordered stages and explicit job dependencies. Resolve the documented implementation gates with executable evidence; this proposal is not part of the alpha.3 package.

The owner has paused implementation and will continue discussion from the office PC. Read the [proposal](data-authored-api.md), issue #56 and the [verified office handoff in #57](https://github.com/IAFahim/tl/issues/57). The documentation is preserved on main; it does not authorize a prototype, production API change or release. Project 6 is the canonical board; Project 5 is historical only. Genuine pending work remains visible in [portable assets #5](https://github.com/IAFahim/tl/issues/5), [large-timeline performance #10](https://github.com/IAFahim/tl/issues/10) and [dependency qualification #58](https://github.com/IAFahim/tl/issues/58). Read and claim the applicable issue before any separately authorized work.

The `Tl.Gen.C` C11 backend and the data-authored Unity host package (`unity/com.iafahim.tl`) with its Unity project receipts were extracted into separate local repositories at commit `3e67333`; their GitHub publication and licensing remain owner decisions.

- Reduce large-stage generated code and route overhead without changing ordered effects.
- Add ARM64 performance and code-generation evidence.
- Migrate heterogeneous catalog semantics into the extracted C backend repository without translating arbitrary C# behavior.
- Define a canonical neutral serializer and conformance fixture package before splitting backend repositories.
- Add designer GUI import as another frontend over the same validated plan.
- Keep networking, threading, visualization, banking, and game-domain operations in separate packages unless they strengthen the irreducible runtime.

The existing C ABI v2 remains supported within its documented scope. C catalog emission, runtime-loaded arbitrary schemas, and designer GUI authoring are outside alpha.3.

Historical reports under `docs/verification`, `docs/alpha3`, and older benchmark result directories remain commit-scoped evidence. They describe earlier APIs and do not override current documentation.
