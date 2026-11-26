using System;
using System.Text;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Txt
{
public readonly struct CaptionClip(TlText line, int size)
{
    public readonly TlText Line = line;
    public readonly int Size = size;
}

public readonly struct CaptionTrack(float weight) : IBlend<CaptionClip>
{
    public readonly float Weight = weight;

    public void Blend(in CaptionClip first, in CaptionClip second, float factor, out CaptionClip result)
        => result = first;
}

public readonly struct SayMarker(TlText line)
{
    public readonly TlText Line = line;
}
}

namespace Tl.Bake.Tests
{
public class TextTableTests
{
    static BakerAssemblyResolver Resolver()
        => BakerAssemblyResolver.FromAssemblies([typeof(TextTableTests).Assembly]);

    static string Film(string first, string second, string said) =>
        "{\"duration\":6,\"tracks\":[{\"namespace\":\"Txt\",\"type\":\"CaptionTrack\",\"data\":{\"Weight\":1},\"clips\":["
        + "{\"namespace\":\"Txt\",\"type\":\"CaptionClip\",\"start\":0,\"end\":2,\"data\":{\"Line\":\"" + first + "\",\"Size\":12}},"
        + "{\"namespace\":\"Txt\",\"type\":\"CaptionClip\",\"start\":2,\"end\":4,\"data\":{\"Line\":\"" + second + "\",\"Size\":12}},"
        + "{\"namespace\":\"Txt\",\"type\":\"CaptionClip\",\"start\":4,\"end\":6,\"data\":{\"Line\":\"" + first + "\",\"Size\":14}}]}],"
        + "\"markers\":[{\"namespace\":\"Txt\",\"type\":\"SayMarker\",\"tick\":1,\"data\":{\"Line\":\"" + said + "\"}}]}";

    [Fact]
    public void TextFieldsBakeEachStringOnceAndDecodeBackToTheSameJson()
    {
        var bytes = TimelineBaker.BakeJson(Film("Hello, world", "Röntgen ✓", "hi"), Resolver());
        Assert.Equal(3, BitConverter.ToInt32(bytes, 76));
        var inspect = TlbInspection.Inspect(bytes);
        Assert.Contains("\"text\": \"Hello, world\"", inspect);
        var json = TimelineJson.Decode(bytes, Resolver());
        Assert.Contains("\"Line\": \"Röntgen ✓\"", json);
        Assert.Contains("\"Line\": \"hi\"", json);
        Assert.Equal(bytes, TimelineBaker.BakeJson(json, Resolver()));
    }

    [Fact]
    public void AnEmptyTextIsNoneAndAddsNoString()
    {
        var bytes = TimelineBaker.BakeJson(Film("", "", ""), Resolver());
        Assert.Equal(0, BitConverter.ToInt32(bytes, 72));
        Assert.Equal(0, BitConverter.ToInt32(bytes, 76));
        Assert.Equal(bytes, TimelineBaker.BakeJson(TimelineJson.Decode(bytes, Resolver()), Resolver()));
    }

    [Fact]
    public void ALibraryLooksUpEveryTimelinesTextsByKey()
    {
        var parts = new[]
        {
            new LibraryPart { Name = "a", Tlb = TimelineBaker.BakeJson(Film("Hello", "Bye", "hi"), Resolver()) },
            new LibraryPart { Name = "b", Tlb = TimelineBaker.BakeJson(Film("Hello", "Again", "yo"), Resolver()) },
        };
        using var world = new TimelineWorld(LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default));
        Assert.Equal(5, world.View.TextCount);
        Assert.Equal("Hello", world.Text(TlText.Of("Hello")));
        Assert.Equal("Again", world.Text(TlText.Of("Again")));
        Assert.Equal("yo", Encoding.UTF8.GetString(LibraryViewReader.Text(world.View, TlText.Of("yo"))));
        Assert.Equal("", world.Text(TlText.Of("missing")));
        Assert.Equal("", world.Text(TlText.None));
    }

    [Fact]
    public void ALibraryRejectsATextThatDoesNotMatchItsKey()
    {
        var tlb = TimelineBaker.BakeJson(Film("Hello", "Bye", "hi"), Resolver());
        var at = Encoding.UTF8.GetBytes("Bye");
        var index = tlb.AsSpan().IndexOf(at);
        tlb[index] = (byte)'b';
        var library = LibraryAssembler.Assemble([new LibraryPart { Name = "a", Tlb = tlb }], 60, LibrarySettings.Default);
        var error = Assert.Throws<ArgumentException>(() => new TimelineWorld(library));
        Assert.Contains("does not match its key", error.Message);
    }

    [Fact]
    public void ANumberInATextFieldFailsTheBake()
    {
        var error = Assert.Throws<BakeDiagnosticException>(() => TimelineBaker.BakeJson(Film("Hello", "Bye", "hi").Replace("\"Bye\"", "7"), Resolver()));
        Assert.Contains("expected text string", error.Message);
    }

    [Fact]
    public void ATypedTimelineCarriesItsTextsAndExportsThem()
    {
        var timeline = new TypedTimeline { Duration = 2 };
        var track = new TypedTrack { Type = typeof(Txt.CaptionTrack), Data = Bytes(new Txt.CaptionTrack(1)) };
        track.Clips.Add(new TypedClip { Type = typeof(Txt.CaptionClip), Start = 0, End = 2, Data = Bytes(new Txt.CaptionClip(TlText.Of("Typed"), 3)) });
        timeline.Tracks.Add(track);
        var missing = Assert.Throws<BakeDiagnosticException>(() => TimelineJson.Export(timeline));
        Assert.Contains("carries no string", missing.Message);
        timeline.Texts.Add("Typed");
        Assert.Contains("\"Line\": \"Typed\"", TimelineJson.Export(timeline));
    }

    static unsafe byte[] Bytes<T>(T value) where T : unmanaged
    {
        var bytes = new byte[sizeof(T)];
        fixed (byte* p = bytes) *(T*)p = value;
        return bytes;
    }
}
}
