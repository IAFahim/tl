# Film: the videos are Tl timelines

The release demo and the "Hello Tl in six steps" tutorial are not edited in a video editor. Each one is a Tl timeline:

| Clip | Track | Kind | Column (`OnScreen`) |
|---|---|---|---|
| `Film/Scene` | `SceneTrack` (feeds the scene number, and how far through the scene we are) | `ShowScene`, `ScenePhase` | `Scene`, `ScenePhase` |
| `Film/Caption` | `CaptionTrack` | `ShowCaption` | `Caption` |
| `Film/Voice` | `VoiceTrack` (a pulse when the clip starts) | `StartVoice` | `Voice` |
| `Film/Footage` | `FootageTrack` (feeds which recorded Unity clip plays) | `ShowFootage` | `Footage` |
| `Film/Beat` | `BeatTrack` (a pulse when the clip starts) | `Flash` | `Flashes` |

`Film.cs` is all of Tl's part (one file, the same four pieces as `demos/hello`). `Program.cs` runs a timeline for N ticks (30 ticks = 1 second = 1 video frame) and writes what is on screen at every tick.

## How a film is made

```sh
python demos/film/make_film.py
```

1. **Narration.** `script/<film>.json` holds the words of each scene. HeyGen TTS (`POST /v3/models/audio/tts`, model `heygen-voice-1-turbo-free`, word timestamps on) makes `audio/<hash of voice and words>.mp3` and its timing file (so editing or inserting a scene never mixes files up). This only happens when the mp3 is missing: the committed files are used otherwise, so you do not need a HeyGen key to rebuild. To remake the voice, set `HEYGEN_API_KEY` in your shell (the script reads the environment variable and never stores it).
2. **Timeline.** `timelines/<film>.json` is written from the narration lengths: scene clips back to back, a caption clip per sentence starting on that sentence's first spoken word, a voice clip per scene, a footage clip while a Unity demo is shown, a beat clip at every scene and sentence start.
3. **Run it in Tl.** The timeline is baked with `tl bake` and `FilmTl` steps it, writing `out/<film>.states.json`.
4. **HyperFrames project.** `out/<film>/index.html` is a [HyperFrames](https://github.com/heygen-com/hyperframes) composition. Its single GSAP tween only moves a frame counter; the counter's setter paints that tick from the states Tl produced. The audio start times are read back from the tick where Tl's `Voice` column changes, not from the script.
5. **Render.** `cd demos/film/out/<film> && npx --yes hyperframes@0.9.26 render`

The numbers drawn for the demos are real: the Hello Tl steps come from running `demos/hello`, the Stats and Inventory bars and bag counts come from `tl run --checkpoints 25 --show` on the demos' scene files (inventory includes its rewind), and the Force, Control, Events, Motion and Crowd cards quote the checked values from the Unity installer run (`PlayMode tests: passed 9, failed 0`). The Unity pictures are real: `unity/FilmCapture.cs` records each demo in Play mode with `Time.captureFramerate = 60` (one frame is one Tl tick), `unity/record_footage.py` runs it on an installer-made project and encodes `footage/<demo>.mp4` plus `footage/<demo>.csv` (the tick and position of every video frame, which are the numbers shown beside each clip). Needs a real graphics device, not `-nographics`. The Stats and Inventory scenes and the Hello number line's drawn parts are data views; the Hello number line also steps through the recorded Unity frames by tick.

## Files

- `Film.cs`, `Program.cs`, `FilmTl.csproj`, `tl.project.json`: the Tl part.
- `script/*.json`: the words and the scene types (`title`, `pipeline`, `code`, `numberline`, `stats`, `inventory`, `footage`, `cards`, `terminal`, `outro`).
- `template/index.template.html`: the scene renderers (one function per scene type).
- `timelines/*.json`, `audio/*`: generated, committed so the film can be rebuilt without the HeyGen key.
- `footage/*`, `unity/*`: recorded Unity footage and the recorder that makes it.
- `release.mp4`, `tutorial.mp4`: the rendered films (1920x1080, 30 fps).

## What is checked

`tests/Tl.Packs.Tests/FilmTests.cs`: both timelines show every scene once and in order, captions never go backwards, every scene is spoken once, each footage clip plays in one stretch, and the page data has one number per tick for every field. `npx hyperframes check` reports 0 errors for both compositions.
