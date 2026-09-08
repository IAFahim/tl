using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Hooks = Tl.Hooks;

namespace Tl.Native;

public sealed class Config : ManualConfig
{
    public Config()
    {
        var job = Job.Default.WithWarmupCount(16).WithIterationCount(12)
            .WithIterationTime(TimeInterval.FromMilliseconds(250));
        AddJob(job.WithId("Jit"));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

public readonly record struct Receipt(float Sum, long Ticks, int Count);

// v0.4 A/B: the SAME sweep fixture (the authored Vitals timeline of the
// Dispatch benchmarks — 4 tracks, 19 clips, 13 regions, 3 blends, 600
// duration) consumed Stay-only through the Playback hub, single-tick and
// four-tick calls, on two engines built from one codebase:
//
//   Managed*  the frozen v0.3 sandbox (benchmarks/Hooks.cs): managed
//             registry Entry + managed tables + managed BindingCache.
//   Native*   the v0.4 src engine (Tl.Core): one native block, native
//             slot table, native bind slots.
//
// Same authoring values, same xorshift tick stream, same consumer
// arithmetic; Setup cross-checks the receipts are IDENTICAL before any
// measurement runs. Expect neutral to slightly better on the native side
// (native-backed spans vs managed arrays) — reported honestly either way.
[Config(typeof(Config))]
public class NativeVsManaged
{
    public const int Operations = 65536;
    private uint[] _ticks = null!;
    private ushort _managed = Hooks.Timeline.None;
    private ushort _native = Timeline.None;

    [GlobalSetup]
    public void Setup()
    {
        _ticks = new uint[Operations];
        uint random = 0xB19F4C27;
        for (var i = 0; i < _ticks.Length; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            _ticks[i] = random % 600;
        }

        _managed = BuildManaged();
        _native = BuildNative();

        // Receipts: the two engines must agree exactly — same algorithm,
        // same authored values, same stream (single and batch shapes).
        var single = ManagedSingle();
        if (ManagedParamsFour() != single || NativeSingle() != single || NativeParamsFour() != single)
            throw new InvalidOperationException("Engine receipts diverged; the A/B is invalid.");
    }

    private static void AuthorManaged(Hooks.TimelineBuilder<ManagedTrack, ManagedClip> b)
    {
        Hooks.TrackRef track0 = b.Track(new ManagedTrack(1));
        Hooks.TrackRef track1 = b.Track(new ManagedTrack(2));
        Hooks.TrackRef track2 = b.Track(new ManagedTrack(3));
        Hooks.TrackRef track3 = b.Track(new ManagedTrack(4));

        b.Clip(track0, new ManagedClip(1f), 0, 7);
        b.Clip(track0, new ManagedClip(13f), 29, 47);
        b.Clip(track0, new ManagedClip(21f), 29, 47);
        b.Clip(track0, new ManagedClip(13f), 47, 76);
        b.Clip(track0, new ManagedClip(1f), 321, 515);

        b.Clip(track1, new ManagedClip(2f), 3, 7);
        b.Clip(track1, new ManagedClip(3f), 3, 11);
        b.Clip(track1, new ManagedClip(2f), 47, 76);
        b.Clip(track1, new ManagedClip(2f), 76, 123);
        b.Clip(track1, new ManagedClip(3f), 515, 600);

        b.Clip(track2, new ManagedClip(8f), 18, 29);
        b.Clip(track2, new ManagedClip(8f), 76, 123);
        b.Clip(track2, new ManagedClip(8f), 321, 515);

        b.Clip(track3, new ManagedClip(5f), 3, 11);
        b.Clip(track3, new ManagedClip(5f), 18, 29);
        b.Clip(track3, new ManagedClip(34f), 76, 123);
        b.Clip(track3, new ManagedClip(55f), 76, 123);
        b.Clip(track3, new ManagedClip(5f), 200, 321);
        b.Clip(track3, new ManagedClip(5f), 321, 515);
    }

    private static void AuthorNative(TimelineBuilder<NativeTrack, NativeClip> b)
    {
        TrackRef track0 = b.Track(new NativeTrack(1));
        TrackRef track1 = b.Track(new NativeTrack(2));
        TrackRef track2 = b.Track(new NativeTrack(3));
        TrackRef track3 = b.Track(new NativeTrack(4));

        b.Clip(track0, new NativeClip(1f), 0, 7);
        b.Clip(track0, new NativeClip(13f), 29, 47);
        b.Clip(track0, new NativeClip(21f), 29, 47);
        b.Clip(track0, new NativeClip(13f), 47, 76);
        b.Clip(track0, new NativeClip(1f), 321, 515);

        b.Clip(track1, new NativeClip(2f), 3, 7);
        b.Clip(track1, new NativeClip(3f), 3, 11);
        b.Clip(track1, new NativeClip(2f), 47, 76);
        b.Clip(track1, new NativeClip(2f), 76, 123);
        b.Clip(track1, new NativeClip(3f), 515, 600);

        b.Clip(track2, new NativeClip(8f), 18, 29);
        b.Clip(track2, new NativeClip(8f), 76, 123);
        b.Clip(track2, new NativeClip(8f), 321, 515);

        b.Clip(track3, new NativeClip(5f), 3, 11);
        b.Clip(track3, new NativeClip(5f), 18, 29);
        b.Clip(track3, new NativeClip(34f), 76, 123);
        b.Clip(track3, new NativeClip(55f), 76, 123);
        b.Clip(track3, new NativeClip(5f), 200, 321);
        b.Clip(track3, new NativeClip(5f), 321, 515);
    }

    private static ushort BuildManaged()
    {
        var index = Hooks.Timeline<ManagedTrack, ManagedClip>.Build(AuthorManaged);
        Hooks.Timeline<ManagedTrack, ManagedClip>.Bind<Hooks.NoInput, ManagedVitals>(index);
        return index;
    }

    private static ushort BuildNative()
    {
        var index = Timeline<NativeTrack, NativeClip>.Build(AuthorNative).InMemory();
        Timeline<NativeTrack, NativeClip>.Bind<NoInput, NativeVitals>(index);
        return index;
    }

    // --- managed baseline (frozen v0.3 sandbox) ---

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public Receipt ManagedSingle()
    {
        var data = new ManagedVitals { Health = 1_000_000f };
        var pb = Hooks.Timeline.Start(_managed);
        foreach (var tick in _ticks.AsSpan())
            pb = Hooks.Timeline.Forward(_managed, in pb, default(Hooks.NoInput), ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt ManagedParamsFour()
    {
        var data = new ManagedVitals { Health = 1_000_000f };
        var pb = Hooks.Timeline.Start(_managed);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Hooks.Timeline.Forward(_managed, in pb, default(Hooks.NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }

    // --- native challenger (v0.4 src) ---

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt NativeSingle()
    {
        var data = new NativeVitals { Health = 1_000_000f };
        var pb = Timeline.Start(_native);
        foreach (var tick in _ticks.AsSpan())
            pb = Timeline.Forward(_native, in pb, default(NoInput), ref data, tick);
        return data.Result;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public Receipt NativeParamsFour()
    {
        var data = new NativeVitals { Health = 1_000_000f };
        var pb = Timeline.Start(_native);
        var ticks = _ticks.AsSpan();
        for (var i = 0; i < ticks.Length; i += 4)
        {
            var t = ticks.Slice(i, 4);
            pb = Timeline.Forward(_native, in pb, default(NoInput), ref data, t[0], t[1], t[2], t[3]);
        }
        return data.Result;
    }
}

// --- the fixture types, mirrored per engine ---

public readonly record struct ManagedClip(float Amount);

public readonly struct ManagedTrack : Hooks.IBlend<ManagedClip>
{
    public readonly int Offset;
    public ManagedTrack(int offset) => Offset = offset;

    public void Blend(in ManagedClip first, in ManagedClip second, float t, out ManagedClip result)
        => result = new ManagedClip(first.Amount * (1f - t) + second.Amount * t);
}

internal struct ManagedVitals :
    Hooks.IForward<ManagedTrack, ManagedClip, Hooks.NoInput, ManagedVitals>,
    Hooks.IBackward<ManagedTrack, ManagedClip, Hooks.NoInput, ManagedVitals>
{
    public float Health;
    public long Ticks;
    public int Count;

    public readonly Receipt Result => new(Health, Ticks, Count);

    public void Forward(in Hooks.Tracks<ManagedTrack, ManagedClip> tracks, in Hooks.NoInput input, in uint tick, ref ManagedVitals result)
    {
        foreach (var work in tracks)
        {
            if (work.State == Hooks.ClipState.Stay)
            {
                result.Ticks += work.Track.Offset;
                result.Health += work.Clip.Amount;
            }
        }
        result.Count++;
    }

    public void Backward(in Hooks.Tracks<ManagedTrack, ManagedClip> tracks, in Hooks.NoInput input, in uint tick, ref ManagedVitals result)
        => Forward(in tracks, in input, in tick, ref result);
}

public readonly record struct NativeClip(float Amount);

// Test-local empty input for the src engine's consumers (the same shape
// the core tests use; production consumers define their own).
public readonly struct NoInput;

public readonly struct NativeTrack : IBlend<NativeClip>
{
    public readonly int Offset;
    public NativeTrack(int offset) => Offset = offset;

    public void Blend(in NativeClip first, in NativeClip second, float t, out NativeClip result)
        => result = new NativeClip(first.Amount * (1f - t) + second.Amount * t);
}

internal struct NativeVitals :
    IForward<NativeTrack, NativeClip, NoInput, NativeVitals>,
    IBackward<NativeTrack, NativeClip, NoInput, NativeVitals>
{
    public float Health;
    public long Ticks;
    public int Count;

    public readonly Receipt Result => new(Health, Ticks, Count);

    public void Forward(in Tracks<NativeTrack, NativeClip> tracks, in NoInput input, in uint tick, ref NativeVitals result)
    {
        foreach (var work in tracks)
        {
            if (work.State == ClipState.Stay)
            {
                result.Ticks += work.Track.Offset;
                result.Health += work.Clip.Amount;
            }
        }
        result.Count++;
    }

    public void Backward(in Tracks<NativeTrack, NativeClip> tracks, in NoInput input, in uint tick, ref NativeVitals result)
        => Forward(in tracks, in input, in tick, ref result);
}
