namespace Tl.Fuzz;

public static class FuzzAssetGen
{
    public static readonly ushort[] Durations = [1, 2, 3, 7, 8, 15, 16, 17, 31, 63, 255];

    public static FuzzModel Next(FuzzRandom random, out ushort duration, out bool looping, out float scale, out FuzzClipSpec[] clips)
    {
        duration = random.Pick(Durations);
        looping = random.NextBool();
        scale = (1 + random.NextInt(7)) * 0.25f;
        var candidates = new List<FuzzClipSpec>();
        var count = random.NextInt(5);
        for (var i = 0; i < count; i++)
        {
            var start = random.NextInt(duration);
            var end = Math.Min((int)duration, start + 1 + random.NextInt(Math.Min((int)duration, 12)));
            if (end <= start) continue;
            candidates.Add(new FuzzClipSpec((uint)start, (uint)end, 1f + i * 0.5f));
        }
        var accepted = new List<FuzzClipSpec>();
        foreach (var candidate in candidates)
            if (ConcurrentMax(accepted, candidate) <= 1)
                accepted.Add(candidate);
        clips = accepted.OrderBy(clip => clip.Start).ThenBy(clip => clip.End).ToArray();
        return new FuzzModel(duration, looping, scale, clips);
    }

    public static FuzzModel OverlapPair(ushort duration, bool looping, float scale)
    {
        var firstEnd = Math.Max(1u, duration / 2u);
        var secondStart = Math.Min(Math.Max(1u, firstEnd - 1u), duration == 0 ? 0u : duration - 1u);
        var clips = new List<FuzzClipSpec> { new(0u, firstEnd, 1f) };
        if (duration > secondStart) clips.Add(new FuzzClipSpec(secondStart, duration, 2f));
        return new FuzzModel(duration, looping, scale, clips);
    }

    static int ConcurrentMax(List<FuzzClipSpec> accepted, FuzzClipSpec candidate)
    {
        var max = 0;
        for (var tick = candidate.Start; tick < candidate.End; tick++)
        {
            var concurrent = 1;
            foreach (var clip in accepted)
                if (clip.Start <= tick && tick < clip.End)
                    concurrent++;
            max = Math.Max(max, concurrent);
        }
        return max;
    }
}
