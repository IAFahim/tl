using System.Reflection;
using System.Text;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public void CSharpGeneratorPublicApiMatchesApproval()
    {
        var actual = GetPublicApi(typeof(GeneratorCli).Assembly);
        var approvedPath = Path.Combine(AppContext.BaseDirectory, "Tl.Gen.CSharp.PublicApi.approved.txt");
        Assert.True(File.Exists(approvedPath), "Missing public API approval: Tl.Gen.CSharp.PublicApi.approved.txt");
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
                .Where(static member => member is not MethodBase { IsSpecialName: true })
                .OrderBy(static member => member.Name, StringComparer.Ordinal)
                .ThenBy(static member => member.ToString(), StringComparer.Ordinal))
                result.AppendLine($"  {member.MemberType} {member}");
        }

        return result.ToString();
    }
}
