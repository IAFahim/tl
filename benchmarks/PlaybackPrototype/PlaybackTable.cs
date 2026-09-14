using System.Runtime.CompilerServices;
using Tl;

namespace PlaybackPrototype;

public readonly record struct TableOptions(int Capacity, int OwnerCount = 0, bool Handles = false, bool WatchColumns = false);

public sealed class PlaybackTable<TTrack, TClip> : IDisposable
    where TTrack : unmanaged, IBlend<TClip>, ICodeTrack
    where TClip : unmanaged, IAmountClip
{
    private struct FrameEntry
    {
        public float Amount;
        public int Code;
        public byte Present;
    }

    private readonly TimelineAsset _asset;
    private readonly uint _duration;
    private readonly bool _loops;
    private readonly bool _pulse;
    private readonly float _pulseAmount;
    private readonly int _pulseCode;
    private readonly FrameEntry[] _frames;

    private readonly uint[] _positions;
    private readonly long[] _cycles;
    private readonly int[] _targets;
    private readonly int[] _owners;
    private readonly Acc[] _accs;
    private readonly int[]? _rowHandle;
    private readonly int[]? _handleRow;
    private readonly int[]? _freeHandles;
    private readonly bool _rowIndexedAccs;
    private int _freeCount;
    private int _handleCount;
    private int _count;

    internal PlaybackTable(TimelineAsset asset, uint duration, bool loops, in TableOptions options)
    {
        _asset = asset;
        _duration = duration;
        _loops = loops;
        _frames = Precompute(asset, duration);
        _pulse = loops && duration == 1 && _frames[0].Present != 0;
        if (_pulse)
        {
            _pulseAmount = _frames[0].Amount;
            _pulseCode = _frames[0].Code;
        }

        _positions = new uint[options.Capacity];
        _cycles = loops ? new long[options.Capacity] : [];
        _rowIndexedAccs = options.OwnerCount == 0;
        _accs = new Acc[_rowIndexedAccs ? options.Capacity : options.OwnerCount];
        _targets = options.WatchColumns ? new int[options.Capacity] : [];
        _owners = options.WatchColumns ? new int[options.Capacity] : [];
        if (options.Handles)
        {
            _rowHandle = new int[options.Capacity];
            _handleRow = new int[options.Capacity];
            _freeHandles = new int[options.Capacity];
        }
    }

    public int Count => _count;

    public int HandleCount => _handleCount;

    public long AccValue(int handle) => _accs[_handleRow![handle]].Value;

    public Acc[] Accs => _accs;

    public uint Duration => _duration;

    public bool Loops => _loops;

    public bool PulseClassified => _pulse;

    private static FrameEntry[] Precompute(TimelineAsset asset, uint duration)
    {
        var frames = new FrameEntry[duration];
        for (uint position = 0; position < duration; position++)
        {
            var component = new TimelineComponent(asset.Reference) { Position = position };
            var occurrences = 0;
            foreach (var frame in Timeline.Query<TTrack, TClip>(in component))
            {
                if (++occurrences > 1)
                    throw new InvalidOperationException("Prototype tables bind pairs with at most one occurrence per tick.");
                frames[position].Amount = frame.Clip.Amount;
                frames[position].Code = frame.Track.Code;
                frames[position].Present = 1;
            }
        }

        return frames;
    }

    public int Spawn(uint position, int watched = 0, int owner = 0)
    {
        if (_count == _positions.Length)
            throw new InvalidOperationException("Table capacity exhausted.");
        var row = _count++;
        _positions[row] = position;
        if (_cycles.Length > 0) _cycles[row] = 0;
        if (_targets.Length > 0) _targets[row] = watched;
        if (_owners.Length > 0) _owners[row] = owner;
        if (_rowIndexedAccs) _accs[row] = default;
        if (_rowHandle == null) return -1;
        var handle = AllocHandle();
        _rowHandle[row] = handle;
        _handleRow![handle] = row;
        return handle;
    }

    public void Retire(int handle)
    {
        if (_rowHandle == null)
            throw new InvalidOperationException("Table was not created with handles.");
        if (handle < 0 || handle >= _handleCount)
            throw new ArgumentException("Unknown handle.");
        RemoveRow(_handleRow![handle]);
        _freeHandles![_freeCount++] = handle;
    }

    private int AllocHandle()
    {
        if (_freeCount > 0) return _freeHandles![--_freeCount];
        if (_handleCount == _handleRow!.Length)
            throw new InvalidOperationException("Handle capacity exhausted.");
        return _handleCount++;
    }

    private void RemoveRow(int row)
    {
        var last = --_count;
        if (row != last)
        {
            _positions[row] = _positions[last];
            if (_cycles.Length > 0) _cycles[row] = _cycles[last];
            if (_targets.Length > 0) _targets[row] = _targets[last];
            if (_owners.Length > 0) _owners[row] = _owners[last];
            if (_rowIndexedAccs) _accs[row] = _accs[last];
            if (_rowHandle != null)
            {
                var moved = _rowHandle[last];
                _rowHandle[row] = moved;
                _handleRow![moved] = row;
            }
        }
    }

    private void RetireRowAt(int row)
    {
        var handle = _rowHandle != null ? _rowHandle[row] : -1;
        RemoveRow(row);
        if (handle >= 0) _freeHandles![_freeCount++] = handle;
    }

    public static PlaybackTable<TTrack, TClip> Attach(TimelineAsset asset, uint duration, bool loops, in TableOptions options)
        => new(asset, duration, loops, options);

    public long Advance(uint gameTick, ReadOnlySpan<Input> inputs, bool fullReduce)
    {
        if (_pulse) return AdvanceConstant(gameTick, inputs, fullReduce);
        if (_cycles.Length == 0)
            throw new InvalidOperationException("Non-looping tables advance through AdvanceChurn.");
        var count = _count;
        var positions = _positions;
        var cycles = _cycles;
        var accs = _accs;
        var frames = _frames;
        var duration = _duration;
        var loops = _loops;
        for (var row = 0; row < count; row++)
        {
            var state = new TimelineState(1u, positions[row], cycles[row]);
            if (!TimelineMovement.Select(in state, duration, loops, false, out var next, out var tick, out var cycle, out _))
                continue;
            positions[row] = next.Position;
            cycles[row] = next.Cycle;
            ref readonly var frame = ref frames[tick];
            if (frame.Present == 0) continue;
            MoveJob.Execute(frame.Amount, frame.Code, gameTick, inputs[row].Value, ref accs[row]);
        }

        return Checksums.Sample(accs, count, fullReduce);
    }

    private long AdvanceConstant(uint gameTick, ReadOnlySpan<Input> inputs, bool fullReduce)
    {
        var count = _count;
        var cycles = _cycles;
        var accs = _accs;
        var amount = _pulseAmount;
        var code = _pulseCode;
        for (var row = 0; row < count; row++)
        {
            cycles[row]++;
            MoveJob.Execute(amount, code, gameTick, inputs[row].Value, ref accs[row]);
        }

        return Checksums.Sample(accs, count, fullReduce);
    }

    public long Observe(uint gameTick, ReadOnlySpan<Input> inputs, bool fullReduce)
    {
        var count = _count;
        var positions = _positions;
        var cycles = _cycles;
        var accs = _accs;
        var targets = _targets;
        var owners = _owners;
        var frames = _frames;
        var duration = _duration;
        for (var row = 0; row < count; row++)
        {
            var state = new TimelineState(1u, positions[row], cycles[row]);
            if (!TimelineMovement.Select(in state, duration, true, false, out var next, out var tick, out _, out _))
                continue;
            positions[row] = next.Position;
            cycles[row] = next.Cycle;
            ref readonly var frame = ref frames[tick];
            if (frame.Present == 0) continue;
            WatchJob.Commit(ref accs[owners[row]], WatchJob.Contribution(frame.Amount, gameTick, inputs[targets[row]].Value));
        }

        return Checksums.Sample(accs, accs.Length, fullReduce);
    }

    public int AdvanceChurn(uint gameTick)
    {
        var row = 0;
        var retired = 0;
        while (row < _count)
        {
            var state = new TimelineState(1u, _positions[row], 0);
            if (!TimelineMovement.Select(in state, _duration, false, false, out var next, out var tick, out _, out _))
            {
                RetireRowAt(row);
                retired++;
                continue;
            }

            _positions[row] = next.Position;
            ref readonly var frame = ref _frames[tick];
            ChurnJob.Execute(frame.Amount, frame.Code, gameTick, ref _accs[row]);
            if (next.Position == _duration)
            {
                RetireRowAt(row);
                retired++;
                continue;
            }

            row++;
        }

        return retired;
    }

    public (long Positions, long Cycles) StateSummary()
    {
        long positions = 0;
        long cycles = 0;
        for (var row = 0; row < _count; row++)
        {
            positions += _positions[row];
            if (_cycles.Length > 0) cycles += _cycles[row];
        }

        return (positions, cycles);
    }

    public void Dispose() { }
}

internal static class Checksums
{
    public static long Sample(Acc[] accs, int count, bool full)
    {
        if (full)
        {
            long value = 0;
            for (var i = 0; i < count; i++) value += accs[i].Value;
            return value;
        }

        long sampled = 0;
        for (var i = 0; i < count; i += 4099) sampled = unchecked(sampled * 31 + accs[i].Value);
        return sampled;
    }

    public static long Mix(long seed, long value) => unchecked(seed * 31 + value);
}
