using System.Buffers.Binary;
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
    public uint Magic, Version, Loops, Duration, TrackCount, StageCount, PairCount, PairOffset, StageOffset, PoolOffset, FrameOffset, HotLength, Bytes;
    public uint Reserved0, Reserved1, Reserved2;
}

[StructLayout(LayoutKind.Sequential)]
struct NativePair
{
    public ulong Key;
    public uint SlotStride;
    public uint TrackPoolOffset;
    public uint TrackPoolCount;
    public uint TrackValueBytes;
    public uint ClipPoolOffset;
    public uint ClipPoolCount;
    public uint ClipValueBytes;
    public ulong Reserved;
}

struct NativeStage { public uint Start, End, ProgramOffset, ProgramCount; }
struct NativeStep { public uint Slot, Pair; }

[StructLayout(LayoutKind.Sequential)]
unsafe struct SlotRow
{
	internal const ushort NoClipValue = 0xFFFF;
	internal const uint RowBytes = 24u;

	public ushort TrackValueIndex;
	public ushort FirstValueIndex;
	public ushort SecondValueIndex;
	public byte TrackIndex;
	public uint WindowStart;
	public uint WindowEnd;
	public uint FactorStart;
	public uint FactorSpan;

	internal static unsafe Frame<TTrack, TClip> ToFrame<TTrack, TClip>(SlotRow* row, byte* pair, ushort tick, FrameFlags flags, TClip* scratch)
		where TTrack : unmanaged, IBlend<TClip>
		where TClip : unmanaged
	{
		var pools = (NativePair*)pair;
		ref var track = ref *(TTrack*)(pair + pools->TrackPoolOffset + row->TrackValueIndex * (nuint)sizeof(TTrack));
		var clipPool = pair + pools->ClipPoolOffset;
		if (row->FactorSpan == 0) *scratch = *(TClip*)(clipPool + row->FirstValueIndex * (nuint)sizeof(TClip));
		else
		{
			var factor = row->FactorSpan <= 1 ? 0.5f : (tick - row->FactorStart) / (float)(row->FactorSpan - 1);
			var first = *(TClip*)(clipPool + row->FirstValueIndex * (nuint)sizeof(TClip));
			var second = *(TClip*)(clipPool + row->SecondValueIndex * (nuint)sizeof(TClip));
			track.Blend(in first, in second, factor, out var blended);
			*scratch = blended;
		}
		if (tick == row->WindowStart) flags |= FrameFlags.ClipStart;
		if (tick == row->WindowEnd - 1) flags |= FrameFlags.ClipEnd;
		return new Frame<TTrack, TClip>(in track, in *scratch, tick, (ushort)(row->WindowEnd - row->WindowStart), (ushort)(tick - row->WindowStart), row->TrackIndex, flags);
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
		=> ExecuteWindow(reverse, tick, flags, row, chains, columns, null, null, null);

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal void ExecuteWindow(bool reverse, ushort tick, FrameFlags flags, int row, Span<int> chains, void** columns, byte* stepCached, int* stepCacheBase, float* cacheValues)
	{
		var stage = Header->StageCount == 1 ? (NativeStage*)(_p + Header->StageOffset) : StageOf(tick);
		if (stage == null) return;
		var steps = (NativeStep*)(_p + stage->ProgramOffset);
		var consumers = PairTable.ConsumerAt;
		var pairs = Pairs;
		var count = (int)stage->ProgramCount;
		var step = reverse ? steps + count - 1 : steps;
		var stride = reverse ? -1 : 1;
		int* rev = stackalloc int[64];
		while (count-- > 0)
		{
			var index = (int)(step - steps);
			var slot = _p + step->Slot;
			var pair = (byte*)(pairs + step->Pair);
			var head = chains[(int)step->Pair];
			if (stepCached != null && stepCached[index] != 0)
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
			else PairTable.RunChain(head, reverse, slot, pair, tick, flags, columns, row, null, null);
			step += stride;
		}
	}

	internal static void Validate(ReadOnlySpan<byte> baked)
	{
		void Fail(string message) => throw new ArgumentException(message);
		if (baked.Length < 64) Fail("TLB truncated.");
		var h = MemoryMarshal.Read<NativeHeader>(baked);
		if (h.Magic != 0x31424C54 || h.Version != 3) Fail("TLB magic or version invalid.");
		if (h.Duration > ushort.MaxValue) Fail("TLB duration exceeds the 65,535-tick position domain.");
		if (h.Bytes != (uint)baked.Length) Fail("TLB size mismatch.");
		if (h.HotLength == 0 || h.HotLength > h.Bytes) Fail("TLB hot length invalid.");
		if (h.PairOffset < 64 || (h.PairOffset | h.StageOffset | h.PoolOffset | h.FrameOffset) % 8 != 0) Fail("TLB offsets must be 8-aligned.");
		if ((ulong)h.PairOffset + 48ul * h.PairCount > h.StageOffset) Fail("TLB pair table out of bounds.");
		if ((ulong)h.StageOffset + 16ul * h.StageCount > h.PoolOffset || h.PoolOffset > h.FrameOffset || h.FrameOffset > h.HotLength) Fail("TLB sections out of bounds.");
		var pairs = MemoryMarshal.Cast<byte, NativePair>(baked.Slice((int)h.PairOffset, 48 * (int)h.PairCount));
		for (var i = 1; i < pairs.Length; i++) if (pairs[i - 1].Key >= pairs[i].Key) Fail("TLB pair keys must be sorted.");
		for (var i = 0; i < pairs.Length; i++)
		{
			var pair = pairs[i];
			if (pair.SlotStride % 8 != 0 || pair.SlotStride < SlotRow.RowBytes) Fail("TLB slot stride must be 8-aligned.");
			ValidatePool(h, (ulong)h.PairOffset + 48ul * (uint)i + pair.TrackPoolOffset, pair.TrackPoolCount, pair.TrackValueBytes, Fail);
			ValidatePool(h, (ulong)h.PairOffset + 48ul * (uint)i + pair.ClipPoolOffset, pair.ClipPoolCount, pair.ClipValueBytes, Fail);
		}
		var stages = MemoryMarshal.Cast<byte, NativeStage>(baked.Slice((int)h.StageOffset, 16 * (int)h.StageCount));
		var programs = (ulong)h.StageOffset + 16ul * h.StageCount;
		uint edge = 0;
		for (var i = 0; i < stages.Length; i++)
		{
			var stage = stages[i];
			if (stage.Start != edge || stage.ProgramOffset < programs || stage.ProgramOffset % 8 != 0 || (ulong)stage.ProgramOffset + 8ul * stage.ProgramCount > h.PoolOffset) Fail("TLB stages must be monotonic.");
			var steps = MemoryMarshal.Cast<byte, NativeStep>(baked.Slice((int)stage.ProgramOffset, 8 * (int)stage.ProgramCount));
			for (var j = 0; j < steps.Length; j++)
			{
				var step = steps[j];
				if (step.Pair >= h.PairCount) Fail("TLB step pair out of bounds.");
				var pair = pairs[(int)step.Pair];
				if (step.Slot < h.FrameOffset || step.Slot % 8 != 0 || (ulong)step.Slot + pair.SlotStride > h.HotLength) Fail("TLB slots must be 8-aligned in bounds.");
				ValidateRow(baked, (int)step.Slot, pair, Fail);
			}
			edge = stage.End;
		}
		if (edge != h.Duration) Fail("TLB stages must cover duration.");
	}

	static void ValidatePool(NativeHeader h, ulong address, uint count, uint valueBytes, Action<string> fail)
	{
		if (address < h.PoolOffset || count > 65535 || address + (ulong)count * valueBytes > h.FrameOffset) fail("TLB value pool out of bounds.");
	}

	static void ValidateRow(ReadOnlySpan<byte> baked, int at, NativePair pair, Action<string> fail)
	{
		var trackValue = BinaryPrimitives.ReadUInt16LittleEndian(baked.Slice(at));
		var first = BinaryPrimitives.ReadUInt16LittleEndian(baked.Slice(at + 2));
		var second = BinaryPrimitives.ReadUInt16LittleEndian(baked.Slice(at + 4));
		var factorSpan = BinaryPrimitives.ReadUInt32LittleEndian(baked.Slice(at + 20));
		if (trackValue >= pair.TrackPoolCount || first >= pair.ClipPoolCount) fail("TLB slot value index out of bounds.");
		if (second != SlotRow.NoClipValue && second >= pair.ClipPoolCount) fail("TLB slot second value index out of bounds.");
		if (factorSpan != 0 && second == SlotRow.NoClipValue) fail("TLB blend window requires a second value index.");
	}
}

public sealed unsafe class TimelineAsset : IDisposable
{
	int _index;
	long _generation;

