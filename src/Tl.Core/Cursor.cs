namespace Tl;

// Caller-owned region cache for single-tick stepping, kept BETWEEN calls:
// `Playback` stays 8 bytes; this side state is opt-in per caller. The cache
// validates against the native entry itself (Owner holds its address; a
// destroyed-and-rebuilt timeline is a NEW entry at a fresh address, and
// indexes are never reused) plus the source tick — a repositioned or
// snapshot-restored Playback must re-search. Purely advisory: any mismatch
// falls back to searching and can never change results.
public struct Cursor
{
    internal nint Owner;
    internal uint Tick;
    internal int Region;
}
