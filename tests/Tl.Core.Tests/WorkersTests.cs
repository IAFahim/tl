using System.Threading;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class WorkersTests
{
    struct Receipt
    {
        public int* Visits;
        public int Running;
        public int InvalidRange;
    }

    static void Visit(void* state, int first, int end, int chunk)
    {
        var receipt = (Receipt*)state;
        Interlocked.Increment(ref receipt->Running);
        if (first != chunk * 3 || end != (chunk + 1) * 3) Interlocked.Increment(ref receipt->InvalidRange);
        Thread.SpinWait((chunk % 7 + 1) * 32);
        Interlocked.Increment(ref receipt->Visits[chunk]);
        Interlocked.Decrement(ref receipt->Running);
    }

    static void VisitAndFail(void* state, int first, int end, int chunk)
    {
        Visit(state,first,end,chunk);
        if (chunk == 17) throw new InvalidOperationException("worker callback");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(11)]
    public void EveryRunCompletesEveryChunkBeforeReturning(int workers)
    {
        const int chunks = 37;
        var ends = stackalloc int[chunks];
        var visits = stackalloc int[chunks];
        for (var i = 0; i < chunks; i++) { ends[i] = (i + 1) * 3; visits[i] = 0; }
        var receipt = new Receipt { Visits = visits };
        using var pool = new TlWorkers(workers);
        for (var round = 1; round <= 128; round++)
        {
            pool.Run(&Visit, &receipt, ends, chunks);
            Assert.Equal(0, Volatile.Read(ref receipt.Running));
            Assert.Equal(0, receipt.InvalidRange);
            for (var i = 0; i < chunks; i++) Assert.Equal(round, Volatile.Read(ref visits[i]));
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void ACallbackFailureCompletesTheRunAndAllowsReuse(int workers)
    {
        const int chunks = 37;
        var ends = stackalloc int[chunks];
        var visits = stackalloc int[chunks];
        for (var i = 0; i < chunks; i++) { ends[i] = (i + 1) * 3; visits[i] = 0; }
        var receipt = new Receipt { Visits = visits };
        using var pool = new TlWorkers(workers);
        var failed = false;
        try { pool.Run(&VisitAndFail,&receipt,ends,chunks); }
        catch (InvalidOperationException error) { Assert.Equal("worker callback",error.Message); failed = true; }
        Assert.True(failed);
        Assert.Equal(0,Volatile.Read(ref receipt.Running));
        for (var i = 0; i < chunks; i++) Assert.Equal(1,Volatile.Read(ref visits[i]));
        pool.Run(&Visit,&receipt,ends,chunks);
        Assert.Equal(0,Volatile.Read(ref receipt.Running));
        for (var i = 0; i < chunks; i++) Assert.Equal(2,Volatile.Read(ref visits[i]));
    }
}