	TimelineAsset(ushort index, long generation)
	{
		_index = index;
		_generation = generation;
	}

	public static ushort Load(ReadOnlySpan<byte> baked) => TimelineTable.Load(baked);

	public static TimelineAsset Of(ushort index)
	{
		TimelineTable.Pin(index, out var generation);
		return new TimelineAsset(index, generation);
	}

	internal static TimelineAsset LoadAsset(ReadOnlySpan<byte> baked) => Of(Load(baked));

	public ushort Index => Volatile.Read(ref _index) < 0
		? throw new InvalidOperationException("Timeline asset view is disposed; call TimelineAsset.Of on a live index.")
		: (ushort)Volatile.Read(ref _index);

	public TimelineRef Reference => Volatile.Read(ref _index) < 0 ? default : TimelineTable.Reference((ushort)Volatile.Read(ref _index));

	public void Dispose()
	{
		var index = Interlocked.Exchange(ref _index, -1);
		if (index >= 0) TimelineTable.Release((ushort)index, Volatile.Read(ref _generation));
	}
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
    public readonly void* Pair;
    public readonly ushort TimelineTick;
    public readonly FrameFlags Flags;

    internal TickFrame(void* slot, void* pair, ushort timelineTick, FrameFlags flags)
    {
        Slot = slot;
        Pair = pair;
        TimelineTick = timelineTick;
        Flags = flags;
    }

