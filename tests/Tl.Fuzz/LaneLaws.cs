using Xunit;


namespace Tl.Fuzz;

public class LaneLaws
{
    const int ExhaustiveBound = 20;

    private readonly struct D0 : IDurationShape { public static ushort Duration => 0; }
    private readonly struct D1 : IDurationShape { public static ushort Duration => 1; }
    private readonly struct D2 : IDurationShape { public static ushort Duration => 2; }
    private readonly struct D3 : IDurationShape { public static ushort Duration => 3; }
    private readonly struct D8 : IDurationShape { public static ushort Duration => 8; }
    private readonly struct D15 : IDurationShape { public static ushort Duration => 15; }
    private readonly struct D16 : IDurationShape { public static ushort Duration => 16; }
    private readonly struct D17 : IDurationShape { public static ushort Duration => 17; }
    private readonly struct D31 : IDurationShape { public static ushort Duration => 31; }
    private readonly struct D63 : IDurationShape { public static ushort Duration => 63; }
    private readonly struct D255 : IDurationShape { public static ushort Duration => 255; }
    private readonly struct D1024 : IDurationShape { public static ushort Duration => 1024; }

    private readonly struct LoopY : ILoopShape { public static bool Looping => true; }
    private readonly struct LoopN : ILoopShape { public static bool Looping => false; }

    [Fact]
    public void AdvanceMatchesTheOracleForEveryLaneShape()
    {
        AdvanceParity(new LaneCase<D0, LoopY>(0));
        AdvanceParity(new LaneCase<D0, LoopN>(0));
        AdvanceParity(new LaneCase<D1, LoopY>(1));
        AdvanceParity(new LaneCase<D1, LoopN>(1));
        AdvanceParity(new LaneCase<D2, LoopY>(2));
        AdvanceParity(new LaneCase<D2, LoopN>(2));
        AdvanceParity(new LaneCase<D3, LoopY>(3));
        AdvanceParity(new LaneCase<D3, LoopN>(3));
        AdvanceParity(new LaneCase<D8, LoopY>(4));
        AdvanceParity(new LaneCase<D8, LoopN>(4));
        AdvanceParity(new LaneCase<D15, LoopY>(5));
        AdvanceParity(new LaneCase<D15, LoopN>(5));
        AdvanceParity(new LaneCase<D16, LoopY>(6));
        AdvanceParity(new LaneCase<D16, LoopN>(6));
        AdvanceParity(new LaneCase<D17, LoopY>(7));
        AdvanceParity(new LaneCase<D17, LoopN>(7));
        AdvanceParity(new LaneCase<D31, LoopY>(8));
        AdvanceParity(new LaneCase<D31, LoopN>(8));
        AdvanceParity(new LaneCase<D63, LoopY>(9));
        AdvanceParity(new LaneCase<D63, LoopN>(9));
        AdvanceParity(new LaneCase<D255, LoopY>(10));
        AdvanceParity(new LaneCase<D255, LoopN>(10));
        AdvanceParity(new LaneCase<D1024, LoopY>(11));
        AdvanceParity(new LaneCase<D1024, LoopN>(11));
    }

    [Fact]
    public void ApplyNeverWritesTheClock()
    {
        ForEveryShape((_, runner) => runner.ApplyClockPurity());
    }

    [Fact]
    public void ApplySpanFormMatchesTheClockOnlyForm()
    {
        ForEveryShape((_, runner) => runner.ApplySpanParity());
    }

    [Fact]
    public void ApplySingleRowFormMatchesTheSpanForm()
    {
        ForEveryShape((_, runner) => runner.ApplyRowFormParity());
    }

