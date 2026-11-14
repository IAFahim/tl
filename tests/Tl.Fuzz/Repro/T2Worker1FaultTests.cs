using Xunit;

namespace Tl.Fuzz.Repro;

public sealed unsafe class T2Worker1FaultTests
{
    static void VisitAndFailOnZero(void* state, int first, int end, int chunk)
    {
        var visits = (int*)state;
        visits[chunk]++;
        if (chunk == 0)
            throw new InvalidOperationException("chunk 0 failed");
    }

    [Fact]
    public void SingleWorkerPool_AbortsRemainingChunksAfterCallbackException()
    {
        var ends = stackalloc int[2] { 1, 2 };
        var visits = stackalloc int[2] { 0, 0 };
        using var pool = new TlWorkers(1);
        Assert.Throws<InvalidOperationException>(() => pool.Run(&VisitAndFailOnZero, visits, ends, 2));
        Assert.Equal(1, visits[0]);
        Assert.Equal(1, visits[1]);
    }
}
