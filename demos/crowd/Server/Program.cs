using Tl;
using Tl.Mathematics;
using Tl.Pack.Motion;

namespace CrowdServer;

public static unsafe class Host
{
    public const int Side = 25;
    public const int Steps = 600;
    const ushort Dance = 1;

    static MoveClip DanceClip()
    {
        var ease = default(TlCurve);
        float[] samples = { 0.0f, 0.1333333333333333f, 0.2666666666666666f, 0.4f, 0.5333333333333333f, 0.6666666666666666f, 0.8f, 0.9333333333333333f, 0.9333333333333333f, 0.8f, 0.6666666666666667f, 0.5333333333333334f, 0.3999999999999999f, 0.2666666666666666f, 0.1333333333333333f, 0.0f };
        for (var i = 0; i < 16; i++) ease[i] = samples[i];
        return new MoveClip { From = new float3(0f, 0f, 0f), To = new float3(0f, 0.6f, 0f), Ease = ease, Priority = 0 };
    }

    static SpinClip DanceSpin()
    {
        var ease = default(TlCurve);
        float[] samples = { 0.0f, 0.06666666666666667f, 0.1333333333333333f, 0.2f, 0.26666666666666666f, 0.3333333333333333f, 0.4f, 0.4666666666666667f, 0.5333333333333333f, 0.6f, 0.6666666666666666f, 0.7333333333333333f, 0.8f, 0.8666666666666667f, 0.9333333333333333f, 1.0f };
        for (var i = 0; i < 16; i++) ease[i] = samples[i];
        return new SpinClip { FromYaw = 0f, ToYaw = 360f, Ease = ease };
    }

    static TintClip DanceTint()
    {
        var ease = default(TlCurve);
        float[] samples = { 0.0f, 0.1333333333333333f, 0.2666666666666666f, 0.4f, 0.5333333333333333f, 0.6666666666666666f, 0.8f, 0.9333333333333333f, 0.9333333333333333f, 0.8f, 0.6666666666666667f, 0.5333333333333334f, 0.3999999999999999f, 0.2666666666666666f, 0.1333333333333333f, 0.0f };
        for (var i = 0; i < 16; i++) ease[i] = samples[i];
        return new TintClip { From = new Rgba(1f, 0f, 0f, 1f), To = new Rgba(1f, 1f, 0f, 1f), Ease = ease };
    }

    public static ulong Run(byte[] library, int steps)
    {
        var count = Side * Side;
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(count);
        using var kinds = new KindStore(world, players);
        var poses = new Pose[count];
        var tints = new Tint[count];
        for (var i = 0; i < count; i++)
        {
            poses[i] = new Pose { Position = new float3(i % Side, 0f, i / Side), Scale = new float3(1f, 1f, 1f) };
            tints[i] = new Tint { Color = new Rgba(1f, 1f, 1f, 1f) };
        }
        var clip = DanceClip();
        var spin = DanceSpin();
        var tintClip = DanceTint();
        var hash = TlStateHash.Begin();
        for (var step = 0; step < steps; step++)
        {
            var forward = !(step >= 300 && step < 420);
            if (step == 0)
                for (var i = 0; i < count; i++)
                    players.Start(i, Dance, (ushort)((i % Side * 7 + i / Side * 11) % 120));
            world.Tick(players, forward);
            var view0 = players.View;
            for (var i = 0; i < count; i++)
            {
                var tick = view0.SlotOf(i, 0)->Tick;
                var spinWithin = tick >= 30 && tick < 60 ? tick - 30 : -1;
                if (spinWithin >= 0)
                {
                    var yaw = SpinTrack.Evaluate(new TlFrame<SpinClip>(new Frame<TlNoSettings<SpinClip>, SpinClip>(default, spin, tick, 30, (ushort)spinWithin, 0, FrameFlags.None)));
                    MotionYaw.Apply(in yaw, ref poses[i]);
                }
                var color = TintTrack.Evaluate(new TlFrame<TintClip>(new Frame<TlNoSettings<TintClip>, TintClip>(default, tintClip, tick, 120, (ushort)tick, 0, FrameFlags.None)));
                MotionTint.Apply(in color, ref tints[i]);
                var within = tick < 30 ? tick : tick >= 60 && tick < 90 ? tick - 60 : -1;
                if (within < 0) continue;
                var frame = new Frame<TlNoSettings<MoveClip>, MoveClip>(default, clip, tick, 30, (ushort)within, 0, FrameFlags.None);
                var placed = MoveTrack.Evaluate(new TlFrame<MoveClip>(in frame));
                MotionPosition.Apply(in placed, ref poses[i]);
            }
            var view = players.View;
            fixed (Pose* pose = poses)
            fixed (Tint* tint = tints)
                for (var i = 0; i < count; i++)
                {
                    hash.Slots(view.SlotOf(i, 0), view.CountOf(i));
                    hash.Bytes((byte*)(pose + i), sizeof(Pose));
                    hash.Bytes((byte*)(tint + i), sizeof(Tint));
                }
        }
        return hash.Value;
    }

    public static int Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "crowd.tlib");
        var hash = Run(File.ReadAllBytes(path), Steps);
        Console.WriteLine($"crowd server steps={Steps} dancers={Side * Side} hash={hash}");
        return 0;
    }
}
