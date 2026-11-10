using System;

namespace Tl;

public readonly struct TickReport
{
    public TickReport(int invalidRequests, int highWaterVisits, int activeCount, int frame)
    {
        InvalidRequests = invalidRequests;
        HighWaterVisits = highWaterVisits;
        ActiveCount = activeCount;
        Frame = frame;
    }

    public readonly int InvalidRequests;
    public readonly int HighWaterVisits;
    public readonly int ActiveCount;
    public readonly int Frame;
}

public sealed unsafe partial class World : IDisposable
{
    internal sealed class EventHolder<T> where T : unmanaged
    {
        public T[] Data = new T[64];
        public int Count;

        public void Raise(T value)
        {
            if (Count == Data.Length)
            {
                var grown = new T[Data.Length * 2];
                Array.Copy(Data, grown, Data.Length);
                Data = grown;
            }
            Data[Count++] = value;
        }

        public Span<T> Drain()
        {
            var span = new Span<T>(Data, 0, Count);
            Count = 0;
            return span;
        }
    }

    public readonly struct WorldEvents<T> where T : unmanaged
    {
        private readonly EventHolder<T>? _holder;

        internal WorldEvents(EventHolder<T> holder)
        {
            _holder = holder;
        }

        public int Count => _holder == null ? 0 : _holder.Count;

        public void Raise(in T value)
        {
            if (_holder != null) _holder.Raise(value);
        }

        public Span<T> Drain()
        {
            if (_holder == null) return default;
            return _holder.Drain();
        }
    }

    public sealed class RequestApi
    {
        private readonly World _world;

        internal RequestApi(World world)
        {
            _world = world;
        }

        public void Start(Row row, TimelineId timeline) => _world.EnqueueStart(row, timeline, 0);
        public void Start(Row row, TimelineId timeline, int phase) => _world.EnqueueStart(row, timeline, phase);
        public void Stop(Row row, TimelineId timeline) => _world.EnqueueStop(row, timeline);
        public void StopChannel(Row row, byte channel) => _world.EnqueueStopChannel(row, channel);
        public void StopAll(Row row) => _world.EnqueueStopAll(row);
        public void Pause(Row row, TimelineId timeline) => _world.EnqueuePause(row, timeline, true);
        public void Resume(Row row, TimelineId timeline) => _world.EnqueuePause(row, timeline, false);
        public void Seek(Row row, TimelineId timeline, int tick) => _world.EnqueueSeek(row, timeline, tick);
        public void Reverse(Row row, TimelineId timeline, bool reverse) => _world.EnqueueReverse(row, timeline, reverse);
        public void PauseChannel(Row row, byte channel) => _world.EnqueuePauseChannel(row, channel, true);
        public void ResumeChannel(Row row, byte channel) => _world.EnqueuePauseChannel(row, channel, false);
        public void SeekChannel(Row row, byte channel, int tick) => _world.EnqueueSeekChannel(row, channel, tick);
        public void ReverseChannel(Row row, byte channel, bool reverse) => _world.EnqueueReverseChannel(row, channel, reverse);
        public void SetSpeed(Row row, TimelineId timeline, int sixteenths) => _world.EnqueueSpeed(row, timeline, sixteenths);
        public void SetChannelSpeed(Row row, byte channel, int sixteenths) => _world.EnqueueSpeedChannel(row, channel, sixteenths);
    }

    private const int ParallelThresholdRows = 4096;

