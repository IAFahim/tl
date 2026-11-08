using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Tl.Mathematics;
using Tl.Pack.Force;
using Xunit;
using ForceState = Tl.Pack.Force.Force;

namespace Tl.Packs.Tests;

public sealed class ForceTests
{
    internal const ulong Pinned = 7683521458411619355UL;

    static string Root()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        return dir;
    }

    static byte[] Bytes(ForceClip clip) => MemoryMarshal.AsBytes(new Span<ForceClip>(ref clip)).ToArray();

    static TlCurve Ramp(float from, float to)
    {
        var curve = default(TlCurve);
        for (var i = 0; i < TlCurve.Samples; i++) curve[i] = from + (to - from) * i / (TlCurve.Samples - 1);
        return curve;
    }

    internal static readonly ForceClip Push = new() { Direction = new float3(1f, 0f, 0.5f), Strength = 2.5f, Falloff = Ramp(1f, 0f) };
    internal static readonly ForceClip Lift = new() { Direction = new float3(0f, 1f, -0.25f), Strength = 1.25f, Falloff = Ramp(0.2f, 1f) };
    internal static readonly ForceClip Gust = new() { Direction = new float3(-0.5f, 0.75f, 1f), Strength = 3f, Falloff = TlCurve.Constant(0.4f) };

    static TypedTrack Track(params (uint Start, uint End, ForceClip Clip)[] clips)
    {
        var track = new TypedTrack { Type = typeof(TlNoSettings<ForceClip>) };
        foreach (var (start, end, clip) in clips) track.Clips.Add(new TypedClip { Type = typeof(ForceClip), Start = start, End = end, Data = Bytes(clip) });
        return track;
    }

    internal static byte[] Library()
    {
        var timelines = new List<TypedTimeline>
        {
            new() { Name = "gust", Duration = 48, Loops = true, Tracks = { Track((8, 40, Gust)) } },
            new() { Name = "hold", Duration = 60, Hold = true, Channel = "move", Tracks = { Track((0, 60, Lift)) } },
            new() { Name = "push", Duration = 180, Channel = "move", Tracks = { Track((0, 120, Push)), Track((30, 150, Lift)) } },
        };
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), ["move"], 60);
        var kinds = LibraryBake.Kinds([typeof(ForceClip).Assembly]);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        var settings = LibrarySettings.Default;
        settings.ChannelCount = 1;
        return LibraryAssembler.Assemble(parts, 60, settings);
    }

    static float3 Evaluate(in ForceClip clip, int within, int length)
        => clip.Direction * clip.Strength * clip.Falloff.Evaluate(length == 0 ? 0f : within / (float)length);

    static float3 Reference(int steps)
    {
        var force = new float3(0f);
        for (var tick = 0; tick < steps && tick < 180; tick++)
        {
            var total = new float3(0f);
            var any = false;
            if (tick < 120)
            {
                total = any ? total + Evaluate(Push, tick, 120) : Evaluate(Push, tick, 120);
                any = true;
            }
            if (tick >= 30 && tick < 150)
            {
                total = any ? total + Evaluate(Lift, tick - 30, 120) : Evaluate(Lift, tick - 30, 120);
                any = true;
            }
            if (any) force += total;
        }
        return force;
    }

    static ForceState[] Single(byte[] library, int steps)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var states = new ForceState[1];
        players.Start(0, 3);
        for (var step = 0; step < steps; step++)
        {
            world.Tick(players, true);
            new Tl.Generated.TlAuto_Tl_Pack_Force { C0_Force = states }.Run(world, players, true);
        }
        return states;
    }

    static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    static ulong Mix(ulong h, float v) => (h ^ (ulong)BitConverter.SingleToInt32Bits(v)) * 1099511628211UL;

    internal static ulong Play(byte[] library, int rows, int steps, out long warm)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(rows);
        using var kinds = new KindStore(world, players);
        var states = new ForceState[rows];
        var rng = 0x6D2B79F5u;
        var h = 14695981039346656037UL;
        warm = 0;
        for (var step = 0; step < steps; step++)
        {
            var forward = !(step >= 300 && step < 340);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var e = 0; e < rows; e++)
            {
                var r = Next(ref rng);
                switch (r & 7)
                {
                    case 0:
                        players.Start(e, (ushort)(1 + (r >> 8) % 3));
                        break;
                    case 1:
                        if (((r >> 8) & 3) == 0) players.Stop(e, (ushort)(1 + (r >> 10) % 3));
                        break;
                    case 2:
                        if (((r >> 8) & 31) == 0) players.StopAll(e);
                        break;
                }
            }
            world.Tick(players, forward);
            new Tl.Generated.TlAuto_Tl_Pack_Force { C0_Force = states }.Run(world, players, forward);
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (step >= 100) warm += delta;
            for (var e = 0; e < rows; e++)
            {
                h = Mix(h, states[e].Value.x);
                h = Mix(h, states[e].Value.y);
                h = Mix(h, states[e].Value.z);
            }
        }
        return h;
    }

    [Fact]
    public void ThePackBuildGeneratesItsPlainSystemAndMatchesAScalarReference()
    {
        var library = Library();
        foreach (var steps in new[] { 1, 31, 119, 120, 151, 200 })
        {
            var expected = Reference(steps);
            var actual = Single(library, steps)[0].Value;
            Assert.Equal(BitConverter.SingleToInt32Bits(expected.x), BitConverter.SingleToInt32Bits(actual.x));
            Assert.Equal(BitConverter.SingleToInt32Bits(expected.y), BitConverter.SingleToInt32Bits(actual.y));
            Assert.Equal(BitConverter.SingleToInt32Bits(expected.z), BitConverter.SingleToInt32Bits(actual.z));
        }
    }

    [Fact]
    public void SixHundredMixedStepsKeepThePinnedHashAndAllocateNothingWarm()
    {
        var library = Library();
        var golden = Path.Combine(Root(), "tests", "Tl.Packs.Tests", "Force", "force.tlib");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1") File.WriteAllBytes(golden, library);
        Assert.Equal(File.ReadAllBytes(golden), library);
        var hash = Play(library, 8, 600, out var warm);
        Assert.Equal(hash, Play(library, 8, 600, out _));
        Assert.Equal(0L, warm);
        Assert.Equal(Pinned, hash);
    }

    [Fact]
    public void ThePackKeepsTheFoldLayoutOfItsClip()
    {
        Assert.Equal(12, Marshal.SizeOf<float3>());
        Assert.Equal(16 + 4 * TlCurve.Samples, Marshal.SizeOf<ForceClip>());
        Assert.Equal(12, Marshal.SizeOf<ForceState>());
    }
}
