# Changelog

## 1.0.0-alpha.2

- Added the 16-byte signed-position playback ABI with an external game-tick anchor.
- Added generated typed `Start(gameTick)` and signed `TrySeek(ref data, delta)` facades.
- Added direction-aware frames, ordered multi-frame replay, finite rejection, and looping normalization.
- Added BlobAsset-compatible timeline records and generated memory reports.
- Added a separately qualified heterogeneous Burst Combat ECS project.
