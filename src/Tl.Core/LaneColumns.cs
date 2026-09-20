using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public static class Lane<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward, Span<TEffect> effects)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        var positionColumn = MemoryMarshal.Cast<TPosition, ushort>(positions);
        Timeline<TTrack, TClip>.Apply(
            MemoryMarshal.Cast<TIndex, ushort>(indices),
            positionColumn,
            positionColumn,
            forward,
            MemoryMarshal.Cast<TEffect, float>(effects));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Step<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Timeline.Step(MemoryMarshal.Cast<TIndex, ushort>(indices), MemoryMarshal.Cast<TPosition, ushort>(positions), forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Apply<TIndex, TPosition, TEffect>(in TIndex index, ref TPosition position, bool forward, ref TEffect effect)
        where TIndex : struct
        where TPosition : struct
        where TEffect : struct
    {
        CheckSizes<TIndex, TPosition, TEffect>();
        ref var slot = ref Unsafe.As<TPosition, ushort>(ref position);
        Timeline<TTrack, TClip>.Apply(
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)), 1),
            MemoryMarshal.CreateReadOnlySpan(ref slot, 1),
            MemoryMarshal.CreateSpan(ref slot, 1),
            forward,
            MemoryMarshal.CreateSpan(ref Unsafe.As<TEffect, float>(ref effect), 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Step<TIndex, TPosition>(in TIndex index, ref TPosition position, bool forward)
        where TIndex : struct
        where TPosition : struct
    {
        if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
            ThrowColumnSizes();
        Timeline.Step(
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)), 1),
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
