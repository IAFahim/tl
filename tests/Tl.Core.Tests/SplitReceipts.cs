using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

/// <summary>
/// v0.2 receipts for the input/result split: the engine treats
/// <c>in TInput</c> as read-only context and <c>ref TResult</c> as the live
/// state. Named receipts:
///  (a) input bit-identical after a full forward walk and backward rewind,
///  (b) same seed on the input gives v0.1-identical accumulation (hand oracle),
///  (c) managed-field results still mutate through pointer dispatch.
/// </summary>
public class SplitReceipts
{
    public enum Kind : byte { Alpha, Beta, Gamma }

    public readonly struct Point2(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }

    /// Managed-free on purpose: several field kinds so the memcmp receipt is meaningful.
    [StructLayout(LayoutKind.Sequential)]
    public struct RichInput
    {
        public byte B;
        public short S;
        public int I;
        public long L;
        public uint U;
        public ulong UL;
        public float F;
        public double D;
        public bool Flag;
        public char C;
        public Kind K;
        public Point2 P;
        public decimal M;
    }

    public readonly record struct RichClip(float Value);

    public readonly struct RichTrack : IBlend<RichClip>
    {
        public void Blend(in RichClip first, in RichClip second, float t, out RichClip result)
            => result = new RichClip(first.Value * (1f - t) + second.Value * t);
    }

    /// Reads every input field (proving the input actually flows in) and
    /// accumulates it into the result. Writing to <c>input</c> is not
    /// expressible: the hooks receive it by <c>in</c>.
    public struct RichResult :
        IForward<RichTrack, RichClip, RichInput, RichResult>,
        IBackward<RichTrack, RichClip, RichInput, RichResult>
    {
        public double Seen;
        public byte LastB;
        public Kind LastK;

        public void Forward(in Tracks<RichTrack, RichClip> tracks, in RichInput input, in uint tick, ref RichResult result)
        {
            foreach (var work in tracks)
                if (work.State == ClipState.Stay)
                {
                    result.Seen += work.Clip.Value * input.I + input.L + input.D + (double)input.M + (double)input.F
                        + input.B + input.S + input.U + input.UL + input.C + (byte)input.K
                        + input.P.X + input.P.Y + (input.Flag ? 1 : 0);
                    result.LastB = input.B;
                    result.LastK = input.K;
                }
        }

        public void Backward(in Tracks<RichTrack, RichClip> tracks, in RichInput input, in uint tick, ref RichResult result)
        {
            foreach (var work in tracks)
                if (work.State == ClipState.Stay)
                    result.Seen -= work.Clip.Value * input.I + input.L + input.D + (double)input.M + (double)input.F
                        + input.B + input.S + input.U + input.UL + input.C + (byte)input.K
                        + input.P.X + input.P.Y + (input.Flag ? 1 : 0);
        }
    }

    [Fact]
    public void Receipt_InputUnchangedAfterForwardWalkAndBackwardRewind()
    {
        var id = Timeline<RichTrack, RichClip>.Build(b =>
        {
            var t = b.Track(new RichTrack());
            b.Clip(in t, new RichClip(1.5f), 0, 50);
            b.Clip(in t, new RichClip(3.5f), 25, 75);
        }).InMemory();

        var input = new RichInput
        {
            B = 211, S = -1234, I = 1_000_000, L = long.MinValue / 7, U = 4_000_000_000u,
            UL = ulong.MaxValue / 3, F = float.Pi, D = double.E, Flag = true, C = 'Z',
            K = Kind.Gamma, P = new Point2(-17, 42), M = 1234.5678m,
        };
        var before = input; // snapshot for comparison

        var result = new RichResult();
        var pb = Timeline.Start(id);

        // Full forward walk across the whole timeline (including the blend region).
        for (uint tick = 0; tick < 75; tick++)
            pb = Timeline.Forward(id, in pb, in input, ref result, tick);

        Assert.NotEqual(0.0, result.Seen);
        Assert.True(BitCompare.Identical(in before, in input), "input changed during the forward walk");
        Assert.Equal((byte)211, input.B);
        Assert.Equal(Kind.Gamma, input.K);
        Assert.Equal(1_000_000, input.I);
        Assert.Equal(long.MinValue / 7, input.L);
        Assert.Equal(1234.5678m, input.M);

        // Full backward rewind back to the start.
        for (uint tick = 74; tick > 0; tick--)
            pb = Timeline.Backward(id, in pb, in input, ref result, tick);
        pb = Timeline.Backward(id, in pb, in input, ref result, 0u);

        Assert.True(BitCompare.Identical(in before, in input), "input changed during the backward rewind");
        Assert.Equal((byte)211, input.B);
        Assert.Equal('Z', input.C);
        Assert.Equal(float.Pi, input.F);

        Timeline.Destroy(id);
    }

    // --- Receipt (b): same seed on the input, v0.1-identical accumulation ---

    public readonly record struct SeedClip(float Value);

    public readonly struct SeedTrack : IBlend<SeedClip>
    {
        public void Blend(in SeedClip first, in SeedClip second, float t, out SeedClip result)
            => result = new SeedClip(first.Value * (1f - t) + second.Value * t);
    }

    public readonly record struct SeedInput(float Seed, float Scale);

