# Archived API mock

This is a pre-v1 design artifact retained for historical context. It is not the current API and no longer has a project file. Generation and playback methods intentionally throw `NotImplementedException`. The current contract is [v1.0-alpha-api.md](../v1.0-alpha-api.md).

The `src` scaffold is separate from this mock.

## Files

| File | Purpose |
| --- | --- |
| [Runtime.cs](Runtime.cs) | Sample/frame/event values and typed consumer interfaces |
| [Attack.g.cs](Attack.g.cs) | An example of the public API the C# adapter would generate |
| [Consumer.cs](Consumer.cs) | A complete consumer of the two payload types |
| [Usage.cs](Usage.cs) | Playback, preview, frames, and resumable traversal examples |
| [Generation.cs](Generation.cs) | Shared model, plan, adapter contract, and C# authoring facade |
| [Authoring.cs](Authoring.cs) | An example definition and generation request |

## Normal consumption

Create one state per independently playing instance:

```cs
var state = Attack.Start();
var output = new Output();
```

On each update:

```cs
output.X = output.Y = 0f;
Attack.Update(ref state, tick, ref output);
```

`Update` emits crossed transitions and then samples at the destination. It updates the supplied state and consumer. The example consumer accumulates weighted poses and applies hit damage on forward enter events.

The application owns state and output. Resetting sampled accumulators is also the application's responsibility; event totals can persist across updates.

## Other consumption paths

```cs
Attack.Seek(ref state, tick);
Attack.Sample(in state, ref output);
```

`Seek` repositions silently. Sampling from state uses the prepared playback position.

```cs
Attack.Sample(tick, ref output);
Attack.Tracks.Body.Sample(tick, ref output);
```

Stateless sampling works independently of a playback state. A named track accepts only the consumer interface for its payload type.

```cs
Attack.Frames(tick, Direction.Forward, ref frames);
```

Frames include timing, phases, eased progress, blend factor, and sample metadata. Sampling and frame callbacks are separate contracts.

```cs
Attack.Advance(ref state, tick);
Attack.Advance(ref state, nextTick, ref events);
```

Advancement without a consumer only moves state. The event overload also emits crossed transitions. Neither overload samples. `Update` is the normal combined operation.

## Budgeted traversal

```cs
Attack.Token token = default;
var result = Attack.Traverse(previousTick, tick, 128, ref token, ref events);
```

When `result.Status` is `More`, call again with the same endpoints and token. The budget may change between calls. `Count` is the number of transition callbacks emitted by that call. Sampling callbacks are not included.

`Completed` clears the token. `TooMany` reports an unbudgeted occurrence count that cannot fit in `long`, before invoking callbacks. `Invalid` reports an invalid budget or resume request, before invoking callbacks. A zero budget returns `More` when occurrences remain, or `Completed` when there are none.

Tokens are generated per timeline and per named track. A body-track token therefore cannot be passed to whole-timeline traversal. A nonfresh token must also be checked against its generation, endpoints, direction, and scope by the eventual implementation.

Traversal does not move a playback state or sample values. After a budgeted traversal completes, an application can `Seek` its state to the destination and `Sample` it. Calling `Update` from the old state instead would emit the transitions again.

## Proposed timing contract

- Public time arguments are absolute `long` ticks. Clip windows and local duration use `int` ticks.
- Clip windows are half-open: `[start, end)`.
- `Start()` positions the previous tick at `-1`; the first update to `0` can emit initial transitions.
- `Start(previousTick)` positions state silently. Creation is not sampling or traversal.
- Forward traversal includes `(previousTick, tick]`. Reverse traversal includes `[tick, previousTick)` and maps phases to the reverse direction.
- A repeated update at the same tick samples again without emitting transitions.
- `Seek` emits no callbacks. The next movement starts from the sought tick.
- Looping normalizes sampling to the local duration. Nonlooping sampling outside the timeline emits nothing. Traversal still emits boundaries crossed while moving into or out of the timeline.
- Longer clips emit enter/exit boundaries on their first/last active ticks. One-tick clips emit a single instant enter transition during traversal.
- Cycles are visited in movement order, tracks in generated track-ID order, and each track's boundaries in movement order. Forward ties use `BoundaryKind`, clip A, then clip B; reverse traversal reverses that boundary order.
- A default playback state is uninitialized. Use `Start` before consuming it. Default tokens are fresh.
- `Update` and unbudgeted event advancement execute all crossed events. An unrepresentable occurrence count throws before callbacks or state changes. Use budgeted traversal when work must be bounded.
- If a consumer throws, its preceding effects remain observable. The affected playback state or token must be restarted; exception recovery is not an exactly-once delivery mechanism.

## Generation

The C# facade accepts typed authoring payloads and produces the shared model:

```cs
var timeline = Timeline.Define("Attack", duration: 60)
    .Track("Body", 0, TrackMode.CrossFade,
        Clip.Range(0, 40, new Pose(0f, 0f)),
        Clip.Range(30, 60, new Pose(1f, 0f)))
    .Track("Impact", 1, TrackMode.Exclusive,
        Clip.At(35, new Hit(20)))
    .Build();

var result = Generator.Generate(
    timeline,
    new Adapter(new Options("Game.Timelines")));
```

The authoring `Pose` and `Hit` types are generation inputs. The example C# output declares corresponding runtime payload types in `Game.Timelines`. The shared model contains logical schemas and values, with no CLR `Type` dependency. Numeric `Value.Bits` preserves the primitive bit pattern; record values contain ordered fields.

The shared generator validates definitions and creates a plan before calling the language adapter. Its plan carries track regions and boundary actions. The adapter chooses its emitted representations and uses Waffle templates to produce source files. A region with `ClipA == -1` is inactive; `ClipB == null` means there is no second active clip.

`Result.IsSuccess` is intended to mean no error diagnostics. Invalid definitions produce diagnostics and no source files. These behaviors are declarations of intent; the mock methods still throw.

Generation returns source files to the integration. File writing and application compilation belong to the CLI, MSBuild, or editor integration.

## Checking the mock

```sh
dotnet build docs/api/Api.csproj -c Release
```

This checks that the proposed API and consumer examples compile. It does not execute the stubs, establish allocation behavior, or establish Unity, Native AOT, or WASM compatibility.
