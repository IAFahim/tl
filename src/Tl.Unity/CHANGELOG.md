# Changelog

## 1.0.0-alpha.3

- Replaced the alpha.2 `Start`/`TrySeek` playback facade with catalog-owned selection, ordered typed operation jobs, and commit.
- Added C# 9 timeline/catalog declarations, total finite and looping movement, shared state, and borrowed operation frames.
- Added deterministic physical Unity source materialization for the Entities source generator and Burst compilation.
- Kept compiler, generator, Roslyn, authoring, and sample assemblies outside the runtime/player package.
