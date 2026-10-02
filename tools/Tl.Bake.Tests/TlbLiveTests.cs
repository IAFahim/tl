using System.Text.Json;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbLiveTests
{
    static readonly string AssemblyPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "Tl.Bake.LiveHost.dll");

    const string LiveJson = """
    {
      "duration": 12,
      "loop": false,
      "tracks": [
        {
          "name": "t",
          "namespace": "LiveHost",
          "type": "LiveTrack",
          "data": { "Scale": 2 },
          "clips": [
            { "name": "c", "namespace": "LiveHost", "type": "LiveClip", "start": 0, "end": 12, "data": { "Value": 5 } }
          ]
        }
      ]
    }
    """;

    const string ForeignJson = """
    {
      "duration": 6,
      "loop": true,
      "tracks": [
        {
          "name": "t",
          "namespace": "LiveHost",
          "type": "ForeignTrack",
          "data": { "Scale": 1 },
          "clips": [
            { "name": "c", "namespace": "LiveHost", "type": "ForeignClip", "start": 0, "end": 6, "data": { "Amount": 3 } }
          ]
        }
      ]
    }
    """;

    static (int Exit, string Stdout, string Stderr) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            return (Program.Main(args), stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    static string BakeToTemp(string json, string stem = "tlb_live_")
    {
        var path = Path.Combine(Path.GetTempPath(), stem + Guid.NewGuid().ToString("N") + ".tlb");
        File.WriteAllBytes(path, TimelineBaker.BakeJson(json, BakerAssemblyResolver.FromAssemblies([typeof(LiveHost.LiveTrack).Assembly])));
        return path;
    }

    [Fact]
    public void Live_ResolvesAssets_FoldsAndReportsFoldedNow()
    {
        var asset = BakeToTemp(LiveJson);
        try
        {
            var first = Run("--live", "--assembly", AssemblyPath, "--asset", asset, "--resolve", "--json");
            Assert.Equal(0, first.Exit);
            var document = JsonDocument.Parse(first.Stdout);
            Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("live", document.RootElement.GetProperty("plane").GetString());
            Assert.True(document.RootElement.GetProperty("deep").GetProperty("available").GetBoolean());
            var tables = document.RootElement.GetProperty("tables");
            Assert.True(tables.GetProperty("intern").GetProperty("live").GetInt64() >= 1);
            Assert.True(tables.GetProperty("pair").GetProperty("pairs").GetInt64() >= 1);
            var banks = document.RootElement.GetProperty("banks");
            var bank = First(banks, "LiveHost.LiveTrack");
            var index = document.RootElement.GetProperty("assets")[0].GetProperty("index").GetInt32();
            var view = ViewAt(bank.GetProperty("views"), index);
            Assert.Contains(view.GetProperty("foldState").GetString(), new[] { "folded", "folded-now" });
            Assert.Equal(12, view.GetProperty("duration").GetInt32());
            Assert.False(view.GetProperty("looping").GetBoolean());
            Assert.Equal(13, view.GetProperty("ticks").GetInt32());
            Assert.Equal(1, view.GetProperty("abiVersion").GetInt32());
            var bytes = bank.GetProperty("bytes");
            Assert.Equal(
                bytes.GetProperty("retained").GetInt64(),
                bytes.GetProperty("header").GetInt64() + bytes.GetProperty("table").GetInt64() + bytes.GetProperty("directory").GetInt64() + bytes.GetProperty("arena").GetInt64());

            var second = Run("--live", "--assembly", AssemblyPath, "--asset", asset, "--resolve", "--json");
            Assert.Equal(0, second.Exit);
            var rerun = JsonDocument.Parse(second.Stdout);
            Assert.Equal("folded", ViewAt(First(rerun.RootElement.GetProperty("banks"), "LiveHost.LiveTrack").GetProperty("views"), index).GetProperty("foldState").GetString());
            Assert.Equal(index, rerun.RootElement.GetProperty("assets")[0].GetProperty("index").GetInt32());
        }
        finally
        {
            File.Delete(asset);
        }
    }

    [Fact]
    public void Live_ClassifiesAbsentAndPendingAcrossBanks()
    {
        var live = BakeToTemp(LiveJson);
        var foreign = BakeToTemp(ForeignJson, "tlb_foreign_");
        try
        {
            var result = Run("--live", "--assembly", AssemblyPath, "--asset", foreign, "--asset", live, "--resolve", "--json");
            Assert.Equal(0, result.Exit);
            var document = JsonDocument.Parse(result.Stdout);
            var assets = document.RootElement.GetProperty("assets");
            Assert.Equal(2, assets.GetArrayLength());
            var liveIndex = AssetIndex(assets, "live");
            var foreignIndex = AssetIndex(assets, "tlb_foreign_");
            var own = First(document.RootElement.GetProperty("banks"), "LiveHost.LiveTrack").GetProperty("views");
            Assert.Contains(FoldState(own, liveIndex), new[] { "folded", "folded-now" });
            Assert.Equal("absent", FoldState(own, foreignIndex));
            var foreignBank = First(document.RootElement.GetProperty("banks"), "LiveHost.ForeignTrack").GetProperty("views");
            Assert.Equal("absent", FoldState(foreignBank, liveIndex));
            Assert.Equal("pending", FoldState(foreignBank, foreignIndex));
        }
        finally
        {
            File.Delete(live);
            File.Delete(foreign);
        }
    }

    [Fact]
    public void Live_WithoutResolveReportsNoBanks()
    {
        var asset = BakeToTemp(LiveJson);
        try
        {
            var result = Run("--live", "--assembly", AssemblyPath, "--asset", asset, "--json");
            Assert.Equal(0, result.Exit);
            var document = JsonDocument.Parse(result.Stdout);
            var banks = document.RootElement.GetProperty("banks");
            for (var i = 0; i < banks.GetArrayLength(); i++)
            {
                var views = banks[i].GetProperty("views");
                for (var v = 0; v < views.GetArrayLength(); v++)
                    Assert.NotEqual("folded-now", views[v].GetProperty("foldState").GetString());
            }
            Assert.True(document.RootElement.GetProperty("tables").GetProperty("intern").GetProperty("live").GetInt64() >= 1);
        }
        finally
        {
            File.Delete(asset);
        }
    }

    [Fact]
    public void Live_SummaryPlanePrintsCompactLines()
    {
        var asset = BakeToTemp(LiveJson);
        try
        {
            var result = Run("--live", "--assembly", AssemblyPath, "--asset", asset, "--resolve", "--summary");
            Assert.Equal(0, result.Exit);
            Assert.DoesNotContain("{", result.Stdout);
            Assert.Contains("engine Tl.Core ", result.Stdout);
            Assert.Contains("bank LiveHost.LiveTrack,LiveHost.LiveClip views ", result.Stdout);
            Assert.Contains("duration 12 ticks 13 lanes 1", result.Stdout);
        }
        finally
        {
            File.Delete(asset);
        }
    }

    [Fact]
    public void Live_InvalidAssetExitsThree()
    {
        var bad = Path.Combine(Path.GetTempPath(), "tlb_live_bad_" + Guid.NewGuid().ToString("N") + ".tlb");
        File.WriteAllText(bad, "{\"not\":\"a baked timeline asset\"} " + new string('x', 64));
        try
        {
            var result = Run("--live", "--assembly", AssemblyPath, "--asset", bad);
            Assert.Equal(3, result.Exit);
            Assert.Contains("Asset error: TLB magic or version invalid", result.Stderr);
        }
        finally
        {
            File.Delete(bad);
        }
    }

    [Fact]
    public void Live_MissingAssemblyExitsFour()
    {
        var result = Run("--live", "--assembly", Path.Combine(Path.GetTempPath(), "tlb_missing_" + Guid.NewGuid().ToString("N") + ".dll"));
        Assert.Equal(4, result.Exit);
        Assert.Contains("Host error:", result.Stderr);
    }

    [Fact]
    public void Live_UnknownOptionAndMissingAssetExitTwo()
    {
        Assert.Equal(2, Run("--live", "--bogus").Exit);
        var missing = Run("--live", "--assembly", AssemblyPath, "--asset", Path.Combine(Path.GetTempPath(), "tlb_absent_" + Guid.NewGuid().ToString("N") + ".tlb"));
        Assert.Equal(2, missing.Exit);
        Assert.Contains("does not exist", missing.Stderr);
    }

    static JsonElement First(JsonElement banks, string track)
    {
        for (var i = 0; i < banks.GetArrayLength(); i++)
            if (banks[i].GetProperty("track").GetString() == track)
                return banks[i];
        throw new Xunit.Sdk.XunitException($"bank {track} missing");
    }

    static JsonElement ViewAt(JsonElement views, int index)
    {
        for (var i = 0; i < views.GetArrayLength(); i++)
            if (views[i].GetProperty("index").GetInt32() == index)
                return views[i];
        throw new Xunit.Sdk.XunitException($"view {index} missing");
    }

    static int AssetIndex(JsonElement assets, string contains)
    {
        for (var i = 0; i < assets.GetArrayLength(); i++)
            if (assets[i].GetProperty("file").GetString()!.Contains(contains))
                return assets[i].GetProperty("index").GetInt32();
        throw new Xunit.Sdk.XunitException($"asset {contains} missing");
    }

    static string? FoldState(JsonElement views, int index)
    {
        for (var i = 0; i < views.GetArrayLength(); i++)
            if (views[i].GetProperty("index").GetInt32() == index)
                return views[i].GetProperty("foldState").GetString();
        return null;
    }
}
