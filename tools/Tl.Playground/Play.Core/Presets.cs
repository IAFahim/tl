using System.Reflection;
using System.Text.Json;

namespace Play;

public static class Presets
{
    public static byte[] Read(string name)
    {
        var assembly = typeof(Presets).Assembly;
        var resource = name.EndsWith(".json", StringComparison.Ordinal) ? name : name + ".json";
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Preset '{name}' is not embedded; known presets: {string.Join(", ", Scenario.PresetNames)}.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static string ReadText(string name) => System.Text.Encoding.UTF8.GetString(Read(name));

    public static Scenario Load(string name)
    {
        using var document = JsonDocument.Parse(ReadText(name));
        return Load(document.RootElement);
    }

    public static Scenario Load(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Scenario preset root must be a JSON object.");
        var scenario = new Scenario();
        if (root.TryGetProperty("crowd", out var crowd)) scenario.Crowd = Math.Clamp(crowd.GetInt32(), Scenario.MinCrowd, Scenario.MaxCrowd);
        if (root.TryGetProperty("pattern", out var pattern)) ApplyPattern(scenario, pattern.GetString());
        if (root.TryGetProperty("dir", out var dir)) scenario.Direction = dir.GetString() is "back" or "backward" ? ScenarioDirection.Backward : ScenarioDirection.Forward;
        if (root.TryGetProperty("play", out var play)) scenario.Autoplay = play.ValueKind == JsonValueKind.True;
        if (root.TryGetProperty("scale", out var scale)) scenario.TrackScale = scale.GetSingle();
        if (root.TryGetProperty("amountA", out var amountA)) scenario.AmountA = amountA.GetSingle();
        if (root.TryGetProperty("amountB", out var amountB)) scenario.AmountB = amountB.GetSingle();
        if (root.TryGetProperty("windowA", out var windowA)) (scenario.WindowAStart, scenario.WindowAEnd) = ReadWindow(windowA);
        if (root.TryGetProperty("windowB", out var windowB)) (scenario.WindowBStart, scenario.WindowBEnd) = ReadWindow(windowB);
        if (root.TryGetProperty("timeline", out var timeline) && timeline.ValueKind == JsonValueKind.Object)
        {
            if (timeline.TryGetProperty("duration", out var duration)) scenario.Duration = Math.Clamp(duration.GetInt32(), Scenario.MinDuration, Scenario.MaxDuration);
            if (timeline.TryGetProperty("loop", out var loop)) scenario.Loop = loop.ValueKind == JsonValueKind.True;
        }
        scenario.ClampWindows();
        return scenario;
    }

    static void ApplyPattern(Scenario scenario, string? text) => scenario.Pattern = text switch
    {
        "uniform" => ScenarioPattern.Uniform,
        "waves" => ScenarioPattern.Waves,
        _ => ScenarioPattern.Staggered,
    };

    static (int, int) ReadWindow(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 2)
            return (element[0].GetInt32(), element[1].GetInt32());
        throw new InvalidDataException("Scenario window must be a two-element [start, end] array.");
    }
}
