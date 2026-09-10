using System;
using System.Runtime.InteropServices;

namespace Tl
{
    [Flags]
    public enum PlaybackFlags : ushort
    {
        None = 0,
        Started = 1,
        Stopped = 2,
        LastLoopFrame = 4,
        Completed = 8
    }

    public enum ClipState : byte
    {
        Enter,
        Stay,
        Exit
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Playback : IEquatable<Playback>
    {
        public readonly uint Tick;
        public readonly ushort Cycles;
        public readonly ushort Owner;
        public readonly PlaybackFlags Flags;

        public Playback(uint tick, ushort cycles, ushort owner, PlaybackFlags flags)
        {
            Tick = tick;
            Cycles = cycles;
            Owner = owner;
            Flags = flags;
        }

        public bool Has(PlaybackFlags flags)
        {
            return (Flags & flags) == flags;
        }

        public bool Equals(Playback other)
        {
            return Tick == other.Tick && Cycles == other.Cycles && Owner == other.Owner && Flags == other.Flags;
        }

        public override bool Equals(object value)
        {
            return value is Playback && Equals((Playback)value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Tick;
                hash = hash * 397 ^ Cycles;
                hash = hash * 397 ^ Owner;
                return hash * 397 ^ (ushort)Flags;
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
