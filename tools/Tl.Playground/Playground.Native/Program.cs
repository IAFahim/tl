using Play;

var report = SmokeRun.Launch();
Console.Write(report);

var checksum = ulong.Parse(report.Split("checksum=")[1..][0], System.Globalization.CultureInfo.InvariantCulture);
if (Pin.Checksum != 0)
    Check(checksum == Pin.Checksum, $"checksum {checksum} does not equal the pinned wasm checksum {Pin.Checksum}");

foreach (var name in Scenario.PresetNames)
{
    var embedded = Presets.Read(name);
    var onDisk = File.ReadAllBytes(Path.Combine(FindPresetsDirectory(), name + ".json"));
    Check(embedded.AsSpan().SequenceEqual(onDisk), $"preset {name}: embedded bytes differ from the committed file");
    var scenario = Presets.Load(name);
    var baked = PlayBake.Bake(scenario.AuthoringJson());
    using var engine = new PlayEngine(scenario.Crowd);
    engine.Load(baked);
    engine.SeedPositions(scenario.Pattern);
    engine.Step(scenario.Direction == ScenarioDirection.Forward);
    Console.WriteLine($"preset: {name} crowd={scenario.Crowd} pattern={scenario.Pattern} duration={engine.Duration} loop={engine.Loop} moved={engine.Last.Moved} OK");
}

if (Pin.Checksum == 0)
{
    Console.WriteLine("native: preset and smoke receipts pass; pin the SMOKE checksum from the wasm receipt into Pin.Checksum");
    return 0;
}
Console.WriteLine($"NATIVE PASS checksum={checksum} pinned={Pin.Checksum}");
return 0;

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"native receipt failed: {message}");
}

static string FindPresetsDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "tools", "Tl.Playground", "presets");
        if (Directory.Exists(candidate)) return candidate;
        directory = directory.Parent;
    }
    throw new InvalidOperationException("native receipt failed: presets directory not found above the build output");
}

static class Pin
{
    public static readonly ulong Checksum = 7131578910045740992ul;
}