    static void ForEveryShape(Action<int, ILaneShapeRunner> laws)
    {
        laws(0, new LaneCase<D0, LoopY>(0));
        laws(0, new LaneCase<D0, LoopN>(0));
        laws(1, new LaneCase<D1, LoopY>(1));
        laws(1, new LaneCase<D1, LoopN>(1));
        laws(2, new LaneCase<D2, LoopY>(2));
        laws(2, new LaneCase<D2, LoopN>(2));
        laws(3, new LaneCase<D3, LoopY>(3));
        laws(3, new LaneCase<D3, LoopN>(3));
        laws(4, new LaneCase<D8, LoopY>(4));
        laws(4, new LaneCase<D8, LoopN>(4));
        laws(5, new LaneCase<D15, LoopY>(5));
        laws(5, new LaneCase<D15, LoopN>(5));
        laws(6, new LaneCase<D16, LoopY>(6));
        laws(6, new LaneCase<D16, LoopN>(6));
        laws(7, new LaneCase<D17, LoopY>(7));
        laws(7, new LaneCase<D17, LoopN>(7));
        laws(8, new LaneCase<D31, LoopY>(8));
        laws(8, new LaneCase<D31, LoopN>(8));
        laws(9, new LaneCase<D63, LoopY>(9));
        laws(9, new LaneCase<D63, LoopN>(9));
        laws(10, new LaneCase<D255, LoopY>(10));
        laws(10, new LaneCase<D255, LoopN>(10));
        laws(11, new LaneCase<D1024, LoopY>(11));
        laws(11, new LaneCase<D1024, LoopN>(11));
    }

    static void AdvanceParity<TD, TL>(LaneCase<TD, TL> lane)
        where TD : IDurationShape
        where TL : ILoopShape
    {
        foreach (var (positions, forward, _) in lane.Cases())
        {
            var next = new ushort[positions.Length];
            Timeline<LaneOf<TD, TL>>.Advance(positions, next, forward);
            var inPlace = (ushort[])positions.Clone();
            Timeline<LaneOf<TD, TL>>.Advance(inPlace, forward);
            for (var i = 0; i < positions.Length; i++)
            {
                var expected = forward ? Movement.Forward(TD.Duration, TL.Looping, positions[i]) : Movement.Backward(TD.Duration, TL.Looping, positions[i]);
                if (next[i] != expected || inPlace[i] != expected)
                    throw new Xunit.Sdk.XunitException($"advance d={TD.Duration} loop={TL.Looping} forward={forward} pos={positions[i]}: next={next[i]} inPlace={inPlace[i]} expected={expected}");
            }
        }
    }

    private interface ILaneShapeRunner
    {
        void ApplyClockPurity();

        void ApplySpanParity();

        void ApplyRowFormParity();
    }

