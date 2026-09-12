using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tl;

static class Keying
{
	const ulong Seed = 14695981039346656037, Prime = 1099511628211;

	internal static ulong Of(string text) { var hash = Seed; foreach (var c in text) hash = (hash ^ c) * Prime; return hash | 1; }
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
	internal nint Address => (nint)_p;
	internal uint PairCount => _p == null ? 0 : Header->PairCount;

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

	internal bool Uses(ulong key)
	{
		var pairs = Pairs;
		for (var i = 0; i < PairCount; i++) if (pairs[i].Key == key) return true;
		return false;
	}

	internal void Resolve(Span<int> chains)
	{
		var pairs = Pairs;
		for (var i = 0; i < PairCount; i++) chains[i] = PairTable.Head(pairs[i].Key);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal void Execute(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int row, Span<int> chains, void** columns)
	{
		var stage = StageOf(tick);
		if (stage == null) return;
		var steps = (NativeStep*)(_p + stage->ProgramOffset);
		var consumers = PairTable.ConsumerAt;
		var count = (int)stage->ProgramCount;
		for (var i = 0; i < count; i++)
		{
			var index = reverse ? count - 1 - i : i;
			var step = steps[index];
			var slot = _p + step.Slot;
			for (var entry = chains[(int)step.Pair]; entry >= 0; entry = consumers[entry].Next)
				consumers[entry].Execute(slot, gameTick, tick, cycle, flags, columns + consumers[entry].Offset, row);
		}
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

	public void Dispose() { var p = Interlocked.Exchange(ref _p, 0); if (p != 0) NativeMemory.AlignedFree((void*)p); }
}

public struct TimelineComponent(TimelineRef reference)
{
	public TimelineRef Reference = reference;
	public uint Position;
	public long Cycle;
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

	public static Frame<TTrack, TClip> ToFrame<TTrack, TClip>(void* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, ref TClip scratch) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
		=> FrameSlot<TTrack, TClip>.ToFrame((FrameSlot<TTrack, TClip>*)slot, gameTick, tick, cycle, flags, (TClip*)Unsafe.AsPointer(ref scratch));
}

static unsafe class PairTable
{
	internal struct Consumer { public int Next, Pair, Offset; public delegate*<byte*, uint, uint, long, FrameFlags, void**, int, void> Execute; public delegate*<in TimelineQuery, byte*, void> Bind; }
	struct Slot { public ulong Key; public int Head; }

	const int SlotCount = 1024, PairCapacity = 512, ConsumerCapacity = 1024, MaxPointers = 256;
	static readonly byte* _block = (byte*)NativeMemory.AlignedAlloc((nuint)(16 * SlotCount + 32 * ConsumerCapacity), 64);
	static volatile int _pairs, _consumers, _gate;

	static Slot* SlotAt => (Slot*)_block;
	internal static Consumer* ConsumerAt => (Consumer*)(_block + 16 * SlotCount);

	internal static void Install(ulong key, delegate*<byte*, uint, uint, long, FrameFlags, void**, int, void> e, delegate*<in TimelineQuery, byte*, void> b)
	{
		while (Interlocked.CompareExchange(ref _gate, 1, 0) != 0) Thread.Yield();
		try
		{
			var slot = Probe(key);
			var slots = SlotAt;
			if (slots[slot].Key == 0)
			{
				if (_pairs == PairCapacity) throw new InvalidOperationException("Pair capacity exhausted.");
				slots[slot].Head = -1;
				Volatile.Write(ref slots[slot].Key, key);
				_pairs++;
			}
			if (_consumers == ConsumerCapacity || _consumers * 4 + 4 > MaxPointers) throw new InvalidOperationException("Consumer capacity exhausted.");
			var consumers = ConsumerAt;
			consumers[_consumers] = new Consumer { Next = slots[slot].Head, Pair = slot, Execute = e, Bind = b, Offset = _consumers * 4 };
			Volatile.Write(ref slots[slot].Head, _consumers);
			_consumers++;
		}
		finally
		{
			_gate = 0;
		}
	}

	static int Probe(ulong key)
	{
		var slots = SlotAt;
		var slot = (int)key & (SlotCount - 1);
		while (true)
		{
			var candidate = Volatile.Read(ref slots[slot].Key);
			if (candidate == 0 || candidate == key) return slot;
			slot = (slot + 1) & (SlotCount - 1);
		}
	}

	internal static int Head(ulong key)
	{
		var slots = SlotAt;
		var slot = Probe(key);
		return slots[slot].Key == 0 ? -1 : Volatile.Read(ref slots[slot].Head);
	}

	internal static void Bind(TimelineRef asset, in TimelineQuery columns, byte* indices, ref int watermark)
	{
		var slots = SlotAt;
		var consumers = ConsumerAt;
		for (var entry = 0; entry < _consumers; entry++)
			if (asset.Uses(slots[consumers[entry].Pair].Key))
			{
				consumers[entry].Bind(in columns, indices + consumers[entry].Offset);
				if ((entry + 1) * 4 > watermark) watermark = (entry + 1) * 4;
			}
	}
}

public static unsafe class PairRuntime<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	public static readonly ulong Key = Keying.Of(typeof(TTrack).AssemblyQualifiedName! + "\0" + typeof(TClip).AssemblyQualifiedName!);

	public static void Consume(delegate*<byte*, uint, uint, long, FrameFlags, void**, int, void> execute, delegate*<in TimelineQuery, byte*, void> bind) => PairTable.Install(Key, execute, bind);
}

public static class Timeline
{
	public static TimelineQuery Rows(TimelineComponent[] timelines) => new(timelines);

