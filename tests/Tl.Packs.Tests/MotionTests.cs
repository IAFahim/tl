using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Tl.Mathematics;
using Tl.Pack.Motion;
using Xunit;

namespace Tl.Packs.Tests;

public sealed class MotionTests
{
    internal const ulong Pinned = 14881653450773647005UL;
    internal const ushort Breathe = 1;
    internal const ushort Glide = 2;
    internal const ushort Hop = 3;

    static byte[] Bytes<T>(T value) where T : unmanaged => MemoryMarshal.AsBytes(new Span<T>(ref value)).ToArray();

    static TlCurve Ramp()
    {
        var curve = default(TlCurve);
        for (var i = 0; i < TlCurve.Samples; i++) curve[i] = i / (float)(TlCurve.Samples - 1);
        return curve;
    }

    static TlCurve Triangle()
    {
        var curve = default(TlCurve);
        for (var i = 0; i < TlCurve.Samples; i++) curve[i] = 1f - Math.Abs(2f * i / (TlCurve.Samples - 1) - 1f);
        return curve;
    }

    static TypedTrack Track<T>(params (uint Start, uint End, T Clip)[] clips) where T : unmanaged
    {
        var track = new TypedTrack { Type = typeof(TlNoSettings<T>) };
        foreach (var (start, end, clip) in clips) track.Clips.Add(new TypedClip { Type = typeof(T), Start = start, End = end, Data = Bytes(clip) });
        return track;
    }

    static readonly MoveClip Right = new() { From = new float3(-2f, 0f, 0f), To = new float3(2f, 0f, 0f), Ease = Ramp() };
    static readonly MoveClip Left = new() { From = new float3(2f, 0f, 0f), To = new float3(-2f, 0f, 0f), Ease = Ramp() };
    static readonly ScaleClip Swell = new() { From = new float3(1f), To = new float3(1.5f), Ease = Triangle() };
    static readonly TintClip Glow = new() { From = new Rgba(1f, 0f, 0f, 1f), To = new Rgba(1f, 1f, 0f, 1f), Ease = Triangle() };
    static readonly SpinClip Turn = new() { FromYaw = 0f, ToYaw = 360f, Ease = Ramp() };
    static readonly MoveClip Jump = new() { From = new float3(4f, 0f, 0f), To = new float3(4f, 1f, 0f), Ease = Triangle() };
    static readonly MoveClip Pull = new() { From = new float3(4f, 0f, 0f), To = new float3(5.5f, 0f, 0f), Ease = Ramp(), Priority = 5 };

