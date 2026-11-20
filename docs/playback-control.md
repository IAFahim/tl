# Playback control

`Playback` remains eight unmanaged bytes. Control requests change a playback or an entity's channel; library replacement keeps the playback's current position.

When a live library is replaced, both hosts call `KindRules.Rebind`. The rule removes entries belonging to the old playback, clamps its tick to the replacement timeline, and preserves pause, direction and held state. An active playback keeps its tick for the replacement step and re-enters only the active spans, marked synthetic on that step. Clip-start, clip-end and marker pulses are not repeated. A playback that has not started yet still begins once. A stopping playback is removed.

A span feeder sees `Synthetic` on the step its playback began mid-clip or re-entered after a replacement or a seek, and not on the later ticks of that clip: spans come from the library's load-time table, not from per-entity entries that carried the flag.

`PlaybackFlags.ReenterPending` is transient host state used for that replacement step. An `IKindRows` storage adapter must implement `Purge(row, slot, timeline)` to remove all old entries for exactly that playback, without removing other playbacks or game pulses. Entry and playback pointers are borrowed only for the current row operation. Hosts finish all jobs using the old library before replacing it; the rule retains no pointers.

`PlaybackRules.Rebind` owns the position and flag rule, including timelines without kinds. Plain tables check the library generation when applying requests or advancing; an attached `KindStore` does the same check before its tick. Legacy consumers still execute their active body at the replacement position, but suppress start, end and marker callbacks on that synthetic step. The following step resumes normal playback.

The public API approval includes the new flag, `KindRules.Rebind` and `IKindRows.Purge`. This is an unreleased source change; no package version, tag or NuGet release is created.

## Control verbs

One request vocabulary in Core (`RequestOp`), written by plain code (`PlaybackTable`, `Requests`) and by Burst jobs (`TlRequests` in com.tl.unity). Requests apply at the start of the step, before advance, in submission order per entity. The law is recorded on IAFahim/tl#577.

- `Seek(timeline, tick)` and `SeekChannel(channel, tick)` land without crossing: the tick is clamped (a looping timeline wraps), the playback's entries are purged, held is cleared, pause and direction are kept, and active spans re-enter synthetically on the next step with no clip-start, clip-end or marker pulse. Seeking a playback that has not begun only moves where it begins.
- `Reverse(timeline, on)` and `ReverseChannel(channel, on)` set the playback's direction. A held playback leaves its hold when the new direction faces away from the end it is parked at: it begins again at that tick. A world-wide rewind (`forward = false`) leaves held playbacks parked.
- `PauseChannel(channel)` and `ResumeChannel(channel)` pause or resume the channel's live playback. Channel 0 means no channel; channel verbs on it do nothing.
- A paused playback does not move with the step in either direction; it moves only by `Resume` or `Seek`.
- `Reset(entity)` (`PlaybackTable.Reset`, `Requests.Reset`, `RequestOp.Reset`) says "this row is a new entity": it zeroes the entity's slots, purges its kind rows, drops its queued game pulses and adds nothing to the closing list, so no end pulse of the old occupant reaches the column the new one now uses. `StopAll` keeps its meaning (interrupt, with end pulses). Order in one step: `Reset(row)`, write the new column value, `Start(row, timeline)`, `Tick`. Fixes #594.

## Speed

`SetSpeed(timeline, sixteenths)` and `SetChannelSpeed(channel, sixteenths)` set how many ticks a playback crosses per step: `n / 16` ticks (16 and 0 mean 1x; 4 is 0.25x, 64 is 4x, at most 255). The step crosses `(Phase + n) >> 4` ticks and keeps `Phase = (Phase + n) & 15`, so 1.5x crosses 1, 2, 1, 2 ticks. Seek clears `Phase`. A playback that crosses no tick in a step contributes nothing that step.

Over any number of steps, a playback at any speed applies exactly what 1x applies over the same crossed ticks, in order: spans, pulses (`[Pulse]` on clip start, clip end and markers), `Each`-mode kinds, handover to the next timeline, loops and holds, forward and backward. Mixed speeds on one entity meet in substep 0, where game pulses (`KindPulses.To`) also land. Scenes can set speed (`"op": "SetSpeed"`, `"speed": 2`, a multiple of 1/16; `docs/json-format.md`).

What is exact: kinds and `[Pulse]`. Tests: `KindSpeedTests`, `SpeedRunTests`, `TwoAssemblyRateTests` (rates 0.5x, 1.5x, 2x, 2.5x, 3x, 4x across a handover, a loop, a foreign feeder and a world rewind equal a 1x run bit for bit), and the Inventory scenes `inventory-fast` and `inventory-mixed`, whose hashes `tl run`, Unity and the IL2CPP and CoreCLR players agree on.

Known limit in Unity: `ITlTrack` event consumers (`OnClipStart`, `OnClipEnd`, `ExecuteActive` with `Events<T>`) run once per step at the playback's final tick. At speeds above 1x a clip boundary crossed in an earlier tick of the same step raises no event. Use `[Pulse]` and kinds when a clip boundary must be seen at every speed.

## Derived kinds

`Sum<T>`, `Set<T>` and `While<T>` name a kind whose value type is the column type `T`, so the clip value, the column and the kind need no mirror struct. Use them in `[Feeds]` or `[Pulse]`: `[Pulse(typeof(Sum<Teleported>), On = ClipEvent.Start)] static Teleported Jump(in TlFrame<TeleportClip> frame) => ...`.

