using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TypedBakeTests
{
    static string Golden(params string[] parts)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine([dir, "golden", .. parts]);
    }

    [Theory]
    [InlineData("fixtures", "minimal.json")]
    [InlineData("fixtures", "blended.json")]
    [InlineData("fixtures", "v2_hold_markers.json")]
    public void TypedBakeEqualsJsonBake(string folder, string file)
    {
        var json = File.ReadAllBytes(Golden(folder, file));
        var resolver = new BakerAssemblyResolver();
        Assert.Equal(TimelineBaker.BakeJson(json, resolver), TimelineBaker.BakeTyped(Typed(json, resolver, null), resolver));
    }

    [Fact]
    public void TypedBakeEqualsJsonBakeWithLabels()
    {
        var json = System.Text.Encoding.UTF8.GetBytes(Recording.OracleJson);
        var resolver = new BakerAssemblyResolver();
        Assert.Equal(TimelineBaker.BakeJson(json, resolver), TimelineBaker.BakeTyped(Typed(json, resolver, null), resolver));
    }

    [Fact]
    public void TypedBakeEqualsJsonBakeForTheKindLibrary()
    {
        var resolver = new BakerAssemblyResolver([typeof(TypedBakeTests).Assembly.Location]);
        var manifest = new LibraryManifest();
        string[] names = ["jump", "potion_hop", "run", "stomp"];
        for (var i = 0; i < names.Length; i++) manifest.Timelines[names[i]] = (ushort)(i + 1);
        manifest.Channels["locomotion"] = 1;
        manifest.Channels["action"] = 2;
        foreach (var name in names)
        {
            var json = File.ReadAllBytes(Golden("kinds", name + ".json"));
            Assert.Equal(TimelineBaker.BakeJson(json, resolver, false, manifest), TimelineBaker.BakeTyped(Typed(json, resolver, manifest), resolver, manifest));
        }
    }

    [Fact]
    public void TypedBakeRejectsWrongDataSize()
    {
        var timeline = new TypedTimeline { Duration = 4 };
        var track = new TypedTrack { Type = typeof(Tlb.EchoTrack) };
        track.Clips.Add(new TypedClip { Type = typeof(Tlb.EchoClip), Start = 0, End = 4, Data = [1] });
        timeline.Tracks.Add(track);
        var error = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeTyped(timeline));
        Assert.Contains("wrong data size", error.Message);
    }

    static TypedTimeline Typed(byte[] json, BakerAssemblyResolver resolver, LibraryManifest? manifest)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var timeline = new TypedTimeline
        {
            Name = root.TryGetProperty("name", out var n) ? n.GetString() : null,
            Duration = root.GetProperty("duration").GetUInt32(),
            Loops = root.TryGetProperty("loop", out var l) && l.GetBoolean(),
            Hold = root.TryGetProperty("hold", out var h) && h.GetBoolean(),
            Next = root.TryGetProperty("next", out var x) ? x.GetString() : null,
            Channel = root.TryGetProperty("channel", out var c) ? c.GetString() : null,
        };
        foreach (var t in root.GetProperty("tracks").EnumerateArray())
        {
            var trackType = TypeOf(t, resolver);
            var track = new TypedTrack { Name = Name(t), Type = trackType, Data = Bytes(trackType, t, manifest) };
            foreach (var k in t.GetProperty("clips").EnumerateArray())
            {
                var clipType = TypeOf(k, resolver);
                track.Clips.Add(new TypedClip { Name = Name(k), Type = clipType, Start = k.GetProperty("start").GetUInt32(), End = k.GetProperty("end").GetUInt32(), Data = Bytes(clipType, k, manifest) });
            }
            timeline.Tracks.Add(track);
        }
        if (root.TryGetProperty("markers", out var markers))
            foreach (var m in markers.EnumerateArray())
            {
                var markerType = TypeOf(m, resolver);
                timeline.Markers.Add(new TypedMarker { Type = markerType, Tick = m.GetProperty("tick").GetUInt32(), Data = Bytes(markerType, m, manifest) });
            }
        return timeline;
    }

    static string? Name(JsonElement e) => e.TryGetProperty("name", out var n) ? n.GetString() : null;

    static Type TypeOf(JsonElement e, BakerAssemblyResolver resolver)
        => resolver.ResolveType(e.GetProperty("namespace").GetString()!, e.GetProperty("type").GetString()!, null, "test");

    static byte[] Bytes(Type type, JsonElement e, LibraryManifest? manifest)
    {
        var box = Activator.CreateInstance(type)!;
        if (e.TryGetProperty("data", out var data))
            foreach (var p in data.EnumerateObject())
            {
                var field = type.GetField(p.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
                object value = field.FieldType == typeof(bool) ? p.Value.GetBoolean()
                    : field.FieldType == typeof(TimelineId) ? new TimelineId(manifest!.Timelines[p.Value.GetString()!])
                    : field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, p.Value.GetInt64())
                    : Convert.ChangeType(p.Value.GetDouble(), field.FieldType);
                field.SetValue(box, value);
            }
        return (byte[])typeof(TypedBakeTests).GetMethod(nameof(Raw), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type).Invoke(null, [box])!;
    }

    static byte[] Raw<T>(object box) where T : unmanaged
    {
        var value = (T)box;
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).ToArray();
    }
}
