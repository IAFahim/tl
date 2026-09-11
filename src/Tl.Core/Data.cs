using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tl;

static class Keying
{
	const ulong Seed = 14695981039346656037, Prime = 1099511628211;

	internal static ulong Of(string text)
	{
		var hash = Seed;
		foreach (var c in text) hash = (hash ^ c) * Prime;
		return hash;
	}
}

public static class TypeKey<T>
{
	public static readonly ulong Value = Keying.Of(typeof(T).AssemblyQualifiedName!);
}

#pragma warning disable CS0649
struct NativeHeader
{
	public uint Magic, Version, Loops, Duration, TrackCount, StageCount, PairCount, PairOffset, StageOffset, FrameOffset, Reserved, Bytes;
}

struct NativePair { public ulong Key; public uint Stride, Reserved; }
struct NativeStage { public uint Start, End, ProgramOffset, ProgramCount; }
struct NativeStep { public uint Slot, Pair; }

struct FrameSlot<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	public TTrack Track;
	public TClip First, Second;
	public uint WindowStart, WindowEnd, FactorStart, FactorSpan;
	public byte TrackIndex;

	internal static unsafe Frame<TTrack, TClip> ToFrame(FrameSlot<TTrack, TClip>* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, TClip* scratch)
	{
		if (slot->FactorSpan == 0) *scratch = slot->First;
		else
		{
			var factor = slot->FactorSpan <= 1 ? 0.5f : (tick - slot->FactorStart) / (float)(slot->FactorSpan - 1);
			slot->Track.Blend(in slot->First, in slot->Second, factor, out var blended);
			*scratch = blended;
		}
		if (tick == slot->WindowStart) flags |= FrameFlags.ClipStart;
		if (tick == slot->WindowEnd - 1) flags |= FrameFlags.ClipEnd;
		return new Frame<TTrack, TClip>(in slot->Track, in *scratch, gameTick, tick, cycle, slot->TrackIndex, flags);
	}
}
public readonly unsafe struct TimelineRef
{
	internal readonly byte* _p;
	internal TimelineRef(void* p) => _p = (byte*)p;

	NativeHeader* Header => (NativeHeader*)_p;
	internal NativePair* Pairs => (NativePair*)(_p + Header->PairOffset);

	internal static byte* Load(ReadOnlySpan<byte> baked)
	{
		Validate(baked);
		var block = (byte*)NativeMemory.AlignedAlloc((nuint)((baked.Length + 63) & ~63), 64);
		baked.CopyTo(new(block, baked.Length));
		return block;
	}

	internal bool Select(bool reverse, uint position, long cycle, out TimelineState next, out uint tick, out long fc, out FrameFlags flags)
	{
		next = default;
		tick = 0;
		fc = 0;
		flags = FrameFlags.None;
		return _p != null && TimelineMovement.Select(new TimelineState(1, position, cycle), Header->Duration, Header->Loops != 0, reverse, out next, out tick, out fc, out flags);
	}

	internal NativeStage* StageOf(uint tick)
	{
		var stages = (NativeStage*)(_p + Header->StageOffset);
		for (var i = 0; i < Header->StageCount; i++) if (tick < stages[i].End) return stages + i;
		return null;
	}

	internal void Execute(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, in TimelineQuery q)
	{
		var stage = StageOf(tick);
		if (stage == null) return;
		var steps = (NativeStep*)(_p + stage->ProgramOffset);
		var pairs = Pairs;
		for (var i = 0; i < stage->ProgramCount; i++) PairTable.Dispatch(pairs[steps[i].Pair].Key, new TickFrame(_p + steps[i].Slot, gameTick, tick, cycle, flags), row, in q);
	}

	static void Validate(ReadOnlySpan<byte> baked)
	{
		void Fail(string message) => throw new ArgumentException(message);
		if (baked.Length < 48) Fail("TLB1 truncated.");
		var h = MemoryMarshal.Read<NativeHeader>(baked);
		if (h.Magic != 0x31424C54 || h.Version != 1) Fail("TLB1 magic or version invalid.");
		if (h.Bytes != (uint)baked.Length) Fail("TLB1 size mismatch.");
		if (h.PairOffset < 48 || (h.PairOffset | h.StageOffset | h.FrameOffset) % 8 != 0) Fail("TLB1 offsets must be 8-aligned.");
		if ((ulong)h.PairOffset + 16ul * h.PairCount > h.StageOffset || (ulong)h.StageOffset + 16ul * h.StageCount > h.FrameOffset || h.FrameOffset > (ulong)baked.Length) Fail("TLB1 sections out of bounds.");
		var pairs = MemoryMarshal.Cast<byte, NativePair>(baked.Slice((int)h.PairOffset, 16 * (int)h.PairCount));
		for (var i = 1; i < pairs.Length; i++) if (pairs[i - 1].Key >= pairs[i].Key) Fail("TLB1 pair keys must be sorted.");
		var stages = MemoryMarshal.Cast<byte, NativeStage>(baked.Slice((int)h.StageOffset, 16 * (int)h.StageCount));
		var programs = (ulong)h.StageOffset + 16ul * h.StageCount;
		uint edge = 0;
		for (var i = 0; i < stages.Length; i++)
		{
			var stage = stages[i];
			if (stage.Start != edge || stage.ProgramOffset < programs || stage.ProgramOffset % 8 != 0 || (ulong)stage.ProgramOffset + 8ul * stage.ProgramCount > h.FrameOffset) Fail("TLB1 stages must be monotonic.");
			var steps = MemoryMarshal.Cast<byte, NativeStep>(baked.Slice((int)stage.ProgramOffset, 8 * (int)stage.ProgramCount));
			for (var j = 0; j < steps.Length; j++)
			{
				var step = steps[j];
				if (step.Pair >= h.PairCount) Fail("TLB1 step pair out of bounds.");
				if (step.Slot < h.FrameOffset || step.Slot % 16 != 0 || (ulong)step.Slot + pairs[(int)step.Pair].Stride > (ulong)baked.Length) Fail("TLB1 slots must be 16-aligned in bounds.");
			}
			edge = stage.End;
		}
		if (edge != h.Duration) Fail("TLB1 stages must cover duration.");
	}
}

