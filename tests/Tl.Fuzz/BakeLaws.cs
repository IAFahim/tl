using System.Text;
using Tl.Gen.Tlb;
using Xunit;
using Xunit.Sdk;

using FuzzDomain;
namespace Tl.Fuzz;

public unsafe class BakeLaws
{
    static BakeLaws() => FuzzPairs.Install();
    const string SingleTrackJson = """
    {"name":"laws_asset","duration":9,"loop":true,"tracks":[
      {"name":"t1","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":0.5},"clips":[
        {"name":"a","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":5,"data":{"Value":1.5}},
        {"name":"b","namespace":"FuzzDomain","type":"FuzzJsonClip","start":4,"end":9,"data":{"Value":3}}]}
    ]}
    """;

    const string TwoTrackJson = """
    {"name":"laws_pair","duration":12,"loop":false,"tracks":[
      {"name":"x","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":1},"clips":[
        {"name":"a","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":6,"data":{"Value":2}},
        {"name":"b","namespace":"FuzzDomain","type":"FuzzJsonClip","start":6,"end":12,"data":{"Value":4}}]},
      {"name":"y","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":0.25},"clips":[
        {"name":"c","namespace":"FuzzDomain","type":"FuzzJsonClip","start":2,"end":10,"data":{"Value":8}}]}
    ]}
    """;

    [Fact]
    public void BakeJsonIsByteDeterministic()
    {
        foreach (var (json, context) in new[] { (SingleTrackJson, "single"), (TwoTrackJson, "pair") })
        {
            var first = Bake(json);
            var second = Bake(json);
            if (!first.AsSpan().SequenceEqual(second))
                throw new XunitException($"JSON bake is not deterministic for the {context} fixture");
        }
    }

    [Fact]
    public void JsonBakeLoadsAndFoldsLikeTheAuthoredModel()
    {
        var bytes = Bake(SingleTrackJson);
        var model = new FuzzModel(9, true, 0.5f,
        [
            new FuzzClipSpec(0, 5, 1.5f),
            new FuzzClipSpec(4, 9, 3f),
        ]);
        using var asset = TimelineAsset.Of(TimelineAsset.Load(bytes));
        var view = Timeline<FuzzJsonTrack, FuzzJsonClip>.View(asset.Index);
        for (ushort tick = 0; tick <= 9; tick++)
        {
            if (view.Forward[tick] != model.Forward(tick))
                throw new XunitException($"JSON fold diverged at tick {tick}: {view.Forward[tick]} != {model.Forward(tick)}");
        }
        var viewBackwardTick = (ushort)(9 - 1);
        var backwardPosition = (ushort)9;
        var reached = (ushort)(backwardPosition - 1);
        if (view.Backward[viewBackwardTick] != model.Scale * model.ClipSum(reached))
            throw new XunitException($"JSON backward fold diverged at tick {viewBackwardTick}");
    }

    [Fact]
    public void JsonBakeRejectsUnknownFieldsWithLocatedDiagnostics()
    {
        foreach (var json in new[]
        {
            """{"name":"a","duration":1,"unknown":true}""",
            """{"name":"a","duration":1,"tracks":[{"nope":1}]}""",
            """{"name":"a","duration":1,"tracks":[{"name":"t","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":1},"clips":[{"name":"c","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":1,"data":{"Nope":1}}]}]}""",
        })
        {
            try
            {
                Bake(json);
                throw new XunitException($"invalid authoring JSON baked successfully: {json}");
            }
            catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
            {
                if (e.Message.Length < 8)
                    throw new XunitException($"diagnostic for {json} is not located or descriptive: {e.Message}");
            }
        }
    }

    [Fact]
    public void JsonBakeHonorsOverlapValidation()
    {
        try
        {
            Bake("""
            {"name":"a","duration":4,"tracks":[
              {"name":"t","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":1},"clips":[
                {"name":"a","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":4,"data":{"Value":1}},
                {"name":"b","namespace":"FuzzDomain","type":"FuzzJsonClip","start":2,"end":4,"data":{"Value":1}},
                {"name":"c","namespace":"FuzzDomain","type":"FuzzJsonClip","start":2,"end":4,"data":{"Value":2}}]}
            ]}
            """);
        }
        catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
        {
            return;
        }
        throw new XunitException("three overlapping clips must fail bake validation with a located diagnostic");
    }

    internal static byte[] Bake(string json) => TimelineBaker.BakeJson(json, BakerAssemblyResolver.FromAssemblies([typeof(FuzzPairs).Assembly]));

    internal static byte[] Bake(byte[] utf8) => TimelineBaker.BakeJson(utf8, BakerAssemblyResolver.FromAssemblies([typeof(FuzzPairs).Assembly]));

    internal static string[] Fixtures() => [SingleTrackJson, TwoTrackJson];

    internal static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);
}
