using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Tl.Bake;

[StructLayout(LayoutKind.Sequential)]
public struct Slot<TTrack, TClip> 
    where TTrack : unmanaged, IBlend<TClip> 
    where TClip : unmanaged
{
    public TTrack Track;
    public TClip First;
    public TClip Second;
    public uint WindowStart;
    public uint WindowEnd;
    public uint FactorStart;
    public uint FactorSpan;
    public byte TrackIndex;
}

internal sealed record BakedClip(uint Start, uint End, object Payload);

internal interface ITrackBaker
{
    ulong Key { get; }
    uint Stride { get; }
    byte Index { get; set; }
    IReadOnlyList<BakedClip> Clips { get; }
    void AddClip(uint start, uint end, object payload);
    void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan);
}

internal sealed class TrackBaker<TTrack, TClip>(TTrack trackValue) : ITrackBaker
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    private readonly List<BakedClip> _clips = [];

    public ulong Key => PairRuntime<TTrack, TClip>.Key;
    public uint Stride => (uint)((Unsafe.SizeOf<Slot<TTrack, TClip>>() + 15) & ~15);
    public byte Index { get; set; }
    public IReadOnlyList<BakedClip> Clips => _clips;

    public void AddClip(uint start, uint end, object payload)
    {
        _clips.Add(new BakedClip(start, end, (TClip)payload));
    }

    public void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan)
    {
        var slot = new Slot<TTrack, TClip>
        {
            Track = trackValue,
            First = (TClip)first!.Payload,
            Second = second is null ? default : (TClip)second.Payload,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            FactorStart = factorStart,
            FactorSpan = factorSpan,
            TrackIndex = Index,
        };
        MemoryMarshal.Write(bytes.AsSpan(offset), in slot);
    }
}

