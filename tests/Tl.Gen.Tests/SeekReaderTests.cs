using Tl.Gen.Analysis;
using Tl.Gen.Model;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class SeekReaderTests
{
    private const string Prefix = """
        using Tl;
        namespace Reader;
        public readonly record struct Clip(int Value);
        public readonly record struct Component(int Value);
        """;

    [Theory]
    [InlineData("", "TLGEN30")]
    [InlineData("public static void Seek(in Frame<Track, Clip> frame) { } public static void Seek(in Frame<Track, Clip> frame, in Component value) { }", "TLGEN30")]
    [InlineData("public static void Seek<T>(in Frame<Track, Clip> frame) { }", "TLGEN30")]
    [InlineData("public void Seek(in Frame<Track, Clip> frame) { }", "TLGEN30")]
    [InlineData("private static void Seek(in Frame<Track, Clip> frame) { }", "TLGEN30")]
    [InlineData("public static int Seek(in Frame<Track, Clip> frame) => 0;", "TLGEN30")]
    [InlineData("public static void Seek(in Frame<Track, Component> frame) { }", "TLGEN31")]
    [InlineData("public static void Seek(in Frame<Track, Clip> frame, Component value = default) { }", "TLGEN32")]
    [InlineData("public static void Seek(in Frame<Track, Clip> frame, params Component[] value) { }", "TLGEN32")]
    [InlineData("public static void Seek(in Frame<Track, Clip> frame, in string value) { }", "TLGEN32")]
    public void RejectsMalformedSeek(string member, string code)
    {
        var diagnostics = Read(Track(member));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData("playback")]
    [InlineData("@playback")]
    public void RejectsReservedPlaybackSlot(string name)
    {
        var diagnostics = Read(Track($"public static void Seek(in Frame<Track, Clip> frame, in Component {name}) {{ }}"));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN50" && diagnostic.Line == 8);
    }

    [Theory]
    [InlineData("Playback")]
    [InlineData("Playback<Timeline>")]
    public void RejectsWritablePlaybackState(string type)
    {
        var diagnostics = Read(Track($"public static void Seek(in Frame<Track, Clip> frame, ref {type} value) {{ }}"));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN51" && diagnostic.Line == 8);
    }

    [Fact]
    public void AllowsReadOnlyPlaybackAndWritableRefOutSharing()
    {
        var source = Prefix + """

            public readonly struct Track : ITrack<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                public static void Seek(in Frame<Track, Clip> frame, in Playback state, ref Component value) { }
            }
            public readonly struct Hook : IHook
            {
                public static void Forward(out Component value) => value = default;
                public static void Backward(ref Component value) { }
            }
            public readonly partial struct Timeline : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                    builder.After<Hook>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Reader.cs", source)]);

        Assert.Empty(diagnostics);
        var timeline = Assert.Single(timelines);
        Assert.Equal("state", Assert.Single(timeline.ReadOnlySlots).Name);
        Assert.Equal("value", Assert.Single(timeline.WritableSlots).Name);
        Assert.Equal(SlotMode.Reference, timeline.WritableSlots[0].Mode);
    }

    [Theory]
    [InlineData("public int Data;")]
    [InlineData("private readonly struct DynamicData { }")]
    [InlineData("private const int Start = 0;")]
    [InlineData("private static void TrySeek() { }")]
    public void RejectsGeneratedMemberCollisions(string member)
    {
        var source = Prefix + $$"""

            public readonly struct Track : ITrack<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                public static void Seek(in Frame<Track, Clip> frame) { }
            }
            public readonly partial struct Timeline : ITimeline
            {
                {{member}}
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                }
            }
            """;
        var diagnostics = Read(source);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN52");
    }

    [Theory]
    [InlineData("in Component shared", "ref Component shared", "cannot be both read-only and writable")]
    [InlineData("in Component shared", "in Playback shared", "incompatible type declarations")]
    public void RejectsSlotConflictsAtLaterParameter(string first, string second, string message)
    {
        var source = Prefix + $$"""

            public readonly struct Track : ITrack<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                public static void Seek(in Frame<Track, Clip> frame, {{first}}) { }
            }
            public readonly struct Hook : IHook
            {
                public static void Forward({{second}}) { }
                public static void Backward({{second}}) { }
            }
            public readonly partial struct Timeline : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                    builder.After<Hook>();
                }
            }
            """;

        var diagnostics = Read(source);
        var diagnostic = Assert.Single(diagnostics, static diagnostic => diagnostic.Code == "TLGEN38");
        Assert.Contains(message, diagnostic.Message);
        Assert.Equal(12, diagnostic.Line);
    }

    private static string Track(string seek)
        => Prefix + $$"""

            public readonly struct Track : ITrack<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                {{seek}}
            }
            public readonly partial struct Timeline : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                }
            }
            """;

    private static IReadOnlyList<DeclarationDiagnostic> Read(string source)
        => HeterogeneousReader.Read([("Reader.cs", source)]).Diagnostics;
}
