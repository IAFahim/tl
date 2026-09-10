# Operation-order feasibility report

Commit base: `5198e080a8e50409351b48ead6511840ead946dc`

## Result

The executable prototype proves that operation-type batching can preserve arbitrary per-entity order when each selected occurrence ordinal is a stage and every stage completes before the next begins. It also refutes two broader claims: grouping every occurrence solely by operation type breaks `A-B-A`, and stage batching changes the order of effects shared across entities.

The tested execution is:

1. Pure selection stores a borrowed slice of immutable occurrences for each live entity. A gap has an advancing selection with zero occurrences. A completed entity has no advancing selection.
2. Stage `s` identifies occurrence ordinal `s`; it is not stored in every occurrence. For each generated operation type, one typed job scans or queries the entities whose occurrence at `s` has that type.
3. A barrier separates stage `s` from stage `s + 1`. Forward reads the immutable slice from first to last. Reverse reads the same slice from last to first.
4. Completion commits each selected entity after all stages. Completion does not depend on another entity having work.

This represents `A-B-A`, `B-A`, repeated operation types, opposite orders in different assets, simultaneous clips, and reverse without inventing a global total order between unrelated entities. The stage ordinal distinguishes repeated occurrences of the same operation. The authored track index and payload index distinguish their data.

## Representation and ownership

The proof uses these fixed-width immutable records on x64:

| Record | Fields | Bytes |
| --- | --- | ---: |
| `Asset` | first frame, duration, flags | 8 |
| `FrameSlice` | first occurrence, count | 8 |
| `Occurrence` | operation ID, authored track index, flags, payload index | 8 |
| `Selection` | first occurrence, next position, count, direction, advancing flag | 12 |
| `EntityState` | prototype asset route, position, selection | 20 |

`ushort Count` is intentional. A frame with 256 occurrences needs to represent the inclusive value 256; a byte cannot. The 0–255 authored track index remains one byte. The prototype does not infer that 256 tracks always means 256 occurrences. A future schedule with hooks, transitions, or multiple emitted operations per track may have more occurrences and must either retain a wider count or reject the asset during validated lowering.

The immutable asset owns `Asset`, `FrameSlice`, and `Occurrence` storage for its published lifetime. Per-entity timeline state owns one fixed-size pending `Selection`; callers never guess or allocate a global queue capacity. A .NET adapter may expose this state as a bounded borrowed column. A Unity adapter may store equivalent unmanaged fields in components or chunk-local scratch that remains valid across its scheduled dependency chain. Borrowed `Frame` values are constructed inside typed execution and are never retained in jobs. The prototype's `ushort` asset index is only a local route and reserves zero for empty, so it represents 65,535 nonempty assets. It is not a stable serialized identity or a claim of 65,536 nonempty values plus empty.

The tested `Occurrence` is the smallest naturally aligned prototype record that carries the currently required identities under the stated widths: a 16-bit operation ID, 8-bit track index, 8-bit static flags, and 32-bit payload index. Stage consumes no record bytes because array order is the stage. These widths are a feasibility encoding, not a frozen production ABI; validated asset limits must decide them before production emission.

## Complexity and execution limits

For `E` selected entities, `K` generated operation types, and maximum selected occurrence count `S`, selection and commit are `O(E)`. The deliberately simple typed-job proof scans `E` rows for each operation type at each stage, so dispatch is `O(E × K × S)` with `O(total occurrences)` actual calls. Unity queries or generated active masks can reduce rejected rows, but cannot remove the stage dependency when one entity has non-commuting operations. The execution count is bounded by immutable selected slices rather than caller storage.

Compatible operations may batch across entities within one stage only when their effects are entity-local, disjoint, commutative, or otherwise proven independent. The executable records a counterexample: entity-major scalar execution and stage-major execution produce different global event traces for assets `A-B-A` and `B-A`. Operations that write shared state, observe global sequence, or have unclassified aliasing require a deterministic scalar entity-major fallback or an explicit dependency model. Arbitrary C# side effects do not qualify for parallel, SIMD, or reordered execution merely because their operation types match.

The stage barrier is also required when operation `B` reads state written by operation `A` on the same entity. Grouping all `A` calls and then all `B` calls fails the tested `A-B-A` sequence. Enableable tags indexed only by operation type therefore cannot represent the general scheduler; they need an occurrence stage or equivalent dependency rank.

## Cases and receipts

`Program.cs` compares the stage-batched trace against an independent entity-major scalar oracle. Every trace token includes operation, authored track, and payload identity. The matrix covers:

- `A-B-A` and `B-A` in the same batch;
- two active clips on different tracks in one frame;
- an advancing gap with no operation;
- 256 authored tracks and 256 selected occurrences, including track indices 0 and 255;
- exact reverse occurrence order;
- completed and live entities in one batch;
- zero managed allocation across 100 warmed selection, dispatch, and commit passes.

Run:

```sh
dotnet run --project experiments/Alpha3/Ordering/Proof.csproj -c Release
python3 benchmarks/source_budget.py
git diff --check
```

## Review of the manager plan

The manager draft at `fb70db89e327f8d0c975eb729bc24e7a28a68d02` matches the proof on the essential laws: pure selection, ordered occurrences, reverse traversal, stage barriers, immutable shared schedules, a count wider than one byte, and a scalar path for cross-entity effects. Its operation-stage identity remains internal, so its decision not to add ordinal or active-count fields to the public `Frame` is compatible with this representation.

The draft's implementation atoms and acceptance order are usable. Atom B should select the tested occurrence slice/count encoding or state a measured alternative before Atom C consumes it. The manager should carry these exact receipts into the frozen contract:

- validate and lower a deterministic occurrence sequence per selected frame, including repeated operation types;
- preserve exact authored order forward and exact reverse order on rewind through stage barriers;
- make gaps advance and let completed entities remain inactive without blocking live entities;
- keep selection independent of gameplay columns and commit only after the stage chain;
- diagnose occurrence-count and schema-width overflow during lowering;
- route shared or unclassified side effects to the scalar entity-major executor when their observable global order must match that baseline;
- require trace parity for .NET and Unity hosts before making performance or vectorization claims.

The current reference under `experiments/Alpha3/Reference` uses a fixed `Damage` then `Animation` stage order and operation-type booleans. It demonstrates the host shape but cannot represent repeated types or opposite per-asset order. The occurrence-stage representation is the smallest change demonstrated here that closes that gap without a caller-sized queue.