    internal static byte[] Library()
    {
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "breathe", Duration = 90, Loops = true, Tracks = { Track((0, 90, Swell)), Track((0, 90, Glow)) } },
            new() { Name = "glide", Duration = 120, Loops = true, Tracks = { Track((0, 60, Right), (60, 120, Left)) } },
            new() { Name = "hop", Duration = 120, Loops = true, Tracks = { Track((0, 120, Turn)), Track((0, 120, Jump)), Track((40, 80, Pull)) } },
        };
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), [], 60);
        var kinds = LibraryBake.Kinds([typeof(Pose).Assembly]);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    static float P(int within, int length) => length == 0 ? 0f : within / (float)length;

    static float3 GlideAt(int tick) => tick < 60
        ? Ease.Lerp(Right.From, Right.To, Right.Ease.Evaluate(P(tick, 60)))
        : Ease.Lerp(Left.From, Left.To, Left.Ease.Evaluate(P(tick - 60, 60)));

    static float3 HopAt(int tick) => tick >= 40 && tick < 80
        ? Ease.Lerp(Pull.From, Pull.To, Pull.Ease.Evaluate(P(tick - 40, 40)))
        : Ease.Lerp(Jump.From, Jump.To, Jump.Ease.Evaluate(P(tick, 120)));

    static ulong Mix(ulong h, float v) { var s = new global::Tl.TlStateHash { Value = h }; s.Float(v); return s.Value; }

    static ulong Mix(ulong h, in Pose pose, in Tint tint)
    {
        h = Mix(Mix(Mix(h, pose.Position.x), pose.Position.y), pose.Position.z);
        h = Mix(Mix(Mix(h, pose.Scale.x), pose.Scale.y), pose.Scale.z);
        h = Mix(Mix(h, pose.Yaw), pose.Has);
        return Mix(Mix(Mix(Mix(Mix(h, tint.Color.R), tint.Color.G), tint.Color.B), tint.Color.A), tint.Has);
    }

    internal static ulong Run(byte[] library, int steps, Action<int, Pose[], Tint[]>? checkpoint, out long warm)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(3);
        using var kinds = new KindStore(world, players);
        var poses = new Pose[3];
        var tints = new Tint[3];
        players.Start(0, Glide);
        players.Start(1, Breathe);
        players.Start(2, Hop);
        var h = global::Tl.TlStateHash.Seed;
        warm = 0;
        for (var step = 0; step < steps; step++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Tick(players, true);
            unsafe
            {
            for (var e = 0; e < 3; e++)
            {
                var tick = players.View.SlotOf(e, 0)->Tick;
                if (e == 0)
                {
                    var clip = tick < 60 ? Right : Left;
                    var within = tick < 60 ? tick : tick - 60;
                    var placed = MoveTrack.Evaluate(new TlFrame<MoveClip>(new Frame<TlNoSettings<MoveClip>, MoveClip>(default, clip, tick, 60, (ushort)within, 0, FrameFlags.None)));
                    MotionPosition.Apply(in placed, ref poses[e]);
                }
                else if (e == 1)
                {
                    var scale = ScaleTrack.Evaluate(new TlFrame<ScaleClip>(new Frame<TlNoSettings<ScaleClip>, ScaleClip>(default, Swell, tick, 90, (ushort)tick, 0, FrameFlags.None)));
                    MotionScale.Apply(in scale, ref poses[e]);
                    var tint = TintTrack.Evaluate(new TlFrame<TintClip>(new Frame<TlNoSettings<TintClip>, TintClip>(default, Glow, tick, 90, (ushort)tick, 0, FrameFlags.None)));
                    MotionTint.Apply(in tint, ref tints[e]);
                }
                else
                {
                    var yaw = SpinTrack.Evaluate(new TlFrame<SpinClip>(new Frame<TlNoSettings<SpinClip>, SpinClip>(default, Turn, tick, 120, (ushort)tick, 0, FrameFlags.None)));
                    MotionYaw.Apply(in yaw, ref poses[e]);
                    var move = tick >= 40 && tick < 80
                        ? MoveTrack.Evaluate(new TlFrame<MoveClip>(new Frame<TlNoSettings<MoveClip>, MoveClip>(default, Pull, tick, 40, (ushort)(tick - 40), 0, FrameFlags.None)))
                        : MoveTrack.Evaluate(new TlFrame<MoveClip>(new Frame<TlNoSettings<MoveClip>, MoveClip>(default, Jump, tick, 120, (ushort)tick, 0, FrameFlags.None)));
                    MotionPosition.Apply(in move, ref poses[e]);
                }
            }
            }
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (step >= 10) warm += delta;
            for (var e = 0; e < 3; e++) h = Mix(h, poses[e], tints[e]);
            checkpoint?.Invoke(step + 1, poses, tints);
        }
        return h;
    }

    static void Same(float3 expected, float3 actual)
    {
        Assert.Equal(BitConverter.SingleToInt32Bits(expected.x), BitConverter.SingleToInt32Bits(actual.x));
        Assert.Equal(BitConverter.SingleToInt32Bits(expected.y), BitConverter.SingleToInt32Bits(actual.y));
        Assert.Equal(BitConverter.SingleToInt32Bits(expected.z), BitConverter.SingleToInt32Bits(actual.z));
    }

    [Fact]
    public void PositionsMatchAScalarReferenceAndTheOverrideWinsByPriority()
    {
        var checks = 0;
        Run(Library(), 120, (step, poses, tints) =>
        {
            if (step % 30 != 0) return;
            var tick = step - 1;
            Same(GlideAt(tick), poses[0].Position);
            Same(HopAt(tick), poses[2].Position);
            Assert.Equal(1, poses[0].Has);
            Assert.Equal(2, poses[1].Has);
            Assert.Equal(5, poses[2].Has);
            Assert.Equal(0, tints[0].Has);
            Assert.Equal(1, tints[1].Has);
            checks++;
        }, out _);
        Assert.Equal(4, checks);
    }

    [Fact]
    public void SixHundredStepsKeepThePinnedHashAndAllocateNothingWarm()
    {
        var library = Library();
        var golden = Path.Combine(ForceTests.Root(), "tests", "Tl.Packs.Tests", "Motion", "motion.tlib");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1") File.WriteAllBytes(golden, library);
        Assert.Equal(File.ReadAllBytes(golden), library);
        var hash = Run(library, 600, null, out var warm);
        Assert.Equal(hash, Run(library, 600, null, out _));
        Assert.Equal(0L, warm);
        Assert.Equal(Pinned, hash);
    }

    [Fact]
    public void ThePackUnityTestPinsTheSameLibraryAndHash()
    {
        var tests = Path.Combine(ForceTests.Root(), "packs", "Motion", "Tests");
        Assert.Contains("\"" + Convert.ToBase64String(Library()) + "\"", File.ReadAllText(Path.Combine(tests, "MotionLibraryBytes.cs")));
        Assert.Contains($"const ulong Pinned = {Pinned}UL;", File.ReadAllText(Path.Combine(tests, "MotionPackTests.cs")));
    }
}
