using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Play;

public enum ScenarioPattern { Uniform, Staggered, Waves }

public enum ScenarioDirection { Forward, Backward }

public sealed class Scenario
{
    public const int MinCrowd = 1;
    public const int MaxCrowd = 2048;
    public const int MinDuration = 4;
    public const int MaxDuration = 1024;
    public const int MinWindowStart = 0;

    public int Crowd = 128;
    public ScenarioPattern Pattern = ScenarioPattern.Staggered;
    public ScenarioDirection Direction = ScenarioDirection.Forward;
    public bool Autoplay;
    public float TrackScale = 1f;
    public float AmountA = 1f;
    public int WindowAStart;
    public int WindowAEnd = 32;
    public float AmountB = -2f;
    public int WindowBStart = 40;
    public int WindowBEnd = 64;
    public int Duration = 64;
    public bool Loop = true;

    public static readonly string[] PresetNames = ["uniform-crowd", "staggered", "waves", "finite-clamp"];

    public Scenario Clone() => (Scenario)MemberwiseClone();

    public static Scenario FromQuery(Dictionary<string, string> query) => new Scenario().ApplyQuery(query);

    public Scenario ApplyQuery(Dictionary<string, string> query)
    {
        if (query.TryGetValue("crowd", out var crowd) && int.TryParse(crowd, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c))
            Crowd = Math.Clamp(c, MinCrowd, MaxCrowd);
        if (query.TryGetValue("pattern", out var pattern))
            Pattern = ParsePattern(pattern);
        if (query.TryGetValue("loop", out var loop))
            Loop = loop is "true" or "1";
        if (query.TryGetValue("dir", out var dir))
            Direction = dir is "back" or "backward" ? ScenarioDirection.Backward : ScenarioDirection.Forward;
        if (query.TryGetValue("play", out var play))
            Autoplay = play is "true" or "1";
        if (query.TryGetValue("scale", out var scale) && float.TryParse(scale, NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
            TrackScale = s;
        if (query.TryGetValue("amountA", out var amountA) && float.TryParse(amountA, NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
            AmountA = a;
        if (query.TryGetValue("windowA", out var windowA) && TryParseWindow(windowA, out var wsA, out var weA))
            (WindowAStart, WindowAEnd) = (wsA, weA);
        if (query.TryGetValue("amountB", out var amountB) && float.TryParse(amountB, NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
            AmountB = b;
        if (query.TryGetValue("windowB", out var windowB) && TryParseWindow(windowB, out var wsB, out var weB))
            (WindowBStart, WindowBEnd) = (wsB, weB);
        if (query.TryGetValue("duration", out var duration) && int.TryParse(duration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
            Duration = Math.Clamp(d, MinDuration, MaxDuration);
        return this;
    }

    public Dictionary<string, string> ToQuery()
    {
        var result = new Dictionary<string, string>
        {
            ["crowd"] = Crowd.ToString(CultureInfo.InvariantCulture),
            ["pattern"] = Pattern switch
            {
                ScenarioPattern.Uniform => "uniform",
                ScenarioPattern.Waves => "waves",
                _ => "staggered",
            },
            ["loop"] = Loop ? "true" : "false",
            ["dir"] = Direction == ScenarioDirection.Backward ? "back" : "fwd",
            ["play"] = Autoplay ? "true" : "false",
            ["scale"] = Format(TrackScale),
            ["amountA"] = Format(AmountA),
            ["windowA"] = $"{WindowAStart.ToString(CultureInfo.InvariantCulture)}:{WindowAEnd.ToString(CultureInfo.InvariantCulture)}",
            ["amountB"] = Format(AmountB),
            ["windowB"] = $"{WindowBStart.ToString(CultureInfo.InvariantCulture)}:{WindowBEnd.ToString(CultureInfo.InvariantCulture)}",
        };
        return result;
    }

    public static string QueryString(Dictionary<string, string> query)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in query)
        {
            if (builder.Length > 0) builder.Append('&');
            builder.Append(key).Append('=').Append(Uri.EscapeDataString(value));
        }
        return builder.ToString();
    }

    public string AuthoringJson() => $$"""
{
  "duration": {{Duration}},
  "loop": {{LoopText}},
  "tracks": [
    {
      "namespace": "Play",
      "type": "ScaleTrack",
      "data": { "Scale": {{Format(TrackScale)}} },
      "clips": [
        { "namespace": "Play", "type": "AmountClip", "data": { "Amount": {{Format(AmountA)}} }, "start": {{WindowAStart}}, "end": {{WindowAEnd}} },
        { "namespace": "Play", "type": "AmountClip", "data": { "Amount": {{Format(AmountB)}} }, "start": {{WindowBStart}}, "end": {{WindowBEnd}} }
      ]
    }
  ]
}
""";

    public void SyncFromTimeline(JsonDocument document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name == "duration" && property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var duration))
                Duration = Math.Clamp(duration, MinDuration, MaxDuration);
            else if (property.Name == "loop" && property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                Loop = property.Value.GetBoolean();
        }
    }

    public void ClampWindows()
    {
        Duration = Math.Clamp(Duration, MinDuration, MaxDuration);
        (WindowAStart, WindowAEnd) = ClampWindow(WindowAStart, WindowAEnd);
        (WindowBStart, WindowBEnd) = ClampWindow(WindowBStart, WindowBEnd);
    }

    public (int, int) ClampWindow(int start, int end)
    {
        start = Math.Clamp(start, MinWindowStart, Duration);
        end = Math.Clamp(end, MinWindowStart + 1, Duration);
        if (start >= end) end = Math.Min(start + 1, Duration);
        if (start >= end) start = end - 1;
        return (start, end);
    }

    static ScenarioPattern ParsePattern(string text) => text switch
    {
        "uniform" => ScenarioPattern.Uniform,
        "waves" => ScenarioPattern.Waves,
        _ => ScenarioPattern.Staggered,
    };

    static bool TryParseWindow(string text, out int start, out int end)
    {
        start = 0;
        end = 0;
        var separator = text.IndexOf(':');
        if (separator <= 0 || separator == text.Length - 1) return false;
        if (!int.TryParse(text.AsSpan(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out start)) return false;
        if (!int.TryParse(text.AsSpan(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out end)) return false;
        return true;
    }

    static string Format(float value) => value.ToString("0.0###", CultureInfo.InvariantCulture);

    string LoopText => Loop ? "true" : "false";
}
