using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace PortableJson;

public sealed class JsonDataTests
{
    const string Curve = "[1,0.9,0.8,0.7,0.6,0.5,0.4,0.3,0.2,0.1,0,0,0,0,0,0]";
    static string Document(string data) => """
        {"schema":1,"name":"nested","duration":4,"tracks":[{"namespace":"PortableJson","type":"Settings","data":{"Origin":{"x":2,"y":3,"z":4}},"clips":[{"namespace":"PortableJson","type":"Clip","start":0,"end":4,"data":
        """ + data + "}]}]}";

    [Fact]
    public unsafe void NestedCurveAndFixedArrayUseTheTypedPackerBytes()
    {
        var json = Document("{\"Direction\":{\"x\":1,\"y\":2,\"z\":3},\"Falloff\":" + Curve + ",\"Counts\":[7,8,9]}");
        var clip = new Clip { Direction = new Vector { x = 1, y = 2, z = 3 } };
        using var curve = JsonDocument.Parse(Curve);
        for (var i = 0; i < TlCurve.Samples; i++) clip.Falloff[i] = curve.RootElement[i].GetSingle();
        for (var i = 0; i < 3; i++) clip.Counts[i] = 7 + i;
        var typed = new TypedTimeline { Name = "nested", Duration = 4 };
        var track = new TypedTrack { Type = typeof(Settings), Data = Bytes(new Settings { Origin = new Vector { x = 2, y = 3, z = 4 } }) };
        track.Clips.Add(new TypedClip { Type = typeof(Clip), Start = 0, End = 4, Data = Bytes(clip) });
        typed.Tracks.Add(track);
        var expected = TimelineBaker.BakeTyped(typed);
        Assert.Equal(expected, TimelineBaker.BakeJson(json));
        Assert.Equal(expected, TimelineBaker.BakeJsonBatch([Encoding.UTF8.GetBytes(json)])[0]);
    }

