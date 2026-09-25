using System.Buffers.Binary;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Gen.CSharp.Consumer.Tests;

public sealed class GeneratorLayoutGuardTests
{
    private const string Json = """
        {
          "duration": 4, "loop": false,
          "tracks": [
            {
              "namespace": "Guard", "type": "JabTrack", "data": { "Scale": 2.0 },
              "clips": [ { "namespace": "Guard", "type": "JabClip", "data": { "Force": 5.0 }, "start": 0, "end": 4 } ]
            }
          ]
        }
        """;

    private static readonly Lazy<Assembly> Fixture = new(Compile);

    [Fact]
    public void BakedPairCarriesTheGeneratedLayoutAndFoldsItWithoutManualRegistration()
    {
        var baked = Bake();

        Assert.NotEqual(0UL, PairLayoutWord(baked));
        Assert.StartsWith("OK|", Play(baked));
    }

    [Fact]
    public void TamperedLayoutWordFailsTheTypedFoldWithTheRebakeRepair()
    {
        var baked = Bake();
        var pairOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(28));
        var layout = BinaryPrimitives.ReadUInt64LittleEndian(baked.AsSpan(pairOffset + 40));
        BinaryPrimitives.WriteUInt64LittleEndian(baked.AsSpan(pairOffset + 40), layout ^ 0x5555AAAA5555AAAAul);

        var result = Play(baked);

        Assert.StartsWith("THROWN|", result);
        Assert.Contains("different field layout", result, StringComparison.Ordinal);
        Assert.Contains("JabTrack", result, StringComparison.Ordinal);
        Assert.Contains("JabClip", result, StringComparison.Ordinal);
        Assert.Contains("rebake", result, StringComparison.Ordinal);
    }

    private static ulong PairLayoutWord(byte[] baked)
    {
        var pairOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(28));
        return BinaryPrimitives.ReadUInt64LittleEndian(baked.AsSpan(pairOffset + 40));
    }

    private static string Play(byte[] baked)
        => (string)Fixture.Value.GetType("Guard.GuardPlay")!.GetMethod("Play", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [baked])!;

    private static byte[] Bake()
        => TimelineBaker.BakeJson(Json, BakerAssemblyResolver.FromAssemblies([Fixture.Value]));

    private static Assembly Compile()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("GeneratorLayoutGuardFixture",
            [CSharpSyntaxTree.ParseText(Guard, options, "Guard.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> References()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path));

    private const string Guard = """
        using System;
        using System.Globalization;
        using Tl;

        namespace Guard;

        public readonly record struct JabClip(float Force);

        public readonly record struct JabTrack(float Scale) : IBlend<JabClip>
        {
            public void Blend(in JabClip first, in JabClip second, float factor, out JabClip result)
                => result = new JabClip(first.Force + (second.Force - first.Force) * factor);
        }

        public readonly struct JabApply : ITrack<JabTrack, JabClip>
        {
            public static void OnActive(in Frame<JabTrack, JabClip> frame, ref float total)
                => total += frame.Direction * frame.Clip.Force * frame.Track.Scale;
        }

        public static class GuardPlay
        {
            public static string Play(byte[] tlb)
            {
                try
                {
                    var id = TimelineAsset.Load(tlb);
                    var positions = new ushort[1];
                    var next = new ushort[1];
                    var effects = new float[1];
                    Timeline<JabTrack, JabClip>.Apply(id, positions, next, true, effects);
                    return "OK|" + effects[0].ToString(CultureInfo.InvariantCulture);
                }
                catch (ArgumentException ex)
                {
                    return "THROWN|" + ex.Message;
                }
            }
        }
        """;
}
