using Xunit;

namespace Tl.Core.Tests;

public unsafe class PairTableStressTests
{
    [Fact]
    public void ConcurrentInstallsReadersAndCollectionStayConsistent()
    {
        const int writers = 4, perWriter = 1250;
        const ulong seed = 0xE000_0000_0000_0000UL;
        var failures = new List<string>();
        var stop = 0;
        var writerThreads = new Thread[writers];
        for (var w = 0; w < writers; w++)
        {
            var writer = w;
            writerThreads[w] = new Thread(() =>
            {
                for (var i = 0; i < perWriter; i++)
                {
                    var key = seed + (ulong)(writer * perWriter + i);
                    PairTable.Install(key, null, null, null, null, false);
                    PairTable.VerifyLayout(key, (ulong)(writer + 1));
                    if (writer == 0 && (i & 127) == 0) GC.Collect();
                }
            });
        }
        var readerThreads = new Thread[3];
        for (var r = 0; r < readerThreads.Length; r++)
        {
            readerThreads[r] = new Thread(() =>
            {
                while (Volatile.Read(ref stop) == 0)
                {
                    for (var w = 0; w < writers; w++)
                    {
                        var key = seed + (ulong)(w * perWriter);
                        var head = PairTable.HeadOf(key);
                        if (head < 0) continue;
                        var consumers = PairTable.ConsumerAt;
                        var steps = 0;
                        for (var e = head; e >= 0; e = consumers[e].Next)
                        {
                            steps++;
                            if (steps > writers * perWriter)
                            {
                                failures.Add("consumer chain cycles");
                                break;
                            }
                        }
                    }
                    for (var w = 0; w < writers; w++)
                    {
                        var layout = PairTable.LayoutOf(seed + (ulong)(w * perWriter));
                        if (layout != 0 && layout != (ulong)(w + 1))
                            failures.Add($"layout torn: {layout:X16} where 0 or {w + 1} was published");
                    }
                }
            });
        }
        foreach (var thread in readerThreads) thread.Start();
        foreach (var thread in writerThreads) thread.Start();
        foreach (var thread in writerThreads) thread.Join();
        Volatile.Write(ref stop, 1);
        foreach (var thread in readerThreads) thread.Join();
        Assert.Empty(failures);
        for (var w = 0; w < writers; w++)
            for (var i = 0; i < perWriter; i++)
            {
                var key = seed + (ulong)(w * perWriter + i);
                Assert.True(PairTable.HeadOf(key) >= 0);
                Assert.Equal((ulong)(w + 1), PairTable.LayoutOf(key));
            }
    }
}
