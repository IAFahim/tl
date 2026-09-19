using System.Globalization;
using System.Text;
using Tl;
using Tl.Gen.Tlb;

namespace Play;

public sealed record BakeResult(byte[] Bytes, int Duration, bool Loop);

public static class PlayBake
{
    public static BakeResult Bake(string authoringJson)
    {
        var bytes = TimelineBaker.BakeJson(authoringJson);
        using var probe = TimelineAsset.LoadAsset(bytes);
        using var measured = MeasuredLanes.Measure(probe);
        return new BakeResult(bytes, measured.Duration, measured.Looping);
    }
}

public readonly record struct FrameStats(int Moved, int Skipped, int Wrapped, float EffectTotal);

public sealed class PlayEngine : IDisposable
{
    readonly int _crowd;
    readonly ushort[] _ids;
    readonly ushort[] _positions;
    readonly ushort[] _before;
    readonly float[] _effects;
    TimelineSet<ScaleTrack, AmountClip>? _set;
    TimelineAsset? _asset;

    public PlayEngine(int crowd)
    {
        PlayRuntime.Ensure();
        _crowd = Math.Clamp(crowd, Scenario.MinCrowd, Scenario.MaxCrowd);
        _ids = new ushort[_crowd];
        _positions = new ushort[_crowd];
        _before = new ushort[_crowd];
        _effects = new float[_crowd];
    }

    public int Crowd => _crowd;
    public int Duration { get; private set; }
    public bool Loop { get; private set; }
    public ulong Checksum { get; private set; }
    public FrameStats Last { get; private set; }
    public ReadOnlySpan<ushort> Positions => _positions;
    public ReadOnlySpan<float> Effects => _effects;

    public void Load(BakeResult baked)
    {
        _set?.Dispose();
        _asset?.Dispose();
        _asset = TimelineAsset.LoadAsset(baked.Bytes);
        _set = new TimelineSet<ScaleTrack, AmountClip>();
        Duration = baked.Duration;
        Loop = baked.Loop;
        _ids.AsSpan().Fill(_set.Add(_asset));
        BakedLane<ScaleTrack, AmountClip>.Bind(_asset);
        var limit = (ushort)Math.Max(0, Duration);
        for (var i = 0; i < _positions.Length; i++)
            if (_positions[i] > limit) _positions[i] = limit;
    }

    public void SeedPositions(ScenarioPattern pattern)
    {
        var duration = Math.Max(1, Duration);
        for (var i = 0; i < _positions.Length; i++)
        {
            _positions[i] = pattern switch
            {
                ScenarioPattern.Uniform => 0,
                ScenarioPattern.Waves => (ushort)((WaveOf(i, _positions.Length) * duration / 8 + i % Math.Max(1, duration / 8)) % duration),
                _ => (ushort)((long)i * duration / _positions.Length % duration),
            };
        }
        _effects.AsSpan().Clear();
    }

    public FrameStats Step(bool forward)
    {
        _positions.CopyTo(_before, 0);
        _set!.Gather(_ids).Seek(_positions, forward).Apply(_effects); Timeline.Step(_ids, _positions, forward);
        var moved = 0;
        var skipped = 0;
        var wrapped = 0;
        var last = (ushort)Math.Max(0, Duration - 1);
        for (var i = 0; i < _positions.Length; i++)
        {
            var before = _before[i];
            var after = _positions[i];
            if (after == before) skipped++;
            else
            {
                moved++;
                if (Loop && after == 0 && before == last && forward) wrapped++;
                else if (Loop && before == 0 && after == last && !forward) wrapped++;
            }
        }
        var total = 0f;
        for (var i = 0; i < _effects.Length; i++) total += _effects[i];
        Last = new FrameStats(moved, skipped, wrapped, total);
        Checksum = unchecked(Checksum * 31 + FrameFingerprint());
        return Last;
    }

    public void ResetChecksum() => Checksum = 0;

    ulong FrameFingerprint()
    {
        var hash = 14695981039346656037ul;
        for (var i = 0; i < _positions.Length; i++)
        {
            hash = unchecked((hash ^ _positions[i]) * 1099511628211ul);
            hash = unchecked((hash ^ (uint)BitConverter.SingleToInt32Bits(_effects[i])) * 1099511628211ul);
        }
        hash = unchecked((hash ^ (uint)Last.Moved) * 1099511628211ul);
        hash = unchecked((hash ^ (uint)Last.Skipped) * 1099511628211ul);
        hash = unchecked((hash ^ (uint)Last.Wrapped) * 1099511628211ul);
        return hash;
    }

    static int WaveOf(int row, int crowd) => row * 8 / crowd;

    public void Dispose()
    {
        _set?.Dispose();
        _set = null;
        _asset?.Dispose();
        _asset = null;
    }
}

public static class Oracle
{
    public static void Step(ReadOnlySpan<ushort> before, Span<ushort> after, Span<float> effects, bool forward, ushort duration, bool looping)
    {
        for (var i = 0; i < before.Length; i++)
        {
            var selected = TimelineMovement.Select(new TimelineState(1, before[i]), duration, looping, !forward, out var next, out var tick, out _);
            after[i] = selected ? next.Position : before[i];
            if (selected)
                effects[i] += forward ? BakedLane<ScaleTrack, AmountClip>.Effect(tick) : BakedLane<ScaleTrack, AmountClip>.InverseEffect(tick);
        }
    }

