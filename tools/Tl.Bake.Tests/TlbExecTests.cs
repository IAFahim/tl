using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlbExecTests
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

    [Fact]
    public void Exec_ReturnValueBecomesExitCode()
    {
        var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.Seven");
        Assert.Equal(7, result.Exit);
    }

    [Fact]
    public void Exec_AssetOverloadSeesTheLoadedTimeline()
    {
        var path = Path.Combine(Path.GetTempPath(), "tlb_exec_" + Guid.NewGuid().ToString("N") + ".tlb");
        var bytes = TimelineBaker.BakeJson(LiveJson, BakerAssemblyResolver.FromAssemblies([typeof(LiveHost.LiveTrack).Assembly]));
        File.WriteAllBytes(path, bytes);
        try
        {
            var expected = TimelineAsset.Load(bytes);
            var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.AssetIndex", "--asset", path);
            Assert.Equal(expected, result.Exit);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Exec_UnknownMethodExitsFour()
    {
        var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.Missing");
        Assert.Equal(4, result.Exit);
        Assert.Contains("no public static int Missing", result.Stderr);
    }

    [Fact]
    public void Exec_UnhandledExceptionExitsFourWithStack()
    {
        var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.Boom");
        Assert.Equal(4, result.Exit);
        Assert.Contains("Host error: LiveHost.Dumps.Boom threw InvalidOperationException: boom", result.Stderr);
        Assert.Contains("Dumps.Boom", result.Stderr);
    }

    [Fact]
    public void Exec_WrongSignatureExitsFour()
    {
        var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.WrongSignature");
        Assert.Equal(4, result.Exit);
    }

    [Fact]
    public void Exec_MissingFlagsAndBadAssetExitTwoOrThree()
    {
        Assert.Equal(2, Run("--exec", "--assembly", AssemblyPath).Exit);
        Assert.Equal(2, Run("--exec", "--method", "LiveHost.Dumps.Seven").Exit);
        Assert.Equal(2, Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.Seven", "--asset", Path.Combine(Path.GetTempPath(), "tlb_exec_absent_" + Guid.NewGuid().ToString("N") + ".tlb")).Exit);
        var bad = Path.Combine(Path.GetTempPath(), "tlb_exec_bad_" + Guid.NewGuid().ToString("N") + ".tlb");
        File.WriteAllText(bad, "{\"not\":\"a baked timeline asset\"} " + new string('x', 64));
        try
        {
            var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.AssetIndex", "--asset", bad);
            Assert.Equal(3, result.Exit);
            Assert.Contains("Asset error: TLB magic or version invalid", result.Stderr);
        }
        finally
        {
            File.Delete(bad);
        }
    }

    [Fact]
    public void Exec_IgnoresArgPairs()
    {
        var result = Run("--exec", "--assembly", AssemblyPath, "--method", "LiveHost.Dumps.Seven", "--arg", "key=value", "--arg", "other=2");
        Assert.Equal(7, result.Exit);
    }
}