	public static FrameQuery<TTrack, TClip> Query<TTrack, TClip>(in TimelineComponent component) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged => new(component);
}

public ref struct TimelineQuery
{
	ref struct Column { public ulong Key; public ReadOnlySpan<byte> Data; public bool Write; }
	struct PairCache { public nint Asset; public int Count; public int BoundSlots; public unsafe fixed int Heads[ResolvedPairs]; public unsafe fixed ulong Columns[256]; public unsafe fixed byte ColumnIndex[256]; }

	const int ResolvedPairs = 16;

	Span<TimelineComponent> _rows;
	Column _a, _b, _c, _d;
	PairCache _cache;
	bool _bound;
	int _count;

	internal unsafe TimelineQuery(Span<TimelineComponent> rows)
	{
		_rows = rows;
		new Span<byte>(Unsafe.AsPointer(ref _cache.ColumnIndex[0]), 256).Clear();
	}

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

	unsafe Span<int> Resolved => new(Unsafe.AsPointer(ref _cache.Heads[0]), _cache.Count);

	public unsafe Span<T> Span<T>(int index) where T : unmanaged => new(Unsafe.AsPointer(ref MemoryMarshal.GetReference(DataOf(index))), DataOf(index).Length / sizeof(T));

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe void Tick(uint gameTick, int delta = 1)
	{
		if (delta == 0 || _rows.IsEmpty) return;
		var indices = (byte*)Unsafe.AsPointer(ref _cache.ColumnIndex[0]);
		var table = (void**)Unsafe.AsPointer(ref _cache.Columns[0]);
		if (!_bound)
		{
			for (var row = 0; row < _rows.Length; row++)
			{
				var reference = _rows[row].Reference;
				if (reference.Address != 0) PairTable.Bind(reference, in this, indices, ref _cache.BoundSlots);
			}
			_bound = true;
		}
		var p0 = _count > 0 ? Unsafe.AsPointer(ref MemoryMarshal.GetReference(_a.Data)) : null;
		var p1 = _count > 1 ? Unsafe.AsPointer(ref MemoryMarshal.GetReference(_b.Data)) : null;
		var p2 = _count > 2 ? Unsafe.AsPointer(ref MemoryMarshal.GetReference(_c.Data)) : null;
		var p3 = _count > 3 ? Unsafe.AsPointer(ref MemoryMarshal.GetReference(_d.Data)) : null;
		for (var i = 0; i < _cache.BoundSlots; i++)
		{
			var n = _cache.ColumnIndex[i];
			table[i] = n == 1 ? p0 : n == 2 ? p1 : n == 3 ? p2 : n == 4 ? p3 : null;
		}
		var warm = true;
		for (var row = 0; row < _rows.Length; row++)
			if (_rows[row].Reference.Address != _cache.Asset)
			{
				warm = false;
				break;
			}
		nint uniformAddress = 0;
		var pairs = 0;
		var cacheable = false;
		if (!warm)
		{
			uniformAddress = _rows[0].Reference.Address;
			var uniform = true;
			for (var row = 0; row < _rows.Length; row++)
			{
				var reference = _rows[row].Reference;
				if (reference.Address != uniformAddress) uniform = false;
				pairs = Math.Max(pairs, (int)reference.PairCount);
			}
			cacheable = uniform && uniformAddress != 0 && pairs <= ResolvedPairs;
		}
		Span<int> chains = warm ? default : stackalloc int[pairs];
		nint attached = warm ? _cache.Asset : cacheable ? uniformAddress : 0;
		if (warm)
			chains = Resolved;
		else if (cacheable)
		{
			_cache.Asset = uniformAddress;
			_cache.Count = pairs;
			chains = Resolved;
			_rows[0].Reference.Resolve(chains);
		}
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
				var address = reference.Address;
				if (address != attached)
				{
					attached = address;
					reference.Resolve(chains);
					PairTable.Bind(reference, in this, indices, ref _cache.BoundSlots);
					for (var i = 0; i < _cache.BoundSlots; i++)
					{
						var n = _cache.ColumnIndex[i];
						table[i] = n == 1 ? p0 : n == 2 ? p1 : n == 3 ? p2 : n == 4 ? p3 : null;
					}
				}
				reference.Execute(reverse, tick, gameTick, cycle, flags, row, chains, table);
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

	public FrameQuery<TTrack, TClip> GetEnumerator() { var copy = this; return copy; }

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
