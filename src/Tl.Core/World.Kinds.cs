using System;
using System.Collections.Generic;

namespace Tl;

public readonly struct KindPulse<T> where T : unmanaged
{
    readonly World _world;
    readonly int _kindId;

    internal KindPulse(World world, int kindId)
    {
        _world = world;
        _kindId = kindId;
    }

    public void To(Row row, T value) => _world.PulseInternal(_kindId, row, value);
}

public readonly struct KindPulse<T, TFold> where T : unmanaged where TFold : struct, IPulseFold<T>
{
    readonly World _world;
    readonly int _kindId;

    internal KindPulse(World world, int kindId)
    {
        _world = world;
        _kindId = kindId;
    }

    public void To(Row row, T value) => _world.PulseFoldedInternal<T, TFold>(_kindId, row, value);
}

public sealed unsafe partial class World
{
    readonly Dictionary<int, object> _kindTables = new Dictionary<int, object>();
    readonly List<int> _kindOrder = new List<int>();
    readonly List<int> _kindOrders = new List<int>();
    readonly List<Action> _kindResets = new List<Action>();
    readonly Dictionary<ushort, MarkerTable> _markerTables = new Dictionary<ushort, MarkerTable>();

    internal struct MarkerTable
    {
        public void* Values;
        public int Stride;
        public int Count;
    }

    public Span<float> ColumnSpan => _applied.AsSpan();

    public void RegisterKind<T>(Kind<T> kind) where T : unmanaged
    {
        if (kind is null) throw new ArgumentNullException(nameof(kind));
        if (_kindTables.ContainsKey(kind.Id)) throw new InvalidOperationException($"Kind {kind.Id} is already registered on this world.");
        var table = new KindTable<T>(kind, _rowCapacity, _rowCapacity);
        _kindTables[kind.Id] = table;
        RegisterKindOrder(kind.Id, kind.Order);
        _kindResets.Insert(_kindOrder.IndexOf(kind.Id), () => table.Reset());
    }

    void RegisterKindOrder(int id, int order)
    {
        var pos = 0;
        while (pos < _kindOrder.Count && (_kindOrders[pos] < order || (_kindOrders[pos] == order && _kindOrder[pos] < id))) pos++;
        _kindOrder.Insert(pos, id);
        _kindOrders.Insert(pos, order);
    }

    public bool TryKindTable<T>(int kindId, out KindTable<T> table) where T : unmanaged
    {
        if (_kindTables.TryGetValue(kindId, out var boxed) && boxed is KindTable<T> typed)
        {
            table = typed;
            return true;
        }
        table = null!;
        return false;
    }

    public KindTable<T> KindTable<T>(int kindId) where T : unmanaged
    {
        if (TryKindTable<T>(kindId, out var table)) return table;
        throw new ArgumentException($"Kind {kindId} of value type {typeof(T).Name} is not registered on this world.", nameof(kindId));
    }

    public void ResetKinds()
    {
        foreach (var reset in _kindResets) reset();
    }

    public KindPulse<T> Pulses<T>(int kindId) where T : unmanaged
    {
        if (!TryKindTable<T>(kindId, out _)) throw new ArgumentException($"Kind {kindId} of value type {typeof(T).Name} is not registered on this world.", nameof(kindId));
        return new KindPulse<T>(this, kindId);
    }

    public KindPulse<T, TFold> Pulses<T, TFold>(int kindId) where T : unmanaged where TFold : struct, IPulseFold<T>
    {
        if (!TryKindTable<T>(kindId, out _)) throw new ArgumentException($"Kind {kindId} of value type {typeof(T).Name} is not registered on this world.", nameof(kindId));
        return new KindPulse<T, TFold>(this, kindId);
    }

    internal void PulseInternal<T>(int kindId, Row row, T value) where T : unmanaged
    {
        var table = KindTable<T>(kindId);
        if (row.Value < 0 || row.Value >= _rowCapacity) throw new ArgumentOutOfRangeException(nameof(row));
        table.Add(row.Value, value);
    }

    internal void PulseFoldedInternal<T, TFold>(int kindId, Row row, T value) where T : unmanaged where TFold : struct, IPulseFold<T>
    {
        var table = KindTable<T>(kindId);
        if (row.Value < 0 || row.Value >= _rowCapacity) throw new ArgumentOutOfRangeException(nameof(row));
        if (!table.IsTouched(row.Value))
        {
            table.Add(row.Value, value);
            return;
        }
        var current = table[row.Value];
        default(TFold).Fold(ref current, in value);
        table.Add(row.Value, value);
    }

    public void Pulse<T>(int kindId, Row row, T value) where T : unmanaged => PulseInternal(kindId, row, value);

    public int FeederCount => _library.FeederCount;

    public Tlib3Feeder FeederAt(int feeder) => _library.Feeder(feeder);

    public int MarkerCount => _library.MarkerCount;

    public Tlib3Marker MarkerAt(int marker) => _library.Marker(marker);

    public void RegisterMarkerTable(TimelineId timeline, void* values, int stride, int count)
    {
        if (timeline.Value == 0) throw new ArgumentOutOfRangeException(nameof(timeline));
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (stride <= 0 || count < 0) throw new ArgumentOutOfRangeException(nameof(stride));
        _markerTables[timeline.Value] = new MarkerTable { Values = values, Stride = stride, Count = count };
    }

    public void ClearMarkerTables() => _markerTables.Clear();

    public bool TryResolveMarker(TimelineId timeline, uint marker, int size, out byte* value)
    {
        value = null;
        if (timeline.Value == 0 || size <= 0) return false;
        if (!_markerTables.TryGetValue(timeline.Value, out var table)) return false;
        if ((ulong)marker >= (ulong)table.Count) return false;
        if (size > table.Stride) return false;
        value = (byte*)table.Values + (nuint)marker * (nuint)table.Stride;
        return true;
    }
}