    public static FrameStats Stats(ReadOnlySpan<ushort> before, ReadOnlySpan<ushort> after, ReadOnlySpan<float> effects, bool forward, ushort duration, bool looping)
    {
        var moved = 0;
        var skipped = 0;
        var wrapped = 0;
        var last = (ushort)Math.Max(0, duration - 1);
        for (var i = 0; i < before.Length; i++)
        {
            var b = before[i];
            var a = after[i];
            if (a == b) skipped++;
            else
            {
                moved++;
                if (looping && a == 0 && b == last && forward) wrapped++;
                else if (looping && b == 0 && a == last && !forward) wrapped++;
            }
        }
        var total = 0f;
        for (var i = 0; i < effects.Length; i++) total += effects[i];
        return new FrameStats(moved, skipped, wrapped, total);
    }
}

public static class SmokeRun
{
    public static string Launch(int crowd = 256)
    {
        var scenario = new Scenario { Crowd = crowd };
        var looping = PlayBake.Bake(scenario.AuthoringJson());
        using var engine = new PlayEngine(crowd);
        engine.Load(looping);
        engine.SeedPositions(ScenarioPattern.Staggered);
        var laneDuration = (ushort)engine.Duration;
        engine.ResetChecksum();

        var report = new StringBuilder();
        report.AppendLine($"smoke: bake bytes={looping.Bytes.Length} crowd={crowd} duration={engine.Duration} loop={engine.Loop}");
        var snapshotPositions = engine.Positions.ToArray();
        var snapshotEffects = engine.Effects.ToArray();
        var oracleEffects = new float[crowd];

        RunPhase(engine, forward: true, frames: 40, laneDuration, looping.Loop, oracleEffects, report);
        RunPhase(engine, forward: false, frames: 40, laneDuration, looping.Loop, oracleEffects, report);
        RequireSameSnapshot(engine, snapshotPositions, snapshotEffects, "A-B-A restore");
        report.AppendLine("a-b-a: 40 forward + 40 backward restored the initial snapshot bit-exactly");

        RunPhase(engine, forward: true, frames: 20, laneDuration, looping.Loop, oracleEffects, report);

        var finite = scenario.Clone();
        finite.Loop = false;
        var finiteBake = PlayBake.Bake(finite.AuthoringJson());
        engine.Load(finiteBake);
        var finiteDuration = (ushort)engine.Duration;
        report.AppendLine($"re-bake: loop=false duration={engine.Duration} bytes={finiteBake.Bytes.Length}");
        var parkedBefore = engine.Last.Skipped;
        RunPhase(engine, forward: true, frames: 20, finiteDuration, looping: false, oracleEffects, report);
        Require(engine.Last.Skipped > 0 || parkedBefore > 0, "finite phase parks rows");

        report.AppendLine($"SMOKE PASS frames=120 checksum={engine.Checksum.ToString(CultureInfo.InvariantCulture)}");
        return report.ToString();
    }

    static void RunPhase(PlayEngine engine, bool forward, int frames, ushort duration, bool looping, float[] oracleEffects, StringBuilder report)
    {
        var crowd = engine.Crowd;
        var before = new ushort[crowd];
        var oraclePositions = new ushort[crowd];
        for (var frame = 0; frame < frames; frame++)
        {
            engine.Positions.CopyTo(before);
            var stats = engine.Step(forward);

            Oracle.Step(before, oraclePositions, oracleEffects, forward, duration, looping);
            for (var i = 0; i < crowd; i++)
            {
                if (engine.Positions[i] != oraclePositions[i])
                    Fail($"frame {frame} row {i}: position {engine.Positions[i]} != oracle {oraclePositions[i]}");
                if (BitConverter.SingleToInt32Bits(engine.Effects[i]) != BitConverter.SingleToInt32Bits(oracleEffects[i]))
                    Fail($"frame {frame} row {i}: effect bits {BitConverter.SingleToInt32Bits(engine.Effects[i]):X8} != oracle {BitConverter.SingleToInt32Bits(oracleEffects[i]):X8}");
            }
            var oracleStats = Oracle.Stats(before, oraclePositions, oracleEffects, forward, duration, looping);
            if (stats.Moved != oracleStats.Moved || stats.Skipped != oracleStats.Skipped || stats.Wrapped != oracleStats.Wrapped)
                Fail($"frame {frame}: aggregates {stats.Moved}/{stats.Skipped}/{stats.Wrapped} != oracle {oracleStats.Moved}/{oracleStats.Skipped}/{oracleStats.Wrapped}");
        }
        report.AppendLine($"phase: {frames} frames {(forward ? "forward" : "backward")} bit-exact (moved/skipped/wrapped at last frame: {engine.Last.Moved}/{engine.Last.Skipped}/{engine.Last.Wrapped})");
    }

    static void RequireSameSnapshot(PlayEngine engine, ushort[] positions, float[] effects, string label)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            if (engine.Positions[i] != positions[i])
                Fail($"{label}: position {i} {engine.Positions[i]} != snapshot {positions[i]}");
            if (BitConverter.SingleToInt32Bits(engine.Effects[i]) != BitConverter.SingleToInt32Bits(effects[i]))
                Fail($"{label}: effect bits {i} differ from snapshot");
        }
    }

    static void Require(bool condition, string label)
    {
        if (!condition) Fail(label);
    }

    static void Fail(string message) => throw new InvalidOperationException($"smoke failed: {message}");
}
