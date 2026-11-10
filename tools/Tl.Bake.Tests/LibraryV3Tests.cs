using System.Buffers.Binary;
using System.Text.Json;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed unsafe class LibraryV3Tests
{
    const string BlendJson = """
    {
      "name": "blend",
      "duration": 20,
      "loop": true,
      "tracks": [
        {
          "namespace": "Tlb",
          "type": "AlphaTrack",
          "data": { "Code": 1 },
          "clips": [
            { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 12, "data": { "Value": 5 } },
            { "namespace": "Tlb", "type": "AlphaClip", "start": 8, "end": 20, "data": { "Value": 9 } }
          ]
        }
      ]
    }
    """;

    const string HoldJson = """
    {
      "name": "hold",
      "duration": 10,
      "loop": false,
      "tracks": [
        {
          "namespace": "Tlb",
          "type": "AlphaTrack",
          "data": { "Code": 2 },
          "clips": [
            { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 10, "data": { "Value": 3 } }
          ]
        }
      ]
    }
    """;

    static BakerAssemblyResolver Resolver() =>
        BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

    static List<LibraryPart> Parts()
    {
        var resolver = Resolver();
        return
        [
            new LibraryPart { Name = "blend", Tlb = TimelineBaker.BakeJson(BlendJson, resolver) },
            new LibraryPart { Name = "hold", Tlb = TimelineBaker.BakeJson(HoldJson, resolver) },
        ];
    }

    [Fact]
    public void TlbPartsConvertToAVerifiableV3Blob()
    {
        var parts = Parts();
        var bytes = LibraryV3Bake.AssembleV3(parts);
        Assert.Equal(0x42494C54u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
        var result = Library.Verify(bytes);
        Assert.True(result.Ok);
        using var library = result.Library!;
        Assert.Equal(2, library.TimelineCount);
        Assert.Equal(20u, library.Duration(0));
        Assert.Equal(10u, library.Duration(1));
        Assert.True(library.Loops(0));
        Assert.False(library.Loops(1));
        Assert.Equal(library.StepCount, parts.Sum(p => SpecsOf(p).Count));
    }

    static List<(uint Start, uint End, ulong Key)> SpecsOf(LibraryPart part)
    {
        var specs = LibraryV3Bake.Specs([part]);
        var spec = Assert.Single(specs);
        return spec.Steps.Select(s => (s.WindowStart, s.WindowEnd, s.PairKey)).ToList();
    }

    [Fact]
    public void ConversionIsDeterministicAndRoundTrips()
    {
        var first = LibraryV3Bake.AssembleV3(Parts());
        var second = LibraryV3Bake.AssembleV3(Parts());
        Assert.Equal(first, second);
        var one = Library.Verify(first);
        Assert.True(one.Ok);
        using var library = one.Library!;
        var again = Library.Verify(library.Blob);
        Assert.True(again.Ok);
        using var reloaded = again.Library!;
        Assert.Equal(library.TimelineCount, reloaded.TimelineCount);
        Assert.Equal(library.StepCount, reloaded.StepCount);
    }

    [Fact]
    public void SpecWindowsMatchSlotWindows()
    {
        var resolver = Resolver();
        var tlb = TimelineBaker.BakeJson(BlendJson, resolver);
        var specs = LibraryV3Bake.Specs([new LibraryPart { Name = "blend", Tlb = tlb }]);
        var spec = Assert.Single(specs);
        Assert.Equal(20u, spec.Duration);
        Assert.True(spec.Loop);
        Assert.True(spec.Steps.Length >= 2);
        Assert.Contains(spec.Steps, s => s.WindowStart == 0u && s.WindowEnd == 12u);
        Assert.Contains(spec.Steps, s => s.WindowStart == 8u && s.WindowEnd == 20u);
        foreach (var step in spec.Steps)
        {
            Assert.True(step.WindowStart < step.WindowEnd);
            Assert.True(step.WindowEnd <= spec.Duration);
            Assert.Equal((int)(step.WindowEnd - step.WindowStart), step.ForwardLane.Length);
            Assert.Equal((int)(step.WindowEnd - step.WindowStart), step.BackwardLane.Length);
        }
    }
    [Fact]
    public void PairsWithoutBakedFoldEmitZeroLanes()
    {
        var bytes = LibraryV3Bake.AssembleV3(Parts());
        var result = Library.Verify(bytes);
        Assert.True(result.Ok);
        using var library = result.Library!;
        for (var step = 0; step < library.StepCount; step++)
        {
            foreach (var value in library.ForwardPlane(step))
                Assert.Equal(0f, value);
            foreach (var value in library.BackwardPlane(step))
                Assert.Equal(0f, value);
            var row = library.Step(step);
            Assert.Equal(0u, row.KindId);
        }
    }

    [Fact]
    public void EmptyPartsGiveAnEmptyVerifiableLibrary()
    {
        var bytes = LibraryV3Bake.AssembleV3([]);
        var result = Library.Verify(bytes);
        Assert.True(result.Ok);
        using var library = result.Library!;
        Assert.Equal(0, library.TimelineCount);
    }

    [Fact]
    public void InvalidTlbIsRejectedWithTimelineName()
    {
        var bad = new LibraryPart { Name = "broken", Tlb = [1, 2, 3] };
        var error = Assert.Throws<BakeDiagnosticException>(() => LibraryV3Bake.AssembleV3([bad]));
        Assert.Contains("broken", error.Message);
    }

    [Fact]
    public void InspectionIsV3OnlyAndRepeatable()
    {
        var bytes = LibraryV3Bake.AssembleV3(Parts());
        Assert.True(Tlib3Inspection.IsV3(bytes));
        var first = Tlib3Inspection.Inspect(bytes);
        Assert.Equal(first, Tlib3Inspection.Inspect(bytes));
        using var doc = JsonDocument.Parse(first);
        Assert.Equal(3, doc.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("header").GetProperty("timelineCount").GetInt32());
        Assert.False(Tlib3Inspection.IsV3(TimelineBaker.BakeJson(HoldJson, Resolver())));
    }

    [Fact]
    public void InspectionRejectsCorruptV3()
    {
        var bytes = LibraryV3Bake.AssembleV3(Parts());
        bytes[200] ^= 0xFF;
        Assert.Throws<BakeDiagnosticException>(() => Tlib3Inspection.Inspect(bytes));
    }

    [Fact]
    public void LibraryPackFormatV3EmitsVerifiableBytesAndInspectReadsThem()
    {
        var temp = Path.Combine(Path.GetTempPath(), "tl-lib-v3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.Copy(GoldenLibrary("jump.json"), Path.Combine(temp, "jump.json"));
            File.Copy(GoldenLibrary("slide.json"), Path.Combine(temp, "slide.json"));
            File.Copy(GoldenLibrary("tl.project.json"), Path.Combine(temp, "tl.project.json"));
            var assembly = typeof(Tlb.AlphaTrack).Assembly.Location;
            Assert.Equal(0, Program.Main(["library", temp, "--assembly", assembly, "--format", "v3"]));
            var bytes = File.ReadAllBytes(Path.Combine(temp, "game.tlib"));
            Assert.Equal(0x42494C54u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
            Assert.Equal(3u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
            var result = Library.Verify(bytes);
            Assert.True(result.Ok);
            using var library = result.Library!;
            Assert.Equal(2, library.TimelineCount);
            Assert.Equal(0, Program.Main(["--inspect", Path.Combine(temp, "game.tlib")]));
            var outJson = Path.Combine(temp, "decoded.json");
            Assert.Equal(0, Program.Main(["decode", Path.Combine(temp, "game.tlib"), "-o", outJson, "--assembly", assembly]));
            using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
            Assert.Equal(3, doc.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(1, LibraryPack.Run([temp, "--assembly", assembly, "--format", "v1"]));
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    static string GoldenLibrary(string name)
    {
        var dir = Path.GetDirectoryName(typeof(LibraryV3Tests).Assembly.Location)!;
        return Path.Combine(dir, "golden", "library", name);
    }
}
