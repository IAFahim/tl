using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Tl.Gen.Tlb;

internal sealed class BakedClip(uint start, uint end, object payload, int authoredIndex)
{
    internal uint Start { get; } = start;
    internal uint End { get; } = end;
    internal object Payload { get; } = payload;
    internal int AuthoredIndex { get; } = authoredIndex;
    internal ushort PoolIndex { get; set; }
}

internal interface ITrackBaker
{
    ulong Key { get; }
    byte TrackIndex { get; set; }
    ushort TrackValueIndex { get; set; }
    Type TrackType { get; }
    Type ClipType { get; }
    int TrackValueBytes { get; }
    int ClipValueBytes { get; }
    IReadOnlyList<BakedClip> Clips { get; }
    void AddClip(uint start, uint end, object payload, int authoredIndex);
    byte[] TrackImage();
    byte[] ClipImage(BakedClip clip);
}

internal sealed class TrackBaker<TTrack, TClip>(TTrack trackValue) : ITrackBaker
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    private readonly List<BakedClip> _clips = [];

    public ulong Key => PairRuntime<TTrack, TClip>.Key;
    public byte TrackIndex { get; set; }
    public ushort TrackValueIndex { get; set; }
    public Type TrackType => typeof(TTrack);
    public Type ClipType => typeof(TClip);
    public int TrackValueBytes => Unsafe.SizeOf<TTrack>();
    public int ClipValueBytes => Unsafe.SizeOf<TClip>();
    public IReadOnlyList<BakedClip> Clips => _clips;

    public void AddClip(uint start, uint end, object payload, int authoredIndex)
    {
        _clips.Add(new BakedClip(start, end, (TClip)payload, authoredIndex));
    }

    public byte[] TrackImage()
    {
        var image = new byte[TrackValueBytes];
        MemoryMarshal.Write(image, in trackValue);
        return image;
    }

    public byte[] ClipImage(BakedClip clip)
    {
        var value = (TClip)clip.Payload;
        var image = new byte[ClipValueBytes];
        MemoryMarshal.Write(image, in value);
        return image;
    }
}

public static class TimelineBaker
{
    private static bool IsRemovedPropertyName(string name) =>
        name is "trackType" or "clipType" or "track" or "payload";

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

    private sealed record ClipSpec(string? Name, string Namespace, string Type, string? Assembly, JsonElement? Data, uint Start, uint End);

    private sealed record TrackSpec(string? Name, string Namespace, string Type, string? Assembly, JsonElement? Data, List<ClipSpec> Clips);

    public static byte[] BakeJson(string json, BakerAssemblyResolver? resolver = null)
    {
        resolver ??= new BakerAssemblyResolver();
        return TimelineBakerFast.BakeJsonUtf8(Encoding.UTF8.GetBytes(json), resolver);
    }

    public static byte[] BakeJson(byte[] utf8Json, BakerAssemblyResolver? resolver = null)
    {
        if (!System.Text.Unicode.Utf8.IsValid(utf8Json))
            throw new BakeDiagnosticException($"invalid UTF-8 in authoring JSON at byte {FirstInvalidUtf8Offset(utf8Json)}: the bake input must be valid UTF-8; fix the input file encoding.");
        resolver ??= new BakerAssemblyResolver();
        return TimelineBakerFast.BakeJsonUtf8(utf8Json, resolver);
    }

    private static int FirstInvalidUtf8Offset(byte[] utf8Json)
    {
        var span = utf8Json.AsSpan();
        var offset = 0;
        while (offset < span.Length)
        {
            var status = System.Text.Rune.DecodeFromUtf8(span[offset..], out _, out var consumed);
            if (status != System.Buffers.OperationStatus.Done)
                return offset;
            offset += consumed;
        }
        return span.Length;
    }

