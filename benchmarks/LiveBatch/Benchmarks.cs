using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;
using Tl;

namespace LiveBatch;

public sealed class LiveBatchConfig : ManualConfig
{
    public LiveBatchConfig()
    {
        AddJob(Job.Default.WithWarmupCount(8).WithIterationCount(16).WithIterationTime(TimeInterval.FromMilliseconds(200)));
        AddColumn(StatisticColumn.Median);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
    }
}

[InProcess]
[MemoryDiagnoser]
[Config(typeof(LiveBatchConfig))]
public class LiveBatchBenchmarks
{
    public const int Rows = 100_000;
    public const int Chunk = 128;

    ushort[] _a = null!;
    ushort[] _b = null!;
    ushort[] _c = null!;
    ushort[] _d = null!;
    ushort[] _clocks = null!;
    float[] _lift = null!;
    float[] _height = null!;
    int[] _pushes = null!;
    float[] _total = null!;
    float[] _d0 = null!;
    float[] _d1 = null!;
    float[] _d2 = null!;
    float[] _d3 = null!;

    public long Sink;

    [GlobalSetup]
    public void Setup()
    {
        _a = Spread(Host.A);
        _b = Spread(Host.B);
        _c = Spread(Host.C);
        _d = Spread(Host.D);
        _clocks = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            _clocks[i] = (ushort)(i % Host.Duration);
        _lift = new float[Rows];
        _height = new float[Rows];
        _pushes = new int[Rows];
        for (var i = 0; i < Rows; i++) _pushes[i] = 1 + i % 4;
        _total = new float[Rows];
        _d0 = new float[Rows];
        _d1 = new float[Rows];
        _d2 = new float[Rows];
        _d3 = new float[Rows];
    }

    static ushort[] Spread(ushort[] bank)
    {
        var ids = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            ids[i] = bank[i % Host.Variants];
        return ids;
    }

    [Benchmark]
    public void A_Folded()
    {
        Timeline<LiftTrack, LiftClip>.Apply(_a, _clocks, true, _lift);
        Sink += (long)_lift[0];
    }

    [Benchmark]
    public void B_Apply()
    {
        var columns = new ColumnSet();
        columns.Add(_height);
        columns.Add(_pushes);
        Timeline<ArcTrack, ArcClip>.Apply(_b, _clocks, true, in columns);
        Sink += (long)_height[0];
    }

    [Benchmark]
    public void B_PreparePlay()
    {
        var columns = new ColumnSet();
        columns.Add(_height);
        columns.Add(_pushes);
        var state = Timeline<ArcTrack, ArcClip>.Prepare(_b, in columns);
        try
        {
            Timeline<ArcTrack, ArcClip>.Play(in state, _clocks, true);
        }
        finally
        {
            Timeline<ArcTrack, ArcClip>.Release(ref state);
        }
        Sink += (long)_height[0];
    }

    [Benchmark]
    public void B_Chunk128()
    {
        for (var start = 0; start < Rows; start += Chunk)
        {
            var count = Math.Min(Chunk, Rows - start);
            var columns = new ColumnSet();
            columns.Add(_height.AsSpan(start, count));
            columns.Add(_pushes.AsSpan(start, count));
            Timeline<ArcTrack, ArcClip>.Apply(
                _b.AsSpan(start, count), _clocks.AsSpan(start, count), true, in columns);
        }
        Sink += (long)_height[0];
    }

    [Benchmark]
    public void C_Frame()
    {
        Timeline<PushTrack, PushClip>.Apply(_c, _clocks, true, _total);
        Sink += (long)_total[0];
    }

    [Benchmark]
    public void D_FourPairs()
    {
        Timeline<D0Track, D0Clip>.Apply(_d, _clocks, true, _d0);
        Timeline<D1Track, D1Clip>.Apply(_d, _clocks, true, _d1);
        Timeline<D2Track, D2Clip>.Apply(_d, _clocks, true, _d2);
        Timeline<D3Track, D3Clip>.Apply(_d, _clocks, true, _d3);
        Sink += (long)(_d0[0] + _d1[0] + _d2[0] + _d3[0]);
    }
}
