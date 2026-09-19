using System.Runtime.InteropServices;

namespace Tl.LaneCeilingProbe;

internal static unsafe class Parity
{
    internal sealed record KernelArm(string Name, Kernels.Kernel Forward, Kernels.Kernel Backward, Func<ushort, bool, bool, bool> Applies, bool BackwardUsesBackTable = false);

    internal static readonly KernelArm[] Arms =
    [
        new("record", Kernels.RecordForward, Kernels.RecordBackward, (_, _, _) => true),
        new("gather256", Kernels.Gather256Forward, Kernels.Gather256Backward, (_, _, _) => Avx2Guard()),
        new("dual256", Kernels.Dual256Forward, Kernels.Dual256Backward, (_, _, _) => Avx2Guard()),
        new("wide512", Kernels.Wide512Forward, Kernels.Wide512Backward, (_, _, _) => Avx2Guard()),
        new("permute8", Kernels.Permute8Forward, Kernels.Permute8Backward, (d, _, _) => d <= 8 && Avx2Guard(), BackwardUsesBackTable: true),
        new("permute32", Kernels.Permute32Forward, Kernels.Permute32Backward, (d, looping, forward) => (forward || looping ? d <= 32 : d <= 31) && Avx2Guard()),
    ];

    static bool Avx2Guard() => System.Runtime.Intrinsics.X86.Avx2.IsSupported;

    static readonly ushort[] Durations = [1, 2, 4, 6, 8, 15, 16, 17, 31, 32, 33, 255, 1024, 65500];

    internal static int Run()
    {
        var failures = 0;
        var cases = 0;
        foreach (var duration in Durations)
        foreach (var looping in new[] { true, false })
        foreach (var forward in new[] { true, false })
        {
            var eff = Tables.BakeForward(duration, looping);
            var back = Tables.BakeBackwardRaw(duration);
            var byp = Tables.ByPositionFrom(back, duration, looping);
            var effPad = PadTo(eff, duration, looping);
            var backPad = PadTo(back, duration, looping);
            var bypPad = PadTo(byp, duration, looping);
            var rec = Tables.BakeRecords(eff, byp, duration, looping, forward);

            foreach (var dist in Distributions(duration))
            {
                var count = dist.Length;
                var expectedPos = new ushort[count];
                var expectedFx = new float[count];
                dist.CopyTo(expectedPos, 0);
                Array.Copy(FxSeed, expectedFx, count);
                fixed (ushort* ep = expectedPos)
                fixed (float* ef = expectedFx)
                {
                    if (forward)
                        Kernels.RefForward(eff, rec, duration, looping, ep, ef, count);
                    else
                        Kernels.RefBackward(byp, rec, duration, looping, ep, ef, count);
                }

                foreach (var arm in Arms)
                {
                    if (!arm.Applies(duration, looping, forward)) continue;
                    cases++;
                    var pos = (ushort[])dist.Data.Clone();
                    var fx = new float[count];
                    Array.Copy(FxSeed, fx, count);
                    fixed (ushort* pp = pos)
                    fixed (float* fp = fx)
                    {
                        if (forward)
                            arm.Forward(effPad, rec, duration, looping, pp, fp, count);
                        else
                            arm.Backward(arm.BackwardUsesBackTable ? backPad : bypPad, rec, duration, looping, pp, fp, count);
                    }
                    if (!pos.AsSpan().SequenceEqual(expectedPos) || !BitIdentical(fx, expectedFx))
                    {
                        failures++;
                        var firstDiff = -1;
                        for (var k = 0; k < count; k++)
                            if (pos[k] != expectedPos[k] || BitConverter.SingleToInt32Bits(fx[k]) != BitConverter.SingleToInt32Bits(expectedFx[k])) { firstDiff = k; break; }
                        Console.WriteLine($"FAIL {arm.Name} duration={duration} looping={looping} forward={forward} dist={dist.Name} first-diff at {firstDiff}: pos {pos[Math.Max(0, firstDiff)]} vs {expectedPos[Math.Max(0, firstDiff)]}, fx {fx[Math.Max(0, firstDiff)]} vs {expectedFx[Math.Max(0, firstDiff)]}");
                    }
                }
            }

            NativeMemory.AlignedFree(eff);
            NativeMemory.AlignedFree(back);
            NativeMemory.AlignedFree(byp);
            NativeMemory.AlignedFree(effPad);
            NativeMemory.AlignedFree(backPad);
            NativeMemory.AlignedFree(bypPad);
            NativeMemory.AlignedFree(rec);
        }
        Console.WriteLine($"parity: {cases - failures}/{cases} cases PASS ({failures} failures)");
        return failures;
    }

    static bool BitIdentical(float[] a, float[] b)
    {
        for (var i = 0; i < a.Length; i++)
            if (BitConverter.SingleToInt32Bits(a[i]) != BitConverter.SingleToInt32Bits(b[i])) return false;
        return true;
    }

    static float* PadTo(float* source, ushort duration, bool looping)
    {
        var size = Math.Max(64, duration + 1);
        var padded = Tables.AllocFloats(size);
        Buffer.MemoryCopy(source, padded, (long)size * sizeof(float), (long)(duration + 1) * sizeof(float));
        for (var i = duration + 1; i < size; i++) padded[i] = 0f;
        return padded;
    }

    static readonly float[] FxSeed = BuildFx(8192);

    static float[] BuildFx(int n)
    {
        var fx = new float[n];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < n; i++)
        {
            state ^= state << 13; state ^= state >> 7; state ^= state << 17;
            fx[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        return fx;
    }

    sealed class Dist
    {
        public ushort[] Data = [];
        public string Name = "";
        public int Length => Data.Length;
        public void CopyTo(ushort[] target, int start) => Array.Copy(Data, 0, target, start, Data.Length);
        public static implicit operator ushort[](Dist d) => d.Data;
    }

    static Dist[] Distributions(ushort duration)
    {
        const int n = 4199;
        var uniform = new Dist { Name = "uniform", Data = new ushort[n] };
        Array.Fill(uniform.Data, (ushort)Math.Min(5, (int)duration));

        var staggered = new Dist { Name = "staggered", Data = new ushort[n] };
        for (var i = 0; i < n; i++) staggered.Data[i] = (ushort)(i % duration);

        var waves = new Dist { Name = "waves", Data = new ushort[n] };
        for (var i = 0; i < n; i++) waves.Data[i] = (ushort)(i / 100 % duration);

        var edges = new Dist { Name = "edges", Data = new ushort[n] };
        var edgeValues = new ushort[] { 0, 1, (ushort)(duration - 1), duration, (ushort)Math.Min(65535, duration + 1), 65535, 7, (ushort)(duration / 2) };
        for (var i = 0; i < n; i++) edges.Data[i] = edgeValues[i % edgeValues.Length];

        var random = new Dist { Name = "random", Data = new ushort[n] };
        var state = 0xB5297A4D3B4D1C35ul ^ duration;
        for (var i = 0; i < n; i++)
        {
            state ^= state << 13; state ^= state >> 7; state ^= state << 17;
            random.Data[i] = (ushort)(state >> 48);
        }

        var tail = new Dist { Name = "tail", Data = new ushort[37] };
        for (var i = 0; i < 37; i++) tail.Data[i] = (ushort)((i * 7) % (duration + 8));

        return [uniform, staggered, waves, edges, random, tail];
    }
}
