using System.Runtime.Intrinsics.X86;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class KernelTailTests
{
    const ushort NextSentinel = 0xABCD;
    const float EffectSentinel = -777.25f;
    const int Pad = 24;

    public static TheoryData<ushort, bool, bool, bool, int, int> Configurations
    {
        get
        {
            var data = new TheoryData<ushort, bool, bool, bool, int, int>();
            foreach (var duration in new ushort[] { 9, 16, 33, 1024 })
            foreach (var looping in new[] { true, false })
            foreach (var forward in new[] { true, false })
            foreach (var hasNext in new[] { true, false })
            foreach (var start in new[] { 0, 16 })
            foreach (var tail in new[] { 0, 1, 7, 8, 15, 16, 17, 33 })
                data.Add(duration, looping, forward, hasNext, start, start + 16 + tail);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void GatherKernelsProcessOnlyFullBlocksAtUnalignedLimits(ushort duration, bool looping, bool forward, bool hasNext, int start, int limit)
    {
        if (!Avx2.IsSupported) return;
        var index = TimelineAsset.Load(Bake(duration, looping));
        var slot = Timeline<RoutingTrack, RoutingClip>.View(index);

        var positions = new ushort[limit + Pad];
        var next = new ushort[limit + Pad];
        var effects = new float[limit + Pad];
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = (ushort)((i * 37 + 11) % (duration + 3));
            next[i] = NextSentinel;
            effects[i] = EffectSentinel;
        }

        if (forward)
            LaneOps.EffectForward(slot.Forward, duration, looping, positions, hasNext ? next : default, effects, start, limit);
        else
            LaneOps.EffectBackward(slot.BackwardByPosition, duration, looping, positions, hasNext ? next : default, effects, start, limit);

        var processedEnd = start + (((limit - start) >> 4) << 4);
        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            Assert.Equal((ushort)((i * 37 + 11) % (duration + 3)), position);
            if (i < start || i >= processedEnd || i >= limit)
            {
                Assert.Equal(NextSentinel, next[i]);
                Assert.Equal(EffectSentinel, effects[i]);
                continue;
            }
            var expectedNext = NextSentinel;
            var expectedEffect = EffectSentinel;
            if (forward)
            {
                if (position < duration)
                {
                    ref var record = ref slot.ForwardRecords[position];
                    expectedEffect += record.Effect;
                    expectedNext = record.Next;
                }
                else expectedNext = position;
            }
            else
            {
                if (position <= duration)
                {
                    ref var record = ref slot.BackwardRecords[position];
                    if (record.Next != LaneMovementRecord.Skipped)
                    {
                        expectedEffect += record.Effect;
                        expectedNext = record.Next;
                    }
                    else expectedNext = position;
                }
                else expectedNext = position;
            }
            if (hasNext) Assert.Equal(expectedNext, next[i]);
            else Assert.Equal(NextSentinel, next[i]);
            Assert.Equal(expectedEffect, effects[i]);
        }
    }

    static byte[] Bake(ushort duration, bool looping)
    {
        var baker = new Tl.TestSupport.DomainBaker()
            .Track<RoutingTrack, RoutingClip>(new RoutingTrack(2f))
            .Clip(0, 0u, (uint)(duration * 6 / 10), new RoutingClip(1.25f))
            .Clip(0, (uint)(duration * 6 / 10), duration, new RoutingClip(-0.5f));
        if (looping) baker.Looping();
        return baker.Bake();
    }
}
