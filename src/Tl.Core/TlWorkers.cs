using System.Runtime.ExceptionServices;
using System.Threading;

namespace Tl;

public sealed unsafe class TlWorkers : IDisposable
{
    readonly int _workers;
    readonly Thread[]? _threads;
    readonly SemaphoreSlim _gate = new(0);
    readonly ManualResetEventSlim _done = new(false);
    delegate*<void*, int, int, int, void> _body;
    void* _state;
    int* _chunkEnds;
    int _chunks;
    int _next;
    int _completed;
    Exception? _error;
    bool _disposed;

    public TlWorkers(int workers)
    {
        if (workers < 1) throw new ArgumentOutOfRangeException(nameof(workers));
        _workers = workers;
        if (workers > 1)
        {
            _threads = new Thread[workers - 1];
            for (var i = 0; i < _threads.Length; i++)
            {
                var thread = new Thread(WorkerLoop) { IsBackground = true, Name = "TlWorkers-" + i };
                _threads[i] = thread;
                thread.Start();
            }
        }
    }

    public int WorkerCount => _workers;

    public static void Split(ulong* bits, int words, int* chunkEnds, int chunks)
    {
        if (bits == null) throw new ArgumentNullException(nameof(bits));
        if (chunkEnds == null) throw new ArgumentNullException(nameof(chunkEnds));
        if (words < 0) throw new ArgumentOutOfRangeException(nameof(words));
        if (chunks <= 0) throw new ArgumentOutOfRangeException(nameof(chunks));
        if (words == 0)
        {
            for (var i = 0; i < chunks; i++) chunkEnds[i] = 0;
            return;
        }
        var total = 0;
        for (var w = 0; w < words; w++) total += KindStore.Pop(bits[w]);
        if (total == 0)
        {
            for (var i = 0; i < chunks; i++) chunkEnds[i] = (int)((long)words * (i + 1) / chunks);
            chunkEnds[chunks - 1] = words;
            return;
        }
        var target = (total + chunks - 1) / chunks;
        var have = 0;
        var chunk = 0;
        for (var w = 0; w < words && chunk < chunks - 1; w++)
        {
            have += KindStore.Pop(bits[w]);
            if (have >= target)
            {
                chunkEnds[chunk++] = w + 1;
                have = 0;
            }
        }
        for (; chunk < chunks - 1; chunk++) chunkEnds[chunk] = words;
        chunkEnds[chunks - 1] = words;
    }

    public void Run(delegate*<void*, int, int, int, void> body, void* state, int* chunkEnds, int chunks)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TlWorkers));
        if (body == null) throw new ArgumentNullException(nameof(body));
        if (chunks < 0) throw new ArgumentOutOfRangeException(nameof(chunks));
        if (chunks > 0 && chunkEnds == null) throw new ArgumentNullException(nameof(chunkEnds));
        if (chunks == 0) return;
        if (_workers == 1)
        {
            for (var chunk = 0; chunk < chunks; chunk++)
                body(state, chunk == 0 ? 0 : chunkEnds[chunk - 1], chunkEnds[chunk], chunk);
            return;
        }
        _next = 0;
        _completed = 0;
        _error = null;
        _body = body;
        _state = state;
        _chunkEnds = chunkEnds;
        _chunks = chunks;
        _done.Reset();
        _gate.Release(_workers - 1);
        while (true)
        {
            var chunk = Interlocked.Increment(ref _next) - 1;
            if (chunk >= chunks) break;
            try
            {
                body(state, chunk == 0 ? 0 : chunkEnds[chunk - 1], chunkEnds[chunk], chunk);
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref _error, ex, null);
            }
        }
        _done.Wait();
        if (_error != null) ExceptionDispatchInfo.Capture(_error).Throw();
    }

    void WorkerLoop()
    {
        while (true)
        {
            var acquired = false;
            var spin = new SpinWait();
            for (var i = 0; i < 200 && !acquired; i++)
            {
                if (_disposed) return;
                if (_gate.Wait(0)) acquired = true;
                else spin.SpinOnce();
            }
            if (!acquired)
            {
                try
                {
                    _gate.Wait();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
            if (_disposed) return;
            var body = _body;
            var state = _state;
            var ends = _chunkEnds;
            var n = _chunks;
            while (true)
            {
                var chunk = Interlocked.Increment(ref _next) - 1;
                if (chunk >= n) break;
                try
                {
                    body(state, chunk == 0 ? 0 : ends[chunk - 1], ends[chunk], chunk);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref _error, ex, null);
                }
            }
            if (Interlocked.Increment(ref _completed) == _workers - 1) _done.Set();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_workers > 1 && _threads != null)
        {
            try
            {
                _gate.Release(_workers - 1);
            }
            catch (SemaphoreFullException)
            {
            }
            foreach (var thread in _threads) thread.Join();
        }
        _gate.Dispose();
        _done.Dispose();
        GC.SuppressFinalize(this);
    }
}
