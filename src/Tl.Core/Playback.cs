using System.Runtime.InteropServices;

namespace Tl;

[Flags]
public enum PlaybackFlags : ushort
{
    None          = 0,
    Started       = 1 << 0,
    Stopped       = 1 << 1,
    LastLoopFrame = 1 << 2,
    Completed     = 1 << 3,
}

public enum ClipState : byte
{
    Enter,
    Stay,
    Exit,
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Playback : IEquatable<Playback>
{
    public readonly uint Tick;
    public readonly ushort Cycles;
    public readonly ushort Owner;
    public readonly PlaybackFlags Flags;

    internal Playback(uint tick, ushort cycles, ushort owner, PlaybackFlags flags)
    {
        Tick = tick;
        Cycles = cycles;
        Owner = owner;
        Flags = flags;
    }

    public bool Has(PlaybackFlags flags) => (Flags & flags) == flags;

    public bool Equals(Playback other) => Tick == other.Tick && Cycles == other.Cycles && Owner == other.Owner && Flags == other.Flags;

    public override bool Equals(object? obj) => obj is Playback other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Tick, Cycles, Owner, (ushort)Flags);

    public static bool operator ==(Playback left, Playback right) => left.Equals(right);

    public static bool operator !=(Playback left, Playback right) => !left.Equals(right);

    public override string ToString() => $"Playback {{ Tick = {Tick}, Cycles = {Cycles}, Owner = {Owner}, Flags = {Flags} }}";
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Playback<TTimeline> : IEquatable<Playback<TTimeline>>
    where TTimeline : unmanaged, ITimeline
{
    public readonly uint Tick;
    public readonly ushort Cycles;
    public readonly PlaybackFlags Flags;

    internal Playback(uint tick, ushort cycles, PlaybackFlags flags)
    {
        Tick = tick;
        Cycles = cycles;
        Flags = flags;
    }

    public bool Has(PlaybackFlags flags) => (Flags & flags) == flags;

    public bool Equals(Playback<TTimeline> other) => Tick == other.Tick && Cycles == other.Cycles && Flags == other.Flags;

    public override bool Equals(object? obj) => obj is Playback<TTimeline> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Tick, Cycles, (ushort)Flags);

    public static bool operator ==(Playback<TTimeline> left, Playback<TTimeline> right) => left.Equals(right);

    public static bool operator !=(Playback<TTimeline> left, Playback<TTimeline> right) => !left.Equals(right);

    public override string ToString() => $"Playback<{typeof(TTimeline).Name}> {{ Tick = {Tick}, Cycles = {Cycles}, Flags = {Flags} }}";
}
