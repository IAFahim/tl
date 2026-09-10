using Tl;

internal static class BatchAliasingReceipt
{
    internal static void Run()
    {
        uint[] ticks = [0u, 1u];
        var calls = 0;
        var input = default(BatchMutationTimeline.Input);
        var output = new BatchMutationTimeline.Output(nextTick: ref ticks[1], calls: ref calls);
        if (!Timeline.TryStart(BatchMutationTimeline.Id, out var playback))
            throw new InvalidOperationException();
        if (!Timeline.TryForward(BatchMutationTimeline.Id, in playback, ticks, in input, ref output, out var next))
            throw new InvalidOperationException();
        if (calls != 2 || next.Tick != 1u || next.Cycles != 0 || ticks[1] != uint.MaxValue)
            throw new InvalidOperationException();

        ticks = [1u, 0u];
        calls = 0;
        output = new BatchMutationTimeline.Output(nextTick: ref ticks[1], calls: ref calls);
        if (!Timeline.TryStart(BatchMutationTimeline.Id, 1u, out playback))
            throw new InvalidOperationException();
        if (!Timeline.TryBackward(BatchMutationTimeline.Id, in playback, ticks, in input, ref output, out next))
            throw new InvalidOperationException();
        if (calls != -2 || next.Tick != 0u || next.Cycles != 0 || next.Flags != PlaybackFlags.Started || ticks[1] != uint.MaxValue)
            throw new InvalidOperationException();

        var oversized = new uint[BatchMutationTimeline.MaxBatchLength + 1];
        var mutation = 17u;
        calls = 0;
        output = new BatchMutationTimeline.Output(nextTick: ref mutation, calls: ref calls);
        if (!Timeline.TryStart(BatchMutationTimeline.Id, out playback))
            throw new InvalidOperationException();
        if (Timeline.TryForward(BatchMutationTimeline.Id, in playback, oversized, in input, ref output, out next))
            throw new InvalidOperationException();
        if (next != playback || calls != 0 || mutation != 17u)
            throw new InvalidOperationException();

        ticks = [1u, ((uint)ushort.MaxValue + 1u) * 2u];
        mutation = 23u;
        calls = 0;
        output = new BatchMutationTimeline.Output(nextTick: ref mutation, calls: ref calls);
        if (!Timeline.TryStart(BatchMutationTimeline.Id, out playback))
            throw new InvalidOperationException();
        if (Timeline.TryForward(BatchMutationTimeline.Id, in playback, ticks, in input, ref output, out next))
            throw new InvalidOperationException();
        if (next != playback || calls != 0 || mutation != 23u)
            throw new InvalidOperationException();

        if (!Timeline.TryStart(BatchMutationTimeline.Id, 1u, out playback))
            throw new InvalidOperationException();
        if (Timeline.TryBackward(BatchMutationTimeline.Id, in playback, oversized, in input, ref output, out next))
            throw new InvalidOperationException();
        if (next != playback || calls != 0 || mutation != 23u)
            throw new InvalidOperationException();
    }
}

public readonly record struct BatchMutationClip(uint Value);

public readonly struct BatchMutationTrack : ITrack<BatchMutationClip>
{
    public void Blend(in BatchMutationClip first, in BatchMutationClip second, float factor, out BatchMutationClip result)
        => result = first;

    public static void Forward(in Frame<BatchMutationTrack, BatchMutationClip> frame, ref uint nextTick, ref int calls)
    {
        calls++;
        nextTick = uint.MaxValue;
    }

    public static void Backward(in Frame<BatchMutationTrack, BatchMutationClip> frame, ref uint nextTick, ref int calls)
    {
        calls--;
        nextTick = uint.MaxValue;
    }
}

public readonly partial struct BatchMutationTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new BatchMutationTrack());
        builder.Clip(track, new BatchMutationClip(1), 0, 2);
        builder.Looping();
    }
}
