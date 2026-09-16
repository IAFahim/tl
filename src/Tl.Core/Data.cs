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

[StructLayout(LayoutKind.Sequential)]
struct FrameSlot<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	public TTrack Track;
	public TClip First, Second;
	public uint WindowStart, WindowEnd, FactorStart, FactorSpan;
	public byte TrackIndex;

	internal static unsafe Frame<TTrack, TClip> ToFrame(FrameSlot<TTrack, TClip>* slot, ushort tick, FrameFlags flags, TClip* scratch)
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
		return new Frame<TTrack, TClip>(in slot->Track, in *scratch, tick, (ushort)(slot->WindowEnd - slot->WindowStart), (ushort)(tick - slot->WindowStart), slot->TrackIndex, flags);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool Advance(bool reverse, ushort pos, out ushort np, out ushort t, out FrameFlags f)
	{
		np = pos; t = 0; f = FrameFlags.None;
		return _p != null && TimelineMovement.Advance((ushort)Header->Duration, Header->Loops != 0, reverse, pos, out np, out t, out f);
	}

	internal bool Select(bool reverse, ushort position, out TimelineState next, out ushort tick, out FrameFlags flags)
	{
		var ok = Advance(reverse, position, out var np, out tick, out flags);
		next = new TimelineState(1, np);
		return ok;
	}
	internal int StageCount => _p == null ? 0 : (int)Header->StageCount;
	internal NativeStage* Stages => (NativeStage*)(_p + Header->StageOffset);
	internal NativeStage* StageOf(ushort tick)
	{
		var count = (int)Header->StageCount;
		var stages = (NativeStage*)(_p + Header->StageOffset);
		if (count <= 4)
		{
			for (var i = 0; i < count; i++) if (tick < stages[i].End) return stages + i;
			return null;
		}
		for (int l = 0, h = count - 1; l <= h;)
		{
			var m = (l + h) >> 1;
			if (tick < stages[m].End) { if (m == 0 || tick >= stages[m - 1].End) return stages + m; h = m - 1; }
			else l = m + 1;
		}
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
	internal void Execute(bool reverse, ushort tick, FrameFlags flags, int row, Span<int> chains, void** columns)
	{
		var stage = Header->StageCount == 1 ? (NativeStage*)(_p + Header->StageOffset) : StageOf(tick);
		if (stage == null) return;
		var steps = (NativeStep*)(_p + stage->ProgramOffset);
		var consumers = PairTable.ConsumerAt;
		var count = (int)stage->ProgramCount;
		var step = reverse ? steps + count - 1 : steps;
		var stride = reverse ? -1 : 1;
		int* rev = stackalloc int[64];
		while (count-- > 0)
		{
			var slot = _p + step->Slot;
			var head = chains[(int)step->Pair];
			if (reverse && head >= 0 && consumers[head].Next >= 0)
			{
				var n = 0;
				for (var e = head; e >= 0; e = consumers[e].Next)
				{
					if (n == 64) throw new InvalidOperationException("Consumer capacity exhausted.");
					rev[n++] = e;
				}
				while (n-- > 0)
				{
					var e = rev[n];
					consumers[e].Execute(slot, tick, flags, columns + consumers[e].Offset, row);
				}
			}
			else
			{
				for (var entry = head; entry >= 0; entry = consumers[entry].Next)
					consumers[entry].Execute(slot, tick, flags, columns + consumers[entry].Offset, row);
			}
			step += stride;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal void ExecuteWindow(bool reverse, ushort tick, FrameFlags flags, int row, Span<int> chains, void** columns, byte* stepCached, int* stepCacheBase, float* cacheValues)
	{
		var stage = Header->StageCount == 1 ? (NativeStage*)(_p + Header->StageOffset) : StageOf(tick);
		if (stage == null) return;
		var steps = (NativeStep*)(_p + stage->ProgramOffset);
		var consumers = PairTable.ConsumerAt;
		var count = (int)stage->ProgramCount;
		var step = reverse ? steps + count - 1 : steps;
		var stride = reverse ? -1 : 1;
		int* rev = stackalloc int[64];
		while (count-- > 0)
		{
			var index = (int)(step - steps);
			var slot = _p + step->Slot;
			var head = chains[(int)step->Pair];
			if (stepCached[index] != 0)
			{
				var cache = stepCacheBase[index];
				if (reverse && head >= 0 && consumers[head].Next >= 0)
				{
					var n = 0;
					for (var e = head; e >= 0; e = consumers[e].Next) rev[n++] = e;
					while (n-- > 0)
					{
						var target = *(float**)(columns + consumers[rev[n]].Offset);
						if (target != null) *target += cacheValues[cache++];
					}
				}
				else
				{
					for (var entry = head; entry >= 0; entry = consumers[entry].Next)
					{
						var target = *(float**)(columns + consumers[entry].Offset);
						if (target != null) *target += cacheValues[cache++];
					}
				}
			}
			else if (reverse && head >= 0 && consumers[head].Next >= 0)
			{
				var n = 0;
				for (var e = head; e >= 0; e = consumers[e].Next)
				{
					if (n == 64) throw new InvalidOperationException("Consumer capacity exhausted.");
					rev[n++] = e;
				}
				while (n-- > 0)
				{
					var e = rev[n];
					consumers[e].Execute(slot, tick, flags, columns + consumers[e].Offset, row);
				}
			}
			else
			{
				for (var entry = head; entry >= 0; entry = consumers[entry].Next)
					consumers[entry].Execute(slot, tick, flags, columns + consumers[entry].Offset, row);
			}
			step += stride;
		}
	}

	static void Validate(ReadOnlySpan<byte> baked)
	{
		void Fail(string message) => throw new ArgumentException(message);
		if (baked.Length < 48) Fail("TLB1 truncated.");
		var h = MemoryMarshal.Read<NativeHeader>(baked);
		if (h.Magic != 0x31424C54 || h.Version != 1) Fail("TLB1 magic or version invalid.");
		if (h.Duration > ushort.MaxValue) Fail("TLB1 duration exceeds the 65,535-tick position domain.");
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

[StructLayout(LayoutKind.Sequential)]
public struct TimelineComponent(TimelineRef reference)
{
    public TimelineRef Reference = reference;
    public ushort Position;
}

public readonly unsafe struct TickFrame
{
    public readonly void* Slot;
    public readonly ushort TimelineTick;
    public readonly FrameFlags Flags;

    internal TickFrame(void* slot, ushort timelineTick, FrameFlags flags)
    {
        Slot = slot;
        TimelineTick = timelineTick;
        Flags = flags;
    }

    public Frame<TTrack, TClip> ToFrame<TTrack, TClip>(ref TClip scratch) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
        => FrameSlot<TTrack, TClip>.ToFrame((FrameSlot<TTrack, TClip>*)Slot, TimelineTick, Flags, (TClip*)Unsafe.AsPointer(ref scratch));

    public static Frame<TTrack, TClip> ToFrame<TTrack, TClip>(void* slot, ushort tick, FrameFlags flags, ref TClip scratch) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
        => FrameSlot<TTrack, TClip>.ToFrame((FrameSlot<TTrack, TClip>*)slot, tick, flags, (TClip*)Unsafe.AsPointer(ref scratch));
}

	static unsafe class PairTable
{
	internal struct Consumer { public int Next, Pair, Offset; public delegate*<byte*, ushort, FrameFlags, void**, int, void> Execute; public delegate*<byte*, ushort, FrameFlags, void**, int, int, void> Range; public delegate*<ulong*, int, byte*, void> Bind; public delegate*<byte*, bool> BlendConstant; public byte WindowConstant; }
	struct Slot { public ulong Key; public int Head; }

	const int SlotCount = 1024, PairCapacity = 512, ConsumerCapacity = 1024, MaxPointers = 256;
	static readonly byte* _block = (byte*)NativeMemory.AlignedAlloc((nuint)(16 * SlotCount + sizeof(Consumer) * ConsumerCapacity), 64);
	static volatile int _gate;
	static int _windowConstant;
	static int _pairs, _consumers;

	static PairTable() => Unsafe.InitBlock(_block, 0, 16 * SlotCount);

	static Slot* SlotAt => (Slot*)_block;
	internal static Consumer* ConsumerAt => (Consumer*)(_block + 16 * SlotCount);
	internal static int ConsumerCount => Volatile.Read(ref _consumers);

	internal static void Install(ulong key, delegate*<byte*, ushort, FrameFlags, void**, int, void> e, delegate*<byte*, ushort, FrameFlags, void**, int, int, void> r, delegate*<ulong*, int, byte*, void> b, delegate*<byte*, bool> blendConstant, bool windowConstant)
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
			consumers[_consumers] = new Consumer { Next = slots[slot].Head, Pair = slot, Execute = e, Range = r, Bind = b, BlendConstant = blendConstant, WindowConstant = windowConstant ? (byte)1 : (byte)0, Offset = _consumers * 4 };
			if (windowConstant) Volatile.Write(ref _windowConstant, 1);
			Volatile.Write(ref slots[slot].Head, _consumers);
			Volatile.Write(ref _consumers, _consumers + 1);
		}
		finally
		{
			_gate = 0;
		}
	}

	internal static bool AnyWindowConstant => Volatile.Read(ref _windowConstant) != 0;

	internal static bool ChainWindowConstant(int head, out delegate*<byte*, bool> blendConstant)
	{
		var consumers = ConsumerAt;
		blendConstant = null;
		for (var entry = head; entry >= 0; entry = consumers[entry].Next)
		{
			if (consumers[entry].WindowConstant == 0) return false;
			if (blendConstant == null) blendConstant = consumers[entry].BlendConstant;
		}
		return blendConstant != null;
	}

	internal static int ChainLength(int head)
	{
		var length = 0;
		var consumers = ConsumerAt;
		for (var entry = head; entry >= 0; entry = consumers[entry].Next) length++;
		return length;
	}

	internal static int ChainNext(int entry) => ConsumerAt[entry].Next;

	internal static void ExecuteEntry(int entry, byte* slot, ushort tick, FrameFlags flags, void** columns, int row)
	{
		var consumers = ConsumerAt;
		consumers[entry].Execute(slot, tick, flags, columns + consumers[entry].Offset, row);
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

	internal static void Bind(TimelineRef asset, ulong* keys, int keyCount, byte* indices, byte* rSlots, byte* rCols, ref int rCount, ref ulong boundMask)
	{
		var slots = SlotAt;
		var consumers = ConsumerAt;
		var total = ConsumerCount;
		for (var entry = 0; entry < total; entry++)
			if (asset.Uses(slots[consumers[entry].Pair].Key) && (boundMask & (1ul << entry)) == 0)
			{
				boundMask |= 1ul << entry;
				var offset = consumers[entry].Offset;
				consumers[entry].Bind(keys, keyCount, indices + offset);
				for (var k = 0; k < 4; k++)
				{
					var col = indices[offset + k];
					if (col != 0) { rSlots[rCount] = (byte)(offset + k); rCols[rCount++] = (byte)(col - 1); }
				}
			}
	}
}

public enum TickPurity
{
	PerTick,
	WindowConstant,
}

public static unsafe class PairRuntime<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	public static readonly ulong Key = Keying.Of(typeof(TTrack).AssemblyQualifiedName! + "\0" + typeof(TClip).AssemblyQualifiedName!);

	public static void Consume(delegate*<byte*, ushort, FrameFlags, void**, int, void> execute, delegate*<ulong*, int, byte*, void> bind) => PairTable.Install(Key, execute, null, bind, null, false);

	public static void Consume(delegate*<byte*, ushort, FrameFlags, void**, int, void> execute, delegate*<byte*, ushort, FrameFlags, void**, int, int, void> range, delegate*<ulong*, int, byte*, void> bind) => PairTable.Install(Key, execute, range, bind, null, false);

	public static void Consume(delegate*<byte*, ushort, FrameFlags, void**, int, void> execute, delegate*<ulong*, int, byte*, void> bind, TickPurity purity) => PairTable.Install(Key, execute, null, bind, purity == TickPurity.WindowConstant ? &BlendConstantInWindow : null, purity == TickPurity.WindowConstant);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool BlendConstantInWindow(byte* slot) => ((FrameSlot<TTrack, TClip>*)slot)->FactorSpan <= 1;

}

public static class Timeline
{
    public static FrameQuery<TTrack, TClip> Query<TTrack, TClip>(in TimelineComponent component) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged => new(component);
}

public unsafe ref struct FrameQuery<TTrack, TClip> where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
{
	readonly TimelineRef _block;
	NativeStep* _steps;
	readonly ushort _tick;
	int _count;
	TClip _scratch;
	Frame<TTrack, TClip> _current;

	internal FrameQuery(in TimelineComponent component)
	{
		_block = component.Reference;
		_tick = component.Position;
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
				_current = FrameSlot<TTrack, TClip>.ToFrame((FrameSlot<TTrack, TClip>*)(block + step.Slot), _tick, FrameFlags.None, (TClip*)Unsafe.AsPointer(ref _scratch));
				return true;
			}
		} while (_count > 0);
		return false;
	}

	public Frame<TTrack, TClip> Current => _current;
}
