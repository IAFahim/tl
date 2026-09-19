using System.Globalization;
using System.IO;
using System.Reflection;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class IntrospectionTests
{
    private static readonly Assembly FixtureAssembly = typeof(Tlb.JobTrack).Assembly;

    [Fact]
    public void Introspection_MatchesGoldenFile()
    {
        Assert.Equal(ExpectedGolden(), TlbIntrospection.Introspect([FixtureAssembly]));
    }

    [Fact]
    public void Introspection_IsCultureIndependent()
    {
        var golden = ExpectedGolden();
        foreach (var name in new[] { "tr-TR", "de-DE", "ar-SA" })
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture(name);
            try
            {
                Assert.Equal(golden, TlbIntrospection.Introspect([FixtureAssembly]));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }

    [Fact]
    public void Introspection_RepeatedCallsAreIdentical()
    {
        Assert.Equal(
            TlbIntrospection.Introspect([FixtureAssembly]),
            TlbIntrospection.Introspect([FixtureAssembly]));
    }

    [Fact]
    public void JsonMode_WithoutAssemblies_Fails()
    {
        Assert.Equal("Error: --json requires at least one --assembly path\n", RunJson(["--json"]));
    }

    [Fact]
    public void JsonMode_WithUnexpectedArgument_Fails()
    {
        Assert.Equal("Error: Unexpected argument 'extra.json'\n", RunJson(["--json", "extra.json", "--assembly", typeof(Tlb.JobTrack).Assembly.Location]));
    }

    static string RunJson(string[] args)
    {
        var error = new StringWriter();
        var original = Console.Error;
        Console.SetError(error);
        try
        {
            Assert.Equal(1, Program.Main(args));
        }
        finally
        {
            Console.SetError(original);
        }
        return error.ToString().Replace("\r\n", "\n");
    }

    private static string ExpectedGolden()
    {
        var goldenPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "golden", "introspection.json");
        return File.ReadAllText(goldenPath);
    }
}