public sealed unsafe class TimelineAsset : IDisposable
{
	nint _p;
	TimelineAsset(nint p) => _p = p;

	public static TimelineAsset Load(ReadOnlySpan<byte> baked) => new((nint)TimelineRef.Load(baked));

	public TimelineRef Reference => new((void*)_p);

	public void Dispose()
	{
		var p = Interlocked.Exchange(ref _p, 0);
		if (p != 0) NativeMemory.AlignedFree((void*)p);
	}
}

public struct TimelineComponent
{
	public TimelineRef Reference;
	public uint Position;
	public long Cycle;

	public TimelineComponent(TimelineRef reference) { Reference = reference; }
}

public readonly unsafe struct TickFrame
{
	public readonly void* Slot;
	public readonly uint GameTick, TimelineTick;
	public readonly long Cycle;
	public readonly FrameFlags Flags;

	internal TickFrame(void* slot, uint gameTick, uint timelineTick, long cycle, FrameFlags flags)
	{
		Slot = slot;
		GameTick = gameTick;
		TimelineTick = timelineTick;
		Cycle = cycle;
		Flags = flags;
	}

	public Frame<TTrack, TClip> ToFrame<TTrack, TClip>(ref TClip scratch) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
		=> FrameSlot<TTrack, TClip>.ToFrame((FrameSlot<TTrack, TClip>*)Slot, GameTick, TimelineTick, Cycle, Flags, (TClip*)Unsafe.AsPointer(ref scratch));
}

static unsafe class PairTable
{
	struct Pair { public ulong Key; public int Head; }
	struct Consumer { public int Next; public delegate*<in TickFrame, in TimelineQuery, int, void> Execute; public delegate*<in TimelineQuery, void> Bind; }

	static readonly Pair[] Pairs = new Pair[512];
	static readonly Consumer[] Consumers = new Consumer[1024];
	static volatile int _pairCount, _consumerCount;

	internal static void Install(ulong key, delegate*<in TickFrame, in TimelineQuery, int, void> e, delegate*<in TimelineQuery, void> b)
	{
		lock (Pairs)
		{
			var index = Search(key);
			if (index < 0)
			{
				if (_pairCount == 512) throw new InvalidOperationException("Pair capacity exhausted.");
				index = ~index;
				for (var i = _pairCount; i > index; i--) Pairs[i] = Pairs[i - 1];
				Pairs[index] = new Pair { Key = key, Head = -1 };
				_pairCount++;
			}
			if (_consumerCount == 1024) throw new InvalidOperationException("Consumer capacity exhausted.");
			Consumers[_consumerCount] = new Consumer { Next = Pairs[index].Head, Execute = e, Bind = b };
			Pairs[index].Head = _consumerCount;
			_consumerCount++;
		}
	}

	static int Search(ulong key)
	{
		var low = 0;
		var high = _pairCount - 1;
		while (low <= high)
		{
			var middle = low + (high - low) / 2;
			var candidate = Pairs[middle].Key;
			if (candidate == key) return middle;
			if (candidate < key) low = middle + 1;
			else high = middle - 1;
		}
		return ~low;
	}

	internal static void Dispatch(ulong key, in TickFrame f, int row, in TimelineQuery q)
	{
		var index = Search(key);
		if (index < 0) return;
		for (var entry = Pairs[index].Head; entry >= 0; entry = Consumers[entry].Next) Consumers[entry].Execute(in f, in q, row);
	}

	internal static void Bind(in TimelineQuery columns)
	{
		var count = _consumerCount;
		for (var entry = 0; entry < count; entry++) Consumers[entry].Bind(in columns);
	}
}

public static unsafe class PairRuntime<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	public static readonly ulong Key = Keying.Of(typeof(TTrack).AssemblyQualifiedName! + "\0" + typeof(TClip).AssemblyQualifiedName!);

	public static void Consume(delegate*<in TickFrame, in TimelineQuery, int, void> execute, delegate*<in TimelineQuery, void> bind) => PairTable.Install(Key, execute, bind);
}

