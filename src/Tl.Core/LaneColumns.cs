using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public static class Lane<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        Timeline<TTrack, TClip>.Apply(
            MemoryMarshal.Cast<TIndex, ushort>(indices),
            MemoryMarshal.Cast<TPosition, ushort>(positions),
            forward,
            MemoryMarshal.Cast<TEffect, float>(effects));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Advance<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Timeline.Advance(MemoryMarshal.Cast<TIndex, ushort>(indices), MemoryMarshal.Cast<TPosition, ushort>(positions), forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(in TIndex index, in TPosition position, bool forward, ref TEffect effect)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        Timeline<TTrack, TClip>.Apply(
            Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TPosition, ushort>(ref Unsafe.AsRef(in position)), 1),
            forward,
            MemoryMarshal.CreateSpan(ref Unsafe.As<TEffect, float>(ref effect), 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Advance<TIndex, TPosition>(in TIndex index, ref TPosition position, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Timeline.Advance(
            Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
            MemoryMarshal.CreateSpan(ref Unsafe.As<TPosition, ushort>(ref position), 1),
            forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void CheckSizes<TIndex, TPosition, TEffect>()
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2 || Unsafe.SizeOf<TEffect>() != 4)
            ThrowColumnSizes();
    }

    static void ThrowColumnSizes()
        => throw new ArgumentException("Lane columns must be single-field: index ushort, position ushort, effect float.");
}
