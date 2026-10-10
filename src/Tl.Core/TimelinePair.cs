using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

internal unsafe struct MemoLane
{
	public SlotView* Slot;
	public ushort Lane;
	public ushort Duration;
}

public static unsafe partial class Timeline<TTrack, TClip>
	where TTrack : unmanaged, IBlend<TClip>
	where TClip : unmanaged
{
	internal static TimelineSet<TTrack, TClip>? _bank;

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
		var duration = slot->Duration;
		var looping = slot->Looping != 0;
		if (forward)
		{
			if (p < duration) position = LaneMovement.ForwardNext(p, duration, looping);
			return;
		}
		if (p > duration) return;
		if (LaneMovement.BackwardPlayable(p, duration, looping)) position = LaneMovement.BackwardNext(p, duration);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static bool TrySample(ushort index, ushort position, ref TClip scratch, out Frame<TTrack, TClip> frame, bool forward = true)
	{
		frame = default;
		Checked.Domain(index, position);
		Resolve(index);
		var reference = TimelineTable.Reference(index);
		if (!reference.Select(!forward, position, out var tick, out var flags))
			return false;
		var key = PairRuntime<TTrack, TClip>.Key;
		var stage = reference.StageOf(tick);
		if (stage == null)
			return false;
		var pairs = reference.Pairs;
		var block = (byte*)reference.Address;
		var steps = (NativeStep*)(block + stage->ProgramOffset);
		var count = (int)stage->ProgramCount;
		for (var i = 0; i < count; i++)
		{
			if (pairs[steps[i].Pair].Key != key)
				continue;
			frame = SlotRow.ToFrame<TTrack, TClip>((SlotRow*)(block + steps[i].Slot), (byte*)(pairs + steps[i].Pair), tick, flags, ref scratch);
			return true;
		}
		return false;
	}

	public static TClip SampleClip(ushort index, ushort position, bool forward = true)
	{
		var scratch = default(TClip);
		return TrySample(index, position, ref scratch, out _, forward)
			? scratch
			: throw new ArgumentException($"Timeline slot {index} has no ({typeof(TTrack).Name}, {typeof(TClip).Name}) window at position {position}.");
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
	static void ApplySharedClock(SlotView* slot, ushort position, bool forward, Span<float> effects)
	{
		var duration = slot->Duration;
		float delta;
		if (forward)
		{
			if (position >= duration) return;
			delta = LaneEncoding.Value(slot, 0, true, position);
		}
		else
		{
			if (position > duration) return;
			if (!LaneMovement.BackwardPlayable(position, duration, slot->Looping != 0)) return;
			delta = LaneEncoding.Value(slot, 0, false, LaneEncoding.BackwardTick(position, duration));
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
	public static void ApplyChunk<TIndex, TPosition, TEffect0, TEffect1>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect0> effects0, Span<TEffect1> effects1)
		where TIndex : struct
		where TPosition : struct
		where TEffect0 : unmanaged
		where TEffect1 : unmanaged
	{
		CheckSizes<TIndex, TPosition>();
		if (indices.Length != positions.Length) Fail.ColumnLength(positions.Length, indices.Length);
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
		=> throw new ArgumentException($"{Head} has no frozen Fold lane of type {typeof(TLane).Name} for column {column}.");

	[DoesNotReturn]
	static void ThrowLaneColumnSizes()
		=> throw new ArgumentException($"{Head} fold lane columns must be equal-length spans of unmanaged 1, 2, 4 or 8-byte results.");

	[DoesNotReturn]
	static void ThrowMemoRow(int bound)
		=> throw new ArgumentException($"{Head} fold-fed columns exceed the {bound}-slot live buffer; split the consumers.");

	[DoesNotReturn]
	static void ThrowLiveFrame(int bound)
		=> throw new ArgumentException($"{Head} live columns exceed the {bound}-column composed frame; split the consumers.");

	[DoesNotReturn]
	static void ThrowColumnRowMismatch(int column, int length, int rows)
		=> throw new ArgumentException($"{Head} ColumnSet column {column} holds {length} rows; the Apply drives {rows}.");

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
		var setKeys = ColumnSet.Keys(in caller);
		var setCells = stackalloc ulong[caller.Count];
		ApplyPinned(ids, clocks, forward, in caller, setKeys, setCells, 0);
	}

	static void ApplyPinned(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> clocks, bool forward, scoped in ColumnSet caller, ulong* setKeys, ulong* setCells, int pinned)
	{
		if (pinned == caller.Count)
		{
			ApplyLive(ids, clocks, forward, 0, null, 0, null, setKeys, setCells, caller.Count);
			return;
		}
		fixed (byte* cell = caller.Column(pinned))
		{
			setCells[pinned] = (ulong)cell;
			ApplyPinned(ids, clocks, forward, in caller, setKeys, setCells, pinned + 1);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
	static void ApplyLive(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> clocks, bool forward, ulong keyFx, void* fx, ulong keyIn, void* source, ulong* setKeys, ulong* setCells, int setCount)
	{
		var key = PairRuntime<TTrack, TClip>.Key;
		var reverse = !forward;
		var consumers = PairTable.ConsumerAt;
		var entryBound = PairTable.ConsumerCount;
		int* feedStart = stackalloc int[entryBound];
		int* feedEnd = stackalloc int[entryBound];
		Unsafe.InitBlock(feedStart, 0xFF, (uint)(entryBound * sizeof(int)));
		Unsafe.InitBlock(feedEnd, 0xFF, (uint)(entryBound * sizeof(int)));
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
		MemoLane* laneCells = stackalloc MemoLane[memoBound];
		byte* cellBlock = stackalloc byte[memoBound * 8];
		byte* cellWide = stackalloc byte[memoBound];
		int* outLane = stackalloc int[memoBound];
		ulong* outKey = stackalloc ulong[memoBound];
		int memo = 0, pairs = 0;
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
				var outs = 0;
				for (var e = PairTable.HeadOf(key); e >= 0; e = consumers[e].Next)
				{
					// entries installed after the entryBound snapshot cannot own feed scratch
					if (e >= entryBound || consumers[e].DispatchOnly != 0 || consumers[e].Keys == null) continue;
					var n = Math.Min(consumers[e].Keys(slotKeys, slotMeta), PairTable.SlotRow);
					var own = 0;
					feedStart[e] = outs;
					for (var j = 0; j < n; j++)
						if ((slotMeta[j] & 0x10) != 0)
						{
							if (outs >= memoBound) ThrowMemoRow(memoBound);
							outKey[outs] = slotKeys[j];
							outLane[outs++] = consumers[e].OutLanes[own++];
						}
					feedEnd[e] = outs;
				}
				for (var e = PairTable.HeadOf(key); e >= 0; e = consumers[e].Next)
				{
					if (consumers[e].DispatchOnly == 0 || consumers[e].Keys == null) continue;
					var n = Math.Min(consumers[e].Keys(slotKeys, slotMeta), PairTable.SlotRow);
					if (consumers[e].Offset + n > columnBound) ThrowLiveFrame(columnBound);
					var linked = consumers[e].Feed >= 0 && consumers[e].Feed < entryBound && feedStart[consumers[e].Feed] >= 0;
					var feed = linked ? feedStart[consumers[e].Feed] : 0;
					var fedBound = linked ? feedEnd[consumers[e].Feed] : outs;
					var seen = 0;
					for (var j = 0; j < n; j++)
					{
						var meta = slotMeta[j];
						var column = consumers[e].Offset + j;
						if ((meta & 0x40) != 0)
						{
							if (feed >= fedBound || outKey[feed] != slotKeys[j])
								throw new ArgumentException($"{Head} ExecuteActive fold-fed 'in' does not match a Fold 'out' result.");
						if (memo >= memoBound) ThrowMemoRow(memoBound);
						cells[memo] = cellBlock + 8 * memo;
						laneCells[memo] = new MemoLane { Slot = slot, Lane = (ushort)outLane[feed], Duration = slot->Duration };
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
					throw new ArgumentException($"{Head} registers no live ExecuteActive column consumer.");
			}
				for (var r = i; r < end; r++)
				{
					var position = clocks[r];
					if (!reference.Select(reverse, position, out var tick, out var flags)) continue;
					for (var k = 0; k < memo; k++)
					{
						var lane = laneCells[k];
						var laneTick = forward ? position : LaneEncoding.BackwardTick(position, lane.Duration);
						if (cellWide[k] != 0)
						{
							var lo = LaneEncoding.Bits(LaneEncoding.Value(lane.Slot, lane.Lane, forward, laneTick));
							var hi = LaneEncoding.Bits(LaneEncoding.Value(lane.Slot, (nuint)lane.Lane + 1, forward, laneTick));
							*(ulong*)cells[k] = (ulong)lo | (ulong)hi << 32;
						}
						else *(float*)cells[k] = LaneEncoding.Value(lane.Slot, lane.Lane, forward, laneTick);
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
				AbiVersion = SlotView.AbiVersionV3,
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
	private const int Capacity = 64;
	internal int Count;
	private Storage _storage;
	private Octet _o0, _o1, _o2, _o3, _o4, _o5, _o6, _o7;
	private struct Storage
	{
		internal fixed ulong Keys[Capacity];
		internal fixed int Lengths[Capacity];
		internal fixed int Bytes[Capacity];
	}

	private ref struct Octet
	{
		private ref byte _c0, _c1, _c2, _c3, _c4, _c5, _c6, _c7;

		internal void Set(int slot, Span<byte> span)
		{
			ref var column = ref MemoryMarshal.GetReference(span);
			switch (slot)
			{
				case 0: _c0 = ref column; break;
				case 1: _c1 = ref column; break;
				case 2: _c2 = ref column; break;
				case 3: _c3 = ref column; break;
				case 4: _c4 = ref column; break;
				case 5: _c5 = ref column; break;
				case 6: _c6 = ref column; break;
				default: _c7 = ref column; break;
			}
		}

		internal readonly ref byte Get(int slot)
		{
			switch (slot)
			{
				case 0: return ref _c0;
				case 1: return ref _c1;
				case 2: return ref _c2;
				case 3: return ref _c3;
				case 4: return ref _c4;
				case 5: return ref _c5;
				case 6: return ref _c6;
				default: return ref _c7;
			}
		}
	}

	[UnscopedRef]
	private ref Octet OctetOf(int slot)
	{
		switch (slot >> 3)
		{
			case 0: return ref _o0;
			case 1: return ref _o1;
			case 2: return ref _o2;
			case 3: return ref _o3;
			case 4: return ref _o4;
			case 5: return ref _o5;
			case 6: return ref _o6;
			default: return ref _o7;
		}
	}

	public void Add<T>(ReadOnlySpan<T> column) where T : unmanaged
	{
		var key = TypeKey<T>.Value;
		for (var i = 0; i < Count; i++)
			if (_storage.Keys[i] == key) ThrowDuplicate<T>();
		if (Count >= Capacity) ThrowFull();
		_storage.Keys[Count] = key;
		var bytes = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(column)), column.Length * sizeof(T));
		OctetOf(Count).Set(Count & 7, bytes);
		_storage.Lengths[Count] = column.Length;
		_storage.Bytes[Count] = bytes.Length;
		Count++;
	}

	internal int LengthAt(int slot) => _storage.Lengths[slot];

	[UnscopedRef]
	internal Span<byte> Column(int slot) => MemoryMarshal.CreateSpan(ref OctetOf(slot).Get(slot & 7), _storage.Bytes[slot]);

	internal static ulong* Keys(in ColumnSet set)
	{
		fixed (ulong* keys = set._storage.Keys)
			return keys;
	}

	[DoesNotReturn]
	static void ThrowDuplicate<T>() => throw new ArgumentException($"ColumnSet already holds a {typeof(T).Name} column; TypeKey binding cannot distinguish two columns of one type.");

	[DoesNotReturn]
	static void ThrowFull() => throw new ArgumentException($"ColumnSet holds at most {Capacity} typed columns; split the Apply across pairs.");
}