public static class Timeline
{
	public static TimelineQuery Rows(TimelineComponent[] timelines) => new(timelines);

	public static FrameQuery<TTrack, TClip> Query<TTrack, TClip>(in TimelineComponent component) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged => new(component);
}

public ref struct TimelineQuery
{
	ref struct Column { public ulong Key; public ReadOnlySpan<byte> Data; public bool Write; }

	Span<TimelineComponent> _rows;
	Column _a, _b, _c, _d;
	int _count;

	internal TimelineQuery(Span<TimelineComponent> rows) { _rows = rows; }

	ReadOnlySpan<byte> DataOf(int index) => index == 0 ? _a.Data : index == 1 ? _b.Data : index == 2 ? _c.Data : _d.Data;

	public TimelineQuery Read<T>(ReadOnlySpan<T> column) where T : unmanaged => Append<T>(column, false);
	public TimelineQuery Write<T>(Span<T> column) where T : unmanaged => Append<T>(column, true);

	TimelineQuery Append<T>(ReadOnlySpan<T> column, bool write) where T : unmanaged
	{
		if (column.Length != _rows.Length) throw new ArgumentException("Column length must equal row count.");
		var bytes = MemoryMarshal.AsBytes(column);
		var key = TypeKey<T>.Value;
		for (var i = 0; i < _count; i++)
		{
			var other = i == 0 ? _a : i == 1 ? _b : i == 2 ? _c : _d;
			if (other.Key == key) throw new ArgumentException("Duplicate column type is role-ambiguous.");
			if ((write || other.Write) && bytes.Overlaps(other.Data)) throw new ArgumentException("Writable columns must not overlap.");
		}
		if (bytes.Overlaps(MemoryMarshal.AsBytes(_rows))) throw new ArgumentException("Columns must not overlap the rows.");
		if (_count == 4) throw new ArgumentException("At most four columns.");
		var slot = new Column { Key = key, Data = bytes, Write = write };
		if (_count == 0) _a = slot;
		else if (_count == 1) _b = slot;
		else if (_count == 2) _c = slot;
		else _d = slot;
		_count++;
		return this;
	}

	public int Find(ulong key) => _a.Key == key ? 0 : _b.Key == key ? 1 : _c.Key == key ? 2 : _d.Key == key ? 3 : -1;

	public unsafe Span<T> Span<T>(int index) where T : unmanaged => new(Unsafe.AsPointer(ref MemoryMarshal.GetReference(DataOf(index))), DataOf(index).Length / sizeof(T));

	public void Tick(uint gameTick, int delta = 1)
	{
		if (delta == 0 || _rows.IsEmpty) return;
		PairTable.Bind(in this);
		var reverse = delta < 0;
		long remaining = reverse ? -(long)delta : delta;
		var moved = true;
		while (moved && remaining-- != 0)
		{
			if (reverse) gameTick--;
			moved = false;
			for (var row = 0; row < _rows.Length; row++)
			{
				ref var c = ref _rows[row];
				var reference = c.Reference;
				if (!reference.Select(reverse, c.Position, c.Cycle, out _, out var tick, out var cycle, out var flags)) continue;
				moved = true;
				reference.Execute(tick, gameTick, cycle, flags, row, in this);
			}
			if (!moved) break;
			for (var row = 0; row < _rows.Length; row++)
			{
				ref var c = ref _rows[row];
				if (!c.Reference.Select(reverse, c.Position, c.Cycle, out var next, out _, out _, out _)) continue;
				c.Position = next.Position;
				c.Cycle = next.Cycle;
			}
			if (!reverse) gameTick++;
		}
	}
}

public unsafe ref struct FrameQuery<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	readonly TimelineRef _block;
	NativeStep* _steps;
	readonly uint _tick;
	readonly long _cycle;
	int _count;
	TClip _scratch;
	Frame<TTrack, TClip> _current;

	internal FrameQuery(in TimelineComponent component)
	{
		_block = component.Reference;
		_tick = component.Position;
		_cycle = component.Cycle;
		var stage = _block._p == null ? null : _block.StageOf(_tick);
		if (stage != null)
		{
			_steps = (NativeStep*)(_block._p + stage->ProgramOffset);
			_count = (int)stage->ProgramCount;
		}
	}

	public FrameQuery<TTrack, TClip> GetEnumerator()
	{
		var copy = this;
		return copy;
	}

	public bool MoveNext()
	{
		if (_count == 0) return false;
		var pairs = _block.Pairs;
		var block = _block._p;
		do
		{
			var step = *_steps++;
			_count--;
			if (pairs[step.Pair].Key == PairRuntime<TTrack, TClip>.Key)
			{
				_current = FrameSlot<TTrack, TClip>.ToFrame((FrameSlot<TTrack, TClip>*)(block + step.Slot), 0, _tick, _cycle, FrameFlags.None, (TClip*)Unsafe.AsPointer(ref _scratch));
				return true;
			}
		} while (_count > 0);
		return false;
	}

	public Frame<TTrack, TClip> Current => _current;
}
