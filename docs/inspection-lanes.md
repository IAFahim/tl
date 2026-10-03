# Cold decoded lane copies

This additive API is available from the issue #432 source revision. The published 3.0.0 package does not contain it; release packaging belongs to the next version, with no ABI or playback change required.

```csharp
var view = Timeline<MyTrack, MyClip>.View(asset.Index);
var copy = Inspection.CopyLanes<MyTrack, MyClip>(asset.Index);
```

`View` is the caller's explicit load/fold operation. `CopyLanes` returns null for a missing bank, an out-of-range index, and a pending or absent slot. It never folds or invokes consumers. The caller synchronizes inspection with load, disposal and reclamation using the host's existing lifetime boundary.

The copy owns managed arrays of decoded raw 32-bit words, in physical lane order. Both arrays are indexed by table tick; their length is `SlotView.TableTicks`. Each lane retains its `TypeKey`. An 8-byte result occupies two adjacent lanes, low word followed by high word, whose key has bit 63 toggled. Interpret words according to the discovered result type; floating point conversion would corrupt integer and NaN bits.

Playback positions differ from table ticks. Forward playback at position `p` reads forward tick `p` when `p < Duration`. Backward playback reads backward tick `p == 0 ? Duration - 1 : p - 1`; finite position zero and looping position `Duration` are inactive. Finite timelines include a terminal table entry; that entry is raw storage, not an extra playable frame. Use the runtime's playback rules for editor clock displays.

Returned values include index, duration, looping, slot ABI and generation. Arrays remain valid after the asset or bank is disposed and can be modified without affecting playback. Copies allocate in proportion to table ticks and lane count, so tools should bound retained history and perform this operation outside gameplay.

Migration: existing `Inspection.Bank` and `Inspection.Tables` callers need no changes. Editors previously reading private flat/segmented pointers can use this public operation and remove their decoder. Generated code, container format, slot ABI and warm playback are unchanged.
