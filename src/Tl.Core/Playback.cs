using System.Runtime.InteropServices;

namespace Tl;

[Flags]
public enum PlaybackFlags : byte
{
    None = 0,
    Started = 1 << 0,
    Stopped = 1 << 1,
}

[Flags]
public enum FrameFlags : byte
{
    None = 0,
    ClipStart = 1 << 0,
    ClipEnd = 1 << 1,
    TimelineStart = 1 << 2,
    TimelineEnd = 1 << 3,
    CompletedBefore = 1 << 4,
    CompletedAfter = 1 << 5,
    Looping = 1 << 6,
    Reverse = 1 << 7,
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Playback : IEquatable<Playback>
{
    public readonly long Position;
    public readonly uint GameTick;
    public readonly ushort Owner;
    public readonly PlaybackFlags Flags;

    internal Playback(long position, uint gameTick, ushort owner, PlaybackFlags flags)
    {
        Position = position;
        GameTick = gameTick;
        Owner = owner;
        Flags = flags;
    }

    public bool Has(PlaybackFlags flags) => (Flags & flags) == flags;

    public bool Equals(Playback other)
        => Position == other.Position && GameTick == other.GameTick && Owner == other.Owner && Flags == other.Flags;

    public override bool Equals(object? obj) => obj is Playback other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Position, GameTick, Owner, (byte)Flags);

    public static bool operator ==(Playback left, Playback right) => left.Equals(right);

    public static bool operator !=(Playback left, Playback right) => !left.Equals(right);

    public override string ToString()
        => $"Playback {{ Position = {Position}, GameTick = {GameTick}, Owner = {Owner}, Flags = {Flags} }}";
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Playback<TTimeline> : IEquatable<Playback<TTimeline>>
    where TTimeline : unmanaged, ITimeline
{
    public readonly long Position;
    public readonly uint GameTick;
    public readonly PlaybackFlags Flags;

    internal Playback(long position, uint gameTick, PlaybackFlags flags)
    {
        Position = position;
        GameTick = gameTick;
        Flags = flags;
    }

    public bool Has(PlaybackFlags flags) => (Flags & flags) == flags;

    public bool Equals(Playback<TTimeline> other)
        => Position == other.Position && GameTick == other.GameTick && Flags == other.Flags;

    public override bool Equals(object? obj) => obj is Playback<TTimeline> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Position, GameTick, (byte)Flags);

    public static bool operator ==(Playback<TTimeline> left, Playback<TTimeline> right) => left.Equals(right);

    public static bool operator !=(Playback<TTimeline> left, Playback<TTimeline> right) => !left.Equals(right);

    public override string ToString()
        => $"Playback<{typeof(TTimeline).Name}> {{ Position = {Position}, GameTick = {GameTick}, Flags = {Flags} }}";
}
