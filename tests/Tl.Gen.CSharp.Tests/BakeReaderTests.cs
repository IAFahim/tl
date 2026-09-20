using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class BakeReaderTests
{
    private const string Domain = """
        using Tl;
        namespace Domain;
        public readonly record struct DamageClip(float Amount);
        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
        }
        public sealed class World { public int Marks; }
        public readonly record struct Entity(int Id);
        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
        }
        """;

    private const string SeparateBake = """
        public readonly struct ApplyDamageBake : IBake<ApplyDamage>
        {
            public static void Bake(World world, Entity entity) { world.Marks++; }
        }
        """;

    [Fact]
    public void DiscoversSeparateBakeStructWithConsumerPairingAndFreeFormParameters()
    {
        var result = Read(Domain + SeparateBake);

        Assert.Empty(result.Diagnostics);
        var bake = Assert.Single(result.Bakes);
        Assert.Equal("global::Domain.ApplyDamageBake", bake.TypeName);
        Assert.Equal("global::Domain.ApplyDamage", bake.ConsumerTypeName);
        Assert.Equal(
        [
            new BakeParameter("global::Domain.World", BakeModifier.Value, false),
            new BakeParameter("global::Domain.Entity", BakeModifier.Value, false),
        ], bake.Parameters);
        var pair = Assert.Single(bake.Pairs);
        Assert.Equal("global::Domain.DamageTrack", pair.TrackTypeName);
        Assert.Equal("global::Domain.DamageClip", pair.ClipTypeName);
    }

    [Fact]
    public void DiscoversOnConsumerBakeWithMixedModifiersAndConsumerParameter()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct DamageClip(float Amount);
            public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
            {
                public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
            }
            public sealed class World { public int Marks; }
            public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>, IBake<ApplyDamage>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
                public static void Bake(in ApplyDamage consumer, in World world) { world.Marks++; }
            }
            """;
        var result = Read(source);

        Assert.Empty(result.Diagnostics);
        var bake = Assert.Single(result.Bakes);
        Assert.Equal("global::Domain.ApplyDamage", bake.TypeName);
        Assert.Equal("global::Domain.ApplyDamage", bake.ConsumerTypeName);
        Assert.Equal(
        [
            new BakeParameter("global::Domain.ApplyDamage", BakeModifier.In, true),
            new BakeParameter("global::Domain.World", BakeModifier.In, false),
        ], bake.Parameters);
        Assert.Single(bake.Pairs);
    }

    [Fact]
    public void DiscoversParameterlessBakeMarkedWithTheOneArgumentInterface()
    {
        const string bake = """
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public static void Bake() { }
            }
            """;
        var result = Read(Domain + bake);

        var declaration = Assert.Single(result.Bakes);
        Assert.Empty(declaration.Parameters);
    }

    [Fact]
    public void DiscoversBakesBeyondTheOldContextCapWithSpanParameters()
    {
        const string bake = """
            public readonly struct C0 { }
            public readonly struct C1 { }
            public readonly struct C2 { }
            public readonly struct C3 { }
            public readonly struct C4 { }
            public readonly struct WideBake : IBake<ApplyDamage>
            {
                public static void Bake(in C0 c0, ref C1 c1, C2 c2, global::System.Span<C3> c3, global::System.ReadOnlySpan<C4> c4) { }
            }
            """;
        var result = Read(Domain + bake);

        Assert.Empty(result.Diagnostics);
        var declaration = Assert.Single(result.Bakes);
        Assert.Equal(
        [
            new BakeParameter("global::Domain.C0", BakeModifier.In, false),
            new BakeParameter("global::Domain.C1", BakeModifier.Ref, false),
            new BakeParameter("global::Domain.C2", BakeModifier.Value, false),
            new BakeParameter("global::System.Span<global::Domain.C3>", BakeModifier.Value, false),
            new BakeParameter("global::System.ReadOnlySpan<global::Domain.C4>", BakeModifier.Value, false),
        ], declaration.Parameters);
    }

    [Fact]
    public void MultipleBakesSortByConsumerThenSignatureThenBakeTypeName()
    {
        const string bakes = """
            public readonly record struct BetaClip(float Amount);
            public readonly record struct BetaTrack(float Gain) : IBlend<BetaClip>
            {
                public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
            }
            public readonly struct ApplyBeta : ITrack<BetaTrack, BetaClip>
            {
                public static void OnActive(in Frame<BetaTrack, BetaClip> frame) { }
            }
            public readonly record struct GammaClip(float Amount);
            public readonly record struct GammaTrack(float Gain) : IBlend<GammaClip>
            {
                public void Blend(in GammaClip first, in GammaClip second, float factor, out GammaClip result) => result = first;
            }
            public readonly struct ApplyGamma : ITrack<GammaTrack, GammaClip>
            {
                public static void OnActive(in Frame<GammaTrack, GammaClip> frame) { }
            }
            public readonly struct ZetaBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            public readonly struct AlphaBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            public readonly struct AlphaWideBake : IBake<ApplyBeta>
            {
                public static void Bake(World world, Entity entity) { }
            }
            public readonly struct NarrowBake : IBake<ApplyGamma>
            {
                public static void Bake(Entity entity) { }
            }
            """;
        var result = Read(Domain + bakes);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(
        [
            "global::Domain.AlphaWideBake",
            "global::Domain.AlphaBake",
            "global::Domain.ZetaBake",
            "global::Domain.NarrowBake",
        ], result.Bakes.Select(static bake => bake.TypeName));
    }

    [Fact]
    public void MultiPairingConsumerBakeCarriesEveryRegisteredPairing()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct AlphaClip(int Value);
            public readonly record struct BetaClip(float Amount);
            public readonly record struct DualTrack(int Code) : IBlend<AlphaClip>, IBlend<BetaClip>
            {
                public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
                public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
            }
            public sealed class World { public int Marks; }
            public readonly struct DualJob : ITrack<DualTrack, AlphaClip>, ITrack<DualTrack, BetaClip>, IBake<DualJob>
            {
                public static void OnActive(in Frame<DualTrack, AlphaClip> frame) { }
                public static void OnActive(in Frame<DualTrack, BetaClip> frame) { }
                public static void Bake(World world) { world.Marks++; }
            }
            """;
        var result = Read(source);

        Assert.Empty(result.Diagnostics);
        var bake = Assert.Single(result.Bakes);
        Assert.Equal(
        [
            ("global::Domain.DualTrack", "global::Domain.AlphaClip"),
            ("global::Domain.DualTrack", "global::Domain.BetaClip"),
        ], bake.Pairs.Select(static pair => (pair.TrackTypeName, pair.ClipTypeName)));
    }

    [Fact]
    public void ValidBakesEmitRegistrationAndThunkIntoTheConsumerBinding()
    {
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain + SeparateBake);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_ApplyDamageBake, global::Tl.TypeKey<global::Domain.World>.Value, global::Tl.TypeKey<global::Domain.Entity>.Value);", binding);
        Assert.Contains("private static void Bake_ApplyDamageBake(byte** __tlArgs)", binding);
        Assert.Contains("ref global::Domain.World __tlArg0 = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::Domain.World>(__tlArgs[0]);", binding);
        Assert.Contains("ref global::Domain.Entity __tlArg1 = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::Domain.Entity>(__tlArgs[1]);", binding);
        Assert.Contains("global::Domain.ApplyDamageBake.Bake(__tlArg0, __tlArg1);", binding);
    }

    [Fact]
    public void MixedModifierBakeForwardsEachDeclaredModifier()
    {
        const string bake = """
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public static void Bake(ApplyDamage consumer, in World world, ref Entity entity) { world.Marks++; entity.Id++; }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain + bake);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_ApplyDamageBake, global::Tl.TypeKey<global::Domain.World>.Value, global::Tl.TypeKey<global::Domain.Entity>.Value);", binding);
        Assert.Contains("global::Domain.ApplyDamage __tlConsumer = default;", binding);
        Assert.Contains("ref global::Domain.World __tlArg0 = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::Domain.World>(__tlArgs[0]);", binding);
        Assert.Contains("ref global::Domain.Entity __tlArg1 = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::Domain.Entity>(__tlArgs[1]);", binding);
        Assert.Contains("global::Domain.ApplyDamageBake.Bake(__tlConsumer, in __tlArg0, ref __tlArg1);", binding);
    }

    [Fact]
    public void ParameterlessBakeEmitsInvokelessRegistration()
    {
        const string bake = """
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public static void Bake() { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain + bake);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Contains("global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_ApplyDamageBake);", binding);
        Assert.Contains("global::Domain.ApplyDamageBake.Bake();", binding);
    }

    [Fact]
    public void MultiPairingBakeEmitsOneRegistrationPerPairingInPairOrder()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct AlphaClip(int Value);
            public readonly record struct BetaClip(float Amount);
            public readonly record struct DualTrack(int Code) : IBlend<AlphaClip>, IBlend<BetaClip>
            {
                public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result) => result = first;
                public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
            }
            public sealed class World { public int Marks; }
            public readonly struct DualJob : ITrack<DualTrack, AlphaClip>, ITrack<DualTrack, BetaClip>, IBake<DualJob>
            {
                public static void OnActive(in Frame<DualTrack, AlphaClip> frame) { }
                public static void OnActive(in Frame<DualTrack, BetaClip> frame) { }
                public static void Bake(World world) { world.Marks++; }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(source);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Equal(
        [
            "global::Tl.BakeRuntime<global::Domain.DualTrack, global::Domain.AlphaClip>.Bake(&Bake_DualJob, global::Tl.TypeKey<global::Domain.World>.Value);",
            "global::Tl.BakeRuntime<global::Domain.DualTrack, global::Domain.BetaClip>.Bake(&Bake_DualJob, global::Tl.TypeKey<global::Domain.World>.Value);",
        ], BakeInstalls(binding));
    }

    [Fact]
    public void BakeInstallsFollowTheSortedBakeOrderAfterTheConsumerInstalls()
    {
        const string bakes = """
            public readonly struct ZetaBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            public readonly struct AlphaBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            public readonly struct NarrowBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain + bakes);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        var install = binding[..binding.IndexOf("}", StringComparison.Ordinal)];
        Assert.Equal(
        [
            "global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Consume(&OnActive_ApplyDamage, &OnActiveRange_ApplyDamage, &Bind_ApplyDamage);",
            "global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_AlphaBake, global::Tl.TypeKey<global::Domain.World>.Value);",
            "global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_NarrowBake, global::Tl.TypeKey<global::Domain.World>.Value);",
            "global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_ZetaBake, global::Tl.TypeKey<global::Domain.World>.Value);",
        ], install.Split('\n')[5..^1]);
    }

    [Fact]
    public void ConsumerOnlyBindingStaysByteIdenticalWithoutBakes()
    {
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.DoesNotContain("Bake_", binding);
        Assert.EndsWith("private static int FindKey(ulong* k, int c, ulong v) { for (var i = 0; i < c; i++) if (k[i] == v) return i; return -1; }\n}\n", binding);
    }

    private static IReadOnlyList<string> BakeInstalls(string binding)
        => binding.Split('\n').Where(static line => line.Contains(".Bake(&Bake_", StringComparison.Ordinal)).ToArray();

    [Fact]
    public void RejectsAbstractBakeDeclarationWithDiagnostic()
    {
        const string bake = """
            public abstract class AbstractBake : IBake<ApplyDamage> { }
            """;
        Rejects(bake, "TLGEN70", "cannot be abstract");
    }

    [Fact]
    public void RejectsNonStructBakeDeclarationWithDiagnostic()
    {
        const string bake = """
            public sealed class ClassBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            """;
        Rejects(bake, "TLGEN70", "must be a struct; bakes are declared on structs");
    }

    [Fact]
    public void RejectsGenericBakeDeclarationWithDiagnostic()
    {
        const string bake = """
            public readonly struct GenericBake<T> : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            """;
        Rejects(bake, "TLGEN70", "cannot be generic; open type parameters cannot be registered as bakes");
    }

    [Fact]
    public void RejectsBakeBoundToUnregisteredConsumerWithDiagnostic()
    {
        const string bake = """
            public readonly struct NotAConsumer { }
            public readonly struct StrayBake : IBake<NotAConsumer>
            {
                public static void Bake(NotAConsumer consumer, World world) { }
            }
            """;
        Rejects(bake, "TLGEN72", "'global::Domain.NotAConsumer', which is not a registered Tl.ITrack consumer");
    }

    [Fact]
    public void RejectsInaccessibleParameterTypeWithDiagnostic()
    {
        const string bake = """
            public static class Host
            {
                private sealed class HiddenWorld { }
                internal readonly struct HiddenBake : IBake<ApplyDamage>
                {
                    public static void Bake(HiddenWorld world) { }
                }
            }
            """;
        Rejects(bake, "TLGEN73", "'global::Domain.Host.HiddenWorld' must be accessible from this compilation");
    }

    [Fact]
    public void RejectsBakeWithoutAMatchingMethodShapeWithDiagnostic()
    {
        Rejects("""
            public readonly struct ApplyDamageBake : IBake<ApplyDamage> { }
            """, "TLGEN71", "must declare exactly one accessible static void Bake");
    }

    [Fact]
    public void RejectsInstanceBakeMethodWithDiagnostic()
    {
        Rejects("""
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public void Bake(World world) { }
            }
            """, "TLGEN71", "must declare exactly one accessible static void Bake");
    }

    [Fact]
    public void RejectsNonVoidBakeWithDiagnostic()
    {
        Rejects("""
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public static int Bake(World world) => 0;
            }
            """, "TLGEN71", "static void Bake");
    }

    [Fact]
    public void RejectsOutOptionalAndParamsBakeParametersWithDiagnostic()
    {
        Rejects("""
            public readonly struct OutBake : IBake<ApplyDamage>
            {
                public static void Bake(out World world) { world = null!; }
            }
            """, "TLGEN71", "out, optional, and params parameters are unsupported");

        Rejects("""
            public readonly struct OptionalBake : IBake<ApplyDamage>
            {
                public static void Bake(World world = null) { }
            }
            """, "TLGEN71", "out, optional, and params parameters are unsupported");

        Rejects("""
            public readonly struct ParamsBake : IBake<ApplyDamage>
            {
                public static void Bake(params Entity[] entities) { }
            }
            """, "TLGEN71", "out, optional, and params parameters are unsupported");
    }

    [Fact]
    public void RejectsSeveralBakeMethodCandidatesWithOneDiagnostic()
    {
        Rejects("""
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
                public static void Bake(Entity entity) { }
            }
            """, "TLGEN71", "must declare exactly one accessible static void Bake");
    }

    [Fact]
    public void RejectsMismatchedParameterListsOnOnePairWithOneLocatedDiagnostic()
    {
        const string bakes = Domain + """
            public readonly struct WorldOnlyBake : IBake<ApplyDamage>
            {
                public static void Bake(World world) { }
            }
            public readonly struct WorldAndEntityBake : IBake<ApplyDamage>
            {
                public static void Bake(in World world, ref Entity entity) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(bakes);

        Assert.Empty(sources);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN74", diagnostic.Id);
        Assert.Contains("'global::Domain.WorldAndEntityBake' and 'global::Domain.WorldOnlyBake' bind pair (global::Domain.DamageTrack, global::Domain.DamageClip) with different bake parameter lists: 'global::Domain.WorldAndEntityBake.Bake(in global::Domain.World world, ref global::Domain.Entity entity)' and 'global::Domain.WorldOnlyBake.Bake(global::Domain.World world)'", diagnostic.GetMessage());
    }

    [Fact]
    public void ConflictDroppedBakeLeavesItsOtherPairsUnseeded()
    {
        const string bakes = """
            public readonly record struct BetaClip(float Amount);
            public readonly record struct BetaTrack(float Gain) : IBlend<BetaClip>
            {
                public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result) => result = first;
            }
            public readonly struct BetaJob : ITrack<BetaTrack, BetaClip>
            {
                public static void OnActive(in Frame<BetaTrack, BetaClip> frame) { }
            }
            public readonly struct DualJob : ITrack<DamageTrack, DamageClip>, ITrack<BetaTrack, BetaClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame) { }
                public static void OnActive(in Frame<BetaTrack, BetaClip> frame) { }
            }
            public readonly struct AnchorBake : IBake<ApplyDamage>
            {
                public static void Bake(Entity entity) { }
            }
            public readonly struct DualBake : IBake<DualJob>
            {
                public static void Bake(World world) { }
            }
            public readonly struct GoodBake : IBake<BetaJob>
            {
                public static void Bake(Entity entity) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain + bakes);

        Assert.Empty(sources);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TLGEN74", diagnostic.Id);
        Assert.Contains("'global::Domain.AnchorBake' and 'global::Domain.DualBake' bind pair (global::Domain.DamageTrack, global::Domain.DamageClip)", diagnostic.GetMessage());
        Assert.DoesNotContain("GoodBake", diagnostic.GetMessage());
        Assert.DoesNotContain("BetaTrack", diagnostic.GetMessage());
    }

    [Fact]
    public void SameSignatureBakesOnOnePairCompose()
    {
        const string bakes = Domain + """
            public readonly struct FirstBake : IBake<ApplyDamage>
            {
                public static void Bake(in World world, ref Entity entity) { world.Marks++; }
            }
            public readonly struct SecondBake : IBake<ApplyDamage>
            {
                public static void Bake(in World world, ref Entity entity) { }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(bakes);

        Assert.Empty(diagnostics);
        var binding = Assert.Single(sources).Value;
        Assert.Equal(2, binding.Split('\n').Count(static line => line.Contains(".Bake(&Bake_", StringComparison.Ordinal)));
    }

    [Fact]
    public void BakeDiagnosticsUseTheBakeDeclarationSourceSpan()
    {
        const string stray = Domain + """
            public readonly struct NotAConsumer { }
            public readonly struct StrayBake : IBake<NotAConsumer>
            {
                public static void Bake(NotAConsumer consumer, World world) { }
            }
            """;
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = CSharpSyntaxTree.ParseText(stray, options, "Domain.cs");
        var compilation = CSharpCompilation.Create("BakeReaderSpan" + Guid.NewGuid().ToString("N"),
            [tree], ReferencePaths().Select(static path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGenerators(compilation);

        var declaration = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Single(static type => type.Identifier.ValueText == "StrayBake");
        var diagnostic = Assert.Single(driver.GetRunResult().Diagnostics, static candidate => candidate.Id == "TLGEN72");
        Assert.Equal(declaration.Span, diagnostic.Location.SourceSpan);
        Assert.Equal("Domain.cs", diagnostic.Location.GetLineSpan().Path);
        Assert.Equal("Tl.Generation", diagnostic.Descriptor.Category);
    }

    [Fact]
    public void BakeBindingIsCommittedByteIdentically()
    {
        const string source = """
            using Tl;
            namespace Domain;
            public readonly record struct DamageClip(float Amount);
            public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
            {
                public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result) => result = first;
            }
            public struct Health { public float Value; }
            public sealed class World { public int Marks; }
            public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
            {
                public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref Health health) { }
            }
            public readonly struct ApplyDamageBake : IBake<ApplyDamage>
            {
                public static void Bake(in World world) { world.Marks++; }
            }
            """;
        var (sources, diagnostics) = GenerateWithDiagnostics(source);

        Assert.Empty(diagnostics);
        Assert.Equal(BakeBinding, Assert.Single(sources).Value);
    }

    private const string BakeBinding = """"
        internal static unsafe class TlConsumerBinding
        {
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Install()
        {
        global::Tl.PairRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Consume(&OnActive_ApplyDamage, &OnActiveRange_ApplyDamage, &Bind_ApplyDamage);
        global::Tl.BakeRuntime<global::Domain.DamageTrack, global::Domain.DamageClip>.Bake(&Bake_ApplyDamageBake, global::Tl.TypeKey<global::Domain.World>.Value);
        }
        private static void OnActive_ApplyDamage(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)
        {
        global::Domain.DamageClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.DamageTrack, global::Domain.DamageClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);
        var @health = (global::Domain.Health*)__tlColumns[0];
        global::Domain.ApplyDamage.OnActive(in __tlTyped, ref @health[__tlRow]);
        }
        private static void OnActiveRange_ApplyDamage(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRowStart, int __tlRowCount)
        {
        global::Domain.DamageClip __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<global::Domain.DamageTrack, global::Domain.DamageClip>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);
        var @health = (global::Domain.Health*)__tlColumns[0];
        for (var __tlRow = __tlRowStart; __tlRow < __tlRowStart + __tlRowCount; __tlRow++)
        global::Domain.ApplyDamage.OnActive(in __tlTyped, ref @health[__tlRow]);
        }
        private static void Bind_ApplyDamage(ulong* __tlKeys, int __tlKeyCount, byte* __tlIndices)
        {
        var __tlIdx0 = FindKey(__tlKeys, __tlKeyCount, global::Tl.TypeKey<global::Domain.Health>.Value);
        if (__tlIdx0 < 0) throw new global::System.ArgumentException("global::Domain.ApplyDamage: required column missing for registered consumer: global::Domain.Health");
        __tlIndices[0] = (byte)(__tlIdx0 + 1);
        }
        private static void Bake_ApplyDamageBake(byte** __tlArgs)
        {
        ref global::Domain.World __tlArg0 = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::Domain.World>(__tlArgs[0]);
        global::Domain.ApplyDamageBake.Bake(in __tlArg0);
        }
        private static int FindKey(ulong* k, int c, ulong v) { for (var i = 0; i < c; i++) if (k[i] == v) return i; return -1; }
        }

        """";

    private static void Rejects(string bake, string code, string fragment)
    {
        var (sources, diagnostics) = GenerateWithDiagnostics(Domain + bake);
        Assert.Empty(sources);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(code, diagnostic.Id);
        Assert.Contains(fragment, diagnostic.GetMessage());
    }

    private static JobReadResult Read(string source)
        => JobReader.Read(Compile(source));

    private static (Dictionary<string, string> Sources, ImmutableArray<Diagnostic> Diagnostics) GenerateWithDiagnostics(string source)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = Compile(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        var sources = driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(static generated => generated.HintName, static generated => generated.SourceText.ToString(), StringComparer.Ordinal);
        return (sources, diagnostics);
    }

    private static CSharpCompilation Compile(string source)
        => CSharpCompilation.Create("BakeReaderTests" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "Domain.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

    private static IEnumerable<MetadataReference> References()
        => ReferencePaths().Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));

    internal static string[] ReferencePaths()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(IBake<>).Assembly.Location).Distinct(StringComparer.Ordinal).ToArray();
}
