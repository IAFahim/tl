using System.Runtime.CompilerServices;

namespace Tl.Internal;

internal unsafe readonly struct Run<TInput, TResult>
    where TInput : struct
    where TResult : struct
{
    public readonly object? Owner;
    public readonly delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> Forward;
    public readonly delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> Backward;
    public readonly delegate*<Timeline.Entry, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> ForwardCursor;
    public readonly delegate*<Timeline.Entry, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> BackwardCursor;
    public readonly delegate*<Timeline.Entry, in TInput, ref TResult, ReadOnlySpan<uint>, void> SampleForward;
    public readonly delegate*<Timeline.Entry, in TInput, ref TResult, ReadOnlySpan<uint>, void> SampleBackward;

    public Run(
        object owner,
        delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> forward,
        delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> backward,
        delegate*<Timeline.Entry, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> forwardCursor,
        delegate*<Timeline.Entry, in Playback, ref Cursor, in TInput, ref TResult, ReadOnlySpan<uint>, Playback> backwardCursor,
        delegate*<Timeline.Entry, in TInput, ref TResult, ReadOnlySpan<uint>, void> sampleForward,
        delegate*<Timeline.Entry, in TInput, ref TResult, ReadOnlySpan<uint>, void> sampleBackward)
    {
        Owner = owner;
        Forward = forward;
        Backward = backward;
        ForwardCursor = forwardCursor;
        BackwardCursor = backwardCursor;
        SampleForward = sampleForward;
        SampleBackward = sampleBackward;
    }

    public bool IsBound => Forward != null;
}

internal static unsafe class BindingCache<TInput, TResult>
    where TInput : struct
    where TResult : struct
{
    private static readonly object s_gate = new();
    private static Run<TInput, TResult>[] s_runs = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Run<TInput, TResult> Get(ushort index, Timeline.Entry entry)
    {
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        return BindCold(index, entry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Run<TInput, TResult> BindCold(ushort index, Timeline.Entry entry)
    {
        entry.Bind(typeof(TInput), typeof(TResult));
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        throw new InvalidOperationException($"The input/result pair ({typeof(TInput)}, {typeof(TResult)}) does not implement this timeline's closure.");
    }

    public static void Install(ushort index, Timeline.Entry entry, in Run<TInput, TResult> run)
    {
        lock (s_gate)
        {
            var current = s_runs;
            if ((uint)index < (uint)current.Length && ReferenceEquals(current[index].Owner, entry) && current[index].IsBound)
                return;

            // Grow geometrically only when the index does not fit; see the
            // matching note in Timeline.Register for why always-doubling is
            // not acceptable here.
            var capacity = Math.Max(current.Length, 16);
            while (capacity <= index)
                capacity *= 2;
            var next = new Run<TInput, TResult>[capacity];
            current.CopyTo(next, 0);
            next[index] = run;
            Volatile.Write(ref s_runs, next);
        }
    }
}

internal unsafe readonly struct ScratchRun<TTrack, TClip, TInput, TResult>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
    where TInput : struct
    where TResult : struct
{
    public readonly object? Owner;
    public readonly delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, Span<TClip>, ReadOnlySpan<uint>, Playback> Forward;
    public readonly delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, Span<TClip>, ReadOnlySpan<uint>, Playback> Backward;

    public ScratchRun(
        object owner,
        delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, Span<TClip>, ReadOnlySpan<uint>, Playback> forward,
        delegate*<Timeline.Entry, in Playback, in TInput, ref TResult, Span<TClip>, ReadOnlySpan<uint>, Playback> backward)
    {
        Owner = owner;
        Forward = forward;
        Backward = backward;
    }

    public bool IsBound => Forward != null;
}

internal static unsafe class ScratchCache<TTrack, TClip, TInput, TResult>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
    where TInput : struct
    where TResult : struct
{
    private static readonly object s_gate = new();
    private static ScratchRun<TTrack, TClip, TInput, TResult>[] s_runs = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScratchRun<TTrack, TClip, TInput, TResult> Get(ushort index, Timeline.Entry entry)
    {
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        return BindCold(index, entry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ScratchRun<TTrack, TClip, TInput, TResult> BindCold(ushort index, Timeline.Entry entry)
    {
        entry.Bind(typeof(TInput), typeof(TResult));
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        throw new InvalidOperationException($"The input/result pair ({typeof(TInput)}, {typeof(TResult)}) does not implement this timeline's closure.");
    }

    public static void Install(ushort index, Timeline.Entry entry, in ScratchRun<TTrack, TClip, TInput, TResult> run)
    {
        lock (s_gate)
        {
            var current = s_runs;
            if ((uint)index < (uint)current.Length && ReferenceEquals(current[index].Owner, entry) && current[index].IsBound)
                return;

            // Grow geometrically only when the index does not fit; see the
            // matching note in Timeline.Register.
            var capacity = Math.Max(current.Length, 16);
            while (capacity <= index)
                capacity *= 2;
            var next = new ScratchRun<TTrack, TClip, TInput, TResult>[capacity];
            current.CopyTo(next, 0);
            next[index] = run;
            Volatile.Write(ref s_runs, next);
        }
    }
}
