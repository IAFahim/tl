using Tl.Gen.Model;

namespace Tl.Gen.Analysis;

public static class TimelineValidator
{
    public static void Validate(TimelineDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new ArgumentException("Timeline name must not be empty.", nameof(definition));

        if (definition.Tracks.Count >= ushort.MaxValue)
            throw new InvalidOperationException("Track capacity exceeded.");

        if (definition.Clips.Count >= ushort.MaxValue)
            throw new InvalidOperationException("Clip capacity exceeded.");

        for (var i = 0; i < definition.Clips.Count; i++)
        {
            var clip = definition.Clips[i];
            if (clip.TrackIndex >= definition.Tracks.Count)
                throw new ArgumentOutOfRangeException(nameof(definition), $"Clip {i} references invalid track index {clip.TrackIndex}.");
            if (clip.End <= clip.Start)
                throw new ArgumentOutOfRangeException(nameof(definition), $"Clip {i} end ({clip.End}) must be greater than start ({clip.Start}).");
        }

        // Validate at most 2 clips overlap on any track
        var cuts = new SortedSet<uint> { 0 };
        foreach (var c in definition.Clips)
        {
            cuts.Add(c.Start);
            cuts.Add(c.End);
        }

        var regionStarts = cuts.ToArray();
        for (var r = 0; r < regionStarts.Length; r++)
        {
            var lo = regionStarts[r];
            for (var t = 0; t < definition.Tracks.Count; t++)
            {
                var count = 0;
                foreach (var c in definition.Clips)
                {
                    if (c.TrackIndex == t && c.Start <= lo && c.End > lo)
                    {
                        count++;
                        if (count > 2)
                            throw new NotSupportedException($"Track {t} has more than 2 overlapping clips at tick {lo}.");
                    }
                }
            }
        }
    }
}
