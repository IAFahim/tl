using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public static unsafe class Timeline<TTrack, TClip>
	where TTrack : unmanaged, IBlend<TClip>
	where TClip : unmanaged
{
	static TimelineSet<TTrack, TClip>? _bank;

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
	{
		Checked.Domain(indices, positions);
		Bank().Apply(indices, positions, forward, effects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
	{
		Checked.Domain(index, positions);
		var bank = Bank();
		if (positions.Length > LaneOps.SmallSpan)
		{
			if (!bank.IsFolded(index))
				Resolve(index);
			bank.ApplySlot(index, positions, forward, effects);
			return;
		}
		var slot = bank.FoldedView(index);
		if (slot is null)
		{
			Resolve(index);
			slot = bank.FoldedView(index);
		}
		bank.ApplyRecords(slot, positions, forward, effects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply(ushort index, ushort position, bool forward, Span<float> effects)
	{
		Checked.Domain(index, position);
		var bank = Bank();
		var slot = bank.FoldedView(index);
		if (slot is null)
		{
			Resolve(index);
			slot = bank.FoldedView(index);
		}
		ApplySharedClock(slot, position, forward, effects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Advance(ushort index, ref ushort position, bool forward)
	{
		Checked.Domain(index, position);
		var bank = Bank();
		var slot = bank.FoldedView(index);
		if (slot is null)
		{
			Resolve(index);
			slot = bank.FoldedView(index);
		}
		var p = position;
		if (forward)
		{
			if (p < slot->Duration) position = slot->ForwardRecords[p].Next;
			return;
		}
		if (p > slot->Duration) return;
		var next = slot->BackwardRecords[p].Next;
		if (next != LaneMovementRecord.Skipped) position = next;
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
	static void ApplySharedClock(SlotView* slot, ushort position, bool forward, Span<float> effects)
	{
		float delta;
		if (forward)
		{
			if (position >= slot->Duration) return;
			delta = slot->ForwardRecords[position].Effect;
		}
		else
		{
			if (position > slot->Duration) return;
			ref var record = ref slot->BackwardRecords[position];
			if (record.Next == LaneMovementRecord.Skipped) return;
			delta = record.Effect;
		}
		LaneOps.Add(effects, 0, effects.Length, delta);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Checked.Columns(indices16, positions16);
		Checked.Domain(indices16, positions16);
		var key = PairRuntime<TTrack, TClip>.Key;
		var reverse = !forward;
		int* chains = stackalloc int[256];
		var reference = default(TimelineRef);
		var pairs = 0;
		var lastIndex = -1;
		for (var i = 0; i < positions16.Length; i++)
		{
			var index = indices16[i];
			if (index != lastIndex)
			{
				reference = TimelineTable.Reference(index);
				pairs = checked((int)reference.PairCount);
				reference.Resolve(new Span<int>(chains, pairs), key);
				lastIndex = index;
			}
			if (reference.Select(reverse, positions16[i], out var tick, out var flags))
				reference.ExecuteDispatch(reverse, tick, flags, i, new Span<int>(chains, pairs), null);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
	{
		ArgumentNullException.ThrowIfNull(asset);
		Apply(asset.Index, positions, forward, effects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
	{
		Checked.Domain(indices, positions);
		Bank().Apply(indices, positions, next, forward, effects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
	{
		Checked.Domain(index, positions);
		var bank = Bank();
		if (positions.Length > LaneOps.SmallSpan)
		{
			if (!bank.IsFolded(index))
				Resolve(index);
			bank.ApplySlot(index, positions, next, forward, effects);
			return;
		}
		var slot = bank.FoldedView(index);
		if (slot is null)
		{
			Resolve(index);
			slot = bank.FoldedView(index);
		}
		bank.ApplyRecords(slot, positions, next, forward, effects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition, TEffect>();
		Apply(
			MemoryMarshal.Cast<TIndex, ushort>(indices),
			MemoryMarshal.Cast<TPosition, ushort>(positions),
			forward,
			MemoryMarshal.Cast<TEffect, float>(effects));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Advance<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		Timeline.Advance(MemoryMarshal.Cast<TIndex, ushort>(indices), MemoryMarshal.Cast<TPosition, ushort>(positions), forward);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect>(in TIndex index, in TPosition position, bool forward, ref TEffect effect)
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition, TEffect>();
		Apply(
			Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
			MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TPosition, ushort>(ref Unsafe.AsRef(in position)), 1),
			forward,
			MemoryMarshal.CreateSpan(ref Unsafe.As<TEffect, float>(ref effect), 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void ApplyLanes<TIndex, TPosition, TEffect0, TEffect1>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect0> effects0, Span<TEffect1> effects1)
		where TIndex : struct
		where TPosition : struct
		where TEffect0 : unmanaged
		where TEffect1 : unmanaged
	{
		CheckSizes<TIndex, TPosition>();
		if (Unsafe.SizeOf<TEffect0>() is not (1 or 2 or 4 or 8) || Unsafe.SizeOf<TEffect1>() is not (1 or 2 or 4 or 8)
			|| positions.Length != effects0.Length || positions.Length != effects1.Length)
			ThrowLaneColumnSizes();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Checked.Domain(indices16, positions16);
		var bank = Bank();
		var key0 = TypeKey<TEffect0>.Value;
		var key1 = TypeKey<TEffect1>.Value;
		var ordinal1 = key1 == key0 ? 1 : 0;
		var i = 0;
		while (i < positions16.Length)
		{
			var id = indices16[i];
			var end = i + 1;
			while (end < positions16.Length && indices16[end] == id) end++;
			if (!bank.IsFolded(id)) Resolve(id);
			if (bank.HasView(id))
			{
				var lane0 = bank.LaneFor(id, key0, 0);
				var lane1 = bank.LaneFor(id, key1, ordinal1);
				if (lane0 < 0) ThrowMissingLane<TEffect0>(0);
				if (lane1 < 0) ThrowMissingLane<TEffect1>(1);
				var slice = positions16.Slice(i, end - i);
				bank.ApplyLane(id, slice, forward, lane0, effects0.Slice(i, end - i));
				bank.ApplyLane(id, slice, forward, lane1, effects1.Slice(i, end - i));
			}
			i = end;
		}
	}

	static string Head => $"Timeline<{typeof(TTrack).Name}, {typeof(TClip).Name}>";

	[DoesNotReturn]
	static void ThrowMissingLane<TLane>(int column)
		=> throw new ArgumentException($"{Head} has no frozen OnMemo lane of type {typeof(TLane).Name} for column {column}.");

	[DoesNotReturn]
	static void ThrowLaneColumnSizes()
		=> throw new ArgumentException($"{Head} memo lane columns must be equal-length spans of unmanaged 1, 2, 4 or 8-byte results.");

	[DoesNotReturn]
	static void ThrowMemoRow(int bound)
		=> throw new ArgumentException($"{Head} memo-fed columns exceed the {bound}-slot live buffer; split the consumers.");

	[DoesNotReturn]
	static void ThrowLiveFrame(int bound)
		=> throw new ArgumentException($"{Head} live columns exceed the {bound}-column composed frame; split the consumers.");

	[DoesNotReturn]
	static void ThrowColumnRowMismatch(int column, int length, int rows)
		=> throw new ArgumentException($"{Head} ColumnSet column {column} holds {length} rows; the Apply drives {rows}.");

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect, TInput>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects, ReadOnlySpan<TInput> input)
		where TIndex : struct where TPosition : struct where TEffect : unmanaged where TInput : unmanaged
	{
		if (positions.Length != indices.Length || positions.Length != effects.Length || positions.Length != input.Length)
			throw new ArgumentException("Timeline live columns must be single-field and equal length.");
		CheckSizes<TIndex, TPosition>();
		var ids = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var clocks = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Checked.Domain(ids, clocks);
		ApplyLive(ids, clocks, forward, TypeKey<TEffect>.Value, Unsafe.AsPointer(ref MemoryMarshal.GetReference(effects)), TypeKey<TInput>.Value, Unsafe.AsPointer(ref MemoryMarshal.GetReference(input)), null, null, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, in ColumnSet caller)
		where TIndex : struct where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		var ids = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var clocks = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Checked.Domain(ids, clocks);
		for (var s = 0; s < caller.Count; s++)
			if (caller.LengthAt(s) != clocks.Length)
				ThrowColumnRowMismatch(s, caller.LengthAt(s), clocks.Length);
		ColumnSet.Pointers(in caller, out var setKeys, out var setCells);
		ApplyLive(ids, clocks, forward, 0, null, 0, null, setKeys, setCells, caller.Count);
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
	static void ApplyLive(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> clocks, bool forward, ulong keyFx, void* fx, ulong keyIn, void* source, ulong* setKeys, ulong* setCells, int setCount)
	{
		var key = PairRuntime<TTrack, TClip>.Key;
		var reverse = !forward;
		var consumers = PairTable.ConsumerAt;
		int* chains = stackalloc int[256];
		ulong* slotKeys = stackalloc ulong[PairTable.SlotRow];
		byte* slotMeta = stackalloc byte[PairTable.SlotRow];
		var columnBound = 0;
		var memoBound = 0;
		for (var e = PairTable.HeadOf(key); e >= 0; e = consumers[e].Next)
		{
			if (consumers[e].Keys == null) continue;
			var n = Math.Min(consumers[e].Keys(slotKeys, slotMeta), PairTable.SlotRow);
			if (consumers[e].DispatchOnly != 0)
			{
				if (consumers[e].Offset + n > columnBound) columnBound = consumers[e].Offset + n;
				continue;
			}
			for (var j = 0; j < n; j++)
				if ((slotMeta[j] & 0x10) != 0) memoBound++;
		}
		void** columns = stackalloc void*[columnBound];
		Unsafe.InitBlock(columns, 0, (uint)(columnBound * sizeof(void*)));
		byte** cells = stackalloc byte*[memoBound];
		LaneMovementRecord** laneRecords = stackalloc LaneMovementRecord*[memoBound];
		byte* cellBlock = stackalloc byte[memoBound * 8];
		byte* cellWide = stackalloc byte[memoBound];
		int* outLane = stackalloc int[memoBound];
		ulong* outKey = stackalloc ulong[memoBound];
		int memo = 0, pairs = 0;
		nuint tableTicks = 0;
		var reference = default(TimelineRef);
		var last = -1;
		for (var i = 0; i < clocks.Length;)
		{
			var id = ids[i];
			var end = i + 1;
			while (end < clocks.Length && ids[end] == id) end++;
			if (id != last)
			{
				int live = 0;
				last = id;
				memo = 0;
				Resolve(id);
				reference = TimelineTable.Reference(id);
				pairs = checked((int)reference.PairCount);
				reference.Resolve(new Span<int>(chains, pairs), key);
				var slot = Bank().FoldedView(id);
				tableTicks = slot->TableTicks;
				var outs = 0;
				for (var e = PairTable.HeadOf(key); e >= 0; e = consumers[e].Next)
				{
					if (consumers[e].DispatchOnly != 0 || consumers[e].Keys == null) continue;
					var n = Math.Min(consumers[e].Keys(slotKeys, slotMeta), PairTable.SlotRow);
					var own = 0;
					for (var j = 0; j < n; j++)
						if ((slotMeta[j] & 0x10) != 0)
						{
							if (outs >= memoBound) ThrowMemoRow(memoBound);
							outKey[outs] = slotKeys[j];
							outLane[outs++] = consumers[e].OutLanes[own++];
						}
				}
				for (var e = PairTable.HeadOf(key); e >= 0; e = consumers[e].Next)
				{
					if (consumers[e].DispatchOnly == 0 || consumers[e].Keys == null) continue;
					var n = Math.Min(consumers[e].Keys(slotKeys, slotMeta), PairTable.SlotRow);
					if (consumers[e].Offset + n > columnBound) ThrowLiveFrame(columnBound);
					var feed = 0;
					var seen = 0;
					for (var j = 0; j < n; j++)
					{
						var meta = slotMeta[j];
						var column = consumers[e].Offset + j;
						if ((meta & 0x40) != 0)
						{
							if (feed >= outs || outKey[feed] != slotKeys[j])
								throw new ArgumentException($"{Head} OnActive memo-fed 'in' does not match an OnMemo 'out' result.");
							if (memo >= memoBound) ThrowMemoRow(memoBound);
							cells[memo] = cellBlock + 8 * memo;
							laneRecords[memo] = (forward ? slot->ForwardRecords : slot->BackwardRecords) + (nuint)outLane[feed] * slot->TableTicks;
							cellWide[memo] = (meta & 0xF) == 8 ? (byte)1 : (byte)0;
							columns[column] = cells[memo++];
							feed++;
						}
						else
						{
							var cell = CellOf(setKeys, setCells, setCount, slotKeys[j]);
							if (cell != null) columns[column] = cell;
							else if ((meta & 0x20) != 0)
							{
								if (slotKeys[j] == keyFx) columns[column] = fx;
							}
							else if (slotKeys[j] == keyIn && seen++ == 0) columns[column] = source;
						}
					}
					for (var j = 0; j < n; j++)
						if (columns[consumers[e].Offset + j] == null)
							PairTable.ThrowUnfedLiveColumn(e, j);
					live++;
				}
				if (live == 0)
					throw new ArgumentException($"{Head} registers no live OnActive column consumer.");
			}
			for (var r = i; r < end; r++)
			{
				var position = clocks[r];
				if (!reference.Select(reverse, position, out var tick, out var flags)) continue;
				for (var k = 0; k < memo; k++)
				{
					ref var rec = ref laneRecords[k][position];
					if (cellWide[k] != 0)
					{
						ref var hi = ref (laneRecords[k] + tableTicks)[position];
						*(ulong*)cells[k] = Unsafe.As<float, uint>(ref rec.Effect) | (ulong)Unsafe.As<float, uint>(ref hi.Effect) << 32;
					}
					else *(float*)cells[k] = rec.Effect;
				}
				reference.ExecuteDispatch(reverse, tick, flags, r, new Span<int>(chains, pairs), columns);
			}
			i = end;
		}
	}

	static void* CellOf(ulong* keys, ulong* cells, int n, ulong key)
	{
		for (var i = 0; i < n; i++) if (keys[i] == key) return (void*)cells[i];
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<int> rows, ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition, TEffect>();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Checked.Domain(indices16, positions16);
		Bank().ApplyRows(rows, indices16, positions16, forward, MemoryMarshal.Cast<TEffect, float>(effects));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Advance<TIndex, TPosition>(ReadOnlySpan<int> rows, ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Checked.Domain(indices16, positions16);
		Bank().AdvanceRows(rows, indices16, positions16, forward);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Advance<TIndex, TPosition>(in TIndex index, ref TPosition position, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		Timeline.Advance(
			Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
			MemoryMarshal.CreateSpan(ref Unsafe.As<TPosition, ushort>(ref position), 1),
			forward);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	static void CheckSizes<TIndex, TPosition, TEffect>()
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition>();
		if (Unsafe.SizeOf<TEffect>() != 4)
			ThrowColumnSizes();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void CheckSizes<TIndex, TPosition>()
		where TIndex : struct
		where TPosition : struct
	{
		if (Unsafe.SizeOf<TIndex>() != 2 || Unsafe.SizeOf<TPosition>() != 2)
			ThrowColumnSizes();
	}

	static void ThrowColumnSizes()
		=> throw new ArgumentException("Timeline columns must be single-field: index ushort, position ushort, effect float.");

	internal static void Resolve(ushort index)
	{
		var bank = Bank();
		if (bank.IsFolded(index)) return;
		var reference = TimelineTable.Reference(index);
		var key = PairRuntime<TTrack, TClip>.Key;
		if (!reference.Uses(key))
			throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
		var bakedLayout = reference.LayoutOf(key);
		var verifiedLayout = PairTable.LayoutOf(key);
		if (bakedLayout != 0 && verifiedLayout != 0 && bakedLayout != verifiedLayout)
			throw new ArgumentException($"Asset was baked against a different field layout for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}); rebake the .tlb with the current assembly.");
		if (PairTable.HeadOf(key) < 0)
			throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
		using var measured = MeasuredLanes.Measure(reference, key);
		bank.AddAt(index, measured);
		var cursor = bank.PendingCursor;
		for (var below = cursor; below < index; below++)
			if (bank.IsPending((ushort)below))
				Determine(bank, (ushort)below);
		bank.AdvancePendingCursor(index);
	}

	static void Determine(TimelineSet<TTrack, TClip> bank, ushort index)
	{
		if (!TimelineTable.IsLive(index)) return;
		var reference = TimelineTable.Reference(index);
		var key = PairRuntime<TTrack, TClip>.Key;
		if (!reference.Uses(key))
		{
			bank.MarkAbsent(index);
			return;
		}
		if (PairTable.HeadOf(key) < 0) return;
		using var measured = MeasuredLanes.Measure(reference, key);
		bank.AddAt(index, measured);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static bool ResolveChunk(TimelineSet<TTrack, TClip> set, ReadOnlySpan<ushort> indices, int start, int end)
	{
		var views = set._views;
		var bound = (uint)set._count;
		for (var i = start; i < end; i++)
		{
			var index = indices[i];
			if (index >= bound)
			{
				Resolve(index);
				return true;
			}
			if (views[index] != null) continue;
			Resolve(index);
			return true;
		}
		return false;
	}

	public static SlotView View(ushort index)
	{
		var bank = Bank();
		if (bank.IsFolded(index)) return bank.View(index);
		if (bank.IsAbsent(index))
			return new SlotView
			{
				Absent = 1,
				RecordBytes = (ushort)sizeof(LaneMovementRecord),
				AbiVersion = SlotView.AbiVersionV1,
			};
		Resolve(index);
		return bank.View(index);
	}

	public static SlotView View(TimelineAsset asset)
	{
		ArgumentNullException.ThrowIfNull(asset);
		return View(asset.Index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	static TimelineSet<TTrack, TClip> Bank()
	{
		var bank = _bank;
		if (bank is not null) return bank;
		bank = new TimelineSet<TTrack, TClip> { _lazyResolve = true };
		return Interlocked.CompareExchange(ref _bank, bank, null) ?? bank;
	}
}

public unsafe ref struct ColumnSet
{
	internal const int Capacity = PairTable.MaxPointers / PairTable.SlotRow;
	internal int Count;
	private Storage _storage;
	private struct Storage
	{
		internal fixed ulong Keys[Capacity];
		internal fixed ulong Cells[Capacity];
		internal fixed int Lengths[Capacity];
	}

	public void Add<T>(ReadOnlySpan<T> column) where T : unmanaged
	{
		var key = TypeKey<T>.Value;
		for (var i = 0; i < Count; i++)
			if (_storage.Keys[i] == key) ThrowDuplicate<T>();
		if (Count >= Capacity) ThrowFull();
		_storage.Keys[Count] = key;
		_storage.Cells[Count] = (ulong)Unsafe.AsPointer(ref MemoryMarshal.GetReference(column));
		_storage.Lengths[Count] = column.Length;
		Count++;
	}

	internal int LengthAt(int slot) => _storage.Lengths[slot];

	internal static void Pointers(in ColumnSet set, out ulong* keys, out ulong* cells)
	{
		fixed (ulong* k = set._storage.Keys, c = set._storage.Cells)
		{
			keys = k;
			cells = c;
		}
	}

	[DoesNotReturn]
	static void ThrowDuplicate<T>() => throw new ArgumentException($"ColumnSet already holds a {typeof(T).Name} column; TypeKey binding cannot distinguish two columns of one type.");

	[DoesNotReturn]
	static void ThrowFull() => throw new ArgumentException($"ColumnSet holds at most {Capacity} typed columns; split the Apply across pairs.");
}
