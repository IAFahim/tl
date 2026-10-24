using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Tl;

internal unsafe struct MemoLane
{
    public SlotView* Slot;
    public ushort Lane;
    public ushort Duration;
}

internal struct LivePlanHeader
{
	public int Epoch, SetCount, ColumnBound, MemoCount, Chain;
}

public static unsafe partial class Timeline<TTrack, TClip>
	where TTrack : unmanaged, IBlend<TClip>
	where TClip : unmanaged
{
	internal static TimelineSet<TTrack, TClip>? _bank;
	const int PlanSlots = 4;
	static int _liveVictim; static ulong _planSlots;

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
	{
		Guard.Same(positions, effects);
		Guard.Same(indices, positions);
		Checked.Domain(indices, positions);
		FloatBank().Apply(indices, positions, forward, effects);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply(ushort index, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
	{
		Guard.Same(positions, effects);
		Checked.Domain(index, positions);
		var bank = FloatBank();
		if (positions.Length > LaneOps.SmallSpan)
		{
			if (!bank.IsFolded(index))
				Resolve(index);
			bank.ApplySlot(index, positions, forward, effects);
			return;
		}
		var slot = Pickup(bank, index);
		if (slot is null)
		{
			Resolve(index);
			slot = bank.FoldedView(index);
		}
		bank.ApplyRecords(slot, positions, forward, effects);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply(ushort index, ushort position, bool forward, Span<float> effects)
	{
		Checked.Domain(index, position);
		var bank = FloatBank();
		var slot = Pickup(bank, index);
		if (slot is null)
		{
			Resolve(index);
			slot = bank.FoldedView(index);
		}
		ApplySharedClock(slot, position, forward, effects);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Advance(ushort index, ref ushort position, bool forward)
		=> Timeline.Advance(index, MemoryMarshal.CreateSpan(ref position, 1), forward);

	[MethodImpl(AggressiveOptimization)]
	public static bool TrySample(ushort index, ushort position, ref TClip scratch, out Frame<TTrack, TClip> frame, bool forward = true)
	{
		frame = default;
		Checked.Domain(index, position);
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
			var pair = pairs + steps[i].Pair;
			if (pair->Key != key)
				continue;
			VerifyPair(pair);
			frame = SlotRow.ToFrame<TTrack, TClip>((SlotRow*)(block + steps[i].Slot), (byte*)pair, tick, flags, ref scratch);
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

	[MethodImpl(NoInlining | AggressiveOptimization)]
	static void ApplySharedClock(SlotView* slot, ushort position, bool forward, Span<float> effects)
	{
		if (slot == null) return;
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

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Guard.Same(indices16, positions16);
		Checked.Columns(indices16, positions16);
		Checked.Domain(indices16, positions16);
		var key = PairRuntime<TTrack, TClip>.Key;
		var reverse = !forward;
		int* chains = stackalloc int[256];
		var reference = default(TimelineRef);
		var pairs = 0;
		var lastIndex = -1;
		var rest = false;
		for (var i = 0; i < positions16.Length; i++)
		{
			var index = indices16[i];
			if (index != lastIndex)
			{
				lastIndex = index;
				rest = Rest(index);
				if (!rest)
				{
					reference = TimelineTable.Reference(index);
					pairs = checked((int)reference.PairCount);
					reference.Resolve(new Span<int>(chains, pairs), key);
				}
			}
			if (!rest && reference.Select(reverse, positions16[i], out var tick, out var flags))
				reference.ExecuteDispatch(reverse, tick, flags, i, new Span<int>(chains, pairs), null);
		}
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply(TimelineAsset asset, ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
	{
		ArgumentNullException.ThrowIfNull(asset);
		Apply(asset.Index, positions, forward, effects);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
	{
		Guard.Same(positions, effects);
		Guard.Same(positions, next);
		Guard.Same(indices, positions);
		Checked.Domain(indices, positions);
		FloatBank().Apply(indices, positions, next, forward, effects);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
	{
		Guard.Same(positions, effects);
		Guard.Same(positions, next);
		Checked.Domain(index, positions);
		var bank = FloatBank();
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

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition, TEffect>();
		if (IntegerEffect<TEffect>())
		{
			ApplyInteger<TEffect>(MemoryMarshal.Cast<TIndex, ushort>(indices), MemoryMarshal.Cast<TPosition, ushort>(positions), forward, MemoryMarshal.Cast<TEffect, int>(effects));
			return;
		}
		Apply(
			MemoryMarshal.Cast<TIndex, ushort>(indices),
			MemoryMarshal.Cast<TPosition, ushort>(positions),
			forward,
			MemoryMarshal.Cast<TEffect, float>(effects));
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Advance<TIndex, TPosition>(ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		Timeline.Advance(MemoryMarshal.Cast<TIndex, ushort>(indices), MemoryMarshal.Cast<TPosition, ushort>(positions), forward);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect>(in TIndex index, in TPosition position, bool forward, ref TEffect effect)
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition, TEffect>();
		if (IntegerEffect<TEffect>())
		{
			ApplyInteger<TEffect>(
				MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)), 1),
				MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TPosition, ushort>(ref Unsafe.AsRef(in position)), 1),
				forward,
				MemoryMarshal.CreateSpan(ref Unsafe.As<TEffect, int>(ref effect), 1));
			return;
		}
		Apply(
			Unsafe.As<TIndex, ushort>(ref Unsafe.AsRef(in index)),
			MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TPosition, ushort>(ref Unsafe.AsRef(in position)), 1),
			forward,
			MemoryMarshal.CreateSpan(ref Unsafe.As<TEffect, float>(ref effect), 1));
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void ApplyChunk<TIndex, TPosition, TEffect0, TEffect1>(ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect0> effects0, Span<TEffect1> effects1)
		where TIndex : struct
		where TPosition : struct
		where TEffect0 : unmanaged
		where TEffect1 : unmanaged
	{
		CheckSizes<TIndex, TPosition>();
		Guard.Same(positions, indices);
		if (Unsafe.SizeOf<TEffect0>() is not (1 or 2 or 4 or 8) || Unsafe.SizeOf<TEffect1>() is not (1 or 2 or 4 or 8))
			ThrowLaneColumnSizes();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Guard.Same(positions16, effects0);
		Guard.Same(positions16, effects1);
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
			if (Rest(id) && id < (uint)bank._count) { i = end; continue; }
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

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	static TimelineSet<TTrack, TClip> FloatBank()
	{
		var bank = Bank();
		if (!bank._floatLaneProven) ProveFloatLane(bank);
		return bank;
	}

	[MethodImpl(NoInlining)]
	static void ProveFloatLane(TimelineSet<TTrack, TClip> bank)
	{
		var key = PairRuntime<TTrack, TClip>.Key;
		var consumers = PairTable.ConsumerAt;
		if (PairTable.HeadOf(key) < 0) return;
		ulong* keys = stackalloc ulong[PairTable.SlotRow];
		byte* meta = stackalloc byte[PairTable.SlotRow];
		for (var e = PairTable.HeadOf(key); e >= 0; e = consumers[e].Next)
		{
			if (consumers[e].DispatchOnly != 0) continue;
			if (consumers[e].Keys != null)
			{
				if (consumers[e].Keys(keys, meta) == 0) continue;
				if ((meta[0] & 0x8F) != 4 || NonFloatLane(keys[0]))
					throw new ArgumentException($"{Head} fold lane 0 holds an integer, double, or non-4-byte Fold result; play it through ApplyChunk or Apply<TIndex, TPosition, TEffect> with that result type.");
			}
			bank._floatLaneProven = true;
			return;
		}
		throw new ArgumentException($"{Head} registers no Fold lane; drive its live ExecuteActive consumers through Apply(ids, clocks, forward, in ColumnSet).");
	}

	static bool NonFloatLane(ulong key)
		=> key == TypeKey<int>.Value || key == TypeKey<uint>.Value;

	[MethodImpl(AggressiveInlining)]
	static bool IntegerEffect<TEffect>() => typeof(TEffect) == typeof(int) || typeof(TEffect) == typeof(uint) || typeof(TEffect).IsEnum;

	static void ApplyInteger<TEffect>(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> clocks, bool forward, Span<int> effects)
	{
		Guard.Same(clocks, ids);
		Guard.Same(clocks, effects);
		Checked.Domain(ids, clocks);
		var bank = Bank();
		var key = TypeKey<TEffect>.Value;
		var i = 0;
		while (i < clocks.Length)
		{
			var id = ids[i];
			var end = i + 1;
			while (end < clocks.Length && ids[end] == id) end++;
			if (Rest(id) && id < (uint)bank._count) { i = end; continue; }
			if (!bank.IsFolded(id)) Resolve(id);
			if (bank.HasView(id))
			{
				var lane = bank.LaneFor(id, key, 0);
				if (lane < 0) ThrowMissingLane<TEffect>(0);
				bank.ApplyLane(id, clocks.Slice(i, end - i), forward, lane, effects.Slice(i, end - i));
			}
			i = end;
		}
	}

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

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
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
			ApplyLive(ids, clocks, forward, setKeys, setCells, caller.Count);
			return;
		}
		fixed (byte* cell = &caller.ColumnRef(pinned))
		{
			setCells[pinned] = (ulong)cell;
			ApplyPinned(ids, clocks, forward, in caller, setKeys, setCells, pinned + 1);
		}
	}

	[MethodImpl(NoInlining | AggressiveOptimization)]
	static void ApplyLive(ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> clocks, bool forward, ulong* setKeys, ulong* setCells, int setCount)
	{
		var key = PairRuntime<TTrack, TClip>.Key;
		var reverse = !forward;
		var plan = FindLivePlan(setKeys, setCount);
		int columnBound, memoCount;
		if (plan == null) { columnBound = PairTable.PairBound(key); memoCount = columnBound; }
		else { var bound = (LivePlanHeader*)plan; columnBound = bound->ColumnBound; memoCount = bound->MemoCount; }
		void** columns = stackalloc void*[columnBound];
		MemoLane* laneCells = stackalloc MemoLane[memoCount];
		byte* cellBlock = stackalloc byte[memoCount * 8];
		int* chains = stackalloc int[256];
		var memo = memoCount; int* feeds = null; TimelineRef reference = default;
		int pairs = 0, last = -1;
		for (var i = 0; i < clocks.Length;)
		{
			var id = ids[i];
			var end = i + 1;
			while (end < clocks.Length && ids[end] == id) end++;
			if (Rest(id)) { i = end; continue; }
			if (id != last)
			{
				last = id;
				Resolve(id);
				reference = TimelineTable.Reference(id);
				pairs = checked((int)reference.PairCount);
				reference.Resolve(new Span<int>(chains, pairs), key);
				var slot = Bank().FoldedView(id);
				var viewLanes = (int*)((byte*)slot->LaneKeys + (nuint)slot->ResultCount * 8) + 1;
				var head = -1;
				for (var p = 0; p < pairs && head < 0; p++) head = chains[p];
				var consumers = PairTable.ConsumerAt;
				var chain = 0;
				for (var e = head; e >= 0; e = consumers[e].Next) chain++;
				var live = (LivePlanHeader*)plan;
				if (plan == null || feeds == null || chain != live->Chain)
				{
					plan = BindLivePlan(setKeys, setCount);
					live = (LivePlanHeader*)plan;
					if (live->ColumnBound > columnBound) ThrowLiveFrame(columnBound);
					if (live->MemoCount > memoCount) ThrowMemoRow(memoCount);
					memo = live->MemoCount;
					var bindings = (int*)(plan + sizeof(LivePlanHeader) + 8 * live->SetCount);
					feeds = bindings + live->ColumnBound;
					for (var c = 0; c < live->ColumnBound; c++)
						columns[c] = bindings[c] >= 0 ? (void*)setCells[bindings[c]] : bindings[c] <= -4 ? cellBlock + 8 * (-4 - bindings[c]) : null;
				}
				for (var m = 0; m < memo; m++)
					laneCells[m] = new MemoLane { Slot = slot, Lane = (ushort)viewLanes[feeds[m] & int.MaxValue], Duration = slot->Duration };
			}
			for (var r = i; r < end; r++)
			{
				var position = clocks[r];
				if (!reference.Select(reverse, position, out var tick, out var flags)) continue;
				for (var k = 0; k < memo; k++)
				{
					var lane = laneCells[k];
					var laneTick = forward ? position : LaneEncoding.BackwardTick(position, lane.Duration);
					if ((feeds[k] & int.MinValue) == 0) *(float*)(cellBlock + 8 * k) = LaneEncoding.Value(lane.Slot, lane.Lane, forward, laneTick);
					else *(ulong*)(cellBlock + 8 * k) = (ulong)LaneEncoding.Bits(LaneEncoding.Value(lane.Slot, lane.Lane, forward, laneTick)) | (ulong)LaneEncoding.Bits(LaneEncoding.Value(lane.Slot, (nuint)lane.Lane + 1, forward, laneTick)) << 32;
				}
				reference.ExecuteDispatch(reverse, tick, flags, r, new Span<int>(chains, pairs), columns);
			}
			i = end;
		}
	}
	static byte* FindLivePlan(ulong* setKeys, int setCount)
	{
		var slots = (ulong*)Volatile.Read(ref _planSlots);
		if (slots == null) return null;
		var epoch = Volatile.Read(ref PairTable._epoch);
		for (var s = 0; s < PlanSlots; s++)
		{
			var plan = (byte*)Volatile.Read(ref slots[s]);
			if (plan == null) continue;
			var header = (LivePlanHeader*)plan;
			if (header->Epoch != epoch || header->SetCount != setCount) continue;
			var keys = (ulong*)(plan + sizeof(LivePlanHeader));
			var i = 0;
			while (i < setCount && keys[i] == setKeys[i]) i++;
			if (i == setCount) return plan;
		}
		return null;
	}
	[MethodImpl(NoInlining)]
	static byte* BindLivePlan(ulong* setKeys, int setCount)
	{
		var plan = FindLivePlan(setKeys, setCount);
		if (plan != null) return plan;
		while ((plan = CommitLivePlan(setKeys, setCount)) == null) ;
		var slots = (ulong*)Volatile.Read(ref _planSlots);
		if (slots == null)
		{
			var fresh = (ulong*)NativeMemory.AlignedAlloc(PlanSlots * 8, 64);
			Unsafe.InitBlock(fresh, 0, PlanSlots * 8);
			var raced = Interlocked.CompareExchange(ref _planSlots, (ulong)fresh, 0);
			if (raced != 0) NativeMemory.AlignedFree(fresh);
			slots = raced == 0 ? fresh : (ulong*)raced;
		}
		var slot = -1;
		for (var s = 0; s < PlanSlots; s++)
			if (slots[s] == 0) { slot = s; break; }
		if (slot < 0) slot = Interlocked.Increment(ref _liveVictim) & (PlanSlots - 1);
		Volatile.Write(ref slots[slot], (ulong)plan);
		return plan;
	}
	static byte* CommitLivePlan(ulong* setKeys, int setCount)
	{
		var key = PairRuntime<TTrack, TClip>.Key;
		var consumers = PairTable.ConsumerAt;
		ulong* slotKeys = stackalloc ulong[PairTable.SlotRow];
		byte* slotMeta = stackalloc byte[PairTable.SlotRow];
		var head = PairTable.HeadOf(key);
		var epoch = Volatile.Read(ref PairTable._epoch);
		var pairCount = PairTable.ChainLength(head);
		int* feedRange = stackalloc int[2 * pairCount];
		Unsafe.InitBlock(feedRange, 0xFF, (uint)(8 * pairCount));
		var feedStart = feedRange;
		var feedEnd = feedRange + pairCount;
		ulong* outKey = stackalloc ulong[PairTable.SlotRow * pairCount];
		var columnBound = 0;
		int outs = 0, memo = 0, live = 0;
		for (var e = head; e >= 0; e = consumers[e].Next)
		{
			var con = consumers[e];
			if (con.Ordinal >= pairCount || con.Keys == null) continue;
			var n = Math.Min(con.Keys(slotKeys, slotMeta), PairTable.SlotRow);
			if (con.DispatchOnly != 0) { columnBound = Math.Max(columnBound, con.Offset + n); continue; }
			feedStart[con.Ordinal] = outs;
			for (var j = 0; j < n; j++)
				if ((slotMeta[j] & 0x10) != 0) outKey[outs++] = slotKeys[j];
			feedEnd[con.Ordinal] = outs;
		}
		var memoBound = outs;
		var bytes = sizeof(LivePlanHeader) + 8 * setCount + 4 * columnBound + 4 * memoBound;
		var draft = stackalloc byte[bytes];
		var header = (LivePlanHeader*)draft;
		header->Epoch = epoch; header->SetCount = setCount; header->ColumnBound = columnBound; header->Chain = pairCount;
		Buffer.MemoryCopy(setKeys, draft + sizeof(LivePlanHeader), (nuint)(8 * setCount), (nuint)(8 * setCount));
		var bindings = (int*)(draft + sizeof(LivePlanHeader) + 8 * setCount);
		for (var c = 0; c < columnBound; c++) bindings[c] = -3;
		for (var e = head; e >= 0; e = consumers[e].Next)
		{
			var con = consumers[e];
			if (con.DispatchOnly == 0 || con.Keys == null) continue;
			var n = Math.Min(con.Keys(slotKeys, slotMeta), PairTable.SlotRow);
			if (con.Offset + n > columnBound) ThrowLiveFrame(columnBound);
			var linked = con.Feed >= 0 && con.Feed < pairCount && feedStart[con.Feed] >= 0;
			var feed = linked ? feedStart[con.Feed] : 0;
			var fedBound = linked ? feedEnd[con.Feed] : outs;
			for (var j = 0; j < n; j++)
			{
				var meta = slotMeta[j];
				var column = con.Offset + j;
				if ((meta & 0x40) == 0)
				{
					for (var s = 0; s < setCount; s++)
						if (setKeys[s] == slotKeys[j]) { bindings[column] = s; break; }
					continue;
				}
				if (feed >= fedBound || outKey[feed] != slotKeys[j])
					throw new ArgumentException($"{Head} ExecuteActive fold-fed 'in' does not match a Fold 'out' result.");
				if (memo >= memoBound) ThrowMemoRow(memoBound);
				bindings[columnBound + memo] = feed | ((meta & 0xF) == 8 ? int.MinValue : 0);
				bindings[column] = -4 - memo;
				memo++;
				feed++;
			}
			for (var j = 0; j < n; j++)
				if (bindings[con.Offset + j] == -3) PairTable.ThrowUnfedLiveColumn(e, j);
			live++;
		}
		if (live == 0) throw new ArgumentException($"{Head} registers no live ExecuteActive column consumer.");
		header->MemoCount = memo;
		if (Volatile.Read(ref PairTable._epoch) != epoch) return null;
		var plan = (byte*)NativeMemory.AlignedAlloc((nuint)bytes, 64);
		Buffer.MemoryCopy(draft, plan, (nuint)bytes, (nuint)bytes);
		return plan;
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Apply<TIndex, TPosition, TEffect>(ReadOnlySpan<int> rows, ReadOnlySpan<TIndex> indices, ReadOnlySpan<TPosition> positions, bool forward, Span<TEffect> effects)
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition, TEffect>();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Guard.Same(positions16, effects);
		Guard.Same(indices16, positions16);
		Checked.Domain(indices16, positions16);
		if (IntegerEffect<TEffect>())
		{
			var cells = MemoryMarshal.Cast<TEffect, int>(effects);
			foreach (var row in rows)
				ApplyInteger<TEffect>(indices16.Slice(row, 1), positions16.Slice(row, 1), forward, cells.Slice(row, 1));
			return;
		}
		FloatBank().ApplyRows(rows, indices16, positions16, forward, MemoryMarshal.Cast<TEffect, float>(effects));
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	public static void Advance<TIndex, TPosition>(ReadOnlySpan<int> rows, ReadOnlySpan<TIndex> indices, Span<TPosition> positions, bool forward)
		where TIndex : struct
		where TPosition : struct
	{
		CheckSizes<TIndex, TPosition>();
		var indices16 = MemoryMarshal.Cast<TIndex, ushort>(indices);
		var positions16 = MemoryMarshal.Cast<TPosition, ushort>(positions);
		Guard.Same(indices16, positions16);
		Checked.Domain(indices16, positions16);
		Bank().AdvanceRows(rows, indices16, positions16, forward);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
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

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	static void CheckSizes<TIndex, TPosition, TEffect>()
		where TIndex : struct
		where TPosition : struct
		where TEffect : struct
	{
		CheckSizes<TIndex, TPosition>();
		if (Unsafe.SizeOf<TEffect>() != 4)
			ThrowColumnSizes();
	}

	[MethodImpl(AggressiveInlining)]
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
		if (Rest(index))
		{
			if (index >= (uint)bank._count) throw TimelineTable.NotLoaded(index);
			return;
		}
		var key = PairRuntime<TTrack, TClip>.Key;
		var digest = PairTable.DigestOf(key);
		if (bank.StaleFold(digest))
			bank.DropFolded();
		else
			bank._registryEpoch = Volatile.Read(ref PairTable._epoch);
		if (bank.IsFolded(index)) return;
		var reference = TimelineTable.Reference(index);
		if (!reference.Uses(key))
			throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
		VerifyPair(reference.PairOf(key)!);
		if (PairTable.HeadOf(key) < 0)
			throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
		using var measured = MeasuredLanes.Measure(reference, key);
		bank.AddAt(index, measured, digest);
	}

	static void Determine(TimelineSet<TTrack, TClip> bank, ushort index)
	{
		if (!TimelineTable.IsLive(index))
		{
			bank.MarkAbsent(index, 2);
			return;
		}
		var reference = TimelineTable.Reference(index);
		var key = PairRuntime<TTrack, TClip>.Key;
		if (!reference.Uses(key))
		{
			bank.MarkAbsent(index);
			return;
		}
		if (PairTable.HeadOf(key) < 0) return;
		Resolve(index);
	}

	internal static void GuardSizes(NativePair* pair) { if (pair->TrackValueBytes != (uint)sizeof(TTrack) || pair->ClipValueBytes != (uint)sizeof(TClip)) Fail.PairSizes(); }

	static void VerifyPair(NativePair* pair)
	{
		GuardSizes(pair);
		var verifiedLayout = PairTable.LayoutOf(PairRuntime<TTrack, TClip>.Key);
		if (pair->Layout != 0 && verifiedLayout != 0 && pair->Layout != verifiedLayout)
			throw new ArgumentException($"Asset was baked against a different field layout for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}); rebake the .tlb with the current assembly.");
	}

	[MethodImpl(NoInlining)]
	internal static void Reconcile()
	{
		var bank = Bank();
		var digest = PairTable.DigestOf(PairRuntime<TTrack, TClip>.Key);
		if (bank.StaleFold(digest)) bank.DropFolded();
		bank._registryEpoch = Volatile.Read(ref PairTable._epoch);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	static SlotView* Pickup(TimelineSet<TTrack, TClip> bank, ushort index)
	{
		var slot = bank.FoldedView(index);
		if (slot == null || Volatile.Read(ref PairTable._epoch) == bank._registryEpoch) return slot;
		Reconcile();
		return bank.FoldedView(index);
	}

	static bool Rest(ushort index) => (TimelineTable.Motion[index] & TimelineTable.DeadMotion) != 0;

	[MethodImpl(AggressiveOptimization)]
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
		if (!TimelineTable.IsLive(index)) throw TimelineTable.NotLoaded(index);
		var bank = Bank();
		if (bank.IsFolded(index))
		{
			if (Volatile.Read(ref PairTable._epoch) != bank._registryEpoch) Reconcile();
			if (bank.IsFolded(index)) return bank.View(index);
		}
		if (bank.IsAbsent(index))
			return new SlotView
			{
				Absent = 1,
				AbiVersion = SlotView.AbiVersionV4,
			};
		Resolve(index);
		return bank.View(index);
	}

	public static SlotView View(TimelineAsset asset)
	{
		ArgumentNullException.ThrowIfNull(asset);
		return View(asset.Index);
	}

	[MethodImpl(AggressiveInlining | AggressiveOptimization)]
	static TimelineSet<TTrack, TClip> Bank()
	{
		var bank = _bank;
		if (bank is not null)
		{
			if (TimelineTable.MotionEpoch != bank._restStamp) bank.SweepRest();
			return bank;
		}
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
	private readonly ref readonly Octet OctetOf(int slot)
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
		OctetAt(Count).Set(Count & 7, MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(column)), 1));
		_storage.Lengths[Count] = column.Length;
		Count++;
	}

	[UnscopedRef]
	private ref Octet OctetAt(int slot) => ref Unsafe.AsRef(in OctetOf(slot));

	internal readonly int LengthAt(int slot) => _storage.Lengths[slot];

	internal readonly ref byte ColumnRef(int slot) => ref OctetOf(slot).Get(slot & 7);

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
