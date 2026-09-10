using System.Reflection;
using System.Text;
using Tl.Compiler;
using Xunit;

namespace Tl.Gen.C.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public void CompilerPublicApiMatchesApproval()
        => AssertPublicApi(typeof(TimelinePlan).Assembly, "Tl.Compiler.PublicApi.approved.txt");

    [Fact]
    public void CBackendPublicApiMatchesApproval()
        => AssertPublicApi(typeof(CEmitter).Assembly, "Tl.Gen.C.PublicApi.approved.txt");

    private static void AssertPublicApi(Assembly assembly, string fileName)
    {
        var actual = GetPublicApi(assembly);
        var approvedPath = Path.Combine(AppContext.BaseDirectory, fileName);
        Assert.True(File.Exists(approvedPath), $"Missing public API approval: {fileName}");
        var expected = File.ReadAllText(approvedPath);
        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
    }

    private static string GetPublicApi(Assembly assembly)
    {
        var result = new StringBuilder();
        foreach (var type in assembly.GetExportedTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            result.AppendLine($"type {type.FullName}");
            foreach (var member in type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(static member => member is not MethodInfo { IsSpecialName: true })
                .OrderBy(static member => member.Name, StringComparer.Ordinal)
                .ThenBy(static member => member.ToString(), StringComparer.Ordinal))
                result.AppendLine($"  {member.MemberType} {member}");
        }

        return result.ToString();
    }
}
