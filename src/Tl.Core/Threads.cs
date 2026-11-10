using System;
using System.Threading;

namespace Tl;

public static class ParallelApply
{
    public static void ForEachChunk(int rows, int workers, Action<int, int> body)
    {
        if (body is null) throw new ArgumentNullException(nameof(body));
        if (rows < 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (rows == 0) return;
        var count = workers <= 0 ? Environment.ProcessorCount : workers;
        if (count < 1) count = 1;
        if (count > rows) count = rows;
        if (count == 1)
        {
            body(0, rows);
            return;
        }
        WorkerPool.Get(count).Run(rows, count, body);
    }

    public static void ParallelAccumulate(float[] acc, int[] rows, float[] values, int workers)
    {
        if (acc is null) throw new ArgumentNullException(nameof(acc));
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (rows.Length != values.Length) throw new ArgumentException("rows and values must have the same length.", nameof(rows));
        var count = rows.Length;
        if (count == 0) return;
        ForEachChunk(count, workers, (start, end) =>
        {
            for (var i = start; i < end; i++) acc[rows[i]] += values[i];
        });
    }

    // Cached worker dispatch: Parallel.For allocates per call (~2 KB/tick on the
    // caller thread). A persistent pool with spin-wait dispatch allocates 0 B
    // on the warm path; the caller thread runs slice 0 itself while workers
    // run slices 1..count-1.
    private sealed class WorkerPool
    {
        static WorkerPool? s_pool;
        static int s_poolWorkers;
        static readonly object s_gate = new();

        readonly ManualResetEventSlim _go = new(false);
        readonly int[] _start;
        readonly int[] _end;
        readonly Thread[] _threads;
        readonly object _runGate = new();
        Action<int, int>? _body;
        volatile bool _closing;
        int _gen;
        int _remaining;
        int _runCount;
        Exception? _error;

        private WorkerPool(int workers)
        {
            _start = new int[workers];
            _end = new int[workers];
            _threads = new Thread[workers - 1];
            for (var i = 0; i < _threads.Length; i++)
            {
                var idx = i + 1;
                var t = new Thread(() => WorkerLoop(idx)) { IsBackground = true, Name = "TlWorker" };
                _threads[i] = t;
                t.Start();
            }
        }

        internal static WorkerPool Get(int workers)
        {
            lock (s_gate)
            {
                if (s_pool is null || workers > s_poolWorkers)
                {
                    var old = s_pool;
                    if (old is not null)
                    {
                        // Flush an in-flight Run before shutting the pool down.
                        lock (old._runGate) { }
                        old.Dispose();
                    }
                    s_pool = new WorkerPool(workers);
                    s_poolWorkers = workers;
                }
                return s_pool;
            }
        }

        internal void Run(int rows, int count, Action<int, int> body)
        {
            lock (_runGate)
            {
                var baseSize = rows / count;
                var extra = rows % count;
                for (var i = 0; i < count; i++)
                {
                    _start[i] = i * baseSize + (i < extra ? i : extra);
                    _end[i] = _start[i] + baseSize + (i < extra ? 1 : 0);
                }
                _body = body;
                _error = null;
                _remaining = count - 1;
                _runCount = count;
                Interlocked.Increment(ref _gen);
                _go.Set();
                try
                {
                    body(_start[0], _end[0]);
                }
                catch (Exception ex)
                {
                    _error = ex;
                }
                while (Volatile.Read(ref _remaining) != 0)
                    Thread.SpinWait(512);
                _go.Reset();
                _body = null;
                var error = _error;
                _error = null;
                if (error is not null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
            }
        }

        void WorkerLoop(int idx)
        {
            // seen starts at 0 and _gen at 0: the first dispatch (gen 1) is
            // always detected even if no worker has started yet.
            var seen = 0;
            while (true)
            {
                // Spin briefly for the next dispatch (back-to-back ticks keep
                // the pool hot); fall back to the event for idle waits.
                var spins = 0;
                while (_gen == seen)
                {
                    if (_closing) return;
                    if (++spins > 400)
                    {
                        _go.Wait(1);
                        spins = 0;
                        continue;
                    }
                    Thread.SpinWait(512);
                }
                seen = _gen;
                if (_closing) return;
                // Pool may serve a count smaller than its thread count:
                // threads beyond the run's count idle this dispatch.
                var body = _body;
                if (idx < _runCount && body is not null)
                {
                    try
                    {
                        body(_start[idx], _end[idx]);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref _error, ex, null);
                    }
                }
                if (idx < _runCount)
                    Interlocked.Decrement(ref _remaining);
            }
        }

        public void Dispose()
        {
            _closing = true;
            _go.Set();
            foreach (var t in _threads)
                t.Join();
            _go.Dispose();
        }
    }
}
