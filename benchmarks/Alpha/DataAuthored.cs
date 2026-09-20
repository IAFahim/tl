using Tl;
using Tl.TestSupport;
using System.Diagnostics.CodeAnalysis;

internal static class TickPatterns
{
    internal static void Fill(Span<int> deltas, TickPattern pattern)
    {
        for (var index = 0; index < deltas.Length; index++)
            deltas[index] = pattern == TickPattern.Forward || (index & 1) == 0 ? 1 : -1;
    }
}

internal sealed class LaneCase : IDisposable
{
    internal const int Operations = 4096;
    private readonly int[] _deltas = new int[Operations];
    private readonly TimelineAsset _asset;
    private readonly ushort[] _handles = new ushort[1];
    private readonly ushort[] _positions = new ushort[1];
    private readonly float[] _values = new float[1];

    internal LaneCase(TimelineShape shape, TickPattern pattern)
    {
        Shape = shape;
        TickPatterns.Fill(_deltas, pattern);
        _asset = TimelineAsset.Of(TimelineAsset.Load(Bake(shape)));
        _handles[0] = _asset.Index;
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "fixture model carries the authored shape")]
    internal TimelineShape Shape { get; }

    internal long Run()
    {
        _positions[0] = 0;
        _values[0] = 0;
        for (var index = 0; index < _deltas.Length; index++)
            { { Timeline<AlphaTrack, AlphaClip>.Apply(_handles, _positions, _deltas[index] >= 0, _values); Timeline.Advance(_handles, _positions, _deltas[index] >= 0); } }
        return Checksum(_positions[0], _values[0]);
    }

    public void Dispose() => _asset.Dispose();

    internal static long Checksum(ushort position, float value)
        => unchecked((long)position * 31 + (long)value);

    internal static byte[] Bake(TimelineShape shape)
    {
        var baker = new DomainBaker();
        switch (shape)
        {
            case TimelineShape.OneTrack:
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(1)).Clip(0, 0u, 64u, new AlphaClip(1));
                break;
            case TimelineShape.ThreeTracks:
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(1)).Clip(0, 0u, 64u, new AlphaClip(1));
                baker.Track<BetaTrack, BetaClip>(new BetaTrack(2)).Clip(1, 0u, 64u, new BetaClip(2));
                baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(3)).Clip(2, 0u, 64u, new AlphaClip(3));
                break;
            case TimelineShape.SixteenTracks:
                for (var track = 1; track <= 16; track++)
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(track)).Clip(track - 1, 0u, 64u, new AlphaClip(track));
                break;
            case TimelineShape.TwoHundredFiftySixTracks:
                for (var track = 1; track <= 256; track++)
                    baker.Track<AlphaTrack, AlphaClip>(new AlphaTrack(track)).Clip(track - 1, 0u, 64u, new AlphaClip(track));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
        return baker.Looping().Bake();
    }

    internal static int PerTickEffect(TimelineShape shape) => shape switch
    {
        TimelineShape.OneTrack => 1,
        TimelineShape.ThreeTracks => 6,
        TimelineShape.SixteenTracks => 136,
        TimelineShape.TwoHundredFiftySixTracks => 32896,
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    internal static long Oracle(TimelineShape shape, TickPattern pattern)
    {
        var effect = PerTickEffect(shape);
        var position = (ushort)0;
        var value = 0f;
        var deltas = new int[Operations];
        TickPatterns.Fill(deltas, pattern);
        for (var index = 0; index < deltas.Length; index++)
        {
            var reverse = deltas[index] < 0;
            if (!TimelineMovement.Select(new TimelineState(1, position), 64, true, reverse, out var next, out _, out _))
                throw new InvalidOperationException($"oracle movement failed at position {position}.");
            value += reverse ? -effect : effect;
            position = next.Position;
        }
        return Checksum(position, value);
    }
}
