using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Tl;

public readonly unsafe struct TimelineKernelRange
{
	public readonly delegate*<byte*, uint, uint, long, FrameFlags, void**, int, int, void> Pointer;

	internal TimelineKernelRange(delegate*<byte*, uint, uint, long, FrameFlags, void**, int, int, void> pointer) => Pointer = pointer;
}

public static unsafe class TimelineKernels
{
	const int HashCapacity = 256, EntryCapacity = 64;
	static readonly byte* _block = (byte*)NativeMemory.AlignedAlloc(HashCapacity * 8 + EntryCapacity * 8, 64);
	static volatile int _gate;
	static int _count;

	static TimelineKernels() => Unsafe.InitBlock(_block, 0, HashCapacity * 8 + EntryCapacity * 8);

	internal static int Count => Volatile.Read(ref _count) >> 2;
	internal static int FindCalls;

	public static int Bound;

	static ulong* Hashes => (ulong*)_block;
	static delegate*<byte*, int*, void**, TimelineComponent*, int, uint, int, bool>* Entries => (delegate*<byte*, int*, void**, TimelineComponent*, int, uint, int, bool>*)(_block + HashCapacity * 8);

	public static unsafe void Register(ulong hash0, ulong hash1, ulong hash2, ulong hash3, delegate*<byte*, int*, void**, TimelineComponent*, int, uint, int, bool> tick)
	{
		while (Interlocked.CompareExchange(ref _gate, 1, 0) != 0) Thread.Yield();
		try
		{
			if (_count == HashCapacity) throw new InvalidOperationException("Kernel catalog capacity exhausted.");
			TimelineQuery.Find = &Find;
			var hashes = Hashes;
			(hashes[_count], hashes[_count + 1], hashes[_count + 2], hashes[_count + 3]) = (hash0, hash1, hash2, hash3);
			Entries[_count >> 2] = tick;
			Volatile.Write(ref _count, _count + 4);
		}
		finally
		{
			_gate = 0;
		}
	}

	internal static unsafe void* Find(byte* block, uint bytes)
	{
		Interlocked.Increment(ref FindCalls);
		var count = Volatile.Read(ref _count);
		if (count == 0) return default;
		var metadataOffset = bytes >= 48 ? *(uint*)(block + 40) : 0;
		Span<byte> hash = stackalloc byte[32];
		if (metadataOffset < 48 || metadataOffset > bytes)
		{
			SHA256.TryHashData(new ReadOnlySpan<byte>(block, (int)bytes), hash, out _);
		}
		else
		{
			var view = (byte*)NativeMemory.AlignedAlloc((nuint)((metadataOffset + 63) & ~63), 64);
			new ReadOnlySpan<byte>(block, (int)metadataOffset).CopyTo(new Span<byte>(view, (int)metadataOffset));
			*(uint*)(view + 40) = 0;
			*(uint*)(view + 44) = metadataOffset;
			SHA256.TryHashData(new ReadOnlySpan<byte>(view, (int)metadataOffset), hash, out _);
			NativeMemory.AlignedFree(view);
		}
		var hashes = Hashes;
		for (var i = 0; i < count; i += 4)
		{
			var match = true;
			for (var w = 0; w < 4; w++) match &= hashes[i + w] == BinaryPrimitives.ReadUInt64LittleEndian(hash.Slice(8 * w));
			if (!match) continue;
			Interlocked.Increment(ref Bound);
			return Entries[i >> 2];
		}
		return default;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe TimelineKernelRange Range(int head)
	{
		var consumers = PairTable.ConsumerAt;
		return new(head >= 0 && consumers[head].Next < 0 ? consumers[head].Range : default);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void Chain(int head, bool reverse, int* scratch, byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
	{
		var consumers = PairTable.ConsumerAt;
		if (reverse && head >= 0 && consumers[head].Next >= 0)
		{
			var n = 0;
			for (var e = head; e >= 0; e = consumers[e].Next) scratch[n++] = e;
			while (n-- > 0) { var e = scratch[n]; consumers[e].Execute(slot, gameTick, tick, cycle, flags, columns + consumers[e].Offset, row); }
		}
		else for (var entry = head; entry >= 0; entry = consumers[entry].Next)
			consumers[entry].Execute(slot, gameTick, tick, cycle, flags, columns + consumers[entry].Offset, row);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void ChainRange(int head, bool reverse, int* scratch, byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int rowStart, int rowCount)
	{
		var consumers = PairTable.ConsumerAt;
		if (head < 0) return;
		if (consumers[head].Next >= 0)
		{
			for (var row = rowStart; row < rowStart + rowCount; row++) Chain(head, reverse, scratch, slot, gameTick, tick, cycle, flags, columns, row);
			return;
		}
		var range = consumers[head].Range;
		if (range != null) range(slot, gameTick, tick, cycle, flags, columns, rowStart, rowCount);
		else
		{
			var consumer = consumers + head;
			for (var row = rowStart; row < rowStart + rowCount; row++)
				consumer->Execute(slot, gameTick, tick, cycle, flags, columns + consumer->Offset, row);
		}
	}
}
