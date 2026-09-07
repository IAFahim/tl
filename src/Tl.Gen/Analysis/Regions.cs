using Tl;
using Tl.Gen.Model;
using Tl.Generation;

namespace Tl.Gen.Analysis;

public static class RegionAnalyzer
{
    public static TimelinePlan Analyze(TimelineDefinition definition)
    {
        TimelineValidator.Validate(definition);

        var cuts = new SortedSet<uint> { 0 };
        foreach (var clip in definition.Clips)
        {
            cuts.Add(clip.Start);
            cuts.Add(clip.End);
        }

        var regionStarts = cuts.ToArray();
        var clipRows = new List<ClipRow>();
        var trackRows = new List<TrackRow>();
        var regionRows = new RegionRow[regionStarts.Length];
        var clipEdges = new ClipEdge[definition.Clips.Count];

        for (var i = 0; i < definition.Clips.Count; i++)
        {
            clipEdges[i] = new ClipEdge(definition.Clips[i].Start, definition.Clips[i].End);
        }

        var maxActive = 0;
        var maxBlends = 0;

        for (var r = 0; r < regionRows.Length; r++)
        {
            var lo = regionStarts[r];
            var rowStart = trackRows.Count;

            for (var t = 0; t < definition.Tracks.Count; t++)
            {
                var first = -1;
                var second = -1;

                for (var c = 0; c < definition.Clips.Count; c++)
                {
                    var clip = definition.Clips[c];
                    if (clip.TrackIndex != t || clip.Start > lo || clip.End <= lo)
                        continue;

                    if (first < 0)
                        first = c;
                    else if (second < 0)
                        second = c;
                }

                if (first < 0)
                    continue;

                var clipStart = clipRows.Count;

                if (second < 0)
                {
                    clipRows.Add(new ClipRow(checked((ushort)first), 0, 0));
                    trackRows.Add(new TrackRow(checked((ushort)t), checked((ushort)clipStart), 1));
                }
                else
                {
                    if (definition.Clips[first].Start > definition.Clips[second].Start)
                        (first, second) = (second, first);
                    var a = definition.Clips[first];
                    var b = definition.Clips[second];
                    var factorStart = a.Start > b.Start ? a.Start : b.Start;
                    var factorEnd = a.End < b.End ? a.End : b.End;
                    clipRows.Add(new ClipRow(checked((ushort)first), factorStart, factorEnd - factorStart));
                    clipRows.Add(new ClipRow(checked((ushort)second), factorStart, factorEnd - factorStart));
                    trackRows.Add(new TrackRow(checked((ushort)t), checked((ushort)clipStart), 2));
                }
            }

            var count = trackRows.Count - rowStart;
            regionRows[r] = new RegionRow(checked((ushort)rowStart), checked((ushort)count));

            if (count > maxActive)
                maxActive = count;

            var blends = 0;
            for (var t = rowStart; t < trackRows.Count; t++)
                if (trackRows[t].ClipCount == 2)
                    blends++;
            if (blends > maxBlends)
                maxBlends = blends;
        }

        var regionFlags = new byte[regionStarts.Length];
        for (var r = 0; r < regionStarts.Length; r++)
        {
            var lo = regionStarts[r];
            var re = r + 1 < regionStarts.Length ? regionStarts[r + 1] : 0;
            byte flag = 0;
            foreach (var edge in clipEdges)
            {
                if (edge.Start == lo)
                    flag |= 1;
                if (edge.End == lo)
                    flag |= 2;
                if (r + 1 < regionStarts.Length && edge.End == re)
                    flag |= 4;
            }
            regionFlags[r] = flag;
        }

        return new TimelinePlan
        {
            Definition = definition,
            RegionStarts = regionStarts,
            RegionRows = regionRows,
            RegionFlags = regionFlags,
            TrackRows = [.. trackRows],
            ClipRows = [.. clipRows],
            ClipEdges = clipEdges,
            MaxActiveTracks = maxActive,
            MaxActiveBlends = maxBlends,
            Duration = regionStarts[^1],
        };
    }
}
