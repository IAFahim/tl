# Spike: the film made inside Unity (Timeline + Tl + UI Toolkit)

The same film timeline (`../../timelines/release.json`) as a **Unity Timeline** with Tl tracks, drawn by **UI Toolkit** (UXML + USS), no HyperFrames.

```
Film.playable (Timeline)  ->  Tl  ->  OnScreen column (Scene, ScenePhase, Caption, Flashes)  ->  FilmUi  ->  UIDocument -> RenderTexture -> frames
```

- `Editor/FilmSpikeBuilder.cs` builds the Timeline from `release.json` (Scene, Caption and Beat tracks; `FILM_DIR`, `FILM_SECONDS`, optional `FILM_TRACKS` environment variables), the SubScene, the UIDocument and a PanelSettings that renders into a 1920x1080 RenderTexture.
- `FilmUi.cs` reads the `OnScreen` column every frame and sets the labels and the progress bar. It owns no timing.
- `UI/Film.uxml`, `Film.uss`, `Film.tss`: the layout and look.
- `Editor/FilmSpikeRecord.cs`: an EditMode test that enters Play mode at a fixed frame rate and saves the RenderTexture every frame (`FILM_NORECORD=1`), then `ffmpeg -framerate 30 -i f%05d.png` makes the MP4.
- `Film.cs` is `../../Film.cs` with a block namespace (Unity's `Assembly-CSharp` is C# 9 here, so `namespace Film;` does not compile).

## Result (Unity 7000.0.0a16, 14 s of the film, 420 frames)

Works: the scene title changes ("Tl" then "Five pieces, every time"), the captions follow the sentences, the progress bar follows `ScenePhase`, the beat dot flashes. All of it is read from the Tl column.

Not working / learned:

- **Unity Recorder 5.1.7** installs and compiles on this Unity 7 alpha, but in `-batchmode` the DOTS world stopped ticking while the recorder was recording (the film timeline never started, the MP4 stayed 0 bytes). Without the recorder the same scene runs. Not tried in an interactive Editor.
- **Reading the column from managed code**: `GetSingleton<OnScreen>()` in `LateUpdate` throws `InvalidOperationException ... You must call JobHandle.Complete()` unless you call `query.CompleteDependency()` first (this spike does). The cleaner design is the one BovineLabs Anchor uses: an ECS system publishes unmanaged state to a view model.
- **Not done**: Voice and Footage tracks, narration audio, the other scene types, the live demos as scenes.
