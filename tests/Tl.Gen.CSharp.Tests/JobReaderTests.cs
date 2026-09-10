using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class JobReaderTests
{
    [Fact]
    public void RejectsNullCompilation()
    {
        var error = Assert.Throws<ArgumentNullException>(() => JobReader.Read(null!));

        Assert.Equal("compilation", error.ParamName);
    }

    [Fact]
    public void ReportsMissingContractsOnlyForTimelineSyntax()
    {
        static CSharpCompilation WithoutContracts(string source) => CSharpCompilation.Create(
            "MissingContracts",
            [CSharpSyntaxTree.ParseText(source, path: "Jobs.cs")]);

        var unrelated = JobReader.Read(WithoutContracts("namespace Game { public sealed class Plain; }"));
        var authored = JobReader.Read(WithoutContracts("namespace Tl { public interface ITimeline { } }"));

        Assert.Empty(unrelated.Diagnostics);
        var diagnostic = Assert.Single(authored.Diagnostics);
        Assert.Equal("TLGEN60", diagnostic.Code);
        Assert.Equal("Jobs.cs", diagnostic.File);
        Assert.Equal(1, diagnostic.Line);
        Assert.True(diagnostic.Column > 0);
    }

    [Fact]
    public void DeclarationDiagnosticFormatsCompilerStyleLocation()
    {
        var diagnostic = new DeclarationDiagnostic("Source.cs", 7, 11, "TLGEN42", "broken declaration");

        Assert.Equal("Source.cs(7,11): error TLGEN42: broken declaration", diagnostic.ToString());
    }

    [Fact]
    public void CatalogAssetCapacityReservesOnlyRouteZero()
    {
        Assert.NotNull(JobReader.CatalogAssetCapacityDiagnostic(-1));
        Assert.Null(JobReader.CatalogAssetCapacityDiagnostic(0));
        Assert.Null(JobReader.CatalogAssetCapacityDiagnostic(65_536));
        Assert.Equal(
            ("TLGEN78", "A catalog may contain at most 65,536 nonempty assets because route zero is reserved."),
            JobReader.CatalogAssetCapacityDiagnostic(65_537));
        Assert.NotNull(JobReader.CatalogAssetCapacityDiagnostic(int.MaxValue));
    }

    [Fact]
    public void ReadsFiniteJobsAndCatalogSchemas()
    {
        var compilation = Compile(Runtime + """
            namespace Game
            {

            public readonly record struct DamageTrack(int Multiplier);
            public readonly record struct DamageClip(int Amount);
            public readonly record struct AnimationTrack(int Layer);
            public readonly record struct AnimationClip(int Step);
            public readonly record struct Resistance(int Scale);
            public record struct Health(int Value);
            public record struct Pose(int Frame);

            public readonly struct DamageJob : Tl.ITimelineJob<DamageTrack, DamageClip>
            {
                public static void Execute(in Tl.Frame<DamageTrack, DamageClip> frame, in Resistance resistance, ref Health health) { }
            }

            public readonly struct AnimationJob : Tl.ITimelineJob<AnimationTrack, AnimationClip>
            {
                public static void Execute(in Tl.Frame<AnimationTrack, AnimationClip> frame, in Health health, ref Pose pose) { }
            }

            public readonly partial struct DamageOne : Tl.ITimeline
            {
                public static void Define(scoped Tl.Builder builder)
                {
                    var damage = builder.Track(new DamageTrack(2)).Use<DamageJob>();
                    builder.Clip(damage, new DamageClip(7), 0u, 10u);
                }
            }

            public readonly partial struct DamageTwo : Tl.ITimeline
            {
                public static void Define(scoped Tl.Builder builder)
                {
                    var damage = builder.Track(new DamageTrack(3)).Use<DamageJob>();
                    builder.Clip(damage, new DamageClip(8), 2u, 12u);
                }
            }

            public readonly partial struct Mixed : Tl.ITimeline
            {
                public static void Define(scoped Tl.Builder builder)
                {
                    var damage = builder.Track(new DamageTrack(4)).Use<DamageJob>();
                    var animation = builder.Track(new AnimationTrack(1)).Use<AnimationJob>();
                    builder.Clip(damage, new DamageClip(9), 0u, 10u);
                    builder.Clip(animation, new AnimationClip(1), 0u, 10u);
                }
            }

            public readonly struct DamageRows;
            public readonly struct MixedRows;

            public readonly partial struct Combat : Tl.ITimelineCatalog
            {
                public static void Define(scoped Tl.CatalogBuilder builder)
                {
                    builder.Schema<DamageRows>().Asset<DamageOne>().Asset<DamageTwo>();
                    builder.Schema<MixedRows>().Asset<Mixed>();
                }
            }
            }
            """);
        Assert.Empty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var result = JobReader.Read(compilation);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(3, result.Timelines.Count);
        var mixed = Assert.Single(result.Timelines, static timeline => timeline.Name == "Mixed");
        Assert.Equal(10u, mixed.Duration);
        Assert.Equal(2, mixed.Tracks.Count);
        Assert.Equal("global::Game.DamageTrack", mixed.Tracks[0].TypeName);
        Assert.Equal("global::Game.DamageClip", mixed.Tracks[0].ClipTypeName);
        Assert.Equal("new global::Game.DamageTrack(4)", mixed.Tracks[0].Expression);
        Assert.Equal(
            [
                new TimelineSlot("resistance", "global::Game.Resistance", SlotMode.Input),
                new TimelineSlot("health", "global::Game.Health", SlotMode.Reference),
            ],
            mixed.Tracks[0].Job.Slots);
        Assert.Equal("global::Game.DamageClip", mixed.Clips[0].TypeName);
        Assert.Equal("new global::Game.DamageClip(9)", mixed.Clips[0].Expression);

        var catalog = Assert.Single(result.Catalogs);
        Assert.Equal("Combat", catalog.Name);
        Assert.Equal("Game", catalog.Namespace);
        Assert.Equal(["DamageRows", "MixedRows"], catalog.Schemas.Select(static schema => schema.Name));
        Assert.Equal(["global::Game.DamageOne", "global::Game.DamageTwo"], catalog.Schemas[0].Assets);
        Assert.Equal(["global::Game.Mixed"], catalog.Schemas[1].Assets);
    }

    [Fact]
    public void FlattensLoopingIncludeAndReadsHookFrames()
    {
        var result = Read("""
            namespace Game
            {
                public readonly record struct Track(int Value);
                public readonly record struct Clip(int Value);
                public record struct State(int Value);

                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame, ref State state) { }
                }
                public readonly struct BaseBefore : Tl.IHook
                {
                    public static void Execute(in Tl.TimelineFrame frame, in State state) { }
                }
                public readonly struct BaseAfter : Tl.IHook
                {
                    public static void Execute(in Tl.TimelineFrame frame, ref State state) { }
                }
                public readonly struct OuterBefore : Tl.IHook
                {
                    public static void Execute(in Tl.TimelineFrame frame, ref State state) { }
                }
                public readonly struct OuterAfter : Tl.IHook
                {
                    public static void Execute(in Tl.TimelineFrame frame, in State state) { }
                }

                public readonly partial struct Base : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        builder.Before<BaseBefore>();
                        var track = builder.Track(new Track(1)).Use<Job>();
                        builder.Clip(track, new Clip(2), 0u, 10u);
                        builder.After<BaseAfter>();
                        builder.Looping();
                    }
                }
                public readonly partial struct Outer : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        builder.Before<OuterBefore>();
                        builder.Include<Base>();
                        var track = builder.Track(new Track(3)).Use<Job>();
                        builder.Clip(track, new Clip(4), 0u, 10u);
                        builder.After<OuterAfter>();
                    }
                }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var outer = Assert.Single(result.Timelines, static timeline => timeline.Name == "Outer");
        Assert.True(outer.Loops);
        Assert.Equal([0, 1], outer.Tracks.Select(static track => track.Index));
        Assert.Equal([0, 1], outer.Clips.Select(static clip => clip.TrackIndex));
        Assert.Equal(["global::Game.OuterBefore", "global::Game.BaseBefore"], outer.Before.Select(static hook => hook.TypeName));
        Assert.Equal(["global::Game.BaseAfter", "global::Game.OuterAfter"], outer.After.Select(static hook => hook.TypeName));
        Assert.Equal([new TimelineSlot("state", "global::Game.State", SlotMode.Reference)], outer.Before[0].Slots);
        Assert.Equal([new TimelineSlot("state", "global::Game.State", SlotMode.Input)], outer.After[1].Slots);
    }

    [Fact]
    public void AcceptsExactlyTwoHundredFiftySixAuthoredTracks()
    {
        var declarations = string.Join("\n", Enumerable.Range(0, 256)
            .Select(static index => $"var track{index} = builder.Track(new Track({index})).Use<Job>(); builder.Clip(track{index}, new Clip({index}), 0u, 1u);"));
        var result = Read($$"""
            namespace Game
            {
                public readonly record struct Track(int Value);
                public readonly record struct Clip(int Value);
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        {{declarations}}
                    }
                }
            }
            """);

        Assert.Empty(result.Diagnostics);
        var timeline = Assert.Single(result.Timelines);
        Assert.Equal(256, timeline.Tracks.Count);
        Assert.Equal(Enumerable.Range(0, 256), timeline.Tracks.Select(static track => track.Index));
    }

    [Fact]
    public void RejectsTwoHundredFiftySevenAuthoredTracks()
    {
        var declarations = string.Join("\n", Enumerable.Range(0, 257)
            .Select(static index => $"var track{index} = builder.Track(new Track({index})).Use<Job>(); builder.Clip(track{index}, new Clip({index}), 0u, 1u);"));
        var result = Read($$"""
            namespace Game
            {
                public readonly record struct Track(int Value);
                public readonly record struct Clip(int Value);
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        {{declarations}}
                    }
                }
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics, static item => item.Code == "TLGEN68");
        Assert.Contains("at most 256 authored tracks", diagnostic.Message);
        Assert.Empty(result.Timelines);
    }

    [Fact]
    public void ReadsBoundedPayloadsAndEveryUnsignedBoundKind()
    {
        const string declaration = """
            namespace Game
            {
                public readonly record struct Track(int Value)
                {
                    public const int DefaultValue = 7;
                    public static Track operator -(Track value) => value;
                }
                public readonly record struct Clip(int Value)
                {
                    public Clip(string value) : this(value.Length) { }
                    public static implicit operator Clip(string value) => new(value.Length);
                    public static Clip operator -(Clip value) => value;
                }
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track((int)+checked(Track.DefaultValue))).Use<Job>();
                        builder.Clip(track, new Clip(nameof(Asset)), (byte)0, (ushort)1);
                        builder.Clip(track, (Clip)nameof(Track), '\u0001', 2u);
                        builder.Clip(track, default(Clip), 2, 3);
                        builder.Clip(track, new Clip((string)nameof(Clip)), default, 4u);
                        var implicitTrack = builder.Track<Track>(new(8)).Use<Job>();
                        var parenthesizedTrack = builder.Track((new Track(9))).Use<Job>();
                        var prefixedTrack = builder.Track(-new Track(10)).Use<Job>();
                        var checkedTrack = builder.Track(checked(new Track(11))).Use<Job>();
                        builder.Clip(implicitTrack, new Clip(5), 4u, 5u);
                        builder.Clip(parenthesizedTrack, new Clip(6), 5u, 6u);
                        builder.Clip(prefixedTrack, new Clip(7), 6u, 7u);
                        builder.Clip(checkedTrack, new Clip(8), 7u, 8u);
                    }
                }
            }
            """;
        var compilation = Compile(Runtime + declaration);
        Assert.Empty(compilation.GetDiagnostics().Where(static item => item.Severity == DiagnosticSeverity.Error));
        var result = JobReader.Read(compilation);

        Assert.Empty(result.Diagnostics);
        var timeline = Assert.Single(result.Timelines);
        Assert.Equal(8u, timeline.Duration);
        Assert.Equal([(0u, 1u), (1u, 2u), (2u, 3u), (0u, 4u), (4u, 5u), (5u, 6u), (6u, 7u), (7u, 8u)], timeline.Clips.Select(static clip => (clip.Start, clip.End)));
        Assert.Contains("global::Game.Track.DefaultValue", timeline.Tracks[0].Expression);
        Assert.Contains("\"Asset\"", timeline.Clips[0].Expression);
        Assert.Contains("default(global::Game.Clip)", timeline.Clips[2].Expression);
    }

    [Fact]
    public void ReportsTimelineStructureDiagnosticsAtTheirSource()
    {
        var result = Read("""
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public struct FirstState;
                public struct SecondState;
                public readonly struct FirstJob : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame, ref FirstState state) { }
                }
                public readonly struct SecondJob : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame, ref SecondState state) { }
                }
                public sealed class InvalidHook : Tl.IHook;
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var first = builder.Track(new Track()).Use<FirstJob>();
                        var second = builder.Track(new Track()).Use<SecondJob>();
                        builder.Clip(first, new Clip(), 0u, 2u);
                        builder.Before<InvalidHook>();
                        builder.Looping();
                        builder.Looping();
                        builder.ToString();
                        return;
                    }
                }
            }
            """);

        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN63");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN66");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN67");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN68");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN69");
        Assert.All(result.Diagnostics, static item =>
        {
            Assert.Equal("Jobs.cs", item.File);
            Assert.True(item.Line > 1);
            Assert.True(item.Column > 0);
        });
    }

    [Fact]
    public void ReportsCatalogStructureDiagnosticsAtTheirSource()
    {
        var result = Read("""
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
                public readonly struct Rows;
                public readonly partial struct Catalog : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<Asset>();
                        builder.Schema<Rows>().Asset<Asset>();
                        builder.Schema<Rows>();
                    }
                }
            }
            """);

        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN74");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN75");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN76");
        Assert.All(result.Diagnostics, static item =>
        {
            Assert.Equal("Jobs.cs", item.File);
            Assert.True(item.Line > 1);
            Assert.True(item.Column > 0);
        });
        Assert.Empty(result.Catalogs);
    }

    [Fact]
    public void RejectsInvalidBoundsWithoutExecutingAuthoredCode()
    {
        var result = Read("""
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), -1, 1u);
                    }
                }
            }
            """);

        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN68");
        Assert.Empty(result.Timelines);
    }

    [Fact]
    public void RejectsIncompatibleIncludedLoopingModesAndDurations()
    {
        var result = Read("""
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Finite : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
                public readonly partial struct FiniteMadeLooping : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        builder.Include<Finite>();
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                        builder.Looping();
                    }
                }
                public readonly partial struct Loop : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                        builder.Looping();
                    }
                }
                public readonly partial struct ExtendedLoop : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        builder.Include<Loop>();
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 1u, 2u);
                    }
                }
            }
            """);

        var diagnostics = result.Diagnostics.Where(static item => item.Code == "TLGEN70").ToArray();
        Assert.Contains(diagnostics, static item => item.Message.Contains("incompatible looping modes", StringComparison.Ordinal));
        Assert.Contains(diagnostics, static item => item.Message.Contains("same duration", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsCatalogAssetsDefinedOutsideTheCompilation()
    {
        var reference = Reference("""
            using Tl;
            namespace External;
            public readonly partial struct Asset : ITimeline
            {
                public static void Define(scoped Builder builder) { }
            }
            """);
        var compilation = CSharpCompilation.Create(
            "Consumer",
            [CSharpSyntaxTree.ParseText("""
                using Tl;
                namespace Game;
                public readonly struct Rows;
                public readonly partial struct Catalog : ITimelineCatalog
                {
                    public static void Define(scoped CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<External.Asset>();
                    }
                }
                """, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Catalog.cs")],
            References.Add(reference),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var result = JobReader.Read(compilation);

        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN70" && item.File == "Catalog.cs");
        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN76" && item.File == "Catalog.cs");
        Assert.Empty(result.Catalogs);
    }

    [Fact]
    public void OrdersMultipleCatalogsByNamespaceAndName()
    {
        var result = Read("""
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
                public readonly struct Rows;
                public readonly partial struct Zeta : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder) => DefineBody(builder);
                    private static void DefineBody(scoped Tl.CatalogBuilder builder) { }
                }
                public readonly partial struct Beta : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<Asset>();
                    }
                }
                public readonly partial struct Alpha : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<Asset>();
                    }
                }
            }
            """);

        Assert.Contains(result.Diagnostics, static item => item.Code == "TLGEN73");
        Assert.Equal(["Alpha", "Beta"], result.Catalogs.Select(static catalog => catalog.Name));
    }

    [Fact]
    public void IgnoresLegacyTimelinesUnlessCatalogRequiresThem()
    {
        const string declarations = """
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Legacy : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track());
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
                public readonly partial struct Current : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
            }
            """;
        var free = Read(declarations);

        Assert.Empty(free.Diagnostics);
        Assert.Equal("Current", Assert.Single(free.Timelines).Name);

        var required = Read(declarations + """
            namespace Game
            {
                public readonly struct Rows;
                public readonly partial struct Catalog : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<Legacy>();
                    }
                }
            }
            """);
        Assert.Contains(required.Diagnostics, static diagnostic => diagnostic.Code == "TLGEN63");
        Assert.Empty(required.Catalogs);
    }

    [Theory]
    [MemberData(nameof(InvalidDeclarations))]
    public void DiagnosesUnsupportedAuthoredShapes(string declaration, string code)
    {
        if (code is "TLGEN61" or "TLGEN62")
            declaration += RequiredCatalog("Asset");
        else if (code == "TLGEN71")
            declaration += RequiredCatalog("First");
        var result = Read(declaration);

        Assert.Contains(result.Diagnostics, item => item.Code == code);
        var diagnostic = result.Diagnostics.First(item => item.Code == code);
        Assert.Equal("Jobs.cs", diagnostic.File);
        Assert.True(diagnostic.Line > 1);
        Assert.True(diagnostic.Column > 0);
    }

    public static TheoryData<string, string> InvalidDeclarations => new()
    {
        {
            """
            namespace Game
            {
                public readonly struct Track;
                public readonly struct OtherTrack;
                public readonly struct Clip;
                public readonly struct Wrong : Tl.ITimelineJob<OtherTrack, Clip>
                {
                    public static void Execute(in Tl.Frame<OtherTrack, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Wrong>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
            }
            """,
            "TLGEN65"
        },
        {
            """
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public struct State;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame, out State state) => state = default;
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
            }
            """,
            "TLGEN67"
        },
        {
            """
            namespace Game
            {
                public readonly record struct Track(int Value);
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    private static int Runtime() => 1;
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track(Runtime())).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
            }
            """,
            "TLGEN64"
        },
        {
            """
            namespace Game
            {
                public readonly struct Track;
                public readonly record struct Clip(int Value);
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    private static int Runtime() => 1;
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(Runtime()), 0u, 1u);
                    }
                }
            }
            """,
            "TLGEN68"
        },
        {
            """
            namespace Game
            {
                public struct State;
                public readonly struct BadHook : Tl.IHook
                {
                    public static void Execute(in Tl.TimelineFrame frame, out State state) => state = default;
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder) => builder.Before<BadHook>();
                }
            }
            """,
            "TLGEN62"
        },
        {
            """
            namespace Game
            {
                public readonly partial struct First : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        builder.Include<Second>();
                    }
                }
                public readonly partial struct Second : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        builder.Include<First>();
                    }
                }
            }
            """,
            "TLGEN71"
        },
        {
            """
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public struct FirstState;
                public struct SecondState;
                public readonly struct FirstJob : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame, ref FirstState state) { }
                }
                public readonly struct SecondJob : Tl.ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Tl.Frame<Track, Clip> frame, ref SecondState state) { }
                }
                public readonly partial struct First : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<FirstJob>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
                public readonly partial struct Second : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<SecondJob>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
                public readonly struct Rows;
                public readonly partial struct Catalog : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<First>().Asset<Second>();
                    }
                }
            }
            """,
            "TLGEN77"
        },
        {
            """
            namespace Game
            {
                public readonly struct Track;
                public readonly struct Clip;
                public readonly struct Job : Tl.ITimelineJob<Track, Clip>
                {
                    private static void Execute(in Tl.Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder)
                    {
                        var track = builder.Track(new Track()).Use<Job>();
                        builder.Clip(track, new Clip(), 0u, 1u);
                    }
                }
            }
            """,
            "TLGEN66"
        },
        {
            """
            namespace Game
            {
                public partial struct Asset : Tl.ITimeline
                {
                    public static void Define(scoped Tl.Builder builder) { }
                }
            }
            """,
            "TLGEN61"
        },
        {
            """
            namespace Game
            {
                public partial struct Catalog : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder) { }
                }
            }
            """,
            "TLGEN72"
        },
        {
            """
            namespace Game
            {
                public readonly partial struct Catalog : Tl.ITimelineCatalog
                {
                    public static void Define(Tl.CatalogBuilder builder) { }
                }
            }
            """,
            "TLGEN73"
        },
    };

    private static JobReadResult Read(string declaration)
        => JobReader.Read(Compile(Runtime + declaration));

    private static string RequiredCatalog(string asset)
        => $$"""
            namespace Force
            {
                public readonly struct Rows;
                public readonly partial struct Required : Tl.ITimelineCatalog
                {
                    public static void Define(scoped Tl.CatalogBuilder builder)
                    {
                        builder.Schema<Rows>().Asset<Game.{{asset}}>();
                    }
                }
            }
            """;

    private static CSharpCompilation Compile(string source)
        => CSharpCompilation.Create(
            "JobReaderTests",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Jobs.cs")],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static MetadataReference Reference(string source)
    {
        var compilation = CSharpCompilation.Create(
            "ExternalJobs",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "External.cs")],
            References.Add(MetadataReference.CreateFromFile(typeof(global::Tl.ITimeline).Assembly.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

    private const string Runtime = """
        namespace Tl
        {
            public interface ITimeline { static abstract void Define(scoped Builder builder); }
            public interface ITimelineCatalog { static abstract void Define(scoped CatalogBuilder builder); }
            public interface ITimelineJob<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged { }
            public interface IHook { }
            public readonly ref struct TrackRef<TTrack> where TTrack : unmanaged
            {
                public TrackRef<TTrack, TJob> Use<TJob>() where TJob : unmanaged => default;
            }
            public readonly ref struct TrackRef<TTrack, TJob> where TTrack : unmanaged where TJob : unmanaged { }
            public readonly ref struct Builder
            {
                public TrackRef<TTrack> Track<TTrack>(in TTrack track) where TTrack : unmanaged => default;
                public void Clip<TTrack, TJob, TClip>(in TrackRef<TTrack, TJob> track, in TClip clip, uint start, uint end)
                    where TTrack : unmanaged where TClip : unmanaged where TJob : unmanaged, ITimelineJob<TTrack, TClip> { }
                public void Clip<TTrack, TClip>(in TrackRef<TTrack> track, in TClip clip, uint start, uint end)
                    where TTrack : unmanaged where TClip : unmanaged { }
                public void Looping() { }
                public void Before<THook>() where THook : unmanaged, IHook { }
                public void After<THook>() where THook : unmanaged, IHook { }
                public void Include<TTimeline>() where TTimeline : unmanaged, ITimeline { }
            }
            public readonly ref struct Frame<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged { }
            public readonly struct TimelineFrame { }
            public readonly ref struct CatalogBuilder
            {
                public SchemaBuilder<TSchema> Schema<TSchema>() where TSchema : unmanaged => default;
            }
            public readonly ref struct SchemaBuilder<TSchema> where TSchema : unmanaged
            {
                public SchemaBuilder<TSchema> Asset<TTimeline>() where TTimeline : unmanaged, ITimeline => this;
            }
        }
        """;
}
