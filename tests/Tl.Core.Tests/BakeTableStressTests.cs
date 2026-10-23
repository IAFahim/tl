using Xunit;

namespace Tl.Core.Tests;

public unsafe class BakeTableStressTests
{
    static void Probe(byte** arguments)
    {
    }

    [Fact]
    public void ConcurrentInstallsAndChainReadersSeeFullyPublishedEntries()
    {
        const int writers = 4, perWriter = 300;
        const ulong seed = 0xE100_0000_0000_0000UL;
        delegate*<byte**, void> probe = &Probe;
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
                    BakeTable.Install(seed + (ulong)writer, &Probe, [(ulong)(writer * perWriter + i)]);
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
                        var head = BakeTable.HeadOf(seed + (ulong)w);
                        if (head < 0) continue;
                        var entries = BakeTable.EntryAt;
                        var steps = 0;
                        var lastId = -1;
                        for (var entry = head; entry >= 0; entry = entries[entry].Next)
                        {
                            steps++;
                            if (steps > perWriter)
                            {
                                failures.Add("bake chain cycles");
                                break;
                            }
                            var bake = entries + entry;
                            if ((nint)bake->Invoke != (nint)probe)
                            {
                                failures.Add($"invoke torn: {(nint)bake->Invoke:X16}");
                                break;
                            }
                            if (bake->ParamCount != 1)
                            {
                                failures.Add($"param count torn: {bake->ParamCount}");
                                break;
                            }
                            if (bake->ParamKeys == null)
                            {
                                failures.Add("param keys torn: null before the link that names the entry");
                                break;
                            }
                            var id = (int)bake->ParamKeys[0];
                            if (id < w * perWriter || id >= (w + 1) * perWriter || id <= lastId)
                            {
                                failures.Add($"param key torn: {id} where {(w * perWriter)}..{(w + 1) * perWriter - 1} strictly rising was published");
                                break;
                            }
                            lastId = id;
                        }
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
        {
            var key = seed + (ulong)w;
            Assert.Equal(perWriter, BakeTable.ChainLength(key));
            for (var i = 0; i < perWriter; i++)
            {
                var bake = BakeTable.At(key, i);
                Assert.Equal(1, bake->ParamCount);
                Assert.Equal((ulong)(w * perWriter + i), bake->ParamKeys[0]);
                Assert.True((nint)bake->Invoke == (nint)probe);
            }
        }
    }
}
