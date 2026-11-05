using System.Text;
using Tl;
using Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class ConformanceVectorTests
{
    public static IEnumerable<object[]> VectorNames()
        => ConformanceVectors.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(VectorNames))]
    public void Vector_RegeneratesBitForBit(string name)
    {
        var rendered = ConformanceVectors.Render(ConformanceVectors.Build(name));
        Assert.Equal(rendered, File.ReadAllText(ConformanceVectors.VectorPath(name)));
    }


    [Fact]
    public void Vector_FilesEmitOnRequest()
    {
        if (Environment.GetEnvironmentVariable("TL_EMIT_VECTORS") != "1") return;
        Directory.CreateDirectory(Path.Combine(ConformanceVectors.GoldenRoot(), "vectors"));
        foreach (var name in ConformanceVectors.Names)
            File.WriteAllText(ConformanceVectors.VectorPath(name), ConformanceVectors.Render(ConformanceVectors.Build(name)), new UTF8Encoding(false));
    }
}