| Marker | Accepts | Combine | Apply | Idle |
|---|---|---|---|---|
| `Sum<T>` | spans and pulses | field-wise `+` | `column += total` field-wise | none |
| `Set<T>` | spans and pulses | last entry wins | `column = total` | none |
| `While<T>` | spans | `\|` for bool and integers, `max` for floats | `column = total` | `column = default` |

The generator writes an internal `[TimelineKind]` struct `Tl.Derived.<Template>_<Namespace>_<Type>` for each `(template, T)` it meets, and the library names that kind the same way, so a derived kind and a hand-written Combine/Apply/Idle kind are interchangeable and leave identical column bytes (`DerivedDifferentialTests`: 600 ticks, rewind and every request verb). Against an `Each`-mode kind the result differs only by float addition order when two pulses meet on one row in one tick. Errors: `TLGEN110` (a `Sum<T>` field that is not numeric, or a value type that is not an unmanaged struct), `TLGEN111` (a `While<T>` field that cannot be combined), `TLGEN112` (a `Set<T>` or `While<T>` column that has another writer). The kind and its feeders must be in one assembly.

## Unconnected fields

`TLGEN113` is a warning for a `[TlClip]` field that no track of the clip reads (`frame.Clip.<Field>` never appears; a clip passed whole to a helper counts as read). `TLGEN114` warns for a `[TlState]` field whose name appears nowhere else in the assembly. Both are ordinary warnings: silence them with `NoWarn` or `#pragma`.

## Finding a timeline by name

`world.Find("name")` binary-searches the sorted manifest of the loaded library and returns the timeline id, or 0 when no timeline has that (ordinal) name. Generated `Timelines` id constants stay available and are now optional: a project that calls `Find` compiles without them and without a library file on disk.

## Migration: spans from the load-time table (#652)

This is an unreleased source change after `v4.0.0-preview.1`; the package version stays `4.0.0-preview.1`, and no tag or NuGet release is created. The public API approval lists every change.

- Regenerate systems with `Tl.Gen.CSharp`. Generated kind loops read `KindStore.Score` and call `KindRules.Due(entry, substep, run, count)` and `KindRules.Depth(copies, samples)`.
- `IKindRows` adapters: remove `RemoveSpan`, `Sample` and `Unsample`, and implement `Sampled(row, samples)`. It records how many rated copies the step wrote after the row's slots (0 when none); a kind loop walks those copies by substep.
- Removed with the span entries: `KindTable.RemoveSpan`, `Sample`, `Unsample`, `KindEntryFlags.Sample`, `KindRules.Reenter`, `IsSampled`, `SampleDepth` and `KindRules.Marker`. To count live spans, take `cell = score.CellOf(timeline, tick, kind)`; when `cell >= 0`, read `score.Start[cell + 1] - score.Start[cell]` (a cell of -1 means the timeline does not feed that kind).
- `KindTable.RemovePulses` is gone: every entry a kind row holds is a pulse (timeline pulses, game pulses and a foreign feeder's span values), and an owner clears every row it visited once it has applied it (a grouped kind drops only its own members' entries). A host that appends its own entries with `KindTable.Append` must expect them to be consumed by the owner's next run.
- A foreign feeder must run before its owner in every step. The first step rejects the opposite order (`KindStore.Join`); a host that reorders its systems later gets values one step late.
- A host that does not use `KindStore` builds its own table with `KindScore.Build(in tables, ref steps)`, where `steps` implements `IKindSteps.StepOf(timeline, window, tick)`, and calls `Free` when the library is replaced. `tables` must come from a validated kind section (`Build` throws when a feeder's kind is outside its timeline's kind mask), and the score borrows that section's timeline rows, so the library bytes must outlive it.
- Behaviour: last-wins kinds combine rated copies first (by source slot, then crossing), then the other slots in slot order, each in authored pair order, and a span feeder sees `Synthetic` only on the step its playback began mid-clip or re-entered (see above).

## Precombined spans (#655)

A kind opts in with `Associative = true` on `[TimelineKind]` or `[TlKind]`. That declares its `Combine` associative; tl never infers it from code. The generator then precombines the kind in its system when all of these hold:

- every `[Feeds]` method of the kind in that system takes only the frame (`Frame<,>` or `TlFrame<>`) and its own fold results;
- none of them, nor the `Fold` whose results they take, reads `Synthetic` or `Interrupted`. Reading `frame.Flags`, calling `frame.Has` with anything but a constant without those bits, or passing the frame on counts as a read;
- the kind is not `Mode = KindMode.Each`, and the system does not feed it from another assembly.

A precombined kind reads one value per playback slot and step: `Combine` over the cell's spans for that (timeline, tick, direction), in pair order, plus a presence bit. `Apply`, pulses, game pulses, rated copies and a foreign feeder's values work as before. Per-entity inputs belong in `Apply`, which runs once per entity: `Lift.Apply(in float lift, in JumpPower power, ref Motion motion)`.

- The values are built on the first run after a library loads, by generated code through `KindStore.SpanValues`, and are freed with the library. `KindStore.SpanValueBytes(kind)` reports the shared bytes; it is 0 while the kind is not precombined. A host without `KindStore` calls `KindValues.Build<T, TSpans>(in score, timelines, kind, routes, part, ref spans)` and `Free`.
- With one playback per entity the result is bit for bit the per-span walk, floats included. With several playbacks, `Combine` regroups by slot, so float sums can differ in the last bits; integer and pick kinds stay exact (`PrecombineTests`).
- `TLGEN115` is an info diagnostic for an opted-in kind that keeps the per-span walk, naming the first reason. Derived `Sum<T>`, `Set<T>` and `While<T>` kinds keep the per-span walk for now.
