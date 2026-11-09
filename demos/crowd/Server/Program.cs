using Tl;
using Tl.Mathematics;
using Tl.Pack.Motion;

namespace CrowdServer;

public static unsafe class Host
{
    public const int Side = 25;
    public const int Steps = 600;
    const ushort Dance = 1;

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
        var hash = TlStateHash.Begin();
        for (var step = 0; step < steps; step++)
        {
            var forward = !(step >= 300 && step < 420);
            if (step == 0)
                for (var i = 0; i < count; i++)
                    players.Start(i, Dance, (ushort)((i % Side * 7 + i / Side * 11) % 120));
            world.Tick(players, forward);
            new Tl.Generated.TlAuto_Tl_Pack_Motion { C0_Pose = poses, C1_Tint = tints }.Run(world, players, forward);
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
