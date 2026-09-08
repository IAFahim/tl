using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl.Generation;
using Tl.Internal;

namespace Tl.Authoring;

internal sealed class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y) => x.AsSpan().SequenceEqual(y);

    public int GetHashCode(byte[] obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj);
        return hash.ToHashCode();
    }
}

internal static class TimelineCompiler
{
    public static ushort Compile<TTrack, TClip>(
        TimelineBuilder<TTrack, TClip>.Authoring authoring,
        Action<Type, Type, Timeline.Entry> binder)
        where TTrack : struct, IBlend<TClip>
        where TClip : unmanaged
    {
        var cuts = new SortedSet<uint> { 0 };
        foreach (var clip in authoring.Clips)
        {
            cuts.Add(clip.Start);
            cuts.Add(clip.End);
        }

        var regionStarts = cuts.ToArray();
        var clipRows = new List<ClipRow>();
        var trackRows = new List<TrackRow>();
        var regionRows = new RegionRow[regionStarts.Length];
        var clipData = new TClip[authoring.Clips.Count];
        var clipEdges = new ClipEdge[authoring.Clips.Count];

        for (var i = 0; i < authoring.Clips.Count; i++)
        {
            clipData[i] = authoring.Clips[i].Clip;
            clipEdges[i] = new ClipEdge(authoring.Clips[i].Start, authoring.Clips[i].End);
        }

        var maxActive = 0;
        var maxBlends = 0;

        for (var r = 0; r < regionRows.Length; r++)
        {
            var lo = regionStarts[r];
            var rowStart = trackRows.Count;

            for (var t = 0; t < authoring.Tracks.Count; t++)
            {
                var first = -1;
                var second = -1;

                for (var c = 0; c < authoring.Clips.Count; c++)
                {
                    var clip = authoring.Clips[c];
                    if (clip.Track != t || clip.Start > lo || clip.End <= lo)
                        continue;

                    if (first < 0)
                        first = c;
                    else if (second < 0)
                        second = c;
                    else
                        throw new NotSupportedException("At most two overlapping clips per track per region.");
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
                    if (authoring.Clips[first].Start > authoring.Clips[second].Start)
                        (first, second) = (second, first);
                    var a = authoring.Clips[first];
                    var b = authoring.Clips[second];
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

        ushort[] payloadMap;
        if (authoring.Options.DedupStorage)
        {
            var comparer = new ByteArrayComparer();
            var unique = new Dictionary<byte[], ushort>(comparer);
            var slots = new List<TClip>();
            payloadMap = new ushort[authoring.Clips.Count];
            for (var i = 0; i < authoring.Clips.Count; i++)
            {
                var bytes = new byte[Unsafe.SizeOf<TClip>()];
                var authored = authoring.Clips[i].Clip;
                MemoryMarshal.Write(bytes, in authored);
                if (unique.TryGetValue(bytes, out var slot))
                {
                    payloadMap[i] = slot;
                    continue;
                }

                slot = checked((ushort)slots.Count);
                unique.Add(bytes, slot);
                slots.Add(authoring.Clips[i].Clip);
                payloadMap[i] = slot;
            }

            clipData = [.. slots];
        }
        else
        {
            payloadMap = [];
        }

        TrackRow[] compactTracks;
        if (authoring.Options.DedupStorage)
        {
            var rowComparer = new ByteArrayComparer();
            var clipRuns = new Dictionary<byte[], ushort>(rowComparer);
            var keptClips = new List<ClipRow>(clipRows.Count);
            var remapped = new TrackRow[trackRows.Count];
            for (var i = 0; i < trackRows.Count; i++)
            {
                var row = trackRows[i];
                var count = row.ClipCount;
                var key = new byte[4 + Unsafe.SizeOf<ClipRow>() * count];
                var trackIndex = row.TrackIndex;
                MemoryMarshal.Write(key.AsSpan(0, 2), in trackIndex);
                MemoryMarshal.Write(key.AsSpan(2, 2), in count);
                MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(clipRows).Slice(row.ClipStart, count))
                    .CopyTo(key.AsSpan(4));
                if (clipRuns.TryGetValue(key, out var canonical))
                {
                    remapped[i] = new TrackRow(row.TrackIndex, canonical, count);
                    continue;
                }

                canonical = checked((ushort)keptClips.Count);
                clipRuns.Add(key, canonical);
                for (var c = 0; c < count; c++)
                    keptClips.Add(clipRows[row.ClipStart + c]);
                remapped[i] = new TrackRow(row.TrackIndex, canonical, count);
            }

            clipRows = keptClips;

            var regionSlices = new Dictionary<byte[], ushort>(rowComparer);
            var keptTracks = new List<TrackRow>(trackRows.Count);
            var sliceBytes = new byte[Unsafe.SizeOf<TrackRow>()];
            for (var r = 0; r < regionRows.Length; r++)
            {
                var row = regionRows[r];
                var key = new byte[Unsafe.SizeOf<TrackRow>() * row.TrackCount];
                for (var t = 0; t < row.TrackCount; t++)
                {
                    var source = remapped[row.TrackStart + t];
                    MemoryMarshal.Write(sliceBytes.AsSpan(0, Unsafe.SizeOf<TrackRow>()), in source);
                    sliceBytes.AsSpan(0, Unsafe.SizeOf<TrackRow>()).CopyTo(key.AsSpan(Unsafe.SizeOf<TrackRow>() * t));
                }

                if (regionSlices.TryGetValue(key, out var canonical))
                {
                    regionRows[r] = new RegionRow(canonical, row.TrackCount);
                    continue;
                }

                canonical = checked((ushort)keptTracks.Count);
                regionSlices.Add(key, canonical);
                for (var t = 0; t < row.TrackCount; t++)
                    keptTracks.Add(remapped[row.TrackStart + t]);
                regionRows[r] = new RegionRow(canonical, row.TrackCount);
            }

            compactTracks = [.. keptTracks];
        }
        else
        {
            compactTracks = [.. trackRows];
        }

        // Build-time region materialization: one WorkSlot per final track
        // row (post-dedup, so aliased row runs share aliased slots), region
        // rows referencing them by slice. Per tick, the engine slices this
        // table — no per-call materialization work at all.
        var workSlots = PlaybackCore.MaterializeWorkSlots(
            compactTracks, CollectionsMarshal.AsSpan(clipRows), payloadMap, clipEdges, regionRows);

        return Timeline.Register(new Timeline.Entry
        {
            RegionStarts = regionStarts,
            RegionRows = regionRows,
            TrackRows = compactTracks,
            ClipRows = [.. clipRows],
            ClipEdges = clipEdges,
            WorkSlots = workSlots,
            Payload = new Timeline<TTrack, TClip>.Tables
            {
                TrackData = [.. authoring.Tracks],
                ClipData = clipData,
                PayloadMap = payloadMap,
            },
            MaxActiveTracks = maxActive,
            MaxActiveBlends = maxBlends,
            Loops = authoring.Loops,
            Duration = regionStarts[^1],
            Binder = binder,
        });
    }
}
