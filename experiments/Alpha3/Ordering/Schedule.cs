using System;
using System.Runtime.InteropServices;

namespace OperationOrderProof
{
    internal enum OperationId : ushort
    {
        A = 1,
        B = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Occurrence
    {
        internal readonly OperationId Operation;
        internal readonly byte TrackIndex;
        internal readonly byte Flags;
        internal readonly uint PayloadIndex;

        internal Occurrence(OperationId operation, byte trackIndex, uint payloadIndex, byte flags = 0)
        {
            Operation = operation;
            TrackIndex = trackIndex;
            Flags = flags;
            PayloadIndex = payloadIndex;
        }

        internal ulong Token
            => (ulong)Operation << 48 | (ulong)TrackIndex << 40 | PayloadIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FrameSlice
    {
        internal readonly uint Offset;
        internal readonly ushort Count;

        internal FrameSlice(uint offset, ushort count)
        {
            Offset = offset;
            Count = count;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Asset
    {
        internal readonly uint FrameOffset;
        internal readonly ushort Duration;
        internal readonly ushort Flags;

        internal Asset(uint frameOffset, ushort duration, ushort flags = 0)
        {
            FrameOffset = frameOffset;
            Duration = duration;
            Flags = flags;
        }
    }

    [Flags]
    internal enum SelectionFlags : byte
    {
        None = 0,
        Advances = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Selection
    {
        internal readonly uint OccurrenceOffset;
        internal readonly uint NextPosition;
        internal readonly ushort Count;
        internal readonly sbyte Direction;
        internal readonly SelectionFlags Flags;

        internal Selection(
            uint occurrenceOffset,
            uint nextPosition,
            ushort count,
            sbyte direction,
            SelectionFlags flags)
        {
            OccurrenceOffset = occurrenceOffset;
            NextPosition = nextPosition;
            Count = count;
            Direction = direction;
            Flags = flags;
        }

        internal bool Advances => (Flags & SelectionFlags.Advances) != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityState
    {
        internal ushort AssetIndex;
        internal uint Position;
        internal Selection Selection;

        internal EntityState(ushort assetIndex, uint position = 0)
        {
            AssetIndex = assetIndex;
            Position = position;
            Selection = default;
        }
    }

    internal sealed class Schedule
    {
        internal readonly Asset[] Assets;
        internal readonly FrameSlice[] Frames;
        internal readonly Occurrence[] Occurrences;

        internal Schedule(Asset[] assets, FrameSlice[] frames, Occurrence[] occurrences)
        {
            Assets = assets;
            Frames = frames;
            Occurrences = occurrences;
        }

        internal ref readonly Occurrence Get(in Selection selection, int stage)
        {
            var ordinal = selection.Direction > 0 ? stage : selection.Count - 1 - stage;
            return ref Occurrences[selection.OccurrenceOffset + ordinal];
        }
    }
}