    private readonly Library _library;
    private readonly int _rowCapacity;
    private readonly float[] _acc;
    private readonly float[] _applied;
    private readonly int[] _duration;
    private readonly int[] _stepGlobal;
    private readonly int[] _stepWindowStart;
    private readonly int[] _stepWindowEnd;
    private readonly int[] _stepTimeline;
    private readonly int[] _stepSorted;
    private readonly int[] _kindSorted;
    private readonly int[] _kindStepStart;
    private readonly int[] _kindStepEnd;
    private int[] _pbRow;
    private ushort[] _pbTimeline;
    private ushort[] _pbPhase;
    private int[] _workRow;
    private ushort[] _workTimeline;
    private ushort[] _workPhase;
    private int[] _order;
    private int[] _scratch;
    private uint[] _radixKeys;
    private int[] _groupRows;
    private ushort[] _runTimeline;
    private ushort[] _runPhase;
    private int[] _runStart;
    private int[] _runLength;
    private int[] _runTick;
    private bool[] _runDense;
    private int[] _timelineRunStart;
    private int[] _timelineRunCount;
    private object?[] _eventHolders;
    private int _frame;
    private int _invalidPending;
    private int _highWater;
    private int _playbackCount;
    private TickReport _report;
    private bool _sortedDirty = true;
    private int[] _activeRows = new int[0];
    private int _activeRowCount;
    internal int _workers;

