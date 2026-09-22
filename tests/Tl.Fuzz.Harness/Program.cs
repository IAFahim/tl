using System.Text;
using SharpFuzz;
using Tl.Gen.Tlb;

using FuzzDomain;
namespace Tl.Fuzz.Harness;

public static class Program
{
    const string JsonFixture = """
    {"name":"harness","duration":9,"loop":true,"tracks":[
      {"name":"t1","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":0.5},"clips":[
        {"name":"a","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":5,"data":{"Value":1.5}},
        {"name":"b","namespace":"FuzzDomain","type":"FuzzJsonClip","start":4,"end":9,"data":{"Value":3}}]}
    ]}
    """;

    const string JsonPairFixture = """
    {"name":"harness_pair","duration":12,"loop":false,"tracks":[
      {"name":"x","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":1},"clips":[
        {"name":"a","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":6,"data":{"Value":2}},
        {"name":"b","namespace":"FuzzDomain","type":"FuzzJsonClip","start":6,"end":12,"data":{"Value":4}}]},
      {"name":"y","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":0.25},"clips":[
        {"name":"c","namespace":"FuzzDomain","type":"FuzzJsonClip","start":2,"end":10,"data":{"Value":8}}]}
    ]}
    """;

    public static void Main(string[] args)
    {
        var mode = Environment.GetEnvironmentVariable("TL_FUZZ_TARGET");
        if (mode == "seed" && args.Length > 0)
        {
            WriteSeeds(args[0]);
            return;
        }
        switch (mode)
        {
            case "tlb":
                Fuzzer.OutOfProcess.Run(stream => FuzzTlb(stream));
                break;
            case "json":
                Fuzzer.OutOfProcess.Run(stream => FuzzJson(stream, false));
                break;
            case "json-auto":
                Fuzzer.OutOfProcess.Run(stream => FuzzJson(stream, true));
                break;
            default:
                Console.Error.WriteLine("set TL_FUZZ_TARGET to tlb, json, json-auto, or 'seed <dir>'");
                Environment.Exit(2);
                break;
        }
    }

    static void FuzzTlb(Stream stream)
    {
        var input = ReadAll(stream);
        try
        {
            using var asset = TimelineAsset.Of(TimelineAsset.Load(input));
        }
        catch (Exception e) when (FuzzContract.IsLocatedTlbDiagnostic(e) || FuzzContract.IsDocumentedCapacity(e))
        {
        }
    }

    static void FuzzJson(Stream stream, bool autoNamespace)
    {
        var input = ReadAll(stream);
        try
        {
            TimelineBaker.BakeJson(input, Resolver(), autoNamespace);
        }
        catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
        {
        }
    }

    static byte[] ReadAll(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    static BakerAssemblyResolver Resolver() => BakerAssemblyResolver.FromAssemblies([typeof(Tl.Fuzz.FuzzJsonTrack).Assembly]);

    static void WriteSeeds(string directory)
    {
        var tlb = Path.Combine(directory, "tlb");
        var json = Path.Combine(directory, "json");
        var jsonAuto = Path.Combine(directory, "json-auto");
        Directory.CreateDirectory(tlb);
        Directory.CreateDirectory(json);
        Directory.CreateDirectory(jsonAuto);

        var assets = new List<byte[]>
        {
            FuzzBake.Bake(0, false, 1f, []),
            FuzzBake.Bake(1, true, 0.5f, [new FuzzClipSpec(0, 1, 1f)]),
            FuzzBake.Bake(17, false, 1f, [new FuzzClipSpec(0, 8, 1f), new FuzzClipSpec(7, 17, 2f)]),
            FuzzBake.Bake(255, true, 0.25f, [new FuzzClipSpec(0, 64, 1f), new FuzzClipSpec(63, 255, 2f)]),
            BakeJsonUtf8(Encoding.UTF8.GetBytes(JsonFixture)),
            BakeJsonUtf8(Encoding.UTF8.GetBytes(JsonPairFixture)),
        };
        for (var i = 0; i < assets.Count; i++)
            File.WriteAllBytes(Path.Combine(tlb, $"valid_{i}.tlb"), assets[i]);
        File.WriteAllBytes(Path.Combine(tlb, "truncated.tlb"), assets[^2].AsSpan(0, 40).ToArray());
        File.WriteAllBytes(Path.Combine(tlb, "header.tlb"), assets[^1].AsSpan(0, 64).ToArray());

        File.WriteAllText(Path.Combine(json, "single.json"), JsonFixture);
        File.WriteAllText(Path.Combine(json, "pair.json"), JsonPairFixture);
        File.WriteAllText(Path.Combine(json, "invalid.json"), """{"name":"broken","duration":4,"tracks":[{"name":"t","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":1},"clips":[{"name":"c","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":4,"data":{"Nope":1}}]}]}""");
        foreach (var file in Directory.GetFiles(json))
            File.Copy(file, Path.Combine(jsonAuto, Path.GetFileName(file)));
    }

    static byte[] BakeJsonUtf8(byte[] utf8) => TimelineBaker.BakeJson(utf8, Resolver());
}
