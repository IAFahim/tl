using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Frent;

namespace Tl;

public static class TimelineLane
{
    public static void Run<TTrack, TClip, TIndex, TPosition, TEffect>(this World world, bool forward = true)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2 || Unsafe.SizeOf<TEffect>() != 4)
            throw new ArgumentException("Lane components must be single-field: index ushort, position ushort, effect float.");
        foreach (var (ids, positions, effects) in world
                     .Query<TIndex, TPosition, TEffect>()
                     .EnumerateChunks<TIndex, TPosition, TEffect>())
        {
            var idColumn = MemoryMarshal.Cast<TIndex, ushort>(ids);
            var positionColumn = MemoryMarshal.Cast<TPosition, ushort>(positions);
            Timeline<TTrack, TClip>.Apply(
                idColumn,
                positionColumn,
                positionColumn,
                forward,
                MemoryMarshal.Cast<TEffect, float>(effects));
        }
    }
}
