# Playback control

`Playback` remains eight unmanaged bytes. Control requests change a playback or an entity's channel; library replacement keeps the playback's current position.

When a live library is replaced, both hosts call `KindRules.Rebind`. The rule removes entries belonging to the old playback, clamps its tick to the replacement timeline, and preserves pause, direction and held state. An active playback keeps its tick for the replacement step and re-enters only the active spans, marked synthetic. Clip-start, clip-end and marker pulses are not repeated. A playback that has not started yet still begins once. A stopping playback is removed.

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
