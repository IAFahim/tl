using System.Runtime.CompilerServices;

namespace Tl.Internal;

internal unsafe readonly struct Run<TData> where TData : struct
{
    public readonly object? Owner;
    public readonly delegate*<Timeline.Entry, in Playback, ref TData, ReadOnlySpan<uint>, Playback> Forward;
    public readonly delegate*<Timeline.Entry, in Playback, ref TData, ReadOnlySpan<uint>, Playback> Backward;
    public readonly delegate*<Timeline.Entry, in Playback, ref Cursor, ref TData, ReadOnlySpan<uint>, Playback> ForwardCursor;
    public readonly delegate*<Timeline.Entry, in Playback, ref Cursor, ref TData, ReadOnlySpan<uint>, Playback> BackwardCursor;
    public readonly delegate*<Timeline.Entry, ref TData, ReadOnlySpan<uint>, void> SampleForward;
    public readonly delegate*<Timeline.Entry, ref TData, ReadOnlySpan<uint>, void> SampleBackward;

    public Run(
        object owner,
        delegate*<Timeline.Entry, in Playback, ref TData, ReadOnlySpan<uint>, Playback> forward,
        delegate*<Timeline.Entry, in Playback, ref TData, ReadOnlySpan<uint>, Playback> backward,
        delegate*<Timeline.Entry, in Playback, ref Cursor, ref TData, ReadOnlySpan<uint>, Playback> forwardCursor,
        delegate*<Timeline.Entry, in Playback, ref Cursor, ref TData, ReadOnlySpan<uint>, Playback> backwardCursor,
        delegate*<Timeline.Entry, ref TData, ReadOnlySpan<uint>, void> sampleForward,
        delegate*<Timeline.Entry, ref TData, ReadOnlySpan<uint>, void> sampleBackward)
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

internal static unsafe class BindingCache<TData> where TData : struct
{
    private static readonly object s_gate = new();
    private static Run<TData>[] s_runs = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Run<TData> Get(ushort index, Timeline.Entry entry)
    {
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        return BindCold(index, entry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Run<TData> BindCold(ushort index, Timeline.Entry entry)
    {
        entry.Bind(typeof(TData));
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        throw new InvalidOperationException($"The consumer type {typeof(TData)} does not implement this timeline's closure.");
    }

    public static void Install(ushort index, Timeline.Entry entry, in Run<TData> run)
    {
        lock (s_gate)
        {
            var current = s_runs;
            if ((uint)index < (uint)current.Length && ReferenceEquals(current[index].Owner, entry) && current[index].IsBound)
                return;

            var next = new Run<TData>[Math.Max(index + 1, current.Length == 0 ? 16 : current.Length * 2)];
            current.CopyTo(next, 0);
            next[index] = run;
            Volatile.Write(ref s_runs, next);
        }
    }
}

internal unsafe readonly struct ScratchRun<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
    where TData : struct
{
    public readonly object? Owner;
    public readonly delegate*<Timeline.Entry, in Playback, ref TData, Span<TClip>, ReadOnlySpan<uint>, Playback> Forward;
    public readonly delegate*<Timeline.Entry, in Playback, ref TData, Span<TClip>, ReadOnlySpan<uint>, Playback> Backward;

    public ScratchRun(
        object owner,
        delegate*<Timeline.Entry, in Playback, ref TData, Span<TClip>, ReadOnlySpan<uint>, Playback> forward,
        delegate*<Timeline.Entry, in Playback, ref TData, Span<TClip>, ReadOnlySpan<uint>, Playback> backward)
    {
        Owner = owner;
        Forward = forward;
        Backward = backward;
    }

    public bool IsBound => Forward != null;
}

internal static unsafe class ScratchCache<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : unmanaged
    where TData : struct
{
    private static readonly object s_gate = new();
    private static ScratchRun<TTrack, TClip, TData>[] s_runs = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScratchRun<TTrack, TClip, TData> Get(ushort index, Timeline.Entry entry)
    {
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        return BindCold(index, entry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ScratchRun<TTrack, TClip, TData> BindCold(ushort index, Timeline.Entry entry)
    {
        entry.Bind(typeof(TData));
        var runs = Volatile.Read(ref s_runs);
        if ((uint)index < (uint)runs.Length && ReferenceEquals(runs[index].Owner, entry))
            return runs[index];

        throw new InvalidOperationException($"The consumer type {typeof(TData)} does not implement this timeline's closure.");
    }

    public static void Install(ushort index, Timeline.Entry entry, in ScratchRun<TTrack, TClip, TData> run)
    {
        lock (s_gate)
        {
            var current = s_runs;
            if ((uint)index < (uint)current.Length && ReferenceEquals(current[index].Owner, entry) && current[index].IsBound)
                return;

            var next = new ScratchRun<TTrack, TClip, TData>[Math.Max(index + 1, current.Length == 0 ? 16 : current.Length * 2)];
            current.CopyTo(next, 0);
            next[index] = run;
            Volatile.Write(ref s_runs, next);
        }
    }
}