    [Fact]
    public void SchemaDoesNotChangeTheBinaryFormat()
    {
        var json = Document("{}");
        Assert.Equal(TimelineBaker.BakeJson(json.Replace("\"schema\":1,", "")), TimelineBaker.BakeJson(json));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("2")]
    [InlineData("\"1\"")]
    [InlineData("null")]
    [InlineData("{}")]
    public void UnknownOrWrongTypedSchemaFails(string version)
    {
        var error = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(Document("{}").Replace("\"schema\":1", "\"schema\":" + version)));
        Assert.Contains("schema", error.Message);
    }

    [Theory]
    [InlineData("{\"Direction\":{\"x\":1,\"x\":2}}", "duplicate")]
    [InlineData("{\"Direction\":{\"w\":1}}", "unknown field")]
    [InlineData("{\"Direction\":[]}", "expected object")]
    [InlineData("{\"Falloff\":[1,2]}", "exactly 16")]
    [InlineData("{\"Counts\":[1,2]}", "fixed array of 3")]
    [InlineData("{\"Falloff\":[1e39,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]}", "expected float")]
    public void NestedInputFailsWithAUsefulLocation(string data, string expected)
    {
        var error = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(Document(data)));
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public unsafe void CanonicalValuesRoundTripCurvesFixedArraysAndNestedStructs()
    {
        var original = new Clip { Direction = new Vector { x = -0f, y = 1.25f, z = -8 }, Falloff = TlCurve.Constant(0.75f) };
        original.Counts[0] = int.MinValue;
        original.Counts[1] = 42;
        original.Counts[2] = int.MaxValue;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) ValueJson.Write(writer, typeof(Clip), original);
        using var document = JsonDocument.Parse(stream.ToArray());
        var copy = (Clip)ValueJson.Read(typeof(Clip), document.RootElement, "clip");
        Assert.Equal(Bytes(original), Bytes(copy));
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("blended")]
    [InlineData("v2_hold_markers")]
    public void DecodeRebakesTheExistingGoldenBytes(string name)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "golden", "fixtures");
        var bytes = TimelineBaker.BakeJson(File.ReadAllBytes(Path.Combine(directory, name + ".json")));
        var exported = TimelineJson.Decode(bytes);
        var rebaked = TimelineBaker.BakeJson(exported);
        Assert.Equal(bytes, rebaked);
        Assert.Equal(exported, TimelineJson.Decode(rebaked));
    }

    [Fact]
    public void DecoderPreservesNestedPayloadAndHasCanonicalLineEndings()
    {
        var bytes = TimelineBaker.BakeJson(Document("{\"Direction\":{\"x\":1,\"y\":2,\"z\":3},\"Falloff\":" + Curve + ",\"Counts\":[7,8,9]}"));
        var exported = TimelineJson.Decode(bytes);
        Assert.DoesNotContain("\r", exported);
        Assert.Equal(bytes, TimelineBaker.BakeJson(exported));
        Assert.Equal(exported, TimelineJson.Decode(TimelineBaker.BakeJson(exported)));
    }

    [Fact]
    public void GenericZeroSettingsTrackExportsWithoutAnAssemblyName()
    {
        var typed = new TypedTimeline { Name = "generic", Duration = 2 };
        var track = new TypedTrack { Type = typeof(TlNoSettings<Clip>) };
        track.Clips.Add(new TypedClip { Type = typeof(Clip), Start = 0, End = 2 });
        typed.Tracks.Add(track);
        var json = TimelineJson.Export(typed);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("TlNoSettings<PortableJson.Clip>", document.RootElement.GetProperty("tracks")[0].GetProperty("type").GetString());
        var baked = TimelineBaker.BakeJson(json);
        Assert.Equal(TimelineBaker.BakeTyped(typed), baked);
        Assert.Equal(json, TimelineJson.Decode(baked));
    }

    [Fact]
    public void DecoderPreservesAContainedClipAndIdenticalConsecutivePulses()
    {
        var typed = new TypedTimeline { Duration = 8 };
        var track = new TypedTrack { Type = typeof(Settings) };
        track.Clips.Add(new TypedClip { Type = typeof(Clip), Start = 0, End = 6 });
        track.Clips.Add(new TypedClip { Type = typeof(Clip), Start = 2, End = 4 });
        track.Clips.Add(new TypedClip { Type = typeof(Clip), Start = 6, End = 8 });
        typed.Tracks.Add(track);
        var baked = TimelineBaker.BakeTyped(typed);
        Assert.Equal(baked, TimelineBaker.BakeJson(TimelineJson.Decode(baked)));
    }

    [Fact]
    public void DisjointClipsAuthoredOutOfOrderKeepTheirNames()
    {
        var typed = new TypedTimeline { Duration = 8 };
        var track = new TypedTrack { Name = "lane", Type = typeof(Settings) };
        track.Clips.Add(new TypedClip { Name = "late", Type = typeof(Clip), Start = 4, End = 6 });
        track.Clips.Add(new TypedClip { Name = "early", Type = typeof(Clip), Start = 0, End = 2 });
        typed.Tracks.Add(track);
        var baked = TimelineBaker.BakeTyped(typed);
        var decoded = TimelineJson.Read(baked);
        Assert.Equal(("late", 4u, 6u), (decoded.Tracks[0].Clips[0].Name!, decoded.Tracks[0].Clips[0].Start, decoded.Tracks[0].Clips[0].End));
        Assert.Equal(("early", 0u, 2u), (decoded.Tracks[0].Clips[1].Name!, decoded.Tracks[0].Clips[1].Start, decoded.Tracks[0].Clips[1].End));
        var json = TimelineJson.Export(typed);
        using var document = JsonDocument.Parse(json);
        var clips = document.RootElement.GetProperty("tracks")[0].GetProperty("clips");
        Assert.Equal("late", clips[0].GetProperty("name").GetString());
        Assert.Equal(4, clips[0].GetProperty("start").GetInt32());
        Assert.Equal("early", clips[1].GetProperty("name").GetString());
        Assert.Equal(0, clips[1].GetProperty("start").GetInt32());
        Assert.Equal(baked, TimelineBaker.BakeJson(json));
        Assert.Equal(json, TimelineJson.Decode(baked));
    }

    [Fact]
    public void ClipLabelsAreBoundToWindowsInTheMetadata()
    {
        var typed = new TypedTimeline { Duration = 8 };
        var first = new TypedTrack { Type = typeof(Settings) };
        first.Clips.Add(new TypedClip { Name = "late", Type = typeof(Clip), Start = 4, End = 6 });
        first.Clips.Add(new TypedClip { Name = "early", Type = typeof(Clip), Start = 0, End = 2 });
        typed.Tracks.Add(first);
        var swapped = new TypedTimeline { Duration = 8 };
        var second = new TypedTrack { Type = typeof(Settings) };
        second.Clips.Add(new TypedClip { Name = "late", Type = typeof(Clip), Start = 0, End = 2 });
        second.Clips.Add(new TypedClip { Name = "early", Type = typeof(Clip), Start = 4, End = 6 });
        swapped.Tracks.Add(second);
        var a = TimelineBaker.BakeTyped(typed);
        var b = TimelineBaker.BakeTyped(swapped);
        Assert.Equal(TlbMetadata.Strip(a), TlbMetadata.Strip(b));
        Assert.NotEqual(a, b);
        var view = TlbMetadata.Read(a);
        Assert.Equal((4u, 6u), (view.Clips[0].Start, view.Clips[0].End));
        Assert.Equal((0u, 2u), (view.Clips[1].Start, view.Clips[1].End));
    }

    [Fact]
    public void GenericTrackKeepsItsOwnTypeArgument()
    {
        var typed = new TypedTimeline { Duration = 4 };
        var track = new TypedTrack { Type = typeof(IndependentTrack<int>), Data = Bytes(new IndependentTrack<int> { Setting = 7 }) };
        track.Clips.Add(new TypedClip { Type = typeof(Clip), Start = 0, End = 4 });
        typed.Tracks.Add(track);
        var baked = TimelineBaker.BakeTyped(typed);
        var decoded = TimelineJson.Read(baked);
        Assert.Equal(typeof(IndependentTrack<int>), decoded.Tracks[0].Type);
        var json = TimelineJson.Decode(baked);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("IndependentTrack<System.Int32>", document.RootElement.GetProperty("tracks")[0].GetProperty("type").GetString());
        Assert.Equal(7, document.RootElement.GetProperty("tracks")[0].GetProperty("data").GetProperty("Setting").GetInt32());
        Assert.Equal(baked, TimelineBaker.BakeJson(json));
    }

    [Fact]
    public void DirtyStructPaddingIsRejectedByName()
    {
        var typed = new TypedTimeline { Duration = 4 };
        var track = new TypedTrack { Type = typeof(PaddedTrack) };
        var dirty = new byte[8];
        dirty[0] = 1;
        dirty[1] = dirty[2] = dirty[3] = 0xCC;
        dirty[4] = 9;
        track.Clips.Add(new TypedClip { Type = typeof(Padded), Start = 0, End = 4, Data = dirty });
        typed.Tracks.Add(track);
        var baked = TimelineBaker.BakeTyped(typed);
        var decode = Assert.Throws<BakeDiagnosticException>(() => TimelineJson.Decode(baked));
        Assert.Contains("clip 0 on track 0 (Padded)", decode.Message);
        Assert.Contains("byte 1", decode.Message);
        Assert.Throws<BakeDiagnosticException>(() => TimelineJson.Export(typed));
        dirty[1] = dirty[2] = dirty[3] = 0;
        var clean = TimelineBaker.BakeTyped(typed);
        Assert.Equal(clean, TimelineBaker.BakeJson(TimelineJson.Decode(clean)));
    }

    [Fact]
    public void StrippedTlbCannotBeDecoded()
    {
        var bytes = TimelineBaker.BakeJson(Document("{}"));
        var error = Assert.Throws<BakeDiagnosticException>(() => TimelineJson.Decode(TlbMetadata.Strip(bytes)));
        Assert.Contains("metadata", error.Message);
    }

    [Fact]
    public void EmptyTrackOfAZeroLengthTimelineKeepsItsType()
    {
        var typed = new TypedTimeline { Name = "empty", Duration = 0 };
        typed.Tracks.Add(new TypedTrack { Name = "idle", Type = typeof(Settings) });
        var baked = TimelineBaker.BakeTyped(typed);
        var decoded = TimelineJson.Read(baked);
        Assert.Equal(typeof(Settings), decoded.Tracks[0].Type);
        Assert.Equal("idle", decoded.Tracks[0].Name);
        Assert.Equal(baked, TimelineBaker.BakeJson(TimelineJson.Decode(baked)));
    }

    static byte[] Bytes<T>(T value) where T : unmanaged
    {
        var bytes = new byte[System.Runtime.CompilerServices.Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(bytes, in value);
        return bytes;
    }
}

public struct Vector
{
    public float x;
    public float y;
    public float z;
}

public unsafe struct Clip
{
    public Vector Direction;
    public TlCurve Falloff;
    public fixed int Counts[3];
}

public struct Settings : IBlend<Clip>
{
    public Vector Origin;
    public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
}

public struct IndependentTrack<T> : IBlend<Clip> where T : unmanaged
{
    public T Setting;
    public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
}

public struct Padded
{
    public byte Tag;
    public int Value;
}

public struct PaddedTrack : IBlend<Padded>
{
    public void Blend(in Padded first, in Padded second, float factor, out Padded result) => result = first;
}