    public Frame<TTrack, TClip> ToFrame<TTrack, TClip>(ref TClip scratch) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
        => SlotRow.ToFrame<TTrack, TClip>((SlotRow*)Slot, (byte*)Pair, TimelineTick, Flags, (TClip*)Unsafe.AsPointer(ref scratch));

    public static Frame<TTrack, TClip> ToFrame<TTrack, TClip>(void* slot, void* pair, ushort tick, FrameFlags flags, ref TClip scratch) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
        => SlotRow.ToFrame<TTrack, TClip>((SlotRow*)slot, (byte*)pair, tick, flags, (TClip*)Unsafe.AsPointer(ref scratch));
}

	static unsafe class PairTable
{
	internal struct Consumer { public int Next, Pair, Offset; public delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> Execute; public delegate*<byte*, byte*, ushort, FrameFlags, void**, int, int, void> Range; public delegate*<ulong*, int, byte*, void> Bind; public delegate*<byte*, bool> BlendConstant; public byte WindowConstant; }
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

	internal static void Install(ulong key, delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> e, delegate*<byte*, byte*, ushort, FrameFlags, void**, int, int, void> r, delegate*<ulong*, int, byte*, void> b, delegate*<byte*, bool> blendConstant, bool windowConstant)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int RunChain(int head, bool reverse, byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row, float* scratch, float* sink)
	{
		var consumers = ConsumerAt;
		var written = 0;
		if (reverse && head >= 0 && consumers[head].Next >= 0)
		{
			int* rev = stackalloc int[64];
			var n = 0;
			for (var e = head; e >= 0; e = consumers[e].Next)
			{
				if (n == 64) throw new InvalidOperationException("Consumer capacity exhausted.");
				rev[n++] = e;
			}
			while (n-- > 0)
			{
				var e = rev[n];
				if (sink != null) *scratch = 0f;
				consumers[e].Execute(slot, pair, tick, flags, columns + consumers[e].Offset, row);
				if (sink != null) sink[written++] = *scratch;
			}
		}
		else
		{
			for (var entry = head; entry >= 0; entry = consumers[entry].Next)
			{
				if (sink != null) *scratch = 0f;
				consumers[entry].Execute(slot, pair, tick, flags, columns + consumers[entry].Offset, row);
				if (sink != null) sink[written++] = *scratch;
			}
		}
		return written;
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

	public static void Consume(delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> execute, delegate*<ulong*, int, byte*, void> bind) => PairTable.Install(Key, execute, null, bind, null, false);

	public static void Consume(delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> execute, delegate*<byte*, byte*, ushort, FrameFlags, void**, int, int, void> range, delegate*<ulong*, int, byte*, void> bind) => PairTable.Install(Key, execute, range, bind, null, false);

	public static void Consume(delegate*<byte*, byte*, ushort, FrameFlags, void**, int, void> execute, delegate*<ulong*, int, byte*, void> bind, TickPurity purity) => PairTable.Install(Key, execute, null, bind, purity == TickPurity.WindowConstant ? &BlendConstantInWindow : null, purity == TickPurity.WindowConstant);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool BlendConstantInWindow(byte* slot) => ((SlotRow*)slot)->FactorSpan <= 1;

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
				var pair = (byte*)(pairs + step.Pair);
				_current = SlotRow.ToFrame<TTrack, TClip>((SlotRow*)(block + step.Slot), pair, _tick, FrameFlags.None, (TClip*)Unsafe.AsPointer(ref _scratch));
				return true;
			}
		} while (_count > 0);
		return false;
	}

	public Frame<TTrack, TClip> Current => _current;
}
