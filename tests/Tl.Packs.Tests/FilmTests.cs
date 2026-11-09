using System.Text.Json;
using Film;
using Tl.Bake;
using Xunit;

namespace Tl.Packs.Tests;

// The release and tutorial videos are Tl timelines (demos/film). These tests run them and check what the film shows, tick by tick.
public sealed class FilmTests
{
    static string Demo(params string[] parts) => Path.Combine([ForceTests.Root(), "demos", "film", .. parts]);

    static byte[] Library()
    {
        var scene = Path.Combine(Path.GetTempPath(), "film.scene.json");
        var json = JsonSerializer.SerializeToUtf8Bytes(new { schema = 1, project = Demo("tl.project.json"), entities = Array.Empty<object>() });
        File.WriteAllBytes(scene, json);
        return RunCommand.BakeProject(scene, json, [typeof(OnScreen).Assembly.Location]);
    }

    static (int Scenes, int Ticks) Film(string name)
    {
        using var script = JsonDocument.Parse(File.ReadAllText(Demo("script", name + ".json")));
        using var timeline = JsonDocument.Parse(File.ReadAllText(Demo("timelines", name + ".json")));
        return (script.RootElement.GetProperty("scenes").GetArrayLength(), timeline.RootElement.GetProperty("duration").GetInt32());
    }

    [Theory]
    [InlineData("release", 1)]
    [InlineData("tutorial", 2)]
    public void TheFilmShowsEachSceneOnceInOrderAndSpeaksEachOnce(string name, int timelineNumber)
    {
        var (scenes, ticks) = Film(name);
        var screen = Host.Run(Library(), timelineNumber, ticks);

        Assert.Equal(1, screen[0].Scene);
        Assert.Equal(scenes, screen[^1].Scene);
        for (var tick = 1; tick < ticks; tick++)
        {
            Assert.InRange(screen[tick].Scene - screen[tick - 1].Scene, 0, 1);
            Assert.True(screen[tick].Caption >= screen[tick - 1].Caption || screen[tick].Caption == 0, $"caption went backwards at tick {tick}");
            Assert.InRange(screen[tick].ScenePhase, 0f, 1f);
        }

        var cues = screen.Select(s => s.Voice).Where(v => v != 0).Distinct().ToArray();
        Assert.Equal(Enumerable.Range(1, scenes), cues);
        Assert.True(screen[^1].Flashes >= scenes, "every scene start flashes once");

        var footage = screen.Select(s => s.Footage).Where(f => f != 0).Distinct().ToArray();
        Assert.Equal(Enumerable.Range(1, footage.Length), footage);   // each recorded clip plays in one stretch, in order
        Assert.Equal(0, screen[^1].Footage);
    }

    [Fact]
    public void ThePageDataIsOneNumberPerTickForEveryField()
    {
        var (_, ticks) = Film("release");
        var json = JsonDocument.Parse(Host.ToJson(Host.Run(Library(), 1, ticks), 30)).RootElement;
        Assert.Equal(30, json.GetProperty("fps").GetInt32());
        foreach (var field in new[] { "scene", "phase", "caption", "voice", "footage", "flashes" })
            Assert.Equal(ticks, json.GetProperty(field).GetArrayLength());
    }
}
