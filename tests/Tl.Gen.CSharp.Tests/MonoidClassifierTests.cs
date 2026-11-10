using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class MonoidClassifierTests
{
    static IrResult Classify(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        return MonoidClassifier.Classify(method);
    }

    [Fact]
    public void IntAddCombineIsMonoid()
    {
        var source = """
            public static class K
            {
                public static int Combine(int left, int right)
                {
                    return left + right;
                }
            }
            """;

        Assert.Equal(new IrResult(IrKind.Monoid, "+", "int"), Classify(source));
    }

    [Fact]
    public void FloatMultiplyCombineIsMonoid()
    {
        var source = "public static class K { public static float Combine(float left, float right) => left * right; }";

        Assert.Equal(new IrResult(IrKind.Monoid, "*", "float"), Classify(source));
    }

    [Theory]
    [InlineData("int", "left + right", "+")]
    [InlineData("int", "left * right", "*")]
    [InlineData("int", "left | right", "|")]
    [InlineData("int", "left & right", "&")]
    [InlineData("int", "System.Math.Min(left, right)", "min")]
    [InlineData("int", "System.Math.Max(left, right)", "max")]
    public void CombineRecognizesEachMonoidOperation(string type, string body, string op)
    {
        var source = $"public static class K {{ public static {type} Combine({type} left, {type} right) => {body}; }}";

        Assert.Equal(new IrResult(IrKind.Monoid, op, type), Classify(source));
    }

    [Fact]
    public void CombineWithIfIsOpaque()
    {
        var source = """
            public static class K
            {
                public static int Combine(int left, int right)
                {
                    if (left > right) return left;
                    return right;
                }
            }
            """;

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void CombineWithLoopIsOpaque()
    {
        var source = """
            public static class K
            {
                public static int Combine(int left, int right)
                {
                    var total = left;
                    while (total > 0) total = total - 1;
                    return total + right;
                }
            }
            """;

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void CombineWithMethodCallIsOpaque()
    {
        var source = "public static class K { public static int Combine(int left, int right) => System.Math.Abs(left + right); }";
        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void CombineWithSubtractIsOpaque()
    {
        var source = "public static class K { public static int Combine(int left, int right) => left - right; }";

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void CombineWithMixedTypesIsOpaque()
    {
        var source = "public static class K { public static int Combine(int left, float right) => left + right; }";

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void CombineWithRepeatedOperandIsOpaque()
    {
        var source = "public static class K { public static int Combine(int left, int right) => left + left; }";

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void NonCombineNameIsOpaque()
    {
        var source = "public static class K { public static int Add(int left, int right) => left + right; }";

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void ApplyAccumulationIsAffine()
    {
        var source = "public static class K { public static void Apply(in float value, ref float column) => column = column + value; }";

        Assert.Equal(new IrResult(IrKind.Affine, "+", "float"), Classify(source));
    }

    [Fact]
    public void ApplyBlockAccumulationIsAffine()
    {
        var source = """
            public static class K
            {
                public static void Apply(float delta, ref float total)
                {
                    total = total * delta;
                }
            }
            """;

        Assert.Equal(new IrResult(IrKind.Affine, "*", "float"), Classify(source));
    }

    [Fact]
    public void ApplyCompoundAssignmentIsAffine()
    {
        var source = "public static class K { public static void Apply(float delta, ref float total) => total += delta; }";

        Assert.Equal(new IrResult(IrKind.Affine, "+", "float"), Classify(source));
    }

    [Fact]
    public void ApplyMinIsAffine()
    {
        var source = "public static class K { public static void Apply(in int value, ref int column) => column = System.Math.Min(column, value); }";

        Assert.Equal(new IrResult(IrKind.Affine, "min", "int"), Classify(source));
    }

    [Fact]
    public void ApplyPlainCopyIsOpaque()
    {
        var source = "public static class K { public static void Apply(in int value, ref int column) => column = value; }";

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void ApplyWithIfIsOpaque()
    {
        var source = """
            public static class K
            {
                public static void Apply(float delta, ref float total)
                {
                    if (delta > 0) total = total + delta;
                }
            }
            """;

        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(source));
    }

    [Fact]
    public void NullIsOpaque()
    {
        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), MonoidClassifier.Classify(null));
    }

    [Fact]
    public void GarbageIsOpaque()
    {
        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify(""));
        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify("!!!"));
        Assert.Equal(new IrResult(IrKind.Opaque, "", ""), Classify("class X { public static int Combine(int a, int b) {"));
    }
}
