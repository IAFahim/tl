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
        if (playback.Position != 2L || playback.GameTick != 2u || value != 3u || calls != 2)
            throw new InvalidOperationException();

        if (!AliasingTimeline.TrySeek(ref data, -2))
            throw new InvalidOperationException();
        if (playback.Position != 0L || playback.GameTick != 0u || value != 1u || calls != 0)
            throw new InvalidOperationException();

        var before = playback;
        if (AliasingTimeline.TrySeek(ref data, -1))
            throw new InvalidOperationException();
        if (playback != before || value != 1u || calls != 0)
            throw new InvalidOperationException();

        var empty = default(AliasingTimeline.Data);
        if (AliasingTimeline.TrySeek(ref empty, 0))
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
        calls += frame.Direction;
        nextTick = unchecked((uint)((long)currentTick + frame.Direction));
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