    public World(Library library, int rowCapacity)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        if (rowCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(rowCapacity));
        _rowCapacity = rowCapacity;
        _acc = new float[rowCapacity];
        _applied = new float[rowCapacity];
        var timelineCount = _library.TimelineCount;
        var duration = new int[timelineCount];
        var stepCount = new int[timelineCount];
        var stepFlatBase = new int[timelineCount];
        var totalSteps = 0;
        for (var i = 0; i < timelineCount; i++)
        {
            var row = _library.Timeline(i);
            var d = (int)row.Duration;
            duration[i] = d < 0 ? 0 : d;
            var s = (int)row.StepCount;
            stepCount[i] = s < 0 ? 0 : s;
            stepFlatBase[i] = totalSteps;
            totalSteps += stepCount[i];
        }
        _duration = duration;
        _stepGlobal = new int[totalSteps];
        _stepWindowStart = new int[totalSteps];
        _stepWindowEnd = new int[totalSteps];
        _stepTimeline = new int[totalSteps];
        var stepKind = new int[totalSteps];
        var stepKindOrder = new int[totalSteps];
        for (var i = 0; i < timelineCount; i++)
        {
            var globalBase = (int)_library.Timeline(i).StepIndex;
            for (var s = 0; s < stepCount[i]; s++)
            {
                var flat = stepFlatBase[i] + s;
                var g = globalBase + s;
                var step = _library.Step(g);
                _stepGlobal[flat] = g;
                _stepWindowStart[flat] = (int)step.WindowStart;
                _stepWindowEnd[flat] = (int)step.WindowEnd;
                stepKind[flat] = (int)step.KindId;
                stepKindOrder[flat] = KindOrderOf((int)step.KindId);
                _stepTimeline[flat] = i + 1;
            }
        }
        var kindIds = new int[totalSteps < 1 ? 1 : totalSteps];
        var kindCount = 0;
        for (var i = 0; i < totalSteps; i++)
        {
            var kindId = stepKind[i];
            var found = false;
            for (var k = 0; k < kindCount; k++)
            {
                if (kindIds[k] == kindId)
                {
                    found = true;
                    break;
                }
            }
            if (!found) kindIds[kindCount++] = kindId;
        }
        var kinds = new int[kindCount];
        Array.Copy(kindIds, kinds, kindCount);
        for (var i = 1; i < kindCount; i++)
        {
            var key = kinds[i];
            var keyOrder = KindOrderOf(key);
            var j = i - 1;
            while (j >= 0 && CompareKinds(kinds[j], key, keyOrder) > 0)
            {
                kinds[j + 1] = kinds[j];
                j--;
            }
            kinds[j + 1] = key;
        }
        var sorted = new int[totalSteps];
        for (var i = 0; i < totalSteps; i++) sorted[i] = i;
        for (var i = 1; i < totalSteps; i++)
        {
            var key = sorted[i];
            var j = i - 1;
            while (j >= 0 && CompareSteps(sorted[j], key, stepKind, stepKindOrder) > 0)
            {
                sorted[j + 1] = sorted[j];
                j--;
            }
            sorted[j + 1] = key;
        }
        _stepSorted = sorted;
        _kindSorted = kinds;
        _kindStepStart = new int[kindCount];
        _kindStepEnd = new int[kindCount];
        var cursor = 0;
        for (var k = 0; k < kindCount; k++)
        {
            var kindId = kinds[k];
            _kindStepStart[k] = cursor;
            while (cursor < totalSteps && stepKind[sorted[cursor]] == kindId) cursor++;
            _kindStepEnd[k] = cursor;
        }
        var cap = rowCapacity < 64 ? 64 : rowCapacity;
        _pbRow = new int[cap];
        _pbTimeline = new ushort[cap];
        _pbPhase = new ushort[cap];
        _workRow = new int[cap];
        _workTimeline = new ushort[cap];
        _workPhase = new ushort[cap];
        _order = new int[cap];
        _scratch = new int[cap];
        _radixKeys = new uint[cap];
        _groupRows = new int[cap];
        _runTimeline = new ushort[cap];
        _runPhase = new ushort[cap];
        _runStart = new int[cap];
        _runLength = new int[cap];
        _runTick = new int[cap];
        _runDense = new bool[cap];
        _timelineRunStart = new int[timelineCount];
        _timelineRunCount = new int[timelineCount];
        for (var i = 0; i < timelineCount; i++) _timelineRunStart[i] = -1;
        InitSystems(timelineCount);
        _eventHolders = new object?[4];
        _report = new TickReport(0, 0, 0, 0);
    }

    public int RowCapacity => _rowCapacity;

    public int Frame => _frame;

    public int ActiveCount => _playbackCount + _soloCount;

    public float[] Accumulator => _applied;

    public TickReport Report => _report;

    public RequestApi Requests
    {
        get { return new RequestApi(this); }
    }

    public WorldEvents<T> Events<T>() where T : unmanaged
    {
        for (var i = 0; i < _eventHolders.Length; i++)
            if (_eventHolders[i] is EventHolder<T> h) return new WorldEvents<T>(h);
        var holder = new EventHolder<T>();
        for (var i = 0; i < _eventHolders.Length; i++)
        {
            if (_eventHolders[i] != null) continue;
            _eventHolders[i] = holder;
            return new WorldEvents<T>(holder);
        }
        var old = _eventHolders.Length;
        Array.Resize(ref _eventHolders, old * 2);
        _eventHolders[old] = holder;
        return new WorldEvents<T>(holder);
    }

    public void Tick(Direction direction)
    {
        _frame++;
        _closingCount = 0;
        ApplyPendingRequests();
        var dir = direction == Direction.Reverse ? -1 : 1;
        var invalid = _invalidPending;
        _invalidPending = 0;
        var n = _playbackCount;
        if (n == 0)
        {
            var soloVisits = 0;
            if (_soloCount != 0) soloVisits = StepSolo(dir);
            if (soloVisits > _highWater) _highWater = soloVisits;
            CopyActiveToApplied();
            _report = new TickReport(invalid, _highWater, n + _soloCount, _frame);
            return;
        }
        if (_sortedDirty)
        {
            for (var i = 0; i < n; i++)
            {
                _workRow[i] = _pbRow[i];
                _workTimeline[i] = _pbTimeline[i];
                _workPhase[i] = _pbPhase[i];
            }
            SortCohort(n);
            BuildActiveRows(n);
            _sortedDirty = false;
        }
        var runCount = BuildAndPrepareRuns(n, dir);
        var laneDir = dir < 0 ? LaneDirection.Backward : LaneDirection.Forward;
        var totalVisits = CountVisits();
        if (totalVisits >= ParallelThresholdRows && _activeRowCount >= ParallelThresholdRows)
        {
            RunParallel(laneDir);
        }
        else
        {
            for (var k = 0; k < _kindSorted.Length; k++)
            {
                var stepStart = _kindStepStart[k];
                var stepEnd = _kindStepEnd[k];
                for (var s = stepStart; s < stepEnd; s++)
                {
                    var flat = _stepSorted[s];
                    var timelineIndex = _stepTimeline[flat] - 1;
                    var runIndex = _timelineRunStart[timelineIndex];
                    if (runIndex < 0) continue;
                    var runsForTimeline = _timelineRunCount[timelineIndex];
                    var globalStep = _stepGlobal[flat];
                    var windowStart = _stepWindowStart[flat];
                    var windowEnd = _stepWindowEnd[flat];
                    for (var r = 0; r < runsForTimeline; r++)
                    {
                        var runPos = runIndex + r;
                        var tick = _runTick[runPos];
                        if (tick < windowStart || tick >= windowEnd) continue;
                        var value = _library.LaneValue(globalStep, laneDir, tick);
                        var length = _runLength[runPos];
                        var offset = _runStart[runPos];
                        if (_runDense[runPos])
                        {
                            Kernels.BroadcastDense(_acc, _groupRows[offset], value, length);
                        }
                        else
                        {
                            Kernels.BroadcastSparse(_acc, 0, _groupRows, offset, length, value);
                        }
                    }
                }
            }
        }
        if (_soloCount != 0) totalVisits += StepSolo(dir);
        if (totalVisits > _highWater) _highWater = totalVisits;
        CopyActiveToApplied();
        _report = new TickReport(invalid, _highWater, n + _soloCount, _frame);
    }

    public void Dispose()
    {
    }

    private int BuildAndPrepareRuns(int n, int dir)
    {
        for (var t = 0; t < _timelineRunStart.Length; t++)
        {
            _timelineRunStart[t] = -1;
            _timelineRunCount[t] = 0;
        }
        var count = 0;
        var pos = 0;
        while (pos < n)
        {
            var idx = _order[pos];
            var timeline = _workTimeline[idx];
            var phase = _workPhase[idx];
            var start = pos;
            var row = _workRow[idx];
            _groupRows[pos] = row;
            var previousRow = row;
            var ascending = true;
            var dense = true;
            pos++;
            while (pos < n)
            {
                var next = _order[pos];
                if (_workTimeline[next] != timeline || _workPhase[next] != phase) break;
                var nextRow = _workRow[next];
                _groupRows[pos] = nextRow;
                if (nextRow < previousRow) ascending = false;
                if (nextRow != previousRow + 1) dense = false;
                previousRow = nextRow;
                pos++;
            }
            var length = pos - start;
            if (!ascending)
            {
                Array.Sort(_groupRows, start, length);
                dense = true;
                for (var i = 1; i < length; i++)
                {
                    if (_groupRows[start + i] != _groupRows[start + i - 1] + 1)
                    {
                        dense = false;
                        break;
                    }
                }
            }
            _runTimeline[count] = timeline;
            _runPhase[count] = phase;
            _runStart[count] = start;
            _runLength[count] = length;
            var timelineIndex = timeline - 1;
            var duration = timelineIndex >= 0 && timelineIndex < _duration.Length ? _duration[timelineIndex] : 0;
            _runTick[count] = duration > 0 ? Kernels.PhaseToTick(phase, _frame, duration, dir) : -1;
            _runDense[count] = dense;
            if (timelineIndex >= 0 && timelineIndex < _timelineRunStart.Length)
            {
                if (_timelineRunStart[timelineIndex] < 0) _timelineRunStart[timelineIndex] = count;
                _timelineRunCount[timelineIndex]++;
            }
            count++;
        }
        return count;
    }

    private int CountVisits()
    {
        var totalVisits = 0;
        for (var k = 0; k < _kindSorted.Length; k++)
        {
            var stepStart = _kindStepStart[k];
            var stepEnd = _kindStepEnd[k];
            for (var s = stepStart; s < stepEnd; s++)
            {
                var flat = _stepSorted[s];
                var timelineIndex = _stepTimeline[flat] - 1;
                var runIndex = _timelineRunStart[timelineIndex];
                if (runIndex < 0) continue;
                var runsForTimeline = _timelineRunCount[timelineIndex];
                var windowStart = _stepWindowStart[flat];
                var windowEnd = _stepWindowEnd[flat];
                for (var r = 0; r < runsForTimeline; r++)
                {
                    var runPos = runIndex + r;
                    var tick = _runTick[runPos];
                    if (tick < windowStart || tick >= windowEnd) continue;
                    totalVisits += _runLength[runPos];
                }
            }
        }
        return totalVisits;
    }

    private void RunParallel(LaneDirection laneDir)
    {
        var activeCount = _activeRowCount;
        if (activeCount == 0) return;
        var workers = _workers;
        for (var k = 0; k < _kindSorted.Length; k++)
        {
            var kindIndex = k;
            ParallelApply.ForEachChunk(activeCount, workers, (chunkStart, chunkEnd) => ApplyKindChunk(chunkStart, chunkEnd, kindIndex, laneDir));
        }
    }

    private void ApplyKindChunk(int chunkStart, int chunkEnd, int kindIndex, LaneDirection laneDir)
    {
        if (chunkStart >= chunkEnd) return;
        var low = _activeRows[chunkStart];
        var high = chunkEnd < _activeRowCount ? _activeRows[chunkEnd] : _activeRows[_activeRowCount - 1] + 1;
        var stepStart = _kindStepStart[kindIndex];
        var stepEnd = _kindStepEnd[kindIndex];
        for (var s = stepStart; s < stepEnd; s++)
        {
            var flat = _stepSorted[s];
            var timelineIndex = _stepTimeline[flat] - 1;
            var runIndex = _timelineRunStart[timelineIndex];
            if (runIndex < 0) continue;
            var runsForTimeline = _timelineRunCount[timelineIndex];
            var globalStep = _stepGlobal[flat];
            var windowStart = _stepWindowStart[flat];
            var windowEnd = _stepWindowEnd[flat];
            for (var r = 0; r < runsForTimeline; r++)
            {
                var runPos = runIndex + r;
                var tick = _runTick[runPos];
                if (tick < windowStart || tick >= windowEnd) continue;
                var value = _library.LaneValue(globalStep, laneDir, tick);
                var length = _runLength[runPos];
                var offset = _runStart[runPos];
                if (_runDense[runPos])
                {
                    var firstRow = _groupRows[offset];
                    var lastRow = firstRow + length - 1;
                    var overlapStart = firstRow < low ? low : firstRow;
                    var overlapEnd = lastRow + 1 < high ? lastRow + 1 : high;
                    if (overlapStart < overlapEnd) Kernels.BroadcastDense(_acc, overlapStart, value, overlapEnd - overlapStart);
                }
                else
                {
                    var segmentStart = LowerBound(_groupRows, offset, length, low);
                    var segmentEnd = LowerBound(_groupRows, offset, length, high);
                    if (segmentStart < segmentEnd) Kernels.BroadcastSparse(_acc, 0, _groupRows, offset + segmentStart, segmentEnd - segmentStart, value);
                }
            }
        }
    }

    private static int LowerBound(int[] rows, int start, int length, int value)
    {
        var lo = 0;
        var hi = length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (rows[start + mid] < value) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }

    private void BuildActiveRows(int n)
    {
        if (_activeRows.Length < n)
        {
            var cap = n > _activeRows.Length * 2 ? n : _activeRows.Length * 2;
            if (cap < 64) cap = 64;
            Array.Resize(ref _activeRows, cap);
        }
        if (n == 0)
        {
            _activeRowCount = 0;
            return;
        }
        Array.Copy(_pbRow, _activeRows, n);
        var sorted = true;
        for (var i = 1; i < n; i++)
        {
            if (_activeRows[i] < _activeRows[i - 1])
            {
                sorted = false;
                break;
            }
        }
        if (!sorted) Array.Sort(_activeRows, 0, n);
        var m = 0;
        for (var i = 0; i < n; i++)
        {
            var row = _activeRows[i];
            if (m > 0 && _activeRows[m - 1] == row) continue;
            _activeRows[m++] = row;
        }
        _activeRowCount = m;
    }

    private void CopyActiveToApplied()
    {
        if (_playbackCount > 0 && _activeRowCount > 0)
        {
            for (var i = 0; i < _activeRowCount; i++)
            {
                var row = _activeRows[i];
                _applied[row] = _acc[row];
            }
        }
        else if (_playbackCount > 0)
        {
            var plainCount = _playbackCount;
            for (var i = 0; i < plainCount; i++)
            {
                var row = _pbRow[i];
                _applied[row] = _acc[row];
            }
        }
        var soloCount = _soloCount;
        for (var i = 0; i < soloCount; i++)
        {
            var row = _soloRow[i];
            _applied[row] = _acc[row];
        }
    }


    private void SortCohort(int n)
    {
        for (var i = 0; i < n; i++) _order[i] = i;
        for (var i = 0; i < n; i++) _radixKeys[i] = _workPhase[i];
        StableRadixN(_radixKeys, _order, _scratch, n);
        for (var i = 0; i < n; i++) _radixKeys[i] = _workTimeline[i];
        StableRadixN(_radixKeys, _order, _scratch, n);
    }

    private static void StableRadixN(uint[] keys, int[] order, int[] scratch, int n)
    {
        if (n <= 1) return;
        Span<int> counts = stackalloc int[256];
        var source = order;
        var destination = scratch;
        for (var pass = 0; pass < 4; pass++)
        {
            counts.Clear();
            for (var i = 0; i < n; i++) counts[(int)((keys[source[i]] >> (pass * 8)) & 255u)]++;
            var sum = 0;
            for (var b = 0; b < 256; b++)
            {
                var c = counts[b];
                counts[b] = sum;
                sum += c;
            }
            for (var i = 0; i < n; i++)
            {
                var id = source[i];
                var slot = (int)((keys[id] >> (pass * 8)) & 255u);
                destination[counts[slot]++] = id;
            }
            var swap = source;
            source = destination;
            destination = swap;
        }
        if (source != order)
        {
            for (var i = 0; i < n; i++) order[i] = source[i];
        }
    }

    private int KindOrderOf(int kindId)
    {
        var count = _library.KindCount;
        for (var k = 0; k < count; k++)
        {
            var row = _library.Kind(k);
            if ((int)row.Id == kindId) return (int)row.Order;
        }
        return kindId;
    }

    private int CompareKinds(int leftKind, int rightKind, int rightOrder)
    {
        var leftOrder = KindOrderOf(leftKind);
        if (leftOrder != rightOrder) return leftOrder < rightOrder ? -1 : 1;
        if (leftKind != rightKind) return leftKind < rightKind ? -1 : 1;
        return 0;
    }

    private static int CompareSteps(int left, int right, int[] stepKind, int[] stepKindOrder)
    {
        var leftOrder = stepKindOrder[left];
        var rightOrder = stepKindOrder[right];
        if (leftOrder != rightOrder) return leftOrder < rightOrder ? -1 : 1;
        var leftKind = stepKind[left];
        var rightKind = stepKind[right];
        if (leftKind != rightKind) return leftKind < rightKind ? -1 : 1;
        if (left != right) return left < right ? -1 : 1;
        return 0;
    }


    private void EnsurePlaybackCapacity(int needed)
    {
        if (_pbRow.Length >= needed) return;
        var n = Math.Max(needed, _pbRow.Length * 2);
        Array.Resize(ref _pbRow, n);
        Array.Resize(ref _pbTimeline, n);
        Array.Resize(ref _pbPhase, n);
        Array.Resize(ref _workRow, n);
        Array.Resize(ref _workTimeline, n);
        Array.Resize(ref _workPhase, n);
        Array.Resize(ref _order, n);
        Array.Resize(ref _scratch, n);
        Array.Resize(ref _radixKeys, n);
        Array.Resize(ref _groupRows, n);
        Array.Resize(ref _runTimeline, n);
        Array.Resize(ref _runPhase, n);
        Array.Resize(ref _runStart, n);
        Array.Resize(ref _runLength, n);
        Array.Resize(ref _runTick, n);
        Array.Resize(ref _runDense, n);
    }
}