    /// v0.1 semantics: a single struct whose seed start plus per-Stay
    /// accumulation is exactly reproduced when the seed rides the input.
    public struct SeedResult :
        IForward<SeedTrack, SeedClip, SeedInput, SeedResult>,
        IBackward<SeedTrack, SeedClip, SeedInput, SeedResult>
    {
        public float Value;

        public void Forward(in Tracks<SeedTrack, SeedClip> tracks, in SeedInput input, in uint tick, ref SeedResult result)
        {
            foreach (var work in tracks)
                if (work.State == ClipState.Stay)
                    result.Value = result.Value + work.Clip.Value * input.Scale + input.Seed;
        }

        public void Backward(in Tracks<SeedTrack, SeedClip> tracks, in SeedInput input, in uint tick, ref SeedResult result)
        {
            foreach (var work in tracks)
                if (work.State == ClipState.Stay)
                    result.Value = result.Value - work.Clip.Value * input.Scale - input.Seed;
        }
    }

    [Fact]
    public void Receipt_SameSeedSameAccumulationMatchesHandOracle()
    {
        // Single clip [2, 10), value 3f, playback starts AT tick 2: walking
        // 2..9 gives Stay@2..8 (starting at 2 does not cross the entry edge)
        // and Exit@9 - the Stay visits are computable by hand.
        var id = Timeline<SeedTrack, SeedClip>.Build(b =>
        {
            var t = b.Track(new SeedTrack());
            b.Clip(in t, new SeedClip(3f), 2, 10);
        }).InMemory();

        var input = new SeedInput(Seed: 0.25f, Scale: 2f);

        // Hand oracle: replicate the exact v0.1 single-struct accumulation,
        // one Stay visit at a time, in walk order (left-to-right float ops).
        float Oracle(float seed)
        {
            var value = seed;
            for (uint tick = 2; tick <= 8; tick++)
                value = value + 3f * 2f + seed;
            return value;
        }

        var expected = Oracle(input.Seed);

        // Run one: seed rides the input; the result starts from the seed.
        var result = new SeedResult { Value = input.Seed };
        var pb = Timeline.Start(id, 2u);
        pb = Timeline.Forward(id, in pb, in input, ref result, 2u, 3u, 4u, 5u, 6u, 7u, 8u, 9u);

        Assert.Equal(BitConverter.SingleToInt32Bits(expected), BitConverter.SingleToInt32Bits(result.Value));

        // Run two: same input, fresh result - same seed, same accumulation, bit for bit.
        var result2 = new SeedResult { Value = input.Seed };
        var pb2 = Timeline.Start(id, 2u);
        pb2 = Timeline.Forward(id, in pb2, in input, ref result2, 2u, 3u, 4u, 5u, 6u, 7u, 8u, 9u);

        Assert.Equal(BitConverter.SingleToInt32Bits(result.Value), BitConverter.SingleToInt32Bits(result2.Value));

        // A different seed rides the input and shifts the outcome by the oracle delta.
        var otherInput = new SeedInput(Seed: 1f, Scale: 2f);
        var result3 = new SeedResult { Value = otherInput.Seed };
        var pb3 = Timeline.Start(id, 2u);
        pb3 = Timeline.Forward(id, in pb3, in otherInput, ref result3, 2u, 3u, 4u, 5u, 6u, 7u, 8u, 9u);

        Assert.Equal(BitConverter.SingleToInt32Bits(Oracle(1f)), BitConverter.SingleToInt32Bits(result3.Value));

        Timeline.Destroy(id);
    }

    // --- Receipt (c): managed-field result mutates through pointer dispatch ---

    public readonly record struct ManagedClip(float Value);

    public readonly struct ManagedTrack : IBlend<ManagedClip>
    {
        public void Blend(in ManagedClip first, in ManagedClip second, float t, out ManagedClip result)
            => result = new ManagedClip(first.Value * (1f - t) + second.Value * t);
    }

    public struct ManagedResult :
        IForward<ManagedTrack, ManagedClip, NoInput, ManagedResult>,
        IBackward<ManagedTrack, ManagedClip, NoInput, ManagedResult>
    {
        public string Name;
        public List<float> Values;

        public void Forward(in Tracks<ManagedTrack, ManagedClip> tracks, in NoInput input, in uint tick, ref ManagedResult result)
        {
            // The defect-B pattern: allocate and force a compacting GC from
            // inside the hook while the result reference is on the dispatch stack.
            for (int i = 0; i < 50; i++)
                _ = new byte[1024];
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

            foreach (var work in tracks)
            {
                result.Values.Add(work.Clip.Value);
                result.Name = result.Name.ToLowerInvariant();
            }
        }

        public void Backward(in Tracks<ManagedTrack, ManagedClip> tracks, in NoInput input, in uint tick, ref ManagedResult result)
        {
            foreach (var work in tracks)
                result.Values.Remove(work.Clip.Value);
        }
    }

    private sealed class ResultHolder
    {
        public ManagedResult Result;
    }

    [Fact]
    public void Receipt_ManagedFieldResultMutatesThroughPointerDispatch()
    {
        var id = Timeline<ManagedTrack, ManagedClip>.Build(b =>
        {
            var t = b.Track(new ManagedTrack());
            b.Clip(in t, new ManagedClip(9f), 0, 10);
        }).InMemory();

        // The result lives in a class field (a movable heap location) while
        // dispatch holds a byref to it through the function pointer.
        var holder = new ResultHolder
        {
            Result = new ManagedResult { Name = "PLAYER-ONE", Values = [] },
        };
        var input = default(NoInput);

        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, in input, ref holder.Result, 1u, 2u, 3u, 4u);

        Assert.Equal(4, holder.Result.Values.Count);
        Assert.All(holder.Result.Values, v => Assert.Equal(9f, v));
        Assert.Equal("player-one", holder.Result.Name); // string field mutated through the byref

        pb = Timeline.Backward(id, in pb, in input, ref holder.Result, 3u, 2u, 1u, 0u);
        Assert.Empty(holder.Result.Values);

        Timeline.Destroy(id);
    }
}
