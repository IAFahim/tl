using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl.Internal;

// One (TInput, TResult) pair's dispatch record for ONE timeline: the eight
// function pointers of the closure's bridge, address-stable for the entry's
// lifetime. The slot lives in the timeline's native block (inline, or in
// the grown overflow array) — no managed cache, no managed arrays. Slots
// are append-only and immutable once published, so lock-free playback
// lookups can copy one by value and call through it safely.
//
// The pointers are stored as nint and cast back to their exact delegate*
// types at the call sites (the generic contexts that installed them); the
// round trip is exact — install and call sites spell the same signatures.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeBindSlot
{
    public int Token;
    private nint _padding;

    // Hub dispatch (Timeline.Forward/Backward/... <TInput, TResult>).
    public nint Forward;
    public nint Backward;
    public nint ForwardCursor;
    public nint BackwardCursor;
    public nint SampleForward;
    public nint SampleBackward;

    // Caller-owned scratch dispatch (Timeline<TTrack, TClip>.Forward/Backward).
    public nint ScratchForward;
    public nint ScratchBackward;
}

// The token source: (TInput, TResult) pair identity as a small integer.
// This is binding metadata created by consumer code (one machine word per
// closed generic pair), NOT timeline state — the pair exists whether or
// not any timeline does. Documented in docs/v0.4-unmanaged.md.
internal static class BindingIds
{
    private static int s_next;

    public static int Next() => Interlocked.Increment(ref s_next);
}

internal static class BindingIds<TInput, TResult>
    where TInput : struct
    where TResult : struct
{
    public static readonly int Token = BindingIds.Next();
}

// Lookup and install over an entry's native bind table. The inline slots
// (first NativeEntry.InlineBindCapacity pairs) are read lock-free on the
// playback hot path; everything past them runs under the entry's native
// spin gate.
internal static unsafe class NativeBinding
{
    // Hot path: scan the inline slots, copy the match by value. Publication
    // order in Install (slot first, then the count, both volatile) makes a
    // published slot fully visible to this read.
    public static NativeBindSlot Get<TInput, TResult>(NativeEntry* entry)
        where TInput : struct
        where TResult : struct
    {
        var token = BindingIds<TInput, TResult>.Token;
        var slots = entry->InlineBinds;
        var count = Volatile.Read(ref entry->InlineBindCount);
        for (var i = 0; i < count; i++)
            if (slots[i].Token == token)
                return slots[i];

        return GetCold<TInput, TResult>(entry, token);
    }

    // First use of this pair on this timeline: run the closure's automatic
    // binder (reflection under the JIT, an explicit Bind<TInput, TResult>
    // prerequisite under AOT), then retry.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static NativeBindSlot GetCold<TInput, TResult>(NativeEntry* entry, int token)
        where TInput : struct
        where TResult : struct
    {
        entry->Binder(typeof(TInput), typeof(TResult), entry->Index);
        var found = Find(entry, token);
        if (found == null)
            throw new InvalidOperationException(
                $"The input/result pair ({typeof(TInput)}, {typeof(TResult)}) does not implement this timeline's closure.");
        return *found;
    }

    private static NativeBindSlot* Find(NativeEntry* entry, int token)
    {
        var slots = entry->InlineBinds;
        var count = Volatile.Read(ref entry->InlineBindCount);
        for (var i = 0; i < count; i++)
            if (slots[i].Token == token)
                return &slots[i];

        Acquire(ref entry->BindGate);
        try
        {
            var overflow = entry->BindOverflow;
            var overflowCount = entry->BindOverflowCount;
            for (var i = 0; i < overflowCount; i++)
                if (overflow[i].Token == token)
                    return &overflow[i];
            return null;
        }
        finally
        {
            Release(ref entry->BindGate);
        }
    }

    // Install is idempotent per token and runs under the entry's spin gate:
    // safe to call concurrently (auto-bind races converge on one slot).
    // Inline appends publish the slot and only then the count, both with
    // volatile semantics, so lock-free readers never observe a half-written
    // slot. Overflow growth copies into a new array and frees the old one
    // — every overflow reader holds the gate, so nothing dangles.
    public static void Install(NativeEntry* entry, in NativeBindSlot slot)
    {
        Acquire(ref entry->BindGate);
        try
        {
            var inline = entry->InlineBinds;
            var inlineCount = entry->InlineBindCount;
            for (var i = 0; i < inlineCount; i++)
                if (inline[i].Token == slot.Token)
                    return;

            if (inlineCount < NativeEntry.InlineBindCapacity)
            {
                inline[inlineCount] = slot;
                Volatile.Write(ref entry->InlineBindCount, inlineCount + 1);
                return;
            }

            var overflow = entry->BindOverflow;
            var count = entry->BindOverflowCount;
            for (var i = 0; i < count; i++)
                if (overflow[i].Token == slot.Token)
                    return;

            if (count == entry->BindOverflowCapacity)
            {
                var capacity = entry->BindOverflowCapacity == 0 ? 8 : entry->BindOverflowCapacity * 2;
                var grown = (NativeBindSlot*)NativeMemory.AllocZeroed((nuint)(capacity * sizeof(NativeBindSlot)));
                if (overflow != null)
                    Buffer.MemoryCopy(overflow, grown,
                        (nuint)(capacity * sizeof(NativeBindSlot)), (nuint)(count * sizeof(NativeBindSlot)));
                entry->BindOverflow = grown;
                entry->BindOverflowCapacity = capacity;
                overflow = grown;
            }

            overflow[count] = slot;
            entry->BindOverflowCount = count + 1;
        }
        finally
        {
            Release(ref entry->BindGate);
        }
    }

    private static void Acquire(ref int gate)
    {
        while (Interlocked.CompareExchange(ref gate, 1, 0) != 0)
            Thread.SpinWait(16);
    }

    private static void Release(ref int gate) => Volatile.Write(ref gate, 0);
}
