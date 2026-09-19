using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tl.LaneCeilingProbe;

[StructLayout(LayoutKind.Sequential)]
internal struct LaneRec
{
    public float Effect;
    public ushort Next;
    public ushort Pad;
}

internal static unsafe class Tables
{
    internal static float* AllocFloats(int n) => (float*)NativeMemory.AlignedAlloc((nuint)(n * sizeof(float)), 64);

    internal static float* BakeForward(ushort duration, bool looping)
    {
        var eff = AllocFloats(duration + 1);
        var state = 0x9E3779B97F4A7C15ul ^ duration;
        for (var i = 0; i < duration; i++)
        {
            state ^= state << 13; state ^= state >> 7; state ^= state << 17;
            eff[i] = (float)((state >> 11) / 9007199254740992d) * 8f - 4f;
        }
        eff[duration] = 0f;
        return eff;
    }

    internal static float* BakeBackwardByPosition(ushort duration, bool looping)
    {
        var back = BakeForward((ushort)(duration + 313), looping);
        var byp = AllocFloats(duration + 1);
        for (var p = 0; p <= duration; p++)
        {
            if (p == 0) byp[p] = looping ? back[duration - 1] : 0f;
            else if (p == duration) byp[p] = looping ? 0f : back[duration - 1];
            else byp[p] = back[p - 1];
        }
        NativeMemory.AlignedFree(back);
        return byp;
    }

    internal static LaneRec* BakeRecords(float* eff, float* byp, ushort duration, bool looping, bool forward)
    {
        var rec = (LaneRec*)NativeMemory.AlignedAlloc((nuint)((duration + 1) * sizeof(LaneRec)), 64);
        for (var p = 0u; p <= duration; p++)
        {
            if (forward)
            {
                if (p < duration)
                {
                    var next = p + 1;
                    if (looping && next == duration) next = 0;
                    rec[p] = new LaneRec { Effect = eff[p], Next = (ushort)next };
                }
                else rec[p] = new LaneRec { Effect = 0f, Next = 0xFFFF };
            }
            else
            {
                if (p == 0)
                {
                    rec[p] = looping
                        ? new LaneRec { Effect = byp[0], Next = (ushort)(duration - 1) }
                        : new LaneRec { Effect = 0f, Next = 0xFFFF };
                }
                else if (p == duration)
                {
                    rec[p] = looping
                        ? new LaneRec { Effect = 0f, Next = 0xFFFF }
                        : new LaneRec { Effect = byp[duration], Next = (ushort)(duration - 1) };
                }
                else rec[p] = new LaneRec { Effect = byp[p], Next = (ushort)(p - 1) };
            }
        }
        return rec;
    }
}

