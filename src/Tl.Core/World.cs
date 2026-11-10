using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

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

    internal struct TickFlat
    {
        public int Delta;
        public int Dur;
        public int WinStart;
        public int WinEnd;
        public float* Plane;
        public int Len;
    }

    private readonly Library _library;
    private readonly int _rowCapacity;
    private readonly float[] _acc;
    private readonly float[] _applied;
    private readonly int[] _duration;
    private readonly byte[] _looping;
    private readonly int[] _durationMask;
    private readonly int[] _stepGlobal;
    private readonly int[] _stepWindowStart;
    private readonly int[] _stepWindowEnd;
    private readonly int[] _stepTimeline;
    private readonly int[] _stepSorted;
    private readonly int[] _kindSorted;
    private readonly int[] _kindStepStart;
    private readonly int[] _kindStepEnd;
    private readonly int[] _timelineStepCount;
    private readonly int[] _timelineFlatBase;
    private readonly int[] _flatRank;
    private readonly TickFlat[] _tickFlat;
    private LaneDirection _planesDir;
    private bool _planesFresh;
    // Playback storage stays managed (rowCapacity-sized at ctor; see
    // EnsurePlaybackCapacity). Visit/row-major work buffers are native
    // (off-heap): the first Tick builds them at exact size instead of
    // doubling managed arrays from zero inside the setup window, so a bulk
    // setup allocates ~0 large-object bytes on the visit path.
    private int[] _pbRow;
    private ushort[] _pbTimeline;
    private ushort[] _pbPhase;
    private int[] _countByRow = new int[0];
    private int[] _rowToActive = new int[0];
    private int* _rowStart;
    private int _rowStartCap;
    private int* _visitCursor;
    private int _visitCursorCap;
    private int* _visitFlat;
    private int _visitFlatCap;
    private ushort* _visitPhaseMod;
    private int _visitPhaseModCap;
    private int* _rowFlat;
    private int _rowFlatCap;
    private int _singleFlatWord = -1;
    private int _visitCount;
    private readonly int[] _timelineDelta;
    private int _tickVisits;
    private LaneDirection _tickLaneDir;
    private readonly Action<int, int> _applyChunkAction;
    private object?[] _eventHolders;
    private int _frame;
    private int _invalidPending;
    private int _highWater;
    private int _playbackCount;
    private bool _pbOrderSorted = true;
    private TickReport _report;
    private bool _sortedDirty = true;
    private int* _activeRows;
    private int _activeRowsCap;
    private int _activeRowCount;
    internal int _workers;

    public World(Library library, int rowCapacity)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        if (rowCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(rowCapacity));
        _rowCapacity = rowCapacity;
        _acc = new float[rowCapacity];
        // Single accumulator buffer: the reader (Accumulator/StateHash/ColumnSpan)
        // observes _acc in place, so the hot path writes once, never a copy pass.
        _applied = _acc;
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
        var looping = new byte[timelineCount];
        for (var i = 0; i < timelineCount; i++)
            looping[i] = (byte)(_library.Loops(i) ? 1 : 0);
        _looping = looping;
        var durationMask = new int[timelineCount];
        for (var i = 0; i < timelineCount; i++)
        {
            var d = duration[i];
            durationMask[i] = (d > 0 && (d & (d - 1)) == 0) ? d - 1 : -1;
        }
        _durationMask = durationMask;
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
        var flatRank = new int[totalSteps];
        for (var r = 0; r < totalSteps; r++) flatRank[sorted[r]] = r;
        _flatRank = flatRank;
        _timelineStepCount = stepCount;
        _timelineFlatBase = stepFlatBase;
        _timelineDelta = new int[timelineCount];
        var tickFlat = new TickFlat[totalSteps];
        for (var f = 0; f < totalSteps; f++)
        {
            tickFlat[f].Dur = _duration[_stepTimeline[f] - 1];
            tickFlat[f].WinEnd = _stepWindowEnd[f];
            tickFlat[f].WinStart = _stepWindowStart[f];
        }
        _tickFlat = tickFlat;
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
        _countByRow = new int[rowCapacity];
        _rowToActive = new int[rowCapacity];
        // Visit work buffers start small and grow exact-fit (single realloc)
        // on first build; every slot is written before it is read.
        _activeRows = AllocInts(64);
        _activeRowsCap = 64;
        _visitFlat = AllocInts(64);
        _visitFlatCap = 64;
        _visitPhaseMod = AllocUshorts(64);
        _visitPhaseModCap = 64;
        _rowStart = AllocInts(64);
        _rowStartCap = 64;
        _visitCursor = AllocInts(64);
        _visitCursorCap = 64;
        _rowFlat = AllocInts(64);
        _rowFlatCap = 64;
        InitSystems(timelineCount);
        _eventHolders = new object?[4];
        _report = new TickReport(0, 0, 0, 0);
        _applyChunkAction = ApplyChunk;
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
            CopySoloToApplied();
            _report = new TickReport(invalid, _highWater, n + _soloCount, _frame);
            return;
        }
        if (_sortedDirty)
        {
            BuildRowMajor(n);
            _sortedDirty = false;
        }
        var laneDir = dir < 0 ? LaneDirection.Backward : LaneDirection.Forward;
        PrepareTick(laneDir, dir);
        _tickLaneDir = laneDir;
        var effectiveWorkers = _workers <= 0 ? Environment.ProcessorCount : _workers;
        if (effectiveWorkers > 1 && _visitCount >= ParallelThresholdRows && _activeRowCount >= ParallelThresholdRows)
        {
            _tickVisits = 0;
            ParallelApply.ForEachChunk(_activeRowCount, effectiveWorkers, _applyChunkAction);
        }
        else
        {
            _tickVisits = 0;
            ApplyChunk(0, _activeRowCount);
        }
        var totalVisits = _tickVisits;
        if (_soloCount != 0)
        {
            totalVisits += StepSolo(dir);
            CopySoloToApplied();
        }
        if (totalVisits > _highWater) _highWater = totalVisits;
        _report = new TickReport(invalid, _highWater, n + _soloCount, _frame);
    }

    ~World() => ReleaseNative();

    public void Dispose()
    {
        ReleaseNative();
        GC.SuppressFinalize(this);
    }

    private void ReleaseNative()
    {
        if (_activeRows != null) { NativeMemory.AlignedFree(_activeRows); _activeRows = null; }
        if (_visitFlat != null) { NativeMemory.AlignedFree(_visitFlat); _visitFlat = null; }
        if (_visitPhaseMod != null) { NativeMemory.AlignedFree(_visitPhaseMod); _visitPhaseMod = null; }
        if (_rowStart != null) { NativeMemory.AlignedFree(_rowStart); _rowStart = null; }
        if (_visitCursor != null) { NativeMemory.AlignedFree(_visitCursor); _visitCursor = null; }
        if (_rowFlat != null) { NativeMemory.AlignedFree(_rowFlat); _rowFlat = null; }
    }

    private static int* AllocInts(int count) => (int*)NativeMemory.AlignedAlloc((nuint)count * 4u, 8u);

    private static ushort* AllocUshorts(int count) => (ushort*)NativeMemory.AlignedAlloc((nuint)count * 2u, 8u);

    // Exact-fit native growth (single realloc + memcpy, no zeroing): sized
    // once on first build, unlike the managed doubling series it replaces.
    private static void EnsureNative<T>(ref T* buffer, ref int capacity, int needed) where T : unmanaged
    {
        if ((uint)capacity >= (uint)needed) return;
        var grown = (T*)NativeMemory.AlignedAlloc((nuint)needed * (nuint)sizeof(T), 8u);
        if (capacity > 0)
            Buffer.MemoryCopy(buffer, grown, (nuint)capacity * (nuint)sizeof(T), (nuint)capacity * (nuint)sizeof(T));
        NativeMemory.AlignedFree(buffer);
        buffer = grown;
        capacity = needed;
    }

    private void BuildRowMajor(int n)
    {
        _visitCount = 0;
        if (n == 0)
        {
            _activeRowCount = 0;
            EnsureNative(ref _rowStart, ref _rowStartCap, 1);
            _rowStart[0] = 0;
            return;
        }
        // Sortedness of _pbRow is maintained incrementally (appends that
        // would break it and swap-removals clear _pbOrderSorted), so the
        // row-major build does not need a full scan to prove it.
        if (_pbOrderSorted)
        {
            if (_activeRowsCap < n) EnsureNative(ref _activeRows, ref _activeRowsCap, n);
            var rowCap = n < _rowCapacity ? n : _rowCapacity;
            EnsureNative(ref _rowStart, ref _rowStartCap, rowCap + 1);
            // Fused count+prefix+fill. Playback order is visit order here, so
            // visits are appended in one pass while per-row segment
            // boundaries are recorded; no separate _countByRow/_visitCursor/
            // _rowToActive pass is needed. Distinct rows are deduped exactly
            // as in the three-pass path (including rows with zero visits),
            // and visits land in the same order the fill pass produces.
            void EnsureVisitCapacity(int needed)
            {
                if (needed <= _visitFlatCap) return;
                var growTo = _visitFlatCap < 64 ? 64 : _visitFlatCap * 2;
                while (growTo < needed) growTo *= 2;
                EnsureNative(ref _visitFlat, ref _visitFlatCap, growTo);
                EnsureNative(ref _visitPhaseMod, ref _visitPhaseModCap, growTo);
            }
            var cursor = 0;
            var m = 0;
            var prevRow = 0;
            var stepCount = _timelineStepCount;
            var durationArr = _duration;
            var durationMaskArr = _durationMask;
            var flatBaseArr = _timelineFlatBase;
            for (var i = 0; i < n; i++)
            {
                var row = _pbRow[i];
                if (m == 0 || row != prevRow)
                {
                    _activeRows[m] = row;
                    _rowStart[m] = cursor;
                    m++;
                    prevRow = row;
                }
                var tlIdx = _pbTimeline[i] - 1;
                if (tlIdx < 0 || tlIdx >= stepCount.Length) continue;
                var steps = stepCount[tlIdx];
                if (steps == 0) continue;
                var duration = durationArr[tlIdx];
                var mask = durationMaskArr[tlIdx];
                var phase = _pbPhase[i];
                var phaseMod = duration > 0 ? (mask >= 0 ? (ushort)(phase & mask) : (ushort)(phase % duration)) : (ushort)0;
                var flatBase = flatBaseArr[tlIdx];
                var needed = cursor + steps;
                if (needed > _visitFlatCap) EnsureVisitCapacity(needed);
                for (var s = 0; s < steps; s++)
                {
                    _visitFlat[cursor] = flatBase + s;
                    _visitPhaseMod[cursor] = phaseMod;
                    cursor++;
                }
            }
            _activeRowCount = m;
            _rowStart[m] = cursor;
            _visitCount = cursor;
        }
        else
        {
            BuildActiveRows(n);
            if (_countByRow.Length < _rowCapacity) Array.Resize(ref _countByRow, _rowCapacity);
            Array.Clear(_countByRow, 0, _rowCapacity);
            for (var i = 0; i < n; i++)
            {
                var row = _pbRow[i];
                var tlIdx = _pbTimeline[i] - 1;
                var steps = (tlIdx >= 0 && tlIdx < _timelineStepCount.Length) ? _timelineStepCount[tlIdx] : 0;
                _countByRow[row] += steps;
            }
            if (_activeRowCount == 0)
            {
                EnsureNative(ref _rowStart, ref _rowStartCap, 1);
                _rowStart[0] = 0;
                return;
            }
            EnsureNative(ref _rowStart, ref _rowStartCap, _activeRowCount + 1);
            EnsureNative(ref _visitCursor, ref _visitCursorCap, _activeRowCount + 1);
            if (_rowToActive.Length < _rowCapacity) Array.Resize(ref _rowToActive, _rowCapacity);
            var total = 0;
            for (var ai = 0; ai < _activeRowCount; ai++)
            {
                var row = _activeRows[ai];
                _rowToActive[row] = ai;
                _rowStart[ai] = total;
                total += _countByRow[row];
            }
            _rowStart[_activeRowCount] = total;
            _visitCount = total;
            EnsureNative(ref _visitFlat, ref _visitFlatCap, total);
            EnsureNative(ref _visitPhaseMod, ref _visitPhaseModCap, total);
            Buffer.MemoryCopy(_rowStart, _visitCursor, (nuint)_activeRowCount * 4u, (nuint)_activeRowCount * 4u);
            for (var i = 0; i < n; i++)
            {
                var row = _pbRow[i];
                var ai = _rowToActive[row];
                var tlIdx = _pbTimeline[i] - 1;
                if (tlIdx < 0 || tlIdx >= _timelineStepCount.Length) continue;
                var steps = _timelineStepCount[tlIdx];
                if (steps == 0) continue;
                var duration = _duration[tlIdx];
                var phase = _pbPhase[i];
                var mask = _durationMask[tlIdx];
                var phaseMod = duration > 0 ? (mask >= 0 ? (ushort)(phase & mask) : (ushort)(phase % duration)) : (ushort)0;
                var flatBase = _timelineFlatBase[tlIdx];
                var cursor = _visitCursor[ai];
                for (var s = 0; s < steps; s++)
                {
                    _visitFlat[cursor] = flatBase + s;
                    _visitPhaseMod[cursor] = phaseMod;
                    cursor++;
                }
                _visitCursor[ai] = cursor;
            }
        }
        EnsureNative(ref _rowFlat, ref _rowFlatCap, _activeRowCount);
        // Per-row: rank-sort visits, classify the row word, and track the
        // whole-tick single-flat candidate in one pass. After rank-sorting,
        // all visits in a row are equal iff the first and last match
        // (flatRank is injective), and the single-flat word can be tracked
        // as rows are classified (empty rows never touch it).
        var visitFlat = _visitFlat;
        var visitPhaseMod = _visitPhaseMod;
        var flatRank = _flatRank;
        var rowFlat = _rowFlat;
        var single = -1;
        var ok = 1;
        for (var ai = 0; ai < _activeRowCount; ai++)
        {
            var start = _rowStart[ai];
            var end = _rowStart[ai + 1];
            var length = end - start;
            if (length <= 0)
            {
                rowFlat[ai] = -1; // empty rows take the generic path (no visits run)
                continue;
            }
            if (length > 1)
            {
                for (var i = 1; i < length; i++)
                {
                    var idx = start + i;
                    var flat = visitFlat[idx];
                    var rank = flatRank[flat];
                    var phase = visitPhaseMod[idx];
                    var j = i - 1;
                    while (j >= 0 && flatRank[visitFlat[start + j]] > rank)
                    {
                        visitFlat[start + j + 1] = visitFlat[start + j];
                        visitPhaseMod[start + j + 1] = visitPhaseMod[start + j];
                        j--;
                    }
                    visitFlat[start + j + 1] = flat;
                    visitPhaseMod[start + j + 1] = phase;
                }
            }
            var firstFlat = visitFlat[start];
            if (visitFlat[end - 1] != firstFlat)
            {
                rowFlat[ai] = ~firstFlat;
                ok = 0;
            }
            else
            {
                rowFlat[ai] = firstFlat;
                if (ok != 0)
                {
                    if (single < 0) single = firstFlat;
                    else if (firstFlat != single) ok = 0;
                }
            }
        }
        _singleFlatWord = ok != 0 && single >= 0 ? single : -1;
    }

    // Prepare is O(timelines + steps), independent of playback count: the
    // per-timeline delta and per-step plane pointers are refreshed here, and
    // ApplyRowRange only streams the cached visit data. Plane pointers are
    // direction-stable, so they are refreshed only when direction changes.
    private void PrepareTick(LaneDirection laneDir, int dir)
    {
        for (var t = 0; t < _timelineDelta.Length; t++)
        {
            var dur = _duration[t];
            if (dur <= 0) { _timelineDelta[t] = 0; continue; }
            var frameMod = _frame % dur;
            _timelineDelta[t] = dir < 0 ? -frameMod : frameMod;
        }
        var tickFlat = _tickFlat;
        var stepTimeline = _stepTimeline;
        var timelineDelta = _timelineDelta;
        for (var flat = 0; flat < tickFlat.Length; flat++)
            tickFlat[flat].Delta = timelineDelta[stepTimeline[flat] - 1];
        if (_planesFresh && laneDir == _planesDir) return;
        _planesFresh = true;
        _planesDir = laneDir;
        var stepGlobal = _stepGlobal;
        for (var flat = 0; flat < stepGlobal.Length; flat++)
        {
            var plane = laneDir == LaneDirection.Backward ? _library.BackwardPlane(stepGlobal[flat]) : _library.ForwardPlane(stepGlobal[flat]);
            tickFlat[flat].Len = plane.Length;
            tickFlat[flat].Plane = plane.Length == 0 ? null : (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(plane));
        }
    }

    // Row-ordered gather: visits are streamed per active row, each row's
    // accumulator is loaded/stored once, and the per-row float-add order is
    // preserved exactly. Uniform-flat rows (all visits share one step) hoist
    // the per-step tick data out of the inner loop; mixed rows use the
    // generic per-visit lookup. Raw pointers remove array bounds checks.
    // The rowFlat word packs the uniform flag: >=0 is a uniform flat,
    // negative is a mixed row whose generic flat is ~word.
    private int ApplyRowRange(int chunkStart, int chunkEnd, LaneDirection laneDir)
    {
        var visits = 0;
        var accP = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_acc));
        var rowsP = _activeRows;
        var startP = _rowStart;
        var flatP = _visitFlat;
        var phaseP = _visitPhaseMod;
        var rowWordP = _rowFlat;
        var tickFlatP = (TickFlat*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_tickFlat));
        var globalP = (int*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_stepGlobal));
        var start = chunkStart < chunkEnd ? startP[chunkStart] : 0;
        for (var ai = chunkStart; ai < chunkEnd; ai++)
        {
            var row = rowsP[ai];
            var end = startP[ai + 1];
            var accVal = accP[row];
            var word = rowWordP[ai];
            if (word >= 0 && start < end)
            {
                ref var tf = ref tickFlatP[word];
                var dur = tf.Dur;
                var delta = tf.Delta;
                var ws = tf.WinStart;
                var winLen = tf.WinEnd - ws;
                var plane = tf.Plane;
                var len = tf.Len;
                for (var v = start; v < end; v++)
                {
                    // tick in (-dur, 2*dur); two branchless adjusts
                    var t = phaseP[v] + delta;
                    t += t < 0 ? dur : 0;
                    t -= t >= dur ? dur : 0;
                    if ((uint)(t - ws) >= (uint)winLen) continue;
                    visits++;
                    float value;
                    if (len == 0) value = _library.LaneValue(globalP[word], laneDir, t);
                    else if (len == 1) value = plane[0];
                    else value = plane[t - ws];
                    accVal += value;
                }
            }
            else
            {
                // Mixed rows: hoist per consecutive equal-flat run within the
                // row segment (visits are flatRank-sorted, so equal flats are
                // contiguous). Bit order per row is preserved.
                var v = start;
                while (v < end)
                {
                    var flat = flatP[v];
                    var runEnd = v + 1;
                    while (runEnd < end && flatP[runEnd] == flat) runEnd++;
                    ref var tfref = ref tickFlatP[flat];
                    var dur = tfref.Dur;
                    if (dur > 0)
                    {
                        var delta = tfref.Delta;
                        var ws = tfref.WinStart;
                        var winLen = tfref.WinEnd - ws;
                        var plane = tfref.Plane;
                        var len = tfref.Len;
                        var global = globalP[flat];
                        for (; v < runEnd; v++)
                        {
                            var t = phaseP[v] + delta;
                            t += t < 0 ? dur : 0;
                            t -= t >= dur ? dur : 0;
                            if ((uint)(t - ws) >= (uint)winLen) continue;
                            visits++;
                            float value;
                            if (len == 0) value = _library.LaneValue(global, laneDir, t);
                            else if (len == 1) value = plane[0];
                            else value = plane[t - ws];
                            accVal += value;
                        }
                    }
                    else
                    {
                        v = runEnd;
                    }
                }
            }
            accP[row] = accVal;
            start = end;
        }
        return visits;
    }

    private int ApplyRowRangeHoisted(int chunkStart, int chunkEnd, LaneDirection laneDir, int singleFlat)
    {
        var visits = 0;
        var accP = (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_acc));
        var rowsP = _activeRows;
        var startP = _rowStart;
        var phaseP = _visitPhaseMod;
        ref var tf = ref _tickFlat[singleFlat];
        var dur = tf.Dur;
        var delta = tf.Delta;
        var ws = tf.WinStart;
        var winLen = tf.WinEnd - ws;
        var plane = tf.Plane;
        var len = tf.Len;
        var global = _stepGlobal[singleFlat];
        var start = chunkStart < chunkEnd ? startP[chunkStart] : 0;
        for (var ai = chunkStart; ai < chunkEnd; ai++)
        {
            var row = rowsP[ai];
            var end = startP[ai + 1];
            var accVal = accP[row];
            for (var v = start; v < end; v++)
            {
                // tick in (-dur, 2*dur); two branchless adjusts
                var t = phaseP[v] + delta;
                t += t < 0 ? dur : 0;
                t -= t >= dur ? dur : 0;
                if ((uint)(t - ws) >= (uint)winLen) continue;
                if (dur <= 0) continue;
                visits++;
                float value;
                if (len == 0) value = _library.LaneValue(global, laneDir, t);
                else if (len == 1) value = plane[0];
                else value = plane[t - ws];
                accVal += value;
            }
            accP[row] = accVal;
            start = end;
        }
        return visits;
    }

    private void ApplyChunk(int chunkStart, int chunkEnd)
    {
        var single = _singleFlatWord;
        var c = single >= 0
            ? ApplyRowRangeHoisted(chunkStart, chunkEnd, _tickLaneDir, single)
            : ApplyRowRange(chunkStart, chunkEnd, _tickLaneDir);
        Interlocked.Add(ref _tickVisits, c);
    }

    private void BuildActiveRows(int n)
    {
        if (_activeRowsCap < n) EnsureNative(ref _activeRows, ref _activeRowsCap, n);
        if (n == 0)
        {
            _activeRowCount = 0;
            return;
        }
        new ReadOnlySpan<int>(_pbRow, 0, n).CopyTo(new Span<int>(_activeRows, n));
        // Nearly-sorted input (single swap-remove churn) is repaired by
        // insertion sort with a shift budget; random order falls back to
        // Array.Sort. Both produce the identical sorted+deduped sequence.
        var descents = 0;
        for (var i = 1; i < n; i++)
        {
            if (_activeRows[i] < _activeRows[i - 1]) descents++;
        }
        if (descents != 0)
        {
            var sorted = false;
            if (descents * 2 <= n)
            {
                long shifts = 0;
                for (var i = 1; i < n && shifts <= 4L * n; i++)
                {
                    var key = _activeRows[i];
                    var j = i - 1;
                    while (j >= 0 && _activeRows[j] > key)
                    {
                        _activeRows[j + 1] = _activeRows[j];
                        j--;
                        shifts++;
                    }
                    _activeRows[j + 1] = key;
                }
                sorted = shifts <= 4L * n;
            }
            if (!sorted)
            {
                // Span.Sort is not available on the netstandard2.1 target, so
                // the rare unsorted fallback sorts through a managed temp.
                var tmp = new int[n];
                new Span<int>(_activeRows, n).CopyTo(tmp);
                Array.Sort(tmp, 0, n);
                tmp.AsSpan().CopyTo(new Span<int>(_activeRows, n));
            }
        }
        var m = 0;
        for (var i = 0; i < n; i++)
        {
            var row = _activeRows[i];
            if (m > 0 && _activeRows[m - 1] == row) continue;
            _activeRows[m++] = row;
        }
        _activeRowCount = m;
    }

    private void CopySoloToApplied()
    {
        var soloCount = _soloCount;
        for (var i = 0; i < soloCount; i++)
        {
            var row = _soloRow[i];
            _applied[row] = _acc[row];
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
    }
}
