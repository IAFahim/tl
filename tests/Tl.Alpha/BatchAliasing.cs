using Tl;

internal static class DataAliasingReceipt
{
    internal static void Run()
    {
        var value = 1u;
        var calls = 0;
        var playback = AliasingTimeline.Start(0u);
        var data = new AliasingTimeline.Data(ref playback, in value, ref calls, ref value);

        if (!AliasingTimeline.TrySeek(ref data, 2))
            throw new InvalidOperationException();
        RequireState(in playback, value, calls, 2L, 2u, 3u, 2);

        if (!AliasingTimeline.TrySeek(ref data, -2))
            throw new InvalidOperationException();
        RequireState(in playback, value, calls, 0L, 0u, 1u, 0);

        var before = playback;
        if (AliasingTimeline.TrySeek(ref data, -1))
            throw new InvalidOperationException();
        if (playback != before)
            throw new InvalidOperationException();
        RequireState(in playback, value, calls, 0L, 0u, 1u, 0);

        var empty = default(AliasingTimeline.Data);
        if (AliasingTimeline.TrySeek(ref empty, 0))
            throw new InvalidOperationException();
    }

    private static void RequireState(
        in Playback<AliasingTimeline> playback,
        uint value,
        int calls,
        long position,
        uint gameTick,
        uint expectedValue,
        int expectedCalls)
    {
        if (playback.Position != position
            || playback.GameTick != gameTick
            || value != expectedValue
            || calls != expectedCalls)
            throw new InvalidOperationException();
    }
}

public readonly record struct AliasingClip(uint Value);

public readonly struct AliasingTrack : ITrack<AliasingClip>
{
    public void Blend(in AliasingClip first, in AliasingClip second, float factor, out AliasingClip result)
        => result = first;

    public static void Seek(in Frame<AliasingTrack, AliasingClip> frame, in uint currentTick, ref uint nextTick, ref int calls)
    {
        var movement = frame.Direction * checked((int)frame.Clip.Value);
        calls += movement;
        nextTick = unchecked((uint)(currentTick + movement));
    }
}

public readonly partial struct AliasingTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AliasingTrack());
        builder.Clip(track, new AliasingClip(1), 0u, 2u);
    }
}
