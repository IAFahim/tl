using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct ChurnClip(float Amount);
public readonly record struct ChurnTrack(float Scale) : IBlend<ChurnClip>
{
    public void Blend(in ChurnClip first, in ChurnClip second, float factor, out ChurnClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class SnapshotStressConsumers
{
    internal static long Rows;

    [ModuleInitializer]
    internal static void Install()
        => PairRuntime<ChurnTrack, ChurnClip>.ConsumeDispatch(&Row, "churn\0Row");

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => Interlocked.Increment(ref Rows);
}

public sealed class InternChurnStressTests
{
    [Fact]
    public void DispatchStaysTotalWhileLoaderChurnsDeathsAndRevivals()
    {
        using var anchor = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<ChurnTrack, ChurnClip>(new ChurnTrack(2f))
            .Clip(0, 0, 32, new ChurnClip(1f))
            .Bake()));
        var pool = new byte[64][];
        for (var i = 0; i < pool.Length; i++)
            pool[i] = new DomainBaker()
                .Track<ChurnTrack, ChurnClip>(new ChurnTrack(1f + i))
                .Clip(0, 0, (uint)(8 + i % 25), new ChurnClip(i))
                .Bake();
        var ids = new ushort[] { anchor.Index };
        var positions = new ushort[] { 3 };
        var rowsBefore = SnapshotStressConsumers.Rows;
        var failures = new List<Exception>();
        var running = true;

        void Loader()
        {
            try
            {
                var n = 0;
                while (Volatile.Read(ref running))
                {
                    using var lease = TimelineAsset.Of(TimelineAsset.Load(pool[n++ % pool.Length]));
                }
            }
            catch (Exception ex)
            {
                lock (failures) failures.Add(ex);
            }
        }

        void Dispatcher()
        {
            try
            {
                for (var i = 0; i < 40_000; i++)
                    Timeline<ChurnTrack, ChurnClip>.Apply<ushort, ushort>(ids, positions, true);
            }
            catch (Exception ex)
            {
                lock (failures) failures.Add(ex);
            }
        }

        var threads = new Thread[Environment.ProcessorCount + 1];
        for (var i = 0; i < threads.Length - 1; i++)
            threads[i] = new Thread(Dispatcher);
        threads[^1] = new Thread(Loader);
        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads.Take(threads.Length - 1)) thread.Join();
        Volatile.Write(ref running, false);
        threads[^1].Join(2000);

        Assert.Empty(failures);
        Assert.Equal(40_000 * (threads.Length - 1), SnapshotStressConsumers.Rows - rowsBefore);
    }
}
