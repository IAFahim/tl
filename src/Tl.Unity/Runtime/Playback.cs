using System;
using System.Runtime.InteropServices;

namespace Tl
{
    [Flags]
    public enum PlaybackFlags : byte
    {
        None = 0,
        Started = 1 << 0,
        Stopped = 1 << 1
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
        Reverse = 1 << 7
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Playback : IEquatable<Playback>
    {
        public readonly long Position;
        public readonly uint GameTick;
        public readonly ushort Owner;
        public readonly PlaybackFlags Flags;

        public Playback(long position, uint gameTick, ushort owner, PlaybackFlags flags)
        {
            Position = position;
            GameTick = gameTick;
            Owner = owner;
            Flags = flags;
        }

        public bool Has(PlaybackFlags flags)
        {
            return (Flags & flags) == flags;
        }

        public bool Equals(Playback other)
        {
            return Position == other.Position
                && GameTick == other.GameTick
                && Owner == other.Owner
                && Flags == other.Flags;
        }

        public override bool Equals(object value)
        {
            return value is Playback && Equals((Playback)value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Position.GetHashCode();
                hash = hash * 397 ^ (int)GameTick;
                hash = hash * 397 ^ Owner;
                return hash * 397 ^ (byte)Flags;
            }
        }

        public static bool operator ==(Playback left, Playback right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Playback left, Playback right)
        {
            return !left.Equals(right);
        }
    }
}
