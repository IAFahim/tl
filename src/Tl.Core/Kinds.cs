namespace Tl;

public interface IKind
{
    int Id { get; }
    int Order { get; }
}

public interface IKind<T> : IKind
{
    T Idle { get; }
    bool HasIdle { get; }
    T Combine(T acc, T value);
    void Apply(in T value, ref T column);
}

public abstract class Kind<T> : IKind<T>
{
    public abstract int Id { get; }
    public abstract int Order { get; }
    public abstract T Idle { get; }
    public virtual bool HasIdle => true;
    public abstract T Combine(T acc, T value);
    public abstract void Apply(in T value, ref T column);
}

public sealed class IntSumKind : Kind<int>
{
    public override int Id => 0;
    public override int Order => 0;
    public override int Idle => 0;
    public override int Combine(int acc, int value) => acc + value;
    public override void Apply(in int value, ref int column) => column = value;
}

public sealed class FloatSumKind : Kind<float>
{
    public override int Id => 1;
    public override int Order => 1;
    public override float Idle => 0f;
    public override float Combine(float acc, float value) => acc + value;
    public override void Apply(in float value, ref float column) => column = value;
}

public sealed class KindEntries<T>
{
    readonly int[] _rows;
    readonly T[] _values;
    int _length;

    public KindEntries(int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _rows = new int[capacity];
        _values = new T[capacity];
    }

    public int Length => _length;

    public int Capacity => _rows.Length;

    public void Push(int row, T value)
    {
        if (_length >= _rows.Length) throw new InvalidOperationException("kind entries arena is full");
        _rows[_length] = row;
        _values[_length] = value;
        _length++;
    }

    public void Rewind() => _length = 0;

    public int RowAt(int index) => _rows[index];

    public T ValueAt(int index) => _values[index];
}

public sealed class KindTable<T>
{
    readonly Kind<T> _kind;
    readonly T[] _acc;
    readonly ulong[] _touched;
    readonly KindEntries<T> _entries;
    int _touchedCount;

    public KindTable(Kind<T> kind, int rows, int entryCapacity)
    {
        if (kind is null) throw new ArgumentNullException(nameof(kind));
        if (rows < 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (entryCapacity < 0) throw new ArgumentOutOfRangeException(nameof(entryCapacity));
        _kind = kind;
        Rows = rows;
        _acc = new T[rows];
        _touched = new ulong[(rows + 63) >> 6];
        var idle = kind.Idle;
        for (var i = 0; i < rows; i++) _acc[i] = idle;
        _entries = new KindEntries<T>(entryCapacity);
    }

    public int Rows { get; }

    public Kind<T> Kind => _kind;

    public KindEntries<T> Entries => _entries;

    public int TouchedCount => _touchedCount;

    public T this[int row] => _acc[row];

    public void Add(int row, T value)
    {
        if ((uint)row >= (uint)_acc.Length) throw new ArgumentOutOfRangeException(nameof(row));
        _entries.Push(row, value);
        var word = row >> 6;
        var mask = 1ul << (row & 63);
        if ((_touched[word] & mask) == 0)
        {
            _touched[word] |= mask;
            _touchedCount++;
        }
        _acc[row] = _kind.Combine(_acc[row], value);
    }

    public bool IsTouched(int row) => ((_touched[row >> 6] >> (row & 63)) & 1ul) != 0;

    public void IterateTouched(Action<int, T> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        for (var w = 0; w < _touched.Length; w++)
        {
            var bits = _touched[w];
            while (bits != 0)
            {
                var row = (w << 6) + TrailingZeroCount(bits);
                bits &= bits - 1;
                if (row >= _acc.Length) continue;
                callback(row, _acc[row]);
            }
        }
    }

    public void ApplyAll(Span<T> columns)
    {
        if (columns.Length < _acc.Length) throw new ArgumentException("columns is shorter than rows", nameof(columns));
        var hasIdle = _kind.HasIdle;
        var idle = _kind.Idle;
        for (var row = 0; row < _acc.Length; row++)
        {
            if (((_touched[row >> 6] >> (row & 63)) & 1ul) != 0) _kind.Apply(in _acc[row], ref columns[row]);
            else if (hasIdle) _kind.Apply(in idle, ref columns[row]);
        }
    }

    public void Reset()
    {
        _entries.Rewind();
        Array.Clear(_touched, 0, _touched.Length);
        _touchedCount = 0;
        var idle = _kind.Idle;
        for (var i = 0; i < _acc.Length; i++) _acc[i] = idle;
    }

    static int TrailingZeroCount(ulong v)
    {
        var n = 0;
        if ((v & 0xfffffffful) == 0)
        {
            n += 32;
            v >>= 32;
        }
        if ((v & 0xfffful) == 0)
        {
            n += 16;
            v >>= 16;
        }
        if ((v & 0xfful) == 0)
        {
            n += 8;
            v >>= 8;
        }
        if ((v & 0xful) == 0)
        {
            n += 4;
            v >>= 4;
        }
        if ((v & 0x3ul) == 0)
        {
            n += 2;
            v >>= 2;
        }
        if ((v & 0x1ul) == 0) n += 1;
        return n;
    }
}
