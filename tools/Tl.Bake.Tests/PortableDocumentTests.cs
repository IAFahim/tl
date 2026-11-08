using System.Text;
using System.Text.Json;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class PortableDocumentTests
{
    [Fact]
    public void ProjectExportHasOneCanonicalEncoding()
    {
        var json = TlJson.ExportProject(["push.json", "return.json"], ["body"], 60);
        using var project = TlJson.ReadProject(Encoding.UTF8.GetBytes(json));
        Assert.Equal(1, project.RootElement.GetProperty("schema").GetInt32());
        Assert.Equal("push.json", project.RootElement.GetProperty("timelines")[0].GetString());
        Assert.DoesNotContain("\r", json);
        Assert.EndsWith("\n", json);
    }

    [Theory]
    [InlineData("{\"schema\":2,\"timelines\":[]}", "schema")]
    [InlineData("{\"schema\":1}", "required")]
    [InlineData("{\"schema\":1,\"tickRate\":0,\"timelines\":[]}", "tickRate")]
    [InlineData("{\"schema\":1,\"tickRate\":\"60\",\"timelines\":[]}", "tickRate")]
    [InlineData("{\"schema\":1,\"timelines\":[],\"channels\":[\"body\",\"body\"]}", "duplicate")]
    [InlineData("{\"schema\":1,\"timelines\":[],\"unknown\":true}", "unknown")]
    [InlineData("{\"schema\":1,\"schema\":1,\"timelines\":[]}", "duplicate")]
    public void ProjectRejectsInvalidSchemaAndFields(string json, string expected)
    {
        var error = Assert.Throws<BakeDiagnosticException>(() => TlJson.ReadProject(Encoding.UTF8.GetBytes(json)));
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void SceneKeepsEntityAndSameStepRequestOrder()
    {
        const string json = """
            {"schema":1,"project":"tl.project.json","entities":[
              {"name":"first","states":[{"namespace":"Game","type":"Health","data":{"Value":100}}],"play":[{"timeline":"duel"}]},
              {"name":"second"}],
             "requests":[{"step":40,"entity":1,"op":"Pause","timeline":"dash"},{"step":40,"entity":1,"op":"Resume","timeline":"dash"}]}
            """;
        using var scene = TlJson.ReadScene(Encoding.UTF8.GetBytes(json));
        var root = scene.RootElement;
        Assert.Equal("first", root.GetProperty("entities")[0].GetProperty("name").GetString());
        Assert.Equal("Pause", root.GetProperty("requests")[0].GetProperty("op").GetString());
        Assert.Equal("Resume", root.GetProperty("requests")[1].GetProperty("op").GetString());
    }

    [Theory]
    [InlineData("{\"schema\":1,\"entities\":[],\"requests\":[{\"step\":1,\"entity\":0,\"op\":\"StopAll\"}]}", "entity")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"requests\":[{\"step\":1,\"entity\":0,\"op\":\"Seek\",\"timeline\":\"push\"}]}", "required")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"requests\":[{\"step\":1,\"entity\":0,\"op\":\"Pause\"}]}", "required")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"requests\":[{\"step\":1,\"entity\":0,\"op\":\"SetSpeed\",\"timeline\":\"push\",\"speed\":0}]}", "speed")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"requests\":[{\"step\":1,\"entity\":0,\"op\":\"StopAll\",\"speed\":2}]}", "unknown")]
    [InlineData("{\"schema\":1,\"entities\":[{\"states\":[{\"namespace\":\"Game\",\"type\":\"Health\",\"data\":{\"Value\":1,\"Value\":2}}]}]}", "duplicate")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"requests\":[{\"step\":1,\"entity\":0,\"op\":\"SeekChannel\",\"channel\":\"body\"}]}", "required")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"direction\":[{\"step\":1,\"forward\":1}]}", "direction.forward")]
    [InlineData("{\"schema\":1,\"entities\":[{}],\"direction\":[{\"step\":-1,\"forward\":true}]}", "direction.step")]
    public void SceneRejectsIncompleteOrAmbiguousCommands(string json, string expected)
    {
        var error = Assert.Throws<BakeDiagnosticException>(() => TlJson.ReadScene(Encoding.UTF8.GetBytes(json)));
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void AddingTheSchemaDoesNotDisableThePrimitiveFastPath()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden", "fixtures", "minimal.json"));
        var versioned = json.Replace("{", "{\"schema\":1,", StringComparison.Ordinal);
        var first = json.IndexOf('{');
        versioned = json.Insert(first + 1, "\"schema\":1,");
        Assert.True(TimelineBakerSimd.TryParseFast(Encoding.UTF8.GetBytes(versioned), new BakerAssemblyResolver(), null, out _));
        Assert.Equal(TimelineBaker.BakeJson(json), TimelineBaker.BakeJson(versioned));
    }

    [Fact]
    public void DecodeAndBakeCommandsRoundTripALibrary()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "golden", "library");
        var temp = Path.Combine(Path.GetTempPath(), "tl-decode-" + Guid.NewGuid().ToString("N"));
        var work = Path.Combine(temp, "source");
        var decoded = Path.Combine(temp, "decoded");
        Directory.CreateDirectory(work);
        try
        {
            foreach (var name in new[] { "tl.project.json", "jump.json", "slide.json" })
                File.Copy(Path.Combine(source, name), Path.Combine(work, name));
            var assembly = typeof(PortableDocumentTests).Assembly.Location;
            var original = Path.Combine(temp, "original.tlib");
            var rebaked = Path.Combine(temp, "rebaked.tlib");
            Assert.Equal(0, Tl.Bake.Program.Main(["bake", Path.Combine(work, "tl.project.json"), "-o", original, "--assembly", assembly]));
            Assert.Equal(0, Tl.Bake.Program.Main(["decode", original, "-o", decoded, "--assembly", assembly]));
            using (var project = TlJson.ReadProject(File.ReadAllBytes(Path.Combine(decoded, "tl.project.json"))))
                Assert.Equal("locomotion", project.RootElement.GetProperty("channels")[0].GetString());
            Assert.Equal(0, Tl.Bake.Program.Main(["bake", decoded, "-o", rebaked, "--assembly", assembly]));
            Assert.Equal(File.ReadAllBytes(original), File.ReadAllBytes(rebaked));
        }
        finally { Directory.Delete(temp, true); }
    }

    [Fact]
    public void DecodeCommandNeedsAnAssembly()
    {
        Assert.Equal(1, Tl.Bake.Program.Main(["decode", "missing.tlb"]));
    }
}
