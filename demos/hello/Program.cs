using HelloTl;
using Tl;

namespace HelloTl;

public static unsafe class Host
{
    public const int Steps = 150;

    static readonly (ushort Start, ushort End, TeleportClip Clip)[] Clips =
    [
        (30, 60, new TeleportClip { Distance = 5f, ComeBack = false }),
        (90, 120, new TeleportClip { Distance = 8f, ComeBack = true }),
    ];

    public static List<(int Step, float Offset)> Run(byte[] library)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var player = new Teleported[1];
        var changes = new List<(int, float)>();

        players.Start(0, 1);
        var last = 0f;
        for (var step = 0; step < Steps; step++)
        {
            world.Tick(players, true);
            var at = players.View.SlotOf(0, 0)->Tick;
            for (var i = 0; i < Clips.Length; i++)
            {
                var (start, end, clip) = Clips[i];
                if (at != start && at != (ushort)(end - 1)) continue;
                var frame = new TlFrame<TeleportClip>(new Frame<TlNoSettings<TeleportClip>, TeleportClip>(default, clip, at, (ushort)(end - start), (ushort)(at - start), 0, FrameFlags.None));
                float delta;
                if (at == start)
                    delta = TeleportTrack.JumpAtClipStart(in frame);
                else
                    delta = TeleportTrack.ReturnAtClipEnd(in frame);
                TeleportKind.Apply(in delta, ref player[0]);
            }
            if (player[0].Offset != last)
            {
                changes.Add((step, player[0].Offset));
                last = player[0].Offset;
            }
        }
        return changes;
    }

    public static int Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "HelloTl.tlib");
        Console.WriteLine("step  offset");
        foreach (var (step, offset) in Run(File.ReadAllBytes(path)))
            Console.WriteLine($"{step,4}  {offset}");
        return 0;
    }
}
