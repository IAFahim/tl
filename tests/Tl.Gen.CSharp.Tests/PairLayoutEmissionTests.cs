using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class PairLayoutEmissionTests
{
    private const string Domain = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
                => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float total) { }
        }
        public readonly struct ApplyDamageAgain : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float total) { }
        }
        """;

    private const string Heal = """
        using Tl;
        namespace Domain;
        public readonly record struct HealClip(float Amount);
        public readonly record struct HealTrack(float Multiplier) : IBlend<HealClip>
        {
            public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
                => result = new HealClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public readonly struct ApplyHeal : ITrack<HealTrack, HealClip>
        {
            public static void OnActive(in Frame<HealTrack, HealClip> frame, ref float total) { }
        }
        """;

    private const string ReorderedTrack = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(int Layer, float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
                => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float total) { }
        }
        """;

    private const string RetypedClip = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(double Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
                => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
        }
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float total) { }
        }
        """;

    private static CSharpCompilation Compilation(string source, MetadataReference? extra = null)
    {
        var references = ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
        if (extra is not null) references.Add(extra);
        return CSharpCompilation.Create(
            "PairLayoutEmissionTests",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Domain.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    }

    internal static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();

    private static string Binding(params string[] sources)
    {
        var consumers = sources.SelectMany(static source => JobReader.Read(Compilation(source)).Consumers).ToList();
        return JobEmitter.Consumers(consumers);
    }

    [Fact]
    public void BindingRegistersEachPairOnceAndPublishesTheAssemblyAttribute()
    {
        var binding = Binding(Domain, Heal);

        Assert.Contains("[assembly: global::TlConsumerLayoutAttribute(typeof(global::Domain.DamageTrack), typeof(global::Domain.DamageClip), ", binding);
        Assert.Contains("internal sealed class TlConsumerLayoutAttribute : global::System.Attribute", binding);
        Assert.Equal(2, CountOf(binding, "[assembly: global::TlConsumerLayoutAttribute("));
        Assert.Equal(2, CountOf(binding, ".VerifyLayout("));
        Assert.Equal(1, CountOf(binding, "internal sealed class TlConsumerLayoutAttribute"));
    }

    [Fact]
    public void RepeatedReadsOfOneCompilationFoldTheSameLayout()
    {
        var compilation = Compilation(Domain);
        Assert.Equal(JobReader.Read(compilation).Consumers[0].Layout, JobReader.Read(compilation).Consumers[0].Layout);
        Assert.Equal(JobReader.Read(Compilation(Domain)).Consumers[0].Layout, JobReader.Read(compilation).Consumers[0].Layout);
    }

    [Fact]
    public void FoldingSeparatesReorderedAndRetypedShapes()
    {
        var reference = JobReader.Read(Compilation(Domain)).Consumers[0].Layout;
        var reordered = JobReader.Read(Compilation(ReorderedTrack)).Consumers[0].Layout;
        var retyped = JobReader.Read(Compilation(RetypedClip)).Consumers[0].Layout;

        Assert.NotEqual(0UL, reference);
        Assert.NotEqual(reference, reordered);
        Assert.NotEqual(reference, retyped);
        Assert.NotEqual(reordered, retyped);
    }

    [Fact]
    public void StructsFoldIdenticallyThroughSourceAndReferencedAssemblyViews()
    {
        var library = Compilation(Domain);
        using var image = new MemoryStream();
        Assert.True(library.Emit(image).Success);
        image.Position = 0;
        var consumer = CSharpCompilation.Create(
            "PairLayoutReferencingConsumer",
            [CSharpSyntaxTree.ParseText("""
                using Tl;
                namespace Domain;
                public readonly struct ApplyHeal : ITrack<DamageTrack, DamageClip>
                {
                    public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float total) { }
                }
                """, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Heal.cs")],
            ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .Append(MetadataReference.CreateFromStream(image, filePath: "PairLayoutEmissionTests.dll")),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

        var fromSource = JobReader.Read(library).Consumers[0].Layout;
        var fromMetadata = JobReader.Read(consumer).Consumers[0].Layout;

        Assert.Equal(fromSource, fromMetadata);
        Assert.NotEqual(0UL, fromMetadata);
    }

    private static int CountOf(string text, string token)
    {
        var count = 0;
        for (var at = text.IndexOf(token, StringComparison.Ordinal); at >= 0; at = text.IndexOf(token, at + token.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