public static class TimelineBaker
{
    public static void CheckDuplicateKeys(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        var scopeStack = new Stack<HashSet<string>>();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    scopeStack.Push(new HashSet<string>(StringComparer.Ordinal));
                    break;
                case JsonTokenType.EndObject:
                    scopeStack.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    var propName = reader.GetString()!;
                    var currentScope = scopeStack.Peek();
                    if (!currentScope.Add(propName))
                    {
                        var (line, col) = GetLineAndColumn(bytes, reader.TokenStartIndex);
                        throw new BakeDiagnosticException($"duplicate field: '{propName}'", line, col);
                    }
                    break;
            }
        }
    }

    private static (int Line, int Column) GetLineAndColumn(ReadOnlySpan<byte> utf8, long bytesConsumed)
    {
        int line = 1;
        int col = 1;
        long count = Math.Min(bytesConsumed, utf8.Length);
        for (int i = 0; i < count; i++)
        {
            if (utf8[i] == (byte)'\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
        }
        return (line, col);
    }

    public static byte[] BakeJson(string json, BakerAssemblyResolver? resolver = null)
    {
        resolver ??= new BakerAssemblyResolver();

        CheckDuplicateKeys(json);

        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new BakeDiagnosticException("Root of timeline document must be a JSON object.");

        uint duration = 0;
        bool loops = false;
        bool hasDuration = false;
        JsonElement tracksElement = default;
        bool hasTracks = false;

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, "duration", StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out duration))
                    throw new BakeDiagnosticException($"wrong-typed value: 'duration' must be an unsigned integer, got '{prop.Value.GetRawText()}'.");
                hasDuration = true;
            }
            else if (string.Equals(prop.Name, "loop", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(prop.Name, "loops", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(prop.Name, "looping", StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind != JsonValueKind.True && prop.Value.ValueKind != JsonValueKind.False)
                    throw new BakeDiagnosticException($"wrong-typed value: 'loop' must be a boolean, got '{prop.Value.GetRawText()}'.");
                loops = prop.Value.GetBoolean();
            }
            else if (string.Equals(prop.Name, "tracks", StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                    throw new BakeDiagnosticException($"wrong-typed value: 'tracks' must be an array, got '{prop.Value.GetRawText()}'.");
                tracksElement = prop.Value;
                hasTracks = true;
            }
            else
            {
                throw new BakeDiagnosticException($"unknown field name in track/payload: unknown root property '{prop.Name}'.");
            }
        }

        if (!hasDuration)
            throw new BakeDiagnosticException("Missing required property 'duration'.");
        if (!hasTracks)
            throw new BakeDiagnosticException("Missing required property 'tracks'.");

        var bakedTracks = new List<ITrackBaker>();

        var trackIndex = 0;
        foreach (var trackObj in tracksElement.EnumerateArray())
        {
            if (trackObj.ValueKind != JsonValueKind.Object)
                throw new BakeDiagnosticException($"Track at index {trackIndex} must be an object.");

            string? trackTypeName = null;
            string? clipTypeName = null;
            JsonElement trackDataElement = default;
            bool hasTrackData = false;
            JsonElement clipsElement = default;
            bool hasClips = false;

            foreach (var prop in trackObj.EnumerateObject())
            {
                if (string.Equals(prop.Name, "trackType", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(prop.Name, "type", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind != JsonValueKind.String)
                        throw new BakeDiagnosticException($"wrong-typed value: 'trackType' must be a string, got '{prop.Value.GetRawText()}'.");
                    trackTypeName = prop.Value.GetString();
                }
                else if (string.Equals(prop.Name, "clipType", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind != JsonValueKind.String)
                        throw new BakeDiagnosticException($"wrong-typed value: 'clipType' must be a string, got '{prop.Value.GetRawText()}'.");
                    clipTypeName = prop.Value.GetString();
                }
                else if (string.Equals(prop.Name, "track", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                {
                    trackDataElement = prop.Value;
                    hasTrackData = true;
                }
                else if (string.Equals(prop.Name, "clips", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind != JsonValueKind.Array)
                        throw new BakeDiagnosticException($"wrong-typed value: 'clips' must be an array, got '{prop.Value.GetRawText()}'.");
                    clipsElement = prop.Value;
                    hasClips = true;
                }
                else
                {
                    throw new BakeDiagnosticException($"unknown field name in track/payload: unknown track property '{prop.Name}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(trackTypeName))
                throw new BakeDiagnosticException($"Track at index {trackIndex} missing required 'trackType'.");
            if (!hasClips)
                throw new BakeDiagnosticException($"Track at index {trackIndex} missing required 'clips' array.");

            var trackType = resolver.ResolveTrackType(trackTypeName);
            var clipType = resolver.ResolveClipType(trackType, clipTypeName);
            BakerAssemblyResolver.ValidateUnmanaged(trackType, clipType);

            object trackInstance;
            if (hasTrackData)
            {
                trackInstance = BakerAssemblyResolver.PopulateStruct(trackType, trackDataElement, $"track {trackIndex} ({trackType.Name})");
            }
            else
            {
                trackInstance = Activator.CreateInstance(trackType)!;
            }

            var bakerType = typeof(TrackBaker<,>).MakeGenericType(trackType, clipType);
            var baker = (ITrackBaker)Activator.CreateInstance(bakerType, trackInstance)!;
            baker.Index = (byte)trackIndex;

            var clipIndex = 0;
            var clipCount = 0;
            var parsedClips = new List<(uint Start, uint End, object Payload)>();

            foreach (var clipObj in clipsElement.EnumerateArray())
            {
                if (clipObj.ValueKind != JsonValueKind.Object)
                    throw new BakeDiagnosticException($"Clip {clipIndex} on track {trackIndex} must be an object.");

                uint start = 0;
                uint end = 0;
                bool hasStart = false;
                bool hasEnd = false;
                JsonElement payloadElement = default;
                bool hasPayload = false;

                foreach (var prop in clipObj.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "start", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out start))
                            throw new BakeDiagnosticException($"wrong-typed value: 'start' must be an unsigned integer, got '{prop.Value.GetRawText()}'.");
                        hasStart = true;
                    }
                    else if (string.Equals(prop.Name, "end", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out end))
                            throw new BakeDiagnosticException($"wrong-typed value: 'end' must be an unsigned integer, got '{prop.Value.GetRawText()}'.");
                        hasEnd = true;
                    }
                    else if (string.Equals(prop.Name, "payload", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                    {
                        payloadElement = prop.Value;
                        hasPayload = true;
                    }
                    else
                    {
                        throw new BakeDiagnosticException($"unknown field name in track/payload: unknown clip property '{prop.Name}'.");
                    }
                }

                if (!hasStart || !hasEnd)
                    throw new BakeDiagnosticException($"Clip {clipIndex} on track {trackIndex} missing 'start' or 'end'.");

                if (start >= end)
                    throw new BakeDiagnosticException($"start >= end: clip [{start}, {end}) on track {trackIndex} is empty or reversed.");

                if (end > duration)
                    throw new BakeDiagnosticException($"clips outside [0, duration]: clip [{start}, {end}) on track {trackIndex} exceeds timeline duration {duration}.");

                object clipInstance;
                if (hasPayload)
                {
                    clipInstance = BakerAssemblyResolver.PopulateStruct(clipType, payloadElement, $"clip {clipIndex} on track {trackIndex} ({clipType.Name})");
                }
                else
                {
                    clipInstance = Activator.CreateInstance(clipType)!;
                }

                parsedClips.Add((start, end, clipInstance));
                clipCount++;
                clipIndex++;
            }

            if (clipCount == 0 && duration > 0)
            {
                throw new BakeDiagnosticException($"empty tracks with duration > 0 (stages must cover duration): track {trackIndex} has 0 clips.");
            }

            var sortedClips = parsedClips.OrderBy(c => c.Start).ThenBy(c => c.End).ToArray();
            for (int i = 1; i < sortedClips.Length; i++)
            {
                if (sortedClips[i].Start == sortedClips[i - 1].Start)
                {
                    throw new BakeDiagnosticException($"overlapping clips on one track: track {trackIndex} has multiple clips starting at tick {sortedClips[i].Start}.");
                }
            }

            var events = parsedClips
                .SelectMany(static clip => new[] { (Tick: clip.Start, Delta: 1), (Tick: clip.End, Delta: -1) })
                .GroupBy(static item => item.Tick)
                .OrderBy(static group => group.Key);
            var activeClips = 0;
            foreach (var group in events)
            {
                activeClips += group.Where(static item => item.Delta < 0).Sum(static item => item.Delta);
                activeClips += group.Where(static item => item.Delta > 0).Sum(static item => item.Delta);
                if (activeClips > 2)
                {
                    throw new BakeDiagnosticException($"overlapping clips on one track: track {trackIndex} has more than two overlapping clips at tick {group.Key}.");
                }
            }

            foreach (var c in parsedClips)
            {
                baker.AddClip(c.Start, c.End, c.Payload);
            }

            bakedTracks.Add(baker);
            trackIndex++;
        }

        return BakeCore(bakedTracks, duration, loops);
    }

    private static byte[] BakeCore(List<ITrackBaker> tracks, uint duration, bool loops)
    {
        var cuts = new SortedSet<uint>();
        if (duration != 0)
        {
            cuts.Add(0u);
            cuts.Add(duration);
            foreach (var track in tracks)
                foreach (var clip in track.Clips)
                {
                    cuts.Add(clip.Start);
                    cuts.Add(clip.End);
                }
        }

        var boundaries = cuts.ToArray();
        var stageList = new List<List<ITrackBaker>>();
        var stageEdges = new List<(uint Start, uint End)>();
        for (var region = 0; region + 1 < boundaries.Length; region++)
        {
            var edge = boundaries[region];
            var active = new List<ITrackBaker>();
            foreach (var track in tracks)
                if (track.Clips.Any(clip => clip.Start <= edge && edge < clip.End))
                    active.Add(track);
            stageList.Add(active);
            stageEdges.Add((edge, boundaries[region + 1]));
        }

        var pairs = tracks.Select(track => (track.Key, track.Stride)).Distinct().OrderBy(pair => pair.Key).ToArray();
        var pairIndex = new Dictionary<ulong, int>();
        for (var index = 0; index < pairs.Length; index++)
            pairIndex[pairs[index].Key] = index;

        var pairOffset = 48u;
        var stageOffset = pairOffset + 16u * (uint)pairs.Length;
        var programBase = stageOffset + 16u * (uint)stageList.Count;
        var stepCount = stageList.Sum(active => active.Count);
        var frameOffset = (programBase + 8u * (uint)stepCount + 15u) & ~15u;

        var occurrences = new List<(uint Offset, int Pair, ITrackBaker Track, int Stage)>();
        var cursor = frameOffset;
        for (var stage = 0; stage < stageList.Count; stage++)
            foreach (var track in stageList[stage])
            {
                occurrences.Add((cursor, pairIndex[track.Key], track, stage));
                cursor += pairs[pairIndex[track.Key]].Stride;
            }

        var bytes = new byte[cursor];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x31424C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), loops ? 1u : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)tracks.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageList.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), (uint)pairs.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), frameOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), (uint)bytes.Length);

        for (var index = 0; index < pairs.Length; index++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan((int)pairOffset + 16 * index), pairs[index].Key);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 16 * index + 8), pairs[index].Stride);
        }

        var programOffset = programBase;
        for (var stage = 0; stage < stageList.Count; stage++)
        {
            var at = stageOffset + 16u * (uint)stage;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at), stageEdges[stage].Start);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 4), stageEdges[stage].End);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 8), programOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 12), (uint)stageList[stage].Count);
            foreach (var occurrence in occurrences.Where(item => item.Stage == stage))
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset), occurrence.Offset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset + 4), (uint)occurrence.Pair);
                programOffset += 8u;
            }
        }

        foreach (var occurrence in occurrences)
        {
            var edge = stageEdges[occurrence.Stage].Start;
            var clips = occurrence.Track.Clips
                .Select((clip, index) => (clip, index))
                .Where(item => item.clip.Start <= edge && edge < item.clip.End)
                .OrderBy(item => item.clip.Start).ThenBy(item => item.index)
                .Select(item => item.clip)
                .ToArray();
            var windowStart = clips.Min(clip => clip.Start);
            var windowEnd = clips.Max(clip => clip.End);
            var factorStart = 0u;
            var factorSpan = 0u;
            if (clips.Length == 2)
            {
                factorStart = Math.Max(clips[0].Start, clips[1].Start);
                factorSpan = Math.Min(clips[0].End, clips[1].End) - factorStart;
            }

            occurrence.Track.Write(bytes, (int)occurrence.Offset, clips[0], clips.Length == 2 ? clips[1] : null, windowStart, windowEnd, factorStart, factorSpan);
        }

        try
        {
            using var asset = TimelineAsset.Load(bytes);
        }
        catch (ArgumentException ex)
        {
            throw new BakeDiagnosticException($"TLB1 validation failed: {ex.Message}");
        }

        return bytes;
    }
}
