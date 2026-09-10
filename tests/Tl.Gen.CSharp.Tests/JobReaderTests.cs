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

    private static CSharpCompilation Compile(string source)
        => CSharpCompilation.Create(
            "JobReaderTests",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Jobs.cs")],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

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
