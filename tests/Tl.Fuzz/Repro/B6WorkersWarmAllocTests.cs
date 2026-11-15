using System.Threading;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed unsafe class B6WorkersWarmAllocTests
{
    struct WorkState
    {
        public int MainThreadId;
        public int WorkerStarted;
        public int SleepWorkers;
    }

    static void Body(void* ptr, int first, int end, int chunk)
    {
        var state = (WorkState*)ptr;
        if (Environment.CurrentManagedThreadId == state->MainThreadId)
        {
            if (Volatile.Read(ref state->SleepWorkers) == 0)
            {
                Thread.Sleep(1);
                return;
            }
            var spin = new SpinWait();
            while (Volatile.Read(ref state->WorkerStarted) == 0) spin.SpinOnce();
            return;
        }
        if (Volatile.Read(ref state->SleepWorkers) != 0)
        {
            Volatile.Write(ref state->WorkerStarted, 1);
            Thread.Sleep(25);
        }
    }

    [Fact]
    public void TlWorkersRun_FirstContendedWaitAfterWarmup_AllocatesManagedBytes()
    {
        const int chunks = 2;
        var ends = stackalloc int[chunks];
        for (var i = 0; i < chunks; i++) ends[i] = i + 1;

        using var pool = new TlWorkers(2);
        var state = new WorkState { MainThreadId = Environment.CurrentManagedThreadId };

        for (var warm = 0; warm < 8; warm++)
            pool.Run(&Body, &state, ends, chunks);

        Volatile.Write(ref state.SleepWorkers, 1);
        var before = GC.GetAllocatedBytesForCurrentThread();
        pool.Run(&Body, &state, ends, chunks);
        var alloc = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, alloc);
    }
}
