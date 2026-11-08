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