    internal static byte[] BakeJsonLegacy(string json, BakerAssemblyResolver? resolver = null)
    {
        resolver ??= new BakerAssemblyResolver();

        CheckDuplicateKeys(json);

        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new BakeDiagnosticException("Root of timeline document must be a JSON object.");

        string? rootName = null;
        uint duration = 0;
        bool loops = false;
        bool hasDuration = false;
        JsonElement tracksElement = default;
        bool hasTracks = false;

        foreach (var prop in root.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "name":
                    rootName = ReadLabel(prop.Value, "root 'name'");
                    break;
                case "duration":
                    if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out duration))
                        throw new BakeDiagnosticException($"wrong-typed value: 'duration' must be an unsigned integer, got '{prop.Value.GetRawText()}'.");
                    hasDuration = true;
                    break;
                case "loop":
                    if (prop.Value.ValueKind != JsonValueKind.True && prop.Value.ValueKind != JsonValueKind.False)
                        throw new BakeDiagnosticException($"wrong-typed value: 'loop' must be a boolean, got '{prop.Value.GetRawText()}'.");
                    loops = prop.Value.GetBoolean();
                    break;
                case "tracks":
                    if (prop.Value.ValueKind != JsonValueKind.Array)
                        throw new BakeDiagnosticException($"wrong-typed value: 'tracks' must be an array, got '{prop.Value.GetRawText()}'.");
                    tracksElement = prop.Value;
                    hasTracks = true;
                    break;
                default:
                    RejectPropertyName(prop.Name, "root");
                    break;
            }
        }

        if (!hasDuration)
            throw new BakeDiagnosticException("Missing required property 'duration'.");
        if (duration > ushort.MaxValue)
            throw new BakeDiagnosticException($"duration {duration} exceeds the 65535-tick lane position column; the typed playback lane cannot bind longer timelines.");
        if (!hasTracks)
            throw new BakeDiagnosticException("Missing required property 'tracks'.");

        var specs = new List<TrackSpec>();
        var trackIndex = 0;
        foreach (var trackObj in tracksElement.EnumerateArray())
        {
            if (trackObj.ValueKind != JsonValueKind.Object)
                throw new BakeDiagnosticException($"Track at index {trackIndex} must be an object.");
            specs.Add(ParseTrack(trackObj, trackIndex));
            trackIndex++;
        }

        return BakeSpecs(specs, rootName, duration, loops, resolver);
    }

    private static void RejectPropertyName(string name, string context)
    {
        if (IsRemovedPropertyName(name))
            throw new BakeDiagnosticException($"removed property '{name}' in {context}: schema v1 declares 'namespace', 'type' and optional 'assembly' on every track and clip, and 'data' for payloads; type identity is never inherited between levels. See docs/data-authored-api.md.");
        if (name is "loops" or "looping")
            throw new BakeDiagnosticException($"renamed property '{name}' in {context}: use 'loop'.");
        throw new BakeDiagnosticException($"unknown field name in track/payload: unknown {context} property '{name}'.");
    }

    private static string ReadLabel(JsonElement value, string context)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw new BakeDiagnosticException($"wrong-typed value: {context} must be a string, got '{value.GetRawText()}'.");
        return value.GetString()!;
    }

    private static TrackSpec ParseTrack(JsonElement trackObj, int trackIndex)
    {
        string? name = null;
        string? ns = null;
        string? type = null;
        string? assembly = null;
        JsonElement dataElement = default;
        bool hasData = false;
        JsonElement clipsElement = default;
        bool hasClips = false;

        foreach (var prop in trackObj.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "name":
                    name = ReadLabel(prop.Value, $"track {trackIndex} 'name'");
                    break;
                case "namespace":
                    ns = ReadBareName(prop.Value, "namespace", $"track {trackIndex}");
                    break;
                case "type":
                    type = ReadBareName(prop.Value, "type", $"track {trackIndex}");
                    break;
                case "assembly":
                    assembly = ReadLabel(prop.Value, $"track {trackIndex} 'assembly'");
                    break;
                case "data":
                    dataElement = prop.Value;
                    hasData = true;
                    break;
                case "clips":
                    if (prop.Value.ValueKind != JsonValueKind.Array)
                        throw new BakeDiagnosticException($"wrong-typed value: 'clips' must be an array, got '{prop.Value.GetRawText()}'.");
                    clipsElement = prop.Value;
                    hasClips = true;
                    break;
                default:
                    RejectPropertyName(prop.Name, $"track {trackIndex}");
                    break;
            }
        }

        if (ns == null)
            throw new BakeDiagnosticException($"missing required property: track {trackIndex} needs 'namespace'.");
        if (type == null)
            throw new BakeDiagnosticException($"missing required property: track {trackIndex} needs 'type'.");
        if (!hasClips)
            throw new BakeDiagnosticException($"Track at index {trackIndex} missing required 'clips' array.");

        var clips = new List<ClipSpec>();
        var clipIndex = 0;
        foreach (var clipObj in clipsElement.EnumerateArray())
        {
            if (clipObj.ValueKind != JsonValueKind.Object)
                throw new BakeDiagnosticException($"Clip {clipIndex} on track {trackIndex} must be an object.");
            clips.Add(ParseClip(clipObj, trackIndex, clipIndex));
            clipIndex++;
        }

        return new TrackSpec(name, ns, type, assembly, hasData ? dataElement : null, clips);
    }

    private static ClipSpec ParseClip(JsonElement clipObj, int trackIndex, int clipIndex)
    {
        string? name = null;
        string? ns = null;
        string? type = null;
        string? assembly = null;
        JsonElement dataElement = default;
        bool hasData = false;
        uint start = 0;
        uint end = 0;
        bool hasStart = false;
        bool hasEnd = false;

        foreach (var prop in clipObj.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "name":
                    name = ReadLabel(prop.Value, $"clip {clipIndex} on track {trackIndex} 'name'");
                    break;
                case "namespace":
                    ns = ReadBareName(prop.Value, "namespace", $"clip {clipIndex} on track {trackIndex}");
                    break;
                case "type":
                    type = ReadBareName(prop.Value, "type", $"clip {clipIndex} on track {trackIndex}");
                    break;
                case "assembly":
                    assembly = ReadLabel(prop.Value, $"clip {clipIndex} on track {trackIndex} 'assembly'");
                    break;
                case "data":
                    dataElement = prop.Value;
                    hasData = true;
                    break;
                case "start":
                    if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out start))
                        throw new BakeDiagnosticException($"wrong-typed value: 'start' must be an unsigned integer, got '{prop.Value.GetRawText()}'.");
                    hasStart = true;
                    break;
                case "end":
                    if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out end))
                        throw new BakeDiagnosticException($"wrong-typed value: 'end' must be an unsigned integer, got '{prop.Value.GetRawText()}'.");
                    hasEnd = true;
                    break;
                default:
                    RejectPropertyName(prop.Name, $"clip {clipIndex} on track {trackIndex}");
                    break;
            }
        }

        if (ns == null)
            throw new BakeDiagnosticException($"missing required property: clip {clipIndex} on track {trackIndex} needs 'namespace' (type identity is never inherited from the track).");
        if (type == null)
            throw new BakeDiagnosticException($"missing required property: clip {clipIndex} on track {trackIndex} needs 'type' (type identity is never inherited from the track).");
        if (!hasStart || !hasEnd)
            throw new BakeDiagnosticException($"Clip {clipIndex} on track {trackIndex} missing 'start' or 'end'.");

        return new ClipSpec(name, ns, type, assembly, hasData ? dataElement : null, start, end);
    }

    private static string ReadBareName(JsonElement value, string field, string context)
    {
        var text = ReadLabel(value, $"{context} '{field}'");
        if (text.Contains('.') || text.Contains(',') || text.Contains('+') || text.Contains('='))
            throw new BakeDiagnosticException($"dotted name rejected: '{field}' in {context} must be a bare name without '.', ',', '+' or '='; got '{text}'. Empty selects the global namespace.");
        return text;
    }

    private static byte[] BakeSpecs(List<TrackSpec> specs, string? rootName, uint duration, bool loops, BakerAssemblyResolver resolver)
    {
        if (specs.Count > 256)
            throw new BakeDiagnosticException($"asset exceeds 256 authored tracks: {specs.Count} track entries is above the TLB1 TrackIndex capacity.");

        var lanes = new List<ITrackBaker>();
        var labels = new List<(int Track, int Clip, string Name)>();
        if (rootName != null)
            labels.Add((-1, -1, rootName));

        var authoredIndex = 0;
        for (var entryIndex = 0; entryIndex < specs.Count; entryIndex++)
        {
            var spec = specs[entryIndex];
            if (spec.Clips.Count == 0 && duration > 0)
                throw new BakeDiagnosticException($"empty tracks with duration > 0 (stages must cover duration): track {entryIndex} has 0 clips.");

            var trackType = resolver.ResolveType(spec.Namespace, spec.Type, spec.Assembly, $"track {entryIndex}");
            object trackInstance = spec.Data is { } trackData
                ? BakerAssemblyResolver.PopulateStruct(trackType, trackData, $"track {entryIndex} ({trackType.Name})")
                : Activator.CreateInstance(trackType)!;

            if (spec.Name != null)
                labels.Add((entryIndex, -1, spec.Name));

            var resolvedClips = new List<(ClipSpec Spec, Type ClipType, object Instance)>();
            var clipIndex = 0;
            foreach (var clip in spec.Clips)
            {
                if (clip.Start >= clip.End)
                    throw new BakeDiagnosticException($"start >= end: clip [{clip.Start}, {clip.End}) on track {entryIndex} is empty or reversed.");
                if (clip.End > duration)
                    throw new BakeDiagnosticException($"clips outside [0, duration]: clip [{clip.Start}, {clip.End}) on track {entryIndex} exceeds timeline duration {duration}.");

                var clipType = resolver.ResolveType(clip.Namespace, clip.Type, clip.Assembly, $"clip {clipIndex} on track {entryIndex}");
                BakerAssemblyResolver.ValidateUnmanaged(trackType, clipType);
                object clipInstance = clip.Data is { } clipData
                    ? BakerAssemblyResolver.PopulateStruct(clipType, clipData, $"clip {clipIndex} on track {entryIndex} ({clipType.Name})")
                    : Activator.CreateInstance(clipType)!;

                resolvedClips.Add((clip, clipType, clipInstance));
                if (clip.Name != null)
                    labels.Add((entryIndex, clipIndex, clip.Name));
                clipIndex++;
            }

            var groupOrder = new List<Type>();
            foreach (var item in resolvedClips)
                if (!groupOrder.Contains(item.ClipType))
                    groupOrder.Add(item.ClipType);

            foreach (var clipType in groupOrder)
                ValidateBlendPairing(trackType, clipType, entryIndex);

            foreach (var clipType in groupOrder)
            {
                var bakerType = typeof(TrackBaker<,>).MakeGenericType(trackType, clipType);
                var baker = (ITrackBaker)Activator.CreateInstance(bakerType, trackInstance)!;
                baker.TrackIndex = (byte)entryIndex;

                var laneClips = new List<(uint Start, uint End, object Payload, int Authored)>();
                for (var i = 0; i < resolvedClips.Count; i++)
                {
                    var (clip, resolvedType, instance) = resolvedClips[i];
                    if (resolvedType != clipType) continue;
                    laneClips.Add((clip.Start, clip.End, instance, authoredIndex + i));
                }

                var sorted = laneClips.OrderBy(c => c.Start).ThenBy(c => c.End).ToArray();
                for (var i = 1; i < sorted.Length; i++)
                {
                    if (sorted[i].Start == sorted[i - 1].Start)
                        throw new BakeDiagnosticException($"overlapping clips on one track: track {entryIndex} ({trackType.Name}/{clipType.Name}) has multiple clips starting at tick {sorted[i].Start}.");
                }

                var events = laneClips
                    .SelectMany(static clip => new[] { (Tick: clip.Start, Delta: 1), (Tick: clip.End, Delta: -1) })
                    .GroupBy(static item => item.Tick)
                    .OrderBy(static group => group.Key);
                var activeClips = 0;
                foreach (var group in events)
                {
                    activeClips += group.Where(static item => item.Delta < 0).Sum(static item => item.Delta);
                    activeClips += group.Where(static item => item.Delta > 0).Sum(static item => item.Delta);
                    if (activeClips > 2)
                        throw new BakeDiagnosticException($"overlapping clips on one track: track {entryIndex} ({trackType.Name}/{clipType.Name}) has more than two overlapping clips at tick {group.Key}.");
                }

                foreach (var c in laneClips)
                    baker.AddClip(c.Start, c.End, c.Payload, c.Authored);

                lanes.Add(baker);
            }

            authoredIndex += resolvedClips.Count;
        }

        return BakeCore(lanes, specs.Count, duration, loops, labels);
    }

    private static void ValidateBlendPairing(Type trackType, Type clipType, int entryIndex)
    {
        var instantiations = new List<Type>();
        foreach (var iface in trackType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IBlend<>))
                instantiations.Add(iface.GetGenericArguments()[0]);
        }

        if (instantiations.Count == 0)
            throw new BakeDiagnosticException($"missing IBlend<>: track type '{trackType.FullName}' on track {entryIndex} must implement Tl.IBlend<TClip>.");

        if (!instantiations.Contains(clipType))
        {
            var names = string.Join(", ", instantiations.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal));
            throw new BakeDiagnosticException($"clip type not blendable by track: track {entryIndex} resolves clip type '{clipType.FullName}' but track type '{trackType.FullName}' implements only these Tl.IBlend<TClip> pairings: {names}.");
        }
    }

    private static byte[] BakeCore(List<ITrackBaker> lanes, int trackEntryCount, uint duration, bool loops, List<(int Track, int Clip, string Name)> labels)
    {
        var cuts = new SortedSet<uint>();
        if (duration != 0)
        {
            cuts.Add(0u);
            cuts.Add(duration);
            foreach (var lane in lanes)
                foreach (var clip in lane.Clips)
                {
                    cuts.Add(clip.Start);
                    cuts.Add(clip.End);
                }
        }

        var boundaries = cuts.ToArray();
        var stageEdges = new List<(uint Start, uint End)>();
        var stageSteps = new List<List<(ITrackBaker Lane, BakedClip[] Covering)>>();
        for (var region = 0; region + 1 < boundaries.Length; region++)
        {
            var edge = boundaries[region];
            var active = new List<(ITrackBaker Lane, BakedClip[] Covering)>();
            foreach (var lane in lanes)
            {
                var covering = lane.Clips
                    .Select((clip, index) => (clip, index))
                    .Where(item => item.clip.Start <= edge && edge < item.clip.End)
                    .OrderBy(item => item.clip.Start).ThenBy(item => item.clip.AuthoredIndex)
                    .Select(item => item.clip)
                    .ToArray();
                if (covering.Length > 0)
                    active.Add((lane, covering));
            }
            active.Sort((a, b) =>
            {
                var byTrack = a.Lane.TrackIndex.CompareTo(b.Lane.TrackIndex);
                if (byTrack != 0) return byTrack;
                return a.Covering.Min(c => c.AuthoredIndex).CompareTo(b.Covering.Min(c => c.AuthoredIndex));
            });
            stageEdges.Add((edge, boundaries[region + 1]));
            stageSteps.Add(active);
        }

        var pairKeys = lanes.Select(lane => lane.Key).Distinct().OrderBy(key => key).ToArray();
        var pairIndex = new Dictionary<ulong, int>();
        for (var index = 0; index < pairKeys.Length; index++)
            pairIndex[pairKeys[index]] = index;

        var pairTypes = new (Type Track, Type Clip)[pairKeys.Length];
        foreach (var lane in lanes)
            pairTypes[pairIndex[lane.Key]] = (lane.TrackType, lane.ClipType);

        var pools = new ValuePoolSet[pairKeys.Length];
        var trackValueBytes = new int[pairKeys.Length];
        var clipValueBytes = new int[pairKeys.Length];
        foreach (var lane in lanes)
        {
            var index = pairIndex[lane.Key];
            if (pools[index] != null) continue;
            var members = lanes.Where(item => pairIndex[item.Key] == index).ToList();
            pools[index] = ValuePoolSet.Build(
                members.Select(item => item.TrackImage()),
                members.SelectMany(item => item.Clips.Select(item.ClipImage)),
                lane.TrackType.FullName ?? lane.TrackType.Name,
                lane.ClipType.FullName ?? lane.ClipType.Name);
            trackValueBytes[index] = lane.TrackValueBytes;
            clipValueBytes[index] = lane.ClipValueBytes;
        }
        foreach (var lane in lanes)
        {
            var pool = pools[pairIndex[lane.Key]];
            lane.TrackValueIndex = pool.TrackIndex[lane.TrackImage()];
            foreach (var clip in lane.Clips)
                clip.PoolIndex = pool.ClipIndex[lane.ClipImage(clip)];
        }

        var pairOffset = TlbLayout.HeaderBytes;
        var stageOffset = pairOffset + TlbLayout.PairEntryBytes * (uint)pairKeys.Length;
        var programBase = stageOffset + TlbLayout.StageEntryBytes * (uint)stageSteps.Count;
        var stepCount = stageSteps.Sum(active => active.Count);
        var poolOffset = TlbLayout.Align16(programBase + TlbLayout.StepBytes * (uint)stepCount);

        var poolSpans = new (uint TrackRel, uint ClipRel, uint TrackBytes, uint ClipBytes, uint TrackCount, uint ClipCount)[pairKeys.Length];
        var poolCursor = poolOffset;
        for (var index = 0; index < pairKeys.Length; index++)
        {
            var pool = pools[index];
            var trackBytes = (uint)pool.TrackEntries.Count * (uint)trackValueBytes[index];
            var clipBytes = (uint)pool.ClipEntries.Count * (uint)clipValueBytes[index];
            var pairAddress = pairOffset + TlbLayout.PairEntryBytes * (uint)index;
            var trackRel = poolCursor - pairAddress;
            poolCursor = TlbLayout.Align16(poolCursor + trackBytes);
            var clipRel = poolCursor - pairAddress;
            poolCursor = TlbLayout.Align16(poolCursor + clipBytes);
            poolSpans[index] = (trackRel, clipRel, trackBytes, clipBytes, (uint)pool.TrackEntries.Count, (uint)pool.ClipEntries.Count);
        }
        var frameOffset = TlbLayout.Align16(poolCursor);

        var occurrences = new List<(uint Offset, int Pair, ITrackBaker Lane, BakedClip[] Covering, int Stage)>();
        var cursor = frameOffset;
        for (var stage = 0; stage < stageSteps.Count; stage++)
            foreach (var (lane, covering) in stageSteps[stage])
            {
                occurrences.Add((cursor, pairIndex[lane.Key], lane, covering, stage));
                cursor += TlbLayout.SlotRowBytes;
            }

        var hotLength = cursor;
        var tail = TlbMetadataBuilder.Build(pairTypes, labels);
        var bytes = new byte[hotLength + tail.Length];

        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), TlbLayout.Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), TlbLayout.Version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), loops ? 1u : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)trackEntryCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageSteps.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), (uint)pairKeys.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), poolOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(40), frameOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), hotLength);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), (uint)bytes.Length);

        for (var index = 0; index < pairKeys.Length; index++)
        {
            var at = (int)pairOffset + 48 * index;
            var (trackRel, clipRel, trackBytes, clipBytes, trackCount, clipCount) = poolSpans[index];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(at), pairKeys[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), TlbLayout.SlotRowBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), trackRel);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), trackCount);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 20), (uint)trackValueBytes[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 24), clipRel);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 28), clipCount);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 32), (uint)clipValueBytes[index]);
        }

        var programOffset = programBase;
        for (var stage = 0; stage < stageSteps.Count; stage++)
        {
            var at = stageOffset + TlbLayout.StageEntryBytes * (uint)stage;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at), stageEdges[stage].Start);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 4), stageEdges[stage].End);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 8), programOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 12), (uint)stageSteps[stage].Count);
            foreach (var occurrence in occurrences.Where(item => item.Stage == stage))
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset), occurrence.Offset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset + 4), (uint)occurrence.Pair);
                programOffset += TlbLayout.StepBytes;
            }
        }

        for (var index = 0; index < pairKeys.Length; index++)
        {
            var pool = pools[index];
            var pairAddress = (int)(pairOffset + TlbLayout.PairEntryBytes * (uint)index);
            var at = pairAddress + (int)poolSpans[index].TrackRel;
            foreach (var image in pool.TrackEntries)
            {
                image.CopyTo(bytes, at);
                at += image.Length;
            }
            at = pairAddress + (int)poolSpans[index].ClipRel;
            foreach (var image in pool.ClipEntries)
            {
                image.CopyTo(bytes, at);
                at += image.Length;
            }
        }

        foreach (var occurrence in occurrences)
        {
            var clips = occurrence.Covering;
            var windowStart = clips.Min(clip => clip.Start);
            var windowEnd = clips.Max(clip => clip.End);
            var factorStart = 0u;
            var factorSpan = 0u;
            if (clips.Length == 2)
            {
                factorStart = Math.Max(clips[0].Start, clips[1].Start);
                factorSpan = Math.Min(clips[0].End, clips[1].End) - factorStart;
            }

            TlbLayout.WriteRow(
                bytes,
                (int)occurrence.Offset,
                occurrence.Lane.TrackValueIndex,
                clips[0].PoolIndex,
                clips.Length == 2 ? clips[1].PoolIndex : TlbLayout.NoClipIndex,
                windowStart,
                windowEnd,
                factorStart,
                factorSpan,
                occurrence.Lane.TrackIndex);
        }

        tail.CopyTo(bytes, hotLength);

        try
        {
            using var asset = TimelineAsset.Load(bytes);
        }
        catch (ArgumentException ex)
        {
            throw new BakeDiagnosticException($"TLB validation failed: {ex.Message}");
        }

        return bytes;
    }
}