internal static unsafe class Kernels
{
    internal delegate void Kernel(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void RefForward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var p = pos[i];
            if (p < duration)
            {
                fx[i] += eff[p];
                var next = (ushort)(p + 1);
                if (looping && next == duration) next = 0;
                pos[i] = next;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void RefBackward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var p = pos[i];
            if (looping)
            {
                if (p < duration)
                {
                    fx[i] += byp[p];
                    pos[i] = p == 0 ? (ushort)(duration - 1) : (ushort)(p - 1);
                }
            }
            else
            {
                if (p > 0 && p <= duration)
                {
                    fx[i] += byp[p];
                    pos[i] = (ushort)(p - 1);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void RecordForward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var p = pos[i];
            if (p < duration)
            {
                ref var r = ref rec[p];
                fx[i] += r.Effect;
                pos[i] = r.Next;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void RecordBackward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var p = pos[i];
            if (p <= duration)
            {
                ref var r = ref rec[p];
                if (r.Next != 0xFFFF)
                {
                    fx[i] += r.Effect;
                    pos[i] = r.Next;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Gather256Forward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var i = 0;
        var bound = count & ~15;
        while (i < bound)
        {
            var p = Vector256.Load(pos + i);
            var skipMask = Vector256.GreaterThan(p, lastVector);
            var next = p + one;
            if (looping) next = Vector256.ConditionalSelect(Vector256.Equals(p, lastVector), zero, next);
            next = Vector256.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            var clamped = Vector256.Min(p, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(eff, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(eff, wideHi.AsInt32(), 4);
            var skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            var effectLo = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).Store(fx + i);
            var effectHi = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).Store(fx + i + 8);
            i += 16;
        }
        RefForward(eff, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Gather256Backward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var zeroUint = Vector256<uint>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var i = 0;
        var bound = count & ~15;
        while (i < bound)
        {
            var p = Vector256.Load(pos + i);
            var next = p + step;
            Vector256<ushort> skipMask;
            if (looping)
            {
                skipMask = Vector256.GreaterThan(p, durationVector) | Vector256.Equals(p, durationVector);
                next = Vector256.ConditionalSelect(Vector256.Equals(p, zero), lastVector, next);
            }
            else
            {
                skipMask = Vector256.Equals(p, zero) | Vector256.GreaterThan(p, durationVector);
            }
            next = Vector256.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            var clamped = Vector256.Min(p, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(byp, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(byp, wideHi.AsInt32(), 4);
            Vector256<float> skipLo, skipHi;
            if (looping)
            {
                skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
                skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            }
            else
            {
                (var posLo, var posHi) = Vector256.Widen(p);
                skipLo = (Vector256.Equals(posLo, zeroUint) | Vector256.GreaterThan(posLo, durationWide)).AsSingle();
                skipHi = (Vector256.Equals(posHi, zeroUint) | Vector256.GreaterThan(posHi, durationWide)).AsSingle();
            }
            var effectLo = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).Store(fx + i);
            var effectHi = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).Store(fx + i + 8);
            i += 16;
        }
        RefBackward(byp, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Wide512Forward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var last = (ushort)(duration - 1);
        var durationVector = Vector512.Create(duration);
        var durationWide = Vector512.Create((uint)duration);
        var lastVector = Vector512.Create(last);
        var zero = Vector512<ushort>.Zero;
        var one = Vector512.Create((ushort)1);
        var i = 0;
        var bound = count & ~31;
        while (i < bound)
        {
            var p = Vector512.Load(pos + i);
            var skipMask = Vector512.GreaterThan(p, lastVector);
            var next = p + one;
            if (looping) next = Vector512.ConditionalSelect(Vector512.Equals(p, lastVector), zero, next);
            next = Vector512.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            var clamped = Vector512.Min(p, durationVector);
            (var wideLo, var wideHi) = Vector512.Widen(clamped);
            var gather0 = Avx2.GatherVector256(eff, wideLo.GetLower().AsInt32(), 4);
            var gather1 = Avx2.GatherVector256(eff, wideLo.GetUpper().AsInt32(), 4);
            var gather2 = Avx2.GatherVector256(eff, wideHi.GetLower().AsInt32(), 4);
            var gather3 = Avx2.GatherVector256(eff, wideHi.GetUpper().AsInt32(), 4);
            var skipLo = Vector512.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector512.Equals(wideHi, durationWide).AsSingle();
            var e0 = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo.GetLower(), e0, e0 + gather0).Store(fx + i);
            var e1 = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipLo.GetUpper(), e1, e1 + gather1).Store(fx + i + 8);
            var e2 = Vector256.Load(fx + i + 16);
            Vector256.ConditionalSelect(skipHi.GetLower(), e2, e2 + gather2).Store(fx + i + 16);
            var e3 = Vector256.Load(fx + i + 24);
            Vector256.ConditionalSelect(skipHi.GetUpper(), e3, e3 + gather3).Store(fx + i + 24);
            i += 32;
        }
        RefForward(eff, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Wide512Backward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var last = (ushort)(duration - 1);
        var durationVector = Vector512.Create(duration);
        var durationWide = Vector512.Create((uint)duration);
        var lastVector = Vector512.Create(last);
        var zero = Vector512<ushort>.Zero;
        var zeroUint = Vector512<uint>.Zero;
        var step = Vector512.Create((ushort)0xFFFF);
        var i = 0;
        var bound = count & ~31;
        while (i < bound)
        {
            var p = Vector512.Load(pos + i);
            var next = p + step;
            Vector512<ushort> skipMask;
            if (looping)
            {
                skipMask = Vector512.GreaterThan(p, durationVector) | Vector512.Equals(p, durationVector);
                next = Vector512.ConditionalSelect(Vector512.Equals(p, zero), lastVector, next);
            }
            else
            {
                skipMask = Vector512.Equals(p, zero) | Vector512.GreaterThan(p, durationVector);
            }
            next = Vector512.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            var clamped = Vector512.Min(p, durationVector);
            (var wideLo, var wideHi) = Vector512.Widen(clamped);
            var gather0 = Avx2.GatherVector256(byp, wideLo.GetLower().AsInt32(), 4);
            var gather1 = Avx2.GatherVector256(byp, wideLo.GetUpper().AsInt32(), 4);
            var gather2 = Avx2.GatherVector256(byp, wideHi.GetLower().AsInt32(), 4);
            var gather3 = Avx2.GatherVector256(byp, wideHi.GetUpper().AsInt32(), 4);
            Vector512<float> skipLo, skipHi;
            if (looping)
            {
                skipLo = Vector512.Equals(wideLo, durationWide).AsSingle();
                skipHi = Vector512.Equals(wideHi, durationWide).AsSingle();
            }
            else
            {
                (var posLo, var posHi) = Vector512.Widen(p);
                skipLo = (Vector512.Equals(posLo, zeroUint) | Vector512.GreaterThan(posLo, durationWide)).AsSingle();
                skipHi = (Vector512.Equals(posHi, zeroUint) | Vector512.GreaterThan(posHi, durationWide)).AsSingle();
            }
            var e0 = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo.GetLower(), e0, e0 + gather0).Store(fx + i);
            var e1 = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipLo.GetUpper(), e1, e1 + gather1).Store(fx + i + 8);
            var e2 = Vector256.Load(fx + i + 16);
            Vector256.ConditionalSelect(skipHi.GetLower(), e2, e2 + gather2).Store(fx + i + 16);
            var e3 = Vector256.Load(fx + i + 24);
            Vector256.ConditionalSelect(skipHi.GetUpper(), e3, e3 + gather3).Store(fx + i + 24);
            i += 32;
        }
        RefBackward(byp, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Dual256Forward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var i = 0;
        var bound = count & ~31;
        while (i < bound)
        {
            var p0 = Vector256.Load(pos + i);
            var p1 = Vector256.Load(pos + i + 16);
            var skip0 = Vector256.GreaterThan(p0, lastVector);
            var skip1 = Vector256.GreaterThan(p1, lastVector);
            var next0 = p0 + one;
            var next1 = p1 + one;
            if (looping)
            {
                next0 = Vector256.ConditionalSelect(Vector256.Equals(p0, lastVector), zero, next0);
                next1 = Vector256.ConditionalSelect(Vector256.Equals(p1, lastVector), zero, next1);
            }
            next0 = Vector256.ConditionalSelect(skip0, p0, next0);
            next1 = Vector256.ConditionalSelect(skip1, p1, next1);
            next0.Store(pos + i);
            next1.Store(pos + i + 16);
            var clamped0 = Vector256.Min(p0, durationVector);
            var clamped1 = Vector256.Min(p1, durationVector);
            (var loA, var hiA) = Vector256.Widen(clamped0);
            (var loB, var hiB) = Vector256.Widen(clamped1);
            var g0 = Avx2.GatherVector256(eff, loA.AsInt32(), 4);
            var g1 = Avx2.GatherVector256(eff, hiA.AsInt32(), 4);
            var g2 = Avx2.GatherVector256(eff, loB.AsInt32(), 4);
            var g3 = Avx2.GatherVector256(eff, hiB.AsInt32(), 4);
            var sA0 = Vector256.Equals(loA, durationWide).AsSingle();
            var sA1 = Vector256.Equals(hiA, durationWide).AsSingle();
            var sB0 = Vector256.Equals(loB, durationWide).AsSingle();
            var sB1 = Vector256.Equals(hiB, durationWide).AsSingle();
            var e0 = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(sA0, e0, e0 + g0).Store(fx + i);
            var e1 = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(sA1, e1, e1 + g1).Store(fx + i + 8);
            var e2 = Vector256.Load(fx + i + 16);
            Vector256.ConditionalSelect(sB0, e2, e2 + g2).Store(fx + i + 16);
            var e3 = Vector256.Load(fx + i + 24);
            Vector256.ConditionalSelect(sB1, e3, e3 + g3).Store(fx + i + 24);
            i += 32;
        }
        RefForward(eff, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Dual256Backward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var zeroUint = Vector256<uint>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var i = 0;
        var bound = count & ~31;
        while (i < bound)
        {
            var p0 = Vector256.Load(pos + i);
            var p1 = Vector256.Load(pos + i + 16);
            var next0 = p0 + step;
            var next1 = p1 + step;
            Vector256<ushort> skip0, skip1;
            if (looping)
            {
                skip0 = Vector256.GreaterThan(p0, durationVector) | Vector256.Equals(p0, durationVector);
                skip1 = Vector256.GreaterThan(p1, durationVector) | Vector256.Equals(p1, durationVector);
                next0 = Vector256.ConditionalSelect(Vector256.Equals(p0, zero), lastVector, next0);
                next1 = Vector256.ConditionalSelect(Vector256.Equals(p1, zero), lastVector, next1);
            }
            else
            {
                skip0 = Vector256.Equals(p0, zero) | Vector256.GreaterThan(p0, durationVector);
                skip1 = Vector256.Equals(p1, zero) | Vector256.GreaterThan(p1, durationVector);
            }
            next0 = Vector256.ConditionalSelect(skip0, p0, next0);
            next1 = Vector256.ConditionalSelect(skip1, p1, next1);
            next0.Store(pos + i);
            next1.Store(pos + i + 16);
            var clamped0 = Vector256.Min(p0, durationVector);
            var clamped1 = Vector256.Min(p1, durationVector);
            (var loA, var hiA) = Vector256.Widen(clamped0);
            (var loB, var hiB) = Vector256.Widen(clamped1);
            var g0 = Avx2.GatherVector256(byp, loA.AsInt32(), 4);
            var g1 = Avx2.GatherVector256(byp, hiA.AsInt32(), 4);
            var g2 = Avx2.GatherVector256(byp, loB.AsInt32(), 4);
            var g3 = Avx2.GatherVector256(byp, hiB.AsInt32(), 4);
            Vector256<float> sA0, sA1, sB0, sB1;
            if (looping)
            {
                sA0 = Vector256.Equals(loA, durationWide).AsSingle();
                sA1 = Vector256.Equals(hiA, durationWide).AsSingle();
                sB0 = Vector256.Equals(loB, durationWide).AsSingle();
                sB1 = Vector256.Equals(hiB, durationWide).AsSingle();
            }
            else
            {
                (var pl0, var ph0) = Vector256.Widen(p0);
                (var pl1, var ph1) = Vector256.Widen(p1);
                sA0 = (Vector256.Equals(pl0, zeroUint) | Vector256.GreaterThan(pl0, durationWide)).AsSingle();
                sA1 = (Vector256.Equals(ph0, zeroUint) | Vector256.GreaterThan(ph0, durationWide)).AsSingle();
                sB0 = (Vector256.Equals(pl1, zeroUint) | Vector256.GreaterThan(pl1, durationWide)).AsSingle();
                sB1 = (Vector256.Equals(ph1, zeroUint) | Vector256.GreaterThan(ph1, durationWide)).AsSingle();
            }
            var e0 = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(sA0, e0, e0 + g0).Store(fx + i);
            var e1 = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(sA1, e1, e1 + g1).Store(fx + i + 8);
            var e2 = Vector256.Load(fx + i + 16);
            Vector256.ConditionalSelect(sB0, e2, e2 + g2).Store(fx + i + 16);
            var e3 = Vector256.Load(fx + i + 24);
            Vector256.ConditionalSelect(sB1, e3, e3 + g3).Store(fx + i + 24);
            i += 32;
        }
        RefBackward(byp, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Permute8Forward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var table = Vector256.Load(eff);
        var last = (ushort)(duration - 1);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var i = 0;
        var bound = count & ~15;
        while (i < bound)
        {
            var p = Vector256.Load(pos + i);
            var skipMask = Vector256.GreaterThan(p, lastVector);
            var next = p + one;
            if (looping) next = Vector256.ConditionalSelect(Vector256.Equals(p, lastVector), zero, next);
            next = Vector256.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            (var wideLo, var wideHi) = Vector256.Widen(p);
            var idxLo = (wideLo & Vector256.Create(7u)).AsInt32();
            var idxHi = (wideHi & Vector256.Create(7u)).AsInt32();
            var gatherLo = Avx2.PermuteVar8x32(table, idxLo);
            var gatherHi = Avx2.PermuteVar8x32(table, idxHi);
            var skipLo = Vector256.GreaterThan(wideLo, Vector256.Create((uint)last)).AsSingle();
            var skipHi = Vector256.GreaterThan(wideHi, Vector256.Create((uint)last)).AsSingle();
            var effectLo = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).Store(fx + i);
            var effectHi = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).Store(fx + i + 8);
            i += 16;
        }
        RefForward(eff, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Permute8Backward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var table = Vector256.Load(byp);
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastWide = Vector256.Create((uint)last);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var zeroUint = Vector256<uint>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var i = 0;
        var bound = count & ~15;
        while (i < bound)
        {
            var p = Vector256.Load(pos + i);
            var next = p + step;
            Vector256<ushort> skipMask;
            if (looping)
            {
                skipMask = Vector256.GreaterThan(p, durationVector.AsUInt16()) | Vector256.Equals(p, durationVector.AsUInt16());
                next = Vector256.ConditionalSelect(Vector256.Equals(p, zero), lastVector, next);
            }
            else
            {
                skipMask = Vector256.Equals(p, zero) | Vector256.GreaterThan(p, durationVector.AsUInt16());
            }
            next = Vector256.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            (var wideLo, var wideHi) = Vector256.Widen(p);
            var idxLo = (wideLo & Vector256.Create(7u)).AsInt32();
            var idxHi = (wideHi & Vector256.Create(7u)).AsInt32();
            var gatherLo = Avx2.PermuteVar8x32(table, idxLo);
            var gatherHi = Avx2.PermuteVar8x32(table, idxHi);
            Vector256<float> skipLo, skipHi;
            if (looping)
            {
                skipLo = Vector256.GreaterThan(wideLo, lastWide).AsSingle();
                skipHi = Vector256.GreaterThan(wideHi, lastWide).AsSingle();
            }
            else
            {
                skipLo = (Vector256.Equals(wideLo, zeroUint) | Vector256.GreaterThan(wideLo, durationWide)).AsSingle();
                skipHi = (Vector256.Equals(wideHi, zeroUint) | Vector256.GreaterThan(wideHi, durationWide)).AsSingle();
            }
            var effectLo = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).Store(fx + i);
            var effectHi = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).Store(fx + i + 8);
            i += 16;
        }
        RefBackward(byp, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Permute32Forward(float* eff, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var tab0 = Vector256.Load(eff);
        var tab1 = Vector256.Load(eff + 8);
        var tab2 = Vector256.Load(eff + 16);
        var tab3 = Vector256.Load(eff + 24);
        var last = (ushort)(duration - 1);
        var lastVector = Vector256.Create(last);
        var lastWide = Vector256.Create((uint)last);
        var bit3 = Vector256.Create(8u);
        var bit45 = Vector256.Create(16u);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var i = 0;
        var bound = count & ~15;
        while (i < bound)
        {
            var p = Vector256.Load(pos + i);
            var skipMask = Vector256.GreaterThan(p, lastVector);
            var next = p + one;
            if (looping) next = Vector256.ConditionalSelect(Vector256.Equals(p, lastVector), zero, next);
            next = Vector256.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            (var wideLo, var wideHi) = Vector256.Widen(p);
            var gatherLo = Permute32(tab0, tab1, tab2, tab3, wideLo, bit3, bit45);
            var gatherHi = Permute32(tab0, tab1, tab2, tab3, wideHi, bit3, bit45);
            var skipLo = Vector256.GreaterThan(wideLo, lastWide).AsSingle();
            var skipHi = Vector256.GreaterThan(wideHi, lastWide).AsSingle();
            var effectLo = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).Store(fx + i);
            var effectHi = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).Store(fx + i + 8);
            i += 16;
        }
        RefForward(eff, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Permute32Backward(float* byp, LaneRec* rec, ushort duration, bool looping, ushort* pos, float* fx, int count)
    {
        var tab0 = Vector256.Load(byp);
        var tab1 = Vector256.Load(byp + 8);
        var tab2 = Vector256.Load(byp + 16);
        var tab3 = Vector256.Load(byp + 24);
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastWide = Vector256.Create((uint)last);
        var lastVector = Vector256.Create(last);
        var bit3 = Vector256.Create(8u);
        var bit45 = Vector256.Create(16u);
        var zero = Vector256<ushort>.Zero;
        var zeroUint = Vector256<uint>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var i = 0;
        var bound = count & ~15;
        while (i < bound)
        {
            var p = Vector256.Load(pos + i);
            var next = p + step;
            Vector256<ushort> skipMask;
            if (looping)
            {
                skipMask = Vector256.GreaterThan(p, durationVector) | Vector256.Equals(p, durationVector);
                next = Vector256.ConditionalSelect(Vector256.Equals(p, zero), lastVector, next);
            }
            else
            {
                skipMask = Vector256.Equals(p, zero) | Vector256.GreaterThan(p, durationVector);
            }
            next = Vector256.ConditionalSelect(skipMask, p, next);
            next.Store(pos + i);
            (var wideLo, var wideHi) = Vector256.Widen(p);
            var gatherLo = Permute32(tab0, tab1, tab2, tab3, wideLo, bit3, bit45);
            var gatherHi = Permute32(tab0, tab1, tab2, tab3, wideHi, bit3, bit45);
            Vector256<float> skipLo, skipHi;
            if (looping)
            {
                skipLo = Vector256.GreaterThan(wideLo, lastWide).AsSingle();
                skipHi = Vector256.GreaterThan(wideHi, lastWide).AsSingle();
            }
            else
            {
                skipLo = (Vector256.Equals(wideLo, zeroUint) | Vector256.GreaterThan(wideLo, durationWide)).AsSingle();
                skipHi = (Vector256.Equals(wideHi, zeroUint) | Vector256.GreaterThan(wideHi, durationWide)).AsSingle();
            }
            var effectLo = Vector256.Load(fx + i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).Store(fx + i);
            var effectHi = Vector256.Load(fx + i + 8);
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).Store(fx + i + 8);
            i += 16;
        }
        RefBackward(byp, rec, duration, looping, pos + i, fx + i, count - i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector256<float> Permute32(Vector256<float> t0, Vector256<float> t1, Vector256<float> t2, Vector256<float> t3, Vector256<uint> idx, Vector256<uint> bit3, Vector256<uint> bit45)
    {
        var idxI = idx.AsInt32();
        var p0 = Avx2.PermuteVar8x32(t0, idxI);
        var p1 = Avx2.PermuteVar8x32(t1, idxI);
        var p2 = Avx2.PermuteVar8x32(t2, idxI);
        var p3 = Avx2.PermuteVar8x32(t3, idxI);
        var hi01 = Vector256.Equals(idx & bit3, bit3).AsSingle();
        var hi23 = Vector256.Equals(idx & bit45, bit45).AsSingle();
        var lo = Vector256.ConditionalSelect(hi01, p1, p0);
        var hi = Vector256.ConditionalSelect(hi01, p3, p2);
        return Vector256.ConditionalSelect(hi23, hi, lo);
    }
}
