using System.Text.Json;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class KindsCliTests
{
    static readonly string[] Files = ["library.json", "run.json", "jump.json", "potion_hop.json", "stomp.json"];
    const string Namespace = "KindFixture";

    static string Golden(params string[] parts)
    {
        var dir = Path.GetDirectoryName(typeof(KindsCliTests).Assembly.Location)!;
        return Path.Combine([dir, "golden", .. parts]);
    }

    static string Stage(Action<string>? edit = null)
    {
        var temp = Path.Combine(Path.GetTempPath(), "tl-kinds-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        foreach (var name in Files) File.Copy(Golden("kinds", name), Path.Combine(temp, name));
        edit?.Invoke(temp);
        return temp;
    }

    static (int Exit, string Stdout, string Stderr) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var savedOut = Console.Out;
        var savedError = Console.Error;
        Console.SetOut(stdout);
        Console.SetError(stderr);
        try { return (KindsPack.Run(args), stdout.ToString(), stderr.ToString()); }
        finally
        {
            Console.SetOut(savedOut);
            Console.SetError(savedError);
        }
    }

    static string[] WithAssembly(string dir, params string[] rest)
        => ["--library", dir, "--namespace", Namespace, "--assembly", typeof(KindsCliTests).Assembly.Location, .. rest];

    static Dictionary<string, int> Capacities(string source)
    {
        var result = new Dictionary<string, int>();
        foreach (var line in source.Split('\n'))
        {
            var t = line.Trim();
            if (!t.StartsWith("public const int ", StringComparison.Ordinal)) continue;
            var parts = t["public const int ".Length..].TrimEnd(';').Split(" = ");
            result[parts[0]] = int.Parse(parts[1]);
        }
        return result;
    }

    [Fact]
    public void StdoutMatchesLibraryKindsGolden()
    {
        var dir = Stage();
        try
        {
            var (exit, stdout, stderr) = Run(WithAssembly(dir));
            Assert.Equal(0, exit);
            Assert.Equal(File.ReadAllText(Golden("kinds", "Kinds.g.cs.txt")), stdout);
            Assert.Contains("kind Damage: worst 7, capacity 5", stderr);
            Assert.Contains("kind group Effects: worst 0, capacity 0", stderr);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void LibraryJsonFilePathIsAccepted()
    {
        var dir = Stage();
        try
        {
            var (exit, stdout, _) = Run(WithAssembly(Path.Combine(dir, "library.json")));
            Assert.Equal(0, exit);
            Assert.Equal(File.ReadAllText(Golden("kinds", "Kinds.g.cs.txt")), stdout);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void ChannelsStackingCapsPulsesAndOverridesAreHandComputed()
    {
        var dir = Stage(d => File.WriteAllText(Path.Combine(d, "library.json"), """
            {
              "tickRate": 60,
              "channels": ["locomotion", "action"],
              "timelines": ["run.json", "jump.json", "potion_hop.json", "stomp.json"],
              "kinds": {
                "default": { "maxInlineBytes": 16, "stackDepth": 3 },
                "Damage": { "gamePulses": 4, "maxInlineBytes": 120 },
                "Locomotion": { "inline": 0 }
              }
            }
            """));
        try
        {
            var (exit, stdout, _) = Run(WithAssembly(dir));
            Assert.Equal(0, exit);
            var caps = Capacities(stdout);
            Assert.Equal(0, caps["Locomotion"]);
            Assert.Equal(2, caps["Lift"]);
            Assert.Equal(9, caps["Damage"]);
            Assert.Equal(1, caps["StatChange"]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void OutWritesOnlyOnChange()
    {
        var dir = Stage();
        var kinds = Path.Combine(dir, "Kinds.g.cs");
        try
        {
            var (exit, _, stderr) = Run(WithAssembly(dir, "--out", kinds));
            Assert.Equal(0, exit);
            Assert.Contains("kinds: wrote", stderr);
            Assert.Equal(File.ReadAllText(Golden("kinds", "Kinds.g.cs.txt")), File.ReadAllText(kinds));
            var stamp = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(kinds, stamp);
            (_, _, stderr) = Run(WithAssembly(dir, "--out", kinds));
            Assert.Contains("unchanged", stderr);
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(kinds));
            File.WriteAllText(Path.Combine(dir, "library.json"), File.ReadAllText(Path.Combine(dir, "library.json")).Replace("\"gamePulses\": 4", "\"gamePulses\": 0"));
            (_, _, stderr) = Run(WithAssembly(dir, "--out", kinds));
            Assert.Contains("kinds: wrote", stderr);
            Assert.NotEqual(stamp, File.GetLastWriteTimeUtc(kinds));
            Assert.Equal(3, Capacities(File.ReadAllText(kinds))["Damage"]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DiagnosticsJsonReportsRowsAndWriteStatus()
    {
        var dir = Stage();
        var kinds = Path.Combine(dir, "Kinds.g.cs");
        try
        {
            var (exit, stdout, _) = Run(WithAssembly(dir, "--diagnostics", "json"));
            Assert.Equal(0, exit);
            using var first = JsonDocument.Parse(stdout);
            Assert.Equal(4, first.RootElement.GetProperty("timelines").GetInt32());
            var rows = first.RootElement.GetProperty("kinds").EnumerateArray().ToDictionary(k => k.GetProperty("name").GetString()!);
            Assert.Equal(5, rows["Damage"].GetProperty("capacity").GetInt32());
            Assert.Equal(7, rows["Damage"].GetProperty("worst").GetInt32());
            Assert.True(rows["Effects"].GetProperty("group").GetBoolean());
            Assert.Equal(JsonValueKind.Null, first.RootElement.GetProperty("kindsFile").ValueKind);
            Assert.False(first.RootElement.GetProperty("kindsWritten").GetBoolean());
            (exit, stdout, _) = Run(WithAssembly(dir, "--diagnostics", "json", "--out", kinds));
            Assert.Equal(0, exit);
            using var second = JsonDocument.Parse(stdout);
            Assert.True(second.RootElement.GetProperty("kindsWritten").GetBoolean());
            Assert.Equal(kinds, second.RootElement.GetProperty("kindsFile").GetString());
            Assert.True(File.Exists(kinds));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void MissingLibraryFailsFastWithNoStdout()
    {
        var (exit, stdout, stderr) = Run(["--namespace", Namespace]);
        Assert.Equal(1, exit);
        Assert.Equal("", stdout);
        Assert.Contains("Usage: tlb kinds", stderr);
    }

    [Fact]
    public void UnknownKindSettingFailsFastWithoutTouchingOut()
    {
        var dir = Stage(d => File.WriteAllText(Path.Combine(d, "library.json"), """
            { "channels": ["locomotion", "action"], "timelines": ["run.json"], "kinds": { "Poison": { "inline": 0 } } }
            """));
        var kinds = Path.Combine(dir, "Kinds.g.cs");
        try
        {
            var (exit, stdout, stderr) = Run(WithAssembly(dir, "--out", kinds));
            Assert.Equal(1, exit);
            Assert.Equal("", stdout);
            Assert.Contains("'Poison' is not a kind", stderr);
            Assert.False(File.Exists(kinds));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void MissingLibraryPathFailsFast()
    {
        var (exit, stdout, stderr) = Run(["--library", Path.Combine(Path.GetTempPath(), "tl-no-such-dir"), "--namespace", Namespace]);
        Assert.Equal(1, exit);
        Assert.Equal("", stdout);
        Assert.Contains("does not exist", stderr);
    }
}
