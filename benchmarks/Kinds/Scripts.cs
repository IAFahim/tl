namespace Kinds;

public static class Scripts
{
    public static List<TimelineSpec> RandomTimelines(Random rng)
    {
        var list = new List<TimelineSpec>();
        const int count = 10;
        for (var id = 1; id <= count; id++)
        {
            var duration = (ushort)rng.Next(1, 41);
            var spec = new TimelineSpec
            {
                Duration = duration,
                Loop = rng.Next(3) == 0,
                Hold = rng.Next(3) == 0,
                Next = rng.Next(3) == 0 ? (ushort)rng.Next(1, count + 1) : (ushort)0,
                Channel = (byte)(rng.Next(3) == 0 ? rng.Next(1, 3) : 0),
            };
            var clips = rng.Next(0, 6);
            for (var c = 0; c < clips; c++)
            {
                var start = (ushort)rng.Next(duration);
                var end = (ushort)rng.Next(start + 1, duration + 1);
                switch (rng.Next(5))
                {
                    case 0: spec.Add(start, end, KindId.Locomotion, ClipMode.Span, rng.Next(1, 6), rng.Next(1, 3), (byte)rng.Next(0, 20)); break;
                    case 1: spec.Add(start, end, KindId.Lift, ClipMode.Span, rng.Next(1, 6), rng.Next(1, 3)); break;
                    case 2: spec.Add(start, end, KindId.Damage, ClipMode.PulseOnStart, hit: rng.Next(1, 30)); break;
                    case 3: spec.Add(start, end, KindId.StatChange, ClipMode.PulseOnStart, hit: rng.Next(-3, 4)); break;
                    default: spec.Add(start, end, KindId.StatChange, ClipMode.PulseOnEnd, hit: rng.Next(-3, 4)); break;
                }
            }
            list.Add(spec);
        }
        return list;
    }

    public static TimelineSpec Walk(ushort duration, float amount, float scale, byte kind) => new TimelineSpec { Duration = duration, Loop = true }.Add(0, duration, kind, ClipMode.Span, amount, scale);
}