    internal sealed class LaneCase<TD, TL>(ushort shapeSeed) : ILaneShapeRunner
        where TD : IDurationShape
        where TL : ILoopShape
    {
        readonly ushort _shapeSeed = shapeSeed;

        internal readonly record struct Case(ushort[] Positions, bool Forward, float[] Initial);

        static ushort[] SamplePositions(FuzzRandom random, int count)
        {
            var duration = TD.Duration;
            var positions = new ushort[count];
            for (var i = 0; i < count; i++)
                positions[i] = duration <= ExhaustiveBound && random.NextBool()
                    ? (ushort)random.NextInt(Math.Min(65536, duration + 4))
                    : random.Pick(
                    [
                        (ushort)0, (ushort)1, (ushort)Math.Max(0, duration - 1), duration,
                        (ushort)Math.Min(65535, duration + 1), (ushort)65534, (ushort)65535,
                        (ushort)random.NextInt(Math.Min(65536, duration + 4)),
                    ]);
            return positions;
        }

        internal IEnumerable<Case> Cases()
        {
            var lengths = new[] { 0, 1, 7, 8, 15, 16, 17, 31, 32, 63, 64, 65, 129 };
            var shapes = new[] { false, true };
            foreach (var length in lengths)
                foreach (var forward in shapes)
                {
                    var random = FuzzRandom.FromSeeds((uint)(length * 131 + _shapeSeed * 7919 + (forward ? 1 : 0)), 0x51);
                    var distribution = random.NextInt(4);
                    var constant = SamplePositions(random, 1)[0];
                    var positions = new ushort[length];
                    for (var i = 0; i < length; i++)
                        positions[i] = distribution switch
                        {
                            0 => SamplePositions(random, 1)[0],
                            1 => (ushort)(TD.Duration == 0 ? random.NextInt(4) : i % Math.Max(1, Math.Min(TD.Duration + 2, 7))),
                            2 => (ushort)random.NextInt(Math.Min(65536, TD.Duration + 4)),
                            _ => constant,
                        };
                    var initial = new float[length];
                    for (var i = 0; i < length; i++) initial[i] = (random.NextInt(512) - 256) * 0.25f;
                    yield return new Case(positions, forward, initial);
                }
        }

        public void ApplyClockPurity()
        {
            foreach (var (positions, forward, initial) in Cases())
            {
                var clock = (ushort[])positions.Clone();
                var effects = (float[])initial.Clone();
                Timeline<LaneOf<TD, TL>>.Apply(positions, forward, effects);
                for (var i = 0; i < positions.Length; i++)
                {
                    if (clock[i] != positions[i])
                        throw new Xunit.Sdk.XunitException($"Apply moved the clock d={TD.Duration} loop={TL.Looping} row={i}");
                    var delta = EffectDelta(TD.Duration, TL.Looping, forward, positions[i]);
                    if (effects[i] != initial[i] + delta)
                        throw new Xunit.Sdk.XunitException($"Apply effect d={TD.Duration} loop={TL.Looping} forward={forward} pos={positions[i]}: {effects[i]} != {initial[i]} + {delta}");
                }
            }
        }

        public void ApplySpanParity()
        {
            foreach (var (positions, forward, initial) in Cases())
            {
                var next = new ushort[positions.Length];
                var effects = (float[])initial.Clone();
                Timeline<LaneOf<TD, TL>>.Apply(positions, next, forward, effects);
                var clockOnly = (ushort[])positions.Clone();
                var effectsOnly = (float[])initial.Clone();
                Timeline<LaneOf<TD, TL>>.Apply(clockOnly, forward, effectsOnly);
                for (var i = 0; i < positions.Length; i++)
                {
                    var expected = forward ? Movement.Forward(TD.Duration, TL.Looping, positions[i]) : Movement.Backward(TD.Duration, TL.Looping, positions[i]);
                    if (next[i] != expected)
                        throw new Xunit.Sdk.XunitException($"Apply next d={TD.Duration} loop={TL.Looping} forward={forward} pos={positions[i]}: {next[i]} != {expected}");
                    if (effects[i] != effectsOnly[i])
                        throw new Xunit.Sdk.XunitException($"Apply effect diverged from the clock-only form d={TD.Duration} row={i}");
                }
            }
        }

        public void ApplyRowFormParity()
        {
            var random = FuzzRandom.FromSeeds((uint)(_shapeSeed * 6151 + (TL.Looping ? 1 : 0)), 0xD4);
            for (var trial = 0; trial < 64; trial++)
            {
                var position = SamplePositions(random, 1)[0];
                var forward = random.NextBool();
                var initial = (random.NextInt(512) - 256) * 0.25f;

                var spanNext = new ushort[1];
                var spanEffects = new[] { initial };
                Timeline<LaneOf<TD, TL>>.Apply([position], spanNext, forward, spanEffects);

                var clockEffects = new[] { initial };
                Timeline<LaneOf<TD, TL>>.Apply([position], forward, clockEffects);

                var expectedNext = forward ? Movement.Forward(TD.Duration, TL.Looping, position) : Movement.Backward(TD.Duration, TL.Looping, position);
                if (spanNext[0] != expectedNext || spanEffects[0] != clockEffects[0])
                    throw new Xunit.Sdk.XunitException($"row form diverged d={TD.Duration} loop={TL.Looping} pos={position}");
            }
        }
    }

    internal static float EffectDelta(ushort duration, bool looping, bool forward, ushort position)
    {
        var active = forward
            ? position < duration
            : looping ? position < duration : position > 0 && position <= duration;
        if (!active) return 0f;
        var tick = forward ? position : position == 0 ? (ushort)Math.Max(0, duration - 1) : (ushort)(position - 1);
        return forward ? FuzzFx.Effect(position) : FuzzFx.Inverse(tick);
    }

}
